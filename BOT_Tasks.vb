Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.CommFunctions

Public Module BOT_Tasks
    ''' <summary>
    ''' Verifica si un usuario programado no ha editado en el tiempo especificado.
    ''' </summary>
    ''' <returns></returns>
    Function CheckUsers() As IRCMessage()
        EventLogger.Log("CheckUsers: Checking users", "LOCAL")
        Dim Messages As New List(Of IRCMessage)
        Try
            For Each UserdataLine As String() In EventLogger.LogUserData
                Dim username As String = UserdataLine(1)
                Dim OP As String = UserdataLine(0)
                Dim UserDate As String = UserdataLine(2)
                Dim User As New WikiUser(ESWikiBOT, username)
                Dim LastEdit As DateTime = User.LastEdit
                If Not User.Exists Then
                    EventLogger.Log("CheckUsers: The user " & username & " has not edited on this wiki", "IRC")
                    Continue For
                End If

                Dim actualtime As DateTime = DateTime.UtcNow

                Dim LastEditUnix As Integer = CInt(TimeToUnix(LastEdit))
                Dim ActualTimeUnix As Integer = CInt(TimeToUnix(actualtime))

                Dim Timediff As Integer = ActualTimeUnix - LastEditUnix
                If Not OS.ToLower.Contains("unix") Then 'En sistemas windows hay una hora de desfase
                    Timediff = Timediff - 3600
                End If

                Dim TriggerTimeDiff As Long = TimeStringToSeconds(UserDate)

                Dim TimediffToHours As Integer = CInt(Math.Truncate(Timediff / 3600))
                Dim TimediffToMinutes As Integer = CInt(Math.Truncate(Timediff / 60))
                Dim TimediffToDays As Integer = CInt(Math.Truncate(Timediff / 86400))
                Dim responsestring As String = String.Empty

                If Timediff > TriggerTimeDiff Then

                    If TimediffToMinutes <= 1 Then
                        responsestring = String.Format("¡{0} editó recién!", User.UserName)
                    Else
                        If TimediffToMinutes < 60 Then
                            responsestring = String.Format("La última edición de {0} fue hace {1} minutos", User.UserName, TimediffToMinutes)
                        Else
                            If TimediffToMinutes < 120 Then
                                responsestring = String.Format("La última edición de {0} fue hace más de {1} hora", User.UserName, TimediffToHours)
                            Else
                                If TimediffToMinutes < 1440 Then
                                    responsestring = String.Format("La última edición de {0} fue hace más de {1} horas", User.UserName, TimediffToHours)
                                Else
                                    If TimediffToMinutes < 2880 Then
                                        responsestring = String.Format("La última edición de {0} fue hace {1} día", User.UserName, TimediffToDays)
                                    Else
                                        responsestring = String.Format("La última edición de {0} fue hace más de {1} días", User.UserName, TimediffToDays)
                                    End If
                                End If
                            End If
                        End If
                    End If
                    responsestring = responsestring & ". El proximo aviso será en 5 minutos."

                    Messages.Add(New IRCMessage(OP, responsestring))
                End If
            Next
        Catch ex As System.ObjectDisposedException
            EventLogger.Debug_Log("CheckUsers EX: " & ex.Message, "IRC")
        End Try
        Return Messages.ToArray

    End Function


    Sub CheckUsersActivity(ByVal templatePage As Page, ByVal pageToSave As Page)
        If pageToSave Is Nothing Then Exit Sub

        Dim ActiveUsers As New Dictionary(Of String, WikiUser)
        Dim InactiveUsers As New Dictionary(Of String, WikiUser)
        For Each p As Page In ESWikiBOT.GetallInclusionsPages(templatePage)

            If (p.PageNamespace = 3) Or (p.PageNamespace = 2) Then
                Dim Username As String = p.Title.Split(":"c)(1)
                'si es una subpágina
                If Username.Contains("/") Then
                    Username = Username.Split("/"c)(0)
                End If
                'Cargar usuario
                Dim User As New WikiUser(ESWikiBOT, Username)
                'Validar usuario
                If Not ValidUser(User) Then
                    EventLogger.Log("Archive: The user" & User.UserName & " doesn't meet the requirements.", "LOCAL")
                    Continue For
                End If

                If Date.Now.Subtract(User.LastEdit) < New TimeSpan(0, 30, 0) Then
                    If Not ActiveUsers.Keys.Contains(User.UserName) Then
                        ActiveUsers.Add(User.UserName, User)
                    End If
                Else
                    If Not InactiveUsers.Keys.Contains(User.UserName) Then
                        InactiveUsers.Add(User.UserName, User)
                    End If
                End If
            End If
        Next


        Dim t As New Template
        t.Name = "#switch:{{{1|}}}"
        t.Parameters.Add(New Tuple(Of String, String)("", "'''Error''': No se ha indicado usuario."))
        t.Parameters.Add(New Tuple(Of String, String)("#default", "[[Archivo:WX circle red.png|10px|link=]]&nbsp;<span style=""color:red;"">'''Desconectado'''</span>"))

        For Each u As WikiUser In ActiveUsers.Values
            Dim gendertext As String = "Conectado"
            If u.Gender = "female" Then
                gendertext = "Conectada"
            End If
            t.Parameters.Add(New Tuple(Of String, String)(u.UserName, "[[Archivo:WX circle green.png|10px|link=]]&nbsp;<span style=""color:green;"">'''" & gendertext & "'''</span>"))
        Next

        For Each u As WikiUser In InactiveUsers.Values
            Dim gendertext As String = "Desconectado"
            If u.Gender = "female" Then
                gendertext = "Desconectada"
            End If
            Dim lastedit As Date = u.LastEdit
            t.Parameters.Add(New Tuple(Of String, String)(u.UserName, "[[Archivo:WX circle red.png|10px|link=|Última edición el " & Integer.Parse(lastedit.ToString("dd")).ToString & lastedit.ToString(" 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES")) & "]]&nbsp;<span style=""color:red;"">'''" & gendertext & "'''</span>"))
        Next

        Dim templatetext As String = "{{Noart|1=<div style=""position:absolute; z-index:100; right:10px; top:5px;"" class=""metadata"">" & Environment.NewLine & t.Text
        templatetext = templatetext & Environment.NewLine & "</div>}}" & Environment.NewLine & "<noinclude>" & "{{documentación}}" & "</noinclude>"
        pageToSave.Save(templatetext, "Bot: Actualizando lista.")

    End Sub

    ''' <summary>
    ''' Verifica si el usuario que se le pase cumple con los requisitos para verificar su actividad
    ''' </summary>
    ''' <param name="user">Usuario de Wiki</param>
    ''' <returns></returns>
    Private Function ValidUser(ByVal user As WikiUser) As Boolean
        EventLogger.Debug_Log("ValidUser: Check user", "LOCAL")
        'Verificar si el usuario existe
        If Not user.Exists Then
            EventLogger.Log("ValidUser: User " & user.UserName & " doesn't exist", "LOCAL")
            Return False
        End If

        'Verificar si el usuario está bloqueado.
        If user.Blocked Then
            EventLogger.Log("ValidUser: User " & user.UserName & " is blocked", "LOCAL")
            Return False
        End If

        'Verificar si el usuario editó hace al menos 4 días.
        If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
            EventLogger.Log("ValidUser: User " & user.UserName & " is inactive", "LOCAL")
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Crea una nueva instancia de la clase de archivado y actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
    ''' </summary>
    ''' <returns></returns>
    Function ArchiveAllInclusions(ByVal irc As Boolean) As Boolean
        Dim Archive As New GrillitusArchive(ESWikiBOT)
        Return Archive.ArchiveAllInclusions(irc)
    End Function

    ''' <summary>
    ''' Crea una nueva instancia de la clase de actualizacion de temas y actualiza el cafe temático.
    ''' </summary>
    ''' <returns></returns>
    Function UpdateTopics() As Boolean
        Dim topicw As New AddTopic(ESWikiBOT)
        Return topicw.UpdateTopics()
    End Function

    ''' <summary>
    ''' Revisa todas las páginas que llamen a la página indicada y las retorna como sortedlist.
    ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
    ''' el último usuario que la editó y el segundo el título real de la página.
    ''' </summary>
    Function GetAllRequestedpages() As SortedList(Of String, String())
        Dim _bot As Bot = ESWikiBOT
        Dim plist As New SortedList(Of String, String())
        For Each s As String In _bot.GetallInclusions(ResumePageName)
            Dim Pag As Page = _bot.Getpage(s)
            Dim pagetext As String = Pag.Text
            For Each s2 As String In TextInBetween(pagetext, "{{" & ResumePageName & "|", "}}")
                If Not plist.Keys.Contains(s2) Then
                    plist.Add(s2, {Pag.Lastuser, Pag.Title})
                End If
            Next
        Next
        Return plist
    End Function

    ''' <summary>
    ''' Compara las páginas que llaman a la plantilla y retorna retorna un sortedlist.
    ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
    ''' el último usuario que la editó y el segundo el título real de la página.
    ''' Solo contiene las páginas que no existen en la plantilla.
    ''' </summary>
    Function GetResumeRequests() As SortedList(Of String, String())
        Dim _bot As Bot = ESWikiBOT
        Dim slist As SortedList(Of String, String()) = GetAllRequestedpages()
        Dim Reqlist As New SortedList(Of String, String())
        Dim ResumePage As Page = ESWikiBOT.Getpage(ResumePageName)
        Dim rtext As String = ResumePage.Text

        For Each pair As KeyValuePair(Of String, String()) In slist
            Try
                If Not rtext.Contains("|" & pair.Key & "=") Then
                    Dim pag As Page = _bot.Getpage(pair.Key)
                    If pag.Exists Then
                        Reqlist.Add(pair.Key, pair.Value)
                    End If
                End If
            Catch ex As Exception
            End Try
        Next
        Return Reqlist

    End Function

    ''' <summary>
    ''' Actualiza los resúmenes de página basado en varios parámetros,
    ''' por defecto estos son de un máximo de 660 carácteres.
    ''' </summary>
    ''' <returns></returns>
    Function UpdatePageExtracts() As Boolean
        Return UpdatePageExtracts(False)
    End Function

    ''' <summary>
    ''' Actualiza los resúmenes de página basado en varios parámetros,
    ''' por defecto estos son de un máximo de 660 carácteres.
    ''' </summary>
    ''' <param name="IRC">Si se establece este valor envía un comando en IRC avisando de la actualización</param>
    ''' <returns></returns>
    Function UpdatePageExtracts(ByVal irc As Boolean) As Boolean
        Dim _bot As Bot = ESWikiBOT
        If irc Then
            BotIRC.Sendmessage(ColoredText("Actualizando extractos.", 4))
        End If

        EventLogger.Log("UpdatePageExtracts: Beginning update of page extracts", "LOCAL")
        EventLogger.Debug_Log("UpdatePageExtracts: Declaring Variables", "LOCAL")
        Dim NewResumes As New SortedList(Of String, String)
        Dim OldResumes As New SortedList(Of String, String)
        Dim FinalList As New List(Of String)


        EventLogger.Debug_Log("UpdatePageExtracts: Loading resume page", "LOCAL")
        Dim ResumePage As Page = _bot.Getpage(ResumePageName)

        Dim ResumePageText As String = ResumePage.Text
        EventLogger.Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL")


        Dim NewResumePageText As String = "{{#switch:{{{1}}}" & Environment.NewLine
        EventLogger.Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL")

        Dim Safepages As Integer = 0
        Dim NotSafepages As Integer = 0
        Dim NewPages As Integer = 0
        Dim NotSafePagesAdded As Integer = 0

        EventLogger.Debug_Log("UpdatePageExtracts: Parsing resume template", "LOCAL")
        Dim templatelist As List(Of String) = GetTemplateTextArray(ResumePageText)
        Dim ResumeTemplate As New Template(templatelist(0), False)
        EventLogger.Debug_Log("UpdatePageExtracts: Adding Old resumes to list", "LOCAL")
        Dim PageNames As New List(Of String)

        For Each PageResume As Tuple(Of String, String) In ResumeTemplate.Parameters
            PageNames.Add(PageResume.Item1)
            OldResumes.Add(PageResume.Item1, "|" & PageResume.Item1 & "=" & PageResume.Item2)
        Next

        For Each p As KeyValuePair(Of String, String()) In GetResumeRequests()
            PageNames.Add(p.Key)
            NewPages += 1
        Next

        PageNames.Sort()

        EventLogger.Debug_Log("UpdatePageExtracts: Get last revision ID", "LOCAL")
        Dim IDLIST As SortedList(Of String, Integer) = _bot.GetLastRevIds(PageNames.ToArray)

        EventLogger.Debug_Log("UpdatePageExtracts: Adding New resumes to list", "LOCAL")
        '============================================================================================
        ' Adding New resumes to list
        Dim Page_Resume_pair As SortedList(Of String, String) = _bot.GetPagesExtract(PageNames.ToArray, 660, True)
        Dim Page_Image_pair As SortedList(Of String, String) = _bot.GetImagesExtract(PageNames.ToArray)

        For Each Page As String In Page_Resume_pair.Keys

            If Not Page_Image_pair.Item(Page) = String.Empty Then
                'If the page contais a image
                NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                           & "[[File:" & Page_Image_pair(Page) & "|thumb|x120px]]" & Environment.NewLine _
                           & Page_Resume_pair.Item(Page) & Environment.NewLine _
                           & ":'''[[" & Page & "|Leer más...]]'''" & Environment.NewLine)
            Else
                'If the page doesn't contain a image
                NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                          & Page_Resume_pair.Item(Page) & Environment.NewLine _
                          & ":'''[[" & Page & "|Leer más...]]'''" & Environment.NewLine)
            End If
        Next

        '===========================================================================================

        EventLogger.Debug_Log("UpdatePageExtracts: getting ORES of IDS", "LOCAL")
        Dim EditScoreList As SortedList(Of Integer, Double()) = _bot.GetORESScores(IDLIST.Values.ToArray)

        '==========================================================================================
        'Choose between a old resume and a new resume depending if new resume is safe to use
        EventLogger.Debug_Log("UpdatePageExtracts: Recreating text", "LOCAL")
        For Each s As String In PageNames.ToArray
            Try
                If (EditScoreList(IDLIST(s))(0) < 20) And (CountCharacter(NewResumes(s), CType("[", Char)) = CountCharacter(NewResumes(s), CType("]", Char))) Then
                    'Safe edit
                    FinalList.Add(NewResumes(s))
                    Safepages += 1
                Else
                    'Isn't a safe edit
                    Try
                        FinalList.Add(OldResumes(s))
                        NotSafepages += 1
                    Catch ex As KeyNotFoundException
                        FinalList.Add(NewResumes(s))
                        NotSafePagesAdded += 1
                    End Try
                End If
            Catch ex As KeyNotFoundException
                'If the resume doesn't exist, will try to use the old resume text
                FinalList.Add(OldResumes(s))
                NotSafepages += 1
            End Try
        Next
        '==========================================================================================

        EventLogger.Debug_Log("UpdatePageExtracts: Concatenating text", "LOCAL")
        NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "}}" & Environment.NewLine & "<noinclude>{{documentación}}</noinclude>"
        EventLogger.Debug_Log("UpdatePageExtracts: Done, trying to save", "LOCAL")

        Try
            If NotSafepages = 0 Then
                If NewPages = 0 Then
                    ResumePage.Save(NewResumePageText, "(Bot) : Actualizando " & Safepages.ToString & " resúmenes.", False)
                Else
                    ResumePage.Save(NewResumePageText, "(Bot) : Actualizando " & Safepages.ToString & " resúmenes. Se han añadido " & NewPages.ToString & " resúmenes nuevos", False)
                End If
            Else

                Dim NumbText As String = " Resumen inseguro fue omitido. "
                If NotSafepages > 1 Then
                    NumbText = " Resúmenes inseguros fueron omitidos. "
                End If

                If NewPages = 0 Then
                    ResumePage.Save(NewResumePageText,
                                                    "(Bot): Actualizando " & Safepages.ToString & " resúmenes, " _
                                                    & NotSafepages.ToString & NumbText, False)
                Else
                    ResumePage.Save(NewResumePageText,
                                                    "(Bot): Actualizando " & Safepages.ToString & " resúmenes," _
                                                    & NotSafepages.ToString & NumbText & "Se han añadido " & NewPages.ToString & " resúmenes nuevos.", False)
                End If

            End If

            EventLogger.Log("UpdatePageExtracts: Update of page extracts completed successfully", "LOCAL")
            If irc Then
                BotIRC.Sendmessage(ColoredText("¡Extractos actualizados!", 4))
            End If

            Return True

        Catch ex As Exception
            EventLogger.Log("UpdatePageExtracts: Error updating page extracts", "LOCAL")
            EventLogger.Debug_Log(ex.Message, "LOCAL")
            BotIRC.Sendmessage(ColoredText("Error al actualizar los extractos, ver LOG.", 4))
            Return False
        End Try

    End Function


    Function CheckInformalMediation(ByVal workerbot As Bot) As Boolean
        If workerbot Is Nothing Then Return False
        Dim newThreads As Boolean = False
        Dim membPage As Page = workerbot.Getpage(InformalMediationMembers)
        Dim MedPage As Page = workerbot.Getpage(InfMedPage)
        Dim subthreads As String() = workerbot.GetSubThreads(membPage.Text)
        Dim uTempList As List(Of Template) = GetTemplates(subthreads(0))
        Dim userList As New List(Of String)
        For Each temp As Template In uTempList
            If temp.Name = "u" Then
                userList.Add(temp.Parameters(0).Item2)
            End If
        Next

        Dim currentThreads As Integer = GetPageThreads(MedPage).Count

        If BotSettings.Contains("InformalMediationLastThreadCount") Then
            If BotSettings.Get("InformalMediationLastThreadCount").GetType Is GetType(Integer) Then
                Dim lastthreadcount As Integer = Integer.Parse(BotSettings.Get("InformalMediationLastThreadCount").ToString)
                If currentThreads > lastthreadcount Then
                    BotSettings.Set("InformalMediationLastThreadCount", currentThreads)
                    newThreads = True
                Else
                    BotSettings.Set("InformalMediationLastThreadCount", currentThreads) 'Si disminuye la cantidad de hilos entonces lo guarda
                End If
            End If
        Else
            BotSettings.NewVal("InformalMediationLastThreadCount", currentThreads)
        End If

        If newThreads Then
            For Each u As String In userList
                Dim user As New WikiUser(workerbot, u)
                If user.Exists Then
                    Dim userTalkPage As Page = user.TalkPage
                    userTalkPage.AddSection("Atención en [[Wikipedia:Mediación informal/Solicitudes|Mediación informal]]", InfMedMessage, "Bot: Aviso automático de nueva solicitud.", False)
                End If
            Next
        End If
        Return True
    End Function



End Module

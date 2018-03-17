Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC
Public Module PeriodiBOT_Tasks
    ''' <summary>
    ''' Verifica si un usuario programado no ha editado en el tiempo especificado.
    ''' </summary>
    ''' <returns></returns>
    Function CheckUsers() As IRCMessage()
        Log("CheckUsers: Checking users", "LOCAL", BOTName)
        Dim Messages As New List(Of IRCMessage)
        Try
            For Each UserdataLine As String() In Userdata
                Dim username As String = UserdataLine(1)
                Dim OP As String = UserdataLine(0)
                Dim UserDate As String = UserdataLine(2)
                Dim User As New WikiUser(ESWikiBOT, username)
                Dim LastEdit As DateTime = User.LastEdit
                If Not User.Exists Then
                    Log("CheckUsers: The user " & username & " has not edited on this wiki", "IRC", BOTName)
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
            Debug_Log("CheckUsers EX: " & ex.Message, "IRC", BOTName)
        End Try



        Return Messages.ToArray

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
    ''' Compara las páginas que llaman a la plantilla y retorna retorna un sortedlist
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
            BotIRC.Sendmessage(ColoredText("Actualizando extractos...", "04"))
        End If

        Log("UpdatePageExtracts: Beginning update of page extracts", "LOCAL", BOTName)
        Debug_Log("UpdatePageExtracts: Declaring Variables", "LOCAL", BOTName)
        Dim NewResumes As New SortedList(Of String, String)
        Dim OldResumes As New SortedList(Of String, String)
        Dim FinalList As New List(Of String)


        Debug_Log("UpdatePageExtracts: Loading resume page", "LOCAL", BOTName)
        Dim ResumePage As Page = _bot.Getpage(ResumePageName)

        Dim ResumePageText As String = ResumePage.Text
        Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL", BOTName)


        Dim NewResumePageText As String = "{{#switch:{{{1}}}|" & Environment.NewLine
        Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL", BOTName)

        Dim Extracttext As String = String.Empty
        Dim ExtractImage As String = String.Empty
        Dim Safepages As Integer = 0
        Dim NotSafepages As Integer = 0
        Dim NewPages As Integer = 0
        Dim NotSafePagesAdded As Integer = 0
        Debug_Log("UpdatePageExtracts: Match list to ListOf", "LOCAL", BOTName)
        Dim p As New List(Of String)

        p.AddRange(GetTitlesOfTemplate(ResumePageText))

        For Each item As KeyValuePair(Of String, String()) In GetResumeRequests()
            If Not p.Contains(item.Key) Then
                p.Add(item.Key)
                NewPages += 1
            End If
        Next

        Debug_Log("UpdatePageExtracts: Sort of ListOf", "LOCAL", BOTName)
        p.Sort()
        Debug_Log("UpdatePageExtracts: Creating new ResumePageText", "LOCAL", BOTName)


        Debug_Log("UpdatePageExtracts: Adding IDS to IDLIST", "LOCAL", BOTName)
        Dim IDLIST As SortedList(Of String, Integer) = _bot.GetLastRevIds(p.ToArray)

        Debug_Log("UpdatePageExtracts: Adding Old resumes to list", "LOCAL", BOTName)
        For Each s As String In p.ToArray
            ' Adding Old resumes to list
            Try
                Dim cont As String = TextInBetween(ResumePageText, "|" & s & "=", "Leer más...]]'''|")(0)
                OldResumes.Add(s, ("|" & s & "=" & cont & "Leer más...]]'''|" & Environment.NewLine))
            Catch ex As IndexOutOfRangeException
                Debug_Log("UpdatePageExtracts: No old resume of " & s, "LOCAL", BOTName)
            End Try

        Next

        Debug_Log("UpdatePageExtracts: Adding New resumes to list", "LOCAL", BOTName)

        '============================================================================================
        ' Adding New resumes to list
        Dim Page_Resume_pair As SortedList(Of String, String) = _bot.GetPagesExtract(p.ToArray, 660, True)
        Dim Page_Image_pair As SortedList(Of String, String) = _bot.GetImagesExtract(p.ToArray)

        For Each Page As String In Page_Resume_pair.Keys

            If Not Page_Image_pair.Item(Page) = String.Empty Then
                'If the page contais a image
                NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                           & "[[File:" & Page_Image_pair(Page) & "|thumb|x120px]]" & Environment.NewLine _
                           & Page_Resume_pair.Item(Page) & Environment.NewLine _
                           & ":'''[[" & Page & "|Leer más...]]'''|" & Environment.NewLine)
            Else
                'If the page doesn't contain a image
                NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                          & Page_Resume_pair.Item(Page) & Environment.NewLine _
                          & ":'''[[" & Page & "|Leer más...]]'''|" & Environment.NewLine)
            End If
        Next

        '===========================================================================================

        Debug_Log("UpdatePageExtracts: getting ORES of IDS", "LOCAL", BOTName)
        Dim EditScoreList As SortedList(Of Integer, Double()) = _bot.GetORESScores(IDLIST.Values.ToArray)

        '==========================================================================================
        'Choose between a old resume and a new resume depending if new resume is safe to use
        Debug_Log("UpdatePageExtracts: Recreating text", "LOCAL", BOTName)
        For Each s As String In p.ToArray
            Try
                If (EditScoreList(IDLIST(s))(0) > 20) And
      (CountCharacter(NewResumes(s), CType("[", Char)) =
      CountCharacter(NewResumes(s), CType("]", Char))) Then
                    'Is a safe edit
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

        Debug_Log("UpdatePageExtracts: Concatenating text", "LOCAL", BOTName)
        NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "<!-- MARK -->" & Environment.NewLine & "|}}"

        Debug_Log("UpdatePageExtracts: Done, trying to save", "LOCAL", BOTName)

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

            Log("UpdatePageExtracts: Update of page extracts completed successfully", "LOCAL", BOTName)
            If irc Then
                BotIRC.Sendmessage(ColoredText("Extractos actualizados!", "04"))
            End If

            Return True

        Catch ex As Exception
            Log("UpdatePageExtracts: Error updating page extracts", "LOCAL", BOTName)
            Debug_Log(ex.Message, "LOCAL", BOTName)
            BotIRC.Sendmessage(ColoredText("Error al actualizar los extractos, ver LOG.", "04"))
            Return False
        End Try

    End Function


End Module

Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.My.Resources
Imports MWBot.net.WikiBot
Imports MWBot.net.Utility.Utils
Class SpecialTaks
    Private _bot As Bot

    Sub New(ByRef WorkerBot As Bot)
        _bot = WorkerBot
    End Sub













    ''' <summary>
    ''' Obtiene la primera aparición de la plantilla de firmado en la página pasada como parámetro 
    ''' </summary>
    ''' <param name="PageToGet">Pagina de la cual se busca la plantilla de firmado</param>
    ''' <returns></returns>
    Function GetSignTemplate(ByVal PageToGet As Page) As Template
        Dim templist As List(Of Template) = Template.GetTemplates(Template.GetTemplateTextArray(PageToGet.Content))
        Dim signtemp As New Template
        For Each t As Template In templist
            If t.Valid AndAlso Regex.Match(t.Name, WPStrings.AutoSignatureTemplateInsideRegex).Success Then
                signtemp = t
                Exit For
            End If
        Next
        Return signtemp
    End Function

    ''' <summary>
    ''' Revisa todas las páginas que llamen a la página indicada y las retorna como sortedlist.
    ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
    ''' el último usuario que la editó y el segundo el título real de la página.
    ''' </summary>
    Function GetAllRequestedpages(pageName As String) As SortedList(Of String, String())
        Dim plist As New SortedList(Of String, String())
        Dim inclusions As String() = _bot.GetallInclusions(pageName)
        For Each s As String In inclusions
            Try
                Dim Pag As Page = _bot.Getpage(s)
                Dim pagetext As String = Pag.Content
                For Each s2 As String In TextInBetween(pagetext.Replace("_", " "), "{{" & pageName & "|", "}}")
                    If Not plist.Keys.Contains(s2) Then
                        plist.Add(s2, {Pag.Lastuser, Pag.Title})
                    End If
                Next
            Catch ex As Exception When Not Debugger.IsAttached
                EventLogger.EX_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            End Try
        Next
        Return plist
    End Function

    ''' <summary>
    ''' Compara las páginas que llaman a la plantilla y retorna retorna un sortedlist.
    ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
    ''' el último usuario que la editó y el segundo el título real de la página.
    ''' Solo contiene las páginas que no existen en la plantilla.
    ''' </summary>
    Function GetResumeRequests(ByVal pageName As String) As Tuple(Of Page(), SortedList(Of String, String()))
        Dim pagesList As New List(Of Page)
        Dim slist As SortedList(Of String, String()) = GetAllRequestedpages(pageName)
        Dim Reqlist As New SortedList(Of String, String())
        Dim ResumePage As Page = _bot.Getpage(WPStrings.ResumePageName)
        Dim rtext As String = ResumePage.Content

        For Each pair As KeyValuePair(Of String, String()) In slist
            Try
                If Not rtext.Contains("|" & pair.Key & "=") Then
                    Dim pag As Page = _bot.Getpage(pair.Key)
                    If pag.Exists Then
                        Reqlist.Add(pair.Key, pair.Value)
                        pagesList.Add(pag)
                    End If
                End If
            Catch ex As IndexOutOfRangeException
            End Try
        Next
        Return New Tuple(Of Page(), SortedList(Of String, String()))(pagesList.ToArray, Reqlist)
    End Function

    ''' <summary>
    ''' Actualiza los resúmenes de página basado en varios parámetros,
    ''' por defecto estos son de un máximo de 660 carácteres.
    ''' </summary>
    ''' <returns></returns>
    Public Function UpdatePageExtracts(ByVal pageName As String) As Boolean
        Dim NewResumes As New SortedList(Of String, String)
        Dim OldResumes As New SortedList(Of String, String)
        Dim FinalList As New List(Of String)

        Dim ResumePage As Page = _bot.Getpage(pageName)
        Dim ResumePageText As String = ResumePage.Content
        Dim NewResumePageText As String = "{{#switch:{{{1}}}" & Environment.NewLine

        Dim Safepages As Integer = 0
        Dim NotSafepages As Integer = 0
        Dim NewPages As Integer = 0
        Dim NotSafePagesAdded As Integer = 0

        Dim templatelist As List(Of String) = Template.GetTemplateTextArray(ResumePageText)
        Dim ResumeTemplate As New Template(templatelist(0), False)
        EventLogger.Debug_Log(String.Format(BotMessages.LoadingOldExtracts, ResumeTemplate.Parameters.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        Dim PageNames As New List(Of String)

        For Each PageResume As Tuple(Of String, String) In ResumeTemplate.Parameters
            PageNames.Add(PageResume.Item1)
            OldResumes.Add(PageResume.Item1, "|" & PageResume.Item1 & "=" & PageResume.Item2)
        Next
        Dim resRequest As Tuple(Of Page(), SortedList(Of String, String())) = GetResumeRequests(pageName)
        Dim ResumeRequests As SortedList(Of String, String()) = resRequest.Item2
        For Each p As KeyValuePair(Of String, String()) In ResumeRequests
            PageNames.Add(p.Key)
            NewPages += 1
        Next
        PageNames.Sort()
        Dim IDLIST As SortedList(Of String, Integer) = New SortedList(Of String, Integer) '_bot.GetLastRevIds(PageNames.ToArray)   <------ BROKEN

        EventLogger.Debug_Log(String.Format(BotMessages.LoadingNewExtracts, PageNames.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        '============================================================================================
        ' Adding New resumes to list
        Dim Page_Extracts As HashSet(Of WikiExtract) = _bot.GetWikiExtractFromPages(resRequest.Item1, 660)
        Dim Page_Image_pair As SortedList(Of String, String) = _bot.GetImagesExtract(PageNames.ToArray)

        For Each extract As WikiExtract In Page_Extracts
            Dim containsimage As Boolean = False
            If Page_Image_pair.Keys.Contains(extract.PageName) Then
                If Not String.IsNullOrEmpty(Page_Image_pair.Item(extract.PageName)) Then
                    containsimage = True
                End If
            End If
            If containsimage Then
                'If the page contais a image
                NewResumes.Add(extract.PageName, "|" & extract.PageName & "=" & Environment.NewLine _
                       & "[[File:" & Page_Image_pair(extract.PageName) & "|thumb|x120px]]" & Environment.NewLine _
                       & extract.ExtractContent & Environment.NewLine _
                       & ":'''[[" & extract.PageName & "|Leer más...]]'''" & Environment.NewLine)
            Else
                'If the page doesn't contain a image
                NewResumes.Add(extract.PageName, "|" & extract.PageName & "=" & Environment.NewLine _
                  & extract.ExtractContent & Environment.NewLine _
                  & ":'''[[" & extract.PageName & "|Leer más...]]'''" & Environment.NewLine)
            End If
        Next

        '===========================================================================================

        Dim EditScoreList As SortedList(Of Integer, Double()) = _bot.GetORESScores(IDLIST.Values.ToArray)

        If Not SettingsProvider.Contains("ORESBFThreshold") Then
            SettingsProvider.NewVal("ORESBFThreshold", 51)
        End If
        Dim ORESBFThreshold As Integer = CInt(SettingsProvider.Get("ORESBFThreshold"))

        '==========================================================================================
        'Choose between a old resume and a new resume depending if new resume is safe to use
        EventLogger.Debug_Log(BotMessages.RecreatingText, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        For Each s As String In PageNames.ToArray
            Try
                Dim bfscore As Double = EditScoreList(IDLIST(s))(0)
                If (bfscore < ORESBFThreshold) And (CountCharacter(NewResumes(s), CType("[", Char)) = CountCharacter(NewResumes(s), CType("]", Char))) Then
                    'Safe edit
                    FinalList.Add(NewResumes(s))
                    Safepages += 1
                Else
                    'Isn't a safe edit
                    Try
                        NotSafepages += 1
                        If OldResumes.Keys.Contains(s) Then
                            FinalList.Add(OldResumes(s))
                        Else
                            FinalList.Add(NewResumes(s))
                            NotSafePagesAdded += 1
                        End If
                    Catch ex As KeyNotFoundException
                        FinalList.Add(NewResumes(s))
                        NotSafePagesAdded += 1
                    End Try
                End If
            Catch ex As KeyNotFoundException
                'If the resume doesn't exist, will try to use the old resume text
                If OldResumes.Keys.Contains(s) Then
                    FinalList.Add(OldResumes(s))
                Else
                    FinalList.Add(NewResumes(s))
                    NotSafePagesAdded += 1
                End If
                NotSafepages += 1
            End Try
        Next
        '==========================================================================================
        NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "}}" & Environment.NewLine & "<noinclude>{{documentación}}</noinclude>"
        EventLogger.Debug_Log(String.Format(BotMessages.TryingToSave, ResumePage.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)

        Try
            Dim EditSummary As String = String.Format(BotMessages.UpdatedExtracts, Safepages.ToString)

            If NewPages > 0 Then
                Dim NewPageText As String = String.Format(BotMessages.AddedExtract, NewPages.ToString)
                If NewPages > 1 Then
                    NewPageText = String.Format(BotMessages.AddedExtracts, NewPages.ToString)
                End If
                EditSummary &= NewPageText
            End If

            If NotSafepages > 0 Then
                Dim NumbText As String = String.Format(BotMessages.OmittedExtract, NotSafepages.ToString)
                If NotSafepages > 1 Then
                    NumbText = String.Format(BotMessages.OmittedExtracts, NotSafepages.ToString)
                End If
                NumbText = String.Format(NumbText, NotSafepages)
                EditSummary &= NumbText
            End If
            Dim Result As EditResults = ResumePage.Save(NewResumePageText, EditSummary, True, True)

            If Result = EditResults.Edit_successful Then
                EventLogger.Debug_Log(BotMessages.SuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return True
            Else
                EventLogger.Log(BotMessages.UnsuccessfulOperation & " (" & [Enum].GetName(GetType(EditResults), Result) & ").", Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
        Catch ex As IndexOutOfRangeException
            EventLogger.Log(BotMessages.UnsuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            EventLogger.Debug_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End Try
    End Function

    Function CheckInformalMediation() As Boolean
        Dim newThreads As Boolean = False
        Dim membPage As Page = _bot.Getpage(WPStrings.InformalMediationMembers)
        Dim MedPage As Page = _bot.Getpage(WPStrings.InfMedPage)
        Dim subthreads As String() = GetPageSubThreads(membPage.Content)
        Dim uTempList As List(Of Template) = Template.GetTemplates(subthreads(0))
        Dim userList As New List(Of String)
        For Each temp As Template In uTempList
            If temp.Valid AndAlso temp.Name = "u" Then
                userList.Add(temp.Parameters(0).Item2)
            End If
        Next

        Dim currentThreads As Integer = GetPageThreads(MedPage.Content).Count

        If SettingsProvider.Contains(WPStrings.InfMedSettingsName) Then
            If SettingsProvider.Get(WPStrings.InfMedSettingsName).GetType Is GetType(Integer) Then
                Dim lastthreadcount As Integer = Integer.Parse(SettingsProvider.Get(WPStrings.InfMedSettingsName).ToString)
                If currentThreads > lastthreadcount Then
                    SettingsProvider.Set(WPStrings.InfMedSettingsName, currentThreads)
                    newThreads = True
                Else
                    SettingsProvider.Set(WPStrings.InfMedSettingsName, currentThreads) 'Si disminuye la cantidad de hilos entonces lo guarda
                End If
            End If
        Else
            SettingsProvider.NewVal(WPStrings.InfMedSettingsName, currentThreads)
        End If

        If newThreads Then
            For Each u As String In userList
                Dim user As New WikiUser(_bot, u)
                If user.Exists And Not user.Blocked Then
                    Dim userTalkPage As Page = user.TalkPage
                    userTalkPage.AddSection(WPStrings.InfMedTitle, WPStrings.InfMedMsg, WPStrings.InfMedSumm, False)
                End If
            Next
        End If
        Return True
    End Function


    Sub CheckUsersActivity(ByVal templatePage As Page, ByVal pageToSave As Page)
        If pageToSave Is Nothing Then Exit Sub

        Dim ActiveUsers As New Dictionary(Of String, WikiUser)
        Dim InactiveUsers As New Dictionary(Of String, WikiUser)
        Dim InvalidUsers As New Dictionary(Of String, WikiUser)
        Dim inclusions As String() = _bot.GetallInclusions(templatePage)
        For Each p As String In inclusions
            Dim Username As String = p.Split(":"c)(1).Trim()
            'si es una subpágina
            If Username.Contains("/") Then
                Username = Username.Split("/"c)(0)
            End If
            'No cargar usuarios más de una vez
            If ActiveUsers.Keys.Contains(Username) Then
                Continue For
            End If
            If InactiveUsers.Keys.Contains(Username) Then
                Continue For
            End If
            If InvalidUsers.Keys.Contains(Username) Then
                Continue For
            End If
            Dim tpage As Page = _bot.Getpage(p)
            If (tpage.PageNamespace = 3) Or (tpage.PageNamespace = 2) Then
                'Cargar usuario
                Dim User As New WikiUser(_bot, Username)
                'Validar usuario
                If Not Archiver.ValidUser(User, _bot) Then
                    EventLogger.Debug_Log(String.Format(BotMessages.InvalidUser, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                    InvalidUsers.Add(User.UserName, User)
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

        Dim nousertext As String = "'''Error''': No se ha indicado usuario."
        Dim nouserstatus As String = "[[Archivo:WX circle red.png|10px|link=]]&nbsp;<span style=""color:red;"">'''Desconectado'''</span>"

        Dim t As New Template With {.Name = "#switch:{{{1|}}}"}
        t.Parameters.Add(New Tuple(Of String, String)("", nousertext))
        t.Parameters.Add(New Tuple(Of String, String)("#default", nouserstatus))

        For Each u As WikiUser In ActiveUsers.Values
            Dim gendertext As String = If(u.Gender = "female", "Conectada", "Conectado")
            Dim linetext As String = "[[Archivo:WX circle green.png|10px|link=]]&nbsp;<span style=""color:green;"">'''" & gendertext & "'''</span>"
            t.Parameters.Add(New Tuple(Of String, String)(u.UserName, linetext))
        Next

        For Each u As WikiUser In InactiveUsers.Values
            Dim gendertext As String = If(u.Gender = "female", "Desconectada", "Desconectado")
            Dim linetext As String = "[[Archivo:WX circle red.png|10px|link=|Última edición el " _
                & Integer.Parse(u.LastEdit.ToString("dd")).ToString _
                & u.LastEdit.ToString(" 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES")) _
                & "]]&nbsp;<span style=""color:red;"">'''" & gendertext & "'''</span>"

            t.Parameters.Add(New Tuple(Of String, String)(u.UserName, linetext))
        Next

        Dim templatetext As String = "{{Noart|1=<indicator name=""z-estado-usuario"">" & Environment.NewLine & t.Text
        templatetext = templatetext & Environment.NewLine & "</indicator>}}" & Environment.NewLine & "<noinclude>" & "{{documentación}}" & "</noinclude>"
        pageToSave.Save(templatetext, "Bot: Actualizando lista.", True, True, False)

    End Sub

    ''' <summary>
    ''' Actualiza un contador segun los hilos en el macrohilo de la pagina principal
    ''' </summary>
    ''' <param name="tpage">Pagina a analizar.</param>
    ''' <param name="mainthreadtocheck">Pagina a actualizar.</param>
    ''' <returns></returns>
    Function UpdateBotRecuestCount(ByVal tpage As Page, pagetoupdate As Page, mainthreadtocheck As Integer) As Boolean
        Dim mthreads As String() = GetPageMainThreads(tpage.Content)
        If mthreads.Count >= mainthreadtocheck Then
            Dim tthreads As String() = GetPageThreads(mthreads(mainthreadtocheck - 1))
            Dim CountPage As Page = pagetoupdate
            Dim tcounttexts As String() = TextInBetweenInclusive(CountPage.Content, "<onlyinclude>", "</onlyinclude>")
            If tcounttexts.Count >= 1 Then
                Dim newcount As String = "<onlyinclude>" & tthreads.Count & "</onlyinclude>"
                Dim newtext As String = CountPage.Content.Replace(tcounttexts(0), newcount)
                If (Not (newtext = CountPage.Content)) Then
                    CountPage.Save(newtext, BotMessages.UpdatingCount, True, True)
                    Return True
                End If
            End If
        End If
        Return False
    End Function

    Function UpdateBotList(ByVal Controllerpage As Page, PageToUpdate As Page, BotInfoBoxTemplateName As String) As Boolean
        Dim ttemplate As Template = Template.GetTemplates(Controllerpage.Content)(0)
        Dim BotText As String = String.Empty
        For Each p As Tuple(Of String, String) In ttemplate.Parameters
            If p.Item1.Contains("#default") Then Continue For
            Dim tpattern As String = "((<!--)[\s\S]*?(-->)|(<[nN]owiki>)([\s\S]+?)(<\/[nN]owiki>))"
            Dim tbot As WikiUser = New WikiUser(_bot, Regex.Replace(p.Item1, tpattern, "").Trim())
            Dim tcontroller As WikiUser = New WikiUser(_bot, Regex.Replace(p.Item2, tpattern, "").Trim())
            BotText = BotText & Environment.NewLine & "{{/bot|nombre=" & tbot.UserName & "|controlador=" & tcontroller.UserName & "|primera=" & If(tbot.EditCount < 2, "N/A", tbot.FirstEdit.ToString("dd-MM-yyyy")) _
            & "|última=" & If(tbot.EditCount < 2, "N/A", tbot.LastEdit.ToString("dd-MM-yyyy")) & "|días inactivo=" & If(tbot.EditCount < 2, "N/A", Date.UtcNow.Subtract(tbot.LastEdit).Days.ToString) & "|ediciones=" & tbot.EditCount.ToString _
            & "|flag=" & If(tbot.Exists AndAlso tbot.IsBot, "Sí", "No") & "|ficha de bot=" & If(tbot.Exists AndAlso If(tbot.UserPage.Content, "").ToLower.Contains("{{" & BotInfoBoxTemplateName.ToLower), "Sí", "No") _
            & "|bloqueo=" & If(tbot.Exists AndAlso tbot.Blocked, "Sí", "No") & "|bloqueo controlador=" & If(tcontroller.Exists AndAlso tcontroller.Blocked, "Sí", "No") & "}}"
        Next
        Dim tcontent As String = PageToUpdate.Content.Replace(TextInBetween(PageToUpdate.Content, "<!-- Marca de inicio de datos -->", "<!-- Marca de fin de datos -->")(0), BotText & Environment.NewLine)
        PageToUpdate.Save(tcontent, "Bot: Actualizando lista según [[Plantilla:Controlador|la plantilla de controladores]].", False, True, True)
        Return True
    End Function

    Function RemoveTemplateParameterFromContent(ByVal templateName As String, ByVal templateParameterName As String, ByVal wikiContent As String) As String
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                Dim originalTemplateText As String = t.Text
                If t.Name.Trim.Equals(templateName.Trim) Then
                    t.RemoveParameter(templateParameterName)
                    wikiContent = wikiContent.Replace(originalTemplateText, t.Text)
                End If
            Next
        End If
        Return wikiContent
    End Function


    Function ReplaceTemplateParameterNameFromContent(ByVal templateName As String, ByVal templateParameterName As String, ByVal templateParameterNewName As String, ByVal wikiContent As String) As String
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                Dim originalTemplateText As String = t.Text
                If t.Name.Trim.Equals(templateName.Trim) Then
                    t.ChangeNameOfParameter(templateParameterName, templateParameterNewName)
                    wikiContent = wikiContent.Replace(originalTemplateText, t.Text)
                End If
            Next
        End If
        Return wikiContent
    End Function


    Function ReplaceTemplateContentFromContent(ByVal templateName As String, ByVal templateParameterName As String, ByVal templateParameterNewContent As String, ByVal wikiContent As String) As String
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                Dim originalTemplateText As String = t.Text
                If t.Name.Trim.Equals(templateName.Trim) Then
                    t.ReplaceParameterContent(templateParameterName, templateParameterNewContent)
                    wikiContent = wikiContent.Replace(originalTemplateText, t.Text)
                End If
            Next
        End If
        Return wikiContent
    End Function

    Function AppendTemplateContentFromContent(ByVal templateName As String, ByVal templateParameterName As String, ByVal templateParameterContentToAppend As String, ByVal wikiContent As String) As String
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                Dim originalTemplateText As String = t.Text
                If t.Name.Trim.Equals(templateName.Trim) Then
                    t.AppendParameterContent(templateParameterName, templateParameterContentToAppend)
                    wikiContent = wikiContent.Replace(originalTemplateText, t.Text)
                End If
            Next
        End If
        Return wikiContent
    End Function

    Function CheckIfTemplateContainsParamFromContent(ByVal templateName As String, ByVal templateParameterName As String, ByVal wikiContent As String) As Boolean
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                If t.Name.Trim.Equals(templateName.Trim) Then
                    If t.ContainsParameter(templateParameterName) Then Return True
                End If
            Next
        End If
        Return False
    End Function

    Function AddParameterToTemplateFromContent(ByVal templateName As String, ByVal templateNewParameterName As String, ByVal templateNewParameterContent As String, ByVal wikiContent As String) As String
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                Dim originalTemplateText As String = t.Text
                If t.Name.Trim.Equals(templateName.Trim) Then
                    t.AppendParameter(templateNewParameterName, templateNewParameterContent)
                    wikiContent = wikiContent.Replace(originalTemplateText, t.Text)
                End If
            Next
        End If
        Return wikiContent
    End Function

    Function RemoveParameterFromTemplateIfEmptyFromContent(ByVal templateName As String, ByVal templateParameterName As String, ByVal wikiContent As String) As String
        If Template.IsTemplatePresentInText(wikiContent, templateName) Then
            Dim templates As List(Of Template) = Template.GetTemplates(wikiContent)
            For Each t As Template In templates
                Dim originalTemplateText As String = t.Text
                If t.Name.Trim.Equals(templateName.Trim) Then
                    If t.ContainsParameter(templateParameterName) Then
                        If String.IsNullOrWhiteSpace(t.GetParameterContent(templateParameterName)) Then
                            t.RemoveParameter(templateParameterName)
                        End If
                    End If
                End If
                wikiContent = wikiContent.Replace(originalTemplateText, t.Text)
            Next
        End If
        Return wikiContent
    End Function


End Class


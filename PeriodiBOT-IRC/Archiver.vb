Option Explicit On
Option Strict On
Imports System.Text.RegularExpressions
Imports MWBot.net.Utility
Imports MWBot.net.WikiBot
Imports PeriodiBOT_IRC.My.Resources

Public Class Archiver
    Private _bot As Bot
    Public Sub New(wikiBot As Bot)
        _bot = wikiBot
    End Sub


    ''' <summary>
    ''' Actualiza todas las paginas que incluyan la plantilla de archivado automático.
    ''' </summary>
    ''' <returns></returns>
    Function ArchiveAllInclusions(ByVal ArchiveTemplateName As String, DoNotArchiveTemplateName As String(), ProgrammedArchiveTemplateName As String,
                                  ArchiveBoxTemplateName As String, ArchiveMessageTemplateName As String, ExcludingRegexPattern As String) As Boolean
        Dim includedpages As String() = _bot.GetallInclusions(ArchiveTemplateName)
        EventLogger.Log(String.Format(BotMessages.ArchivingInclusions, ArchiveTemplateName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        For Each pa As String In includedpages
            Dim _Page As Page = _bot.Getpage(pa)
            If _Page.Exists Then
                Try
                    AutoArchive(_Page, ArchiveTemplateName, DoNotArchiveTemplateName, ProgrammedArchiveTemplateName,
                                ArchiveBoxTemplateName, ArchiveMessageTemplateName, ExcludingRegexPattern)
                Catch ex As Exception When Not Debugger.IsAttached
                    EventLogger.EX_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                End Try
            End If
        Next
        EventLogger.Log(BotMessages.AllArchived, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        Return True
    End Function

    ''' <summary>
    ''' Realiza un archivado siguiendo una lógica similar a la que seguía Grillitus.
    ''' </summary>
    ''' <param name="PageToArchive">Página a archivar.</param>
    ''' <param name="ArchiveTemplateName">Plantilla de archivado.</param>
    ''' <param name="DoNotArchiveTemplateName">Plantillas de "No archivar".</param>
    ''' <param name="ProgrammedArchiveTemplateName">Plantilla de archivo programado.</param>
    ''' <param name="ArchiveBoxTemplateName">Plantilla de caja de archivos.</param>
    ''' <param name="ArchiveMessageTemplateName">Plantilla de mensaje de archivo.</param>
    ''' <returns></returns>
    Function AutoArchive(ByVal PageToArchive As Page, ArchiveTemplateName As String, DoNotArchiveTemplateName As String(),
                          ProgrammedArchiveTemplateName As String, ArchiveBoxTemplateName As String, ArchiveMessageTemplateName As String, ExcludingRegexPattern As String) As Boolean

        EventLogger.Log(String.Format(BotMessages.AutoArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        If PageToArchive Is Nothing Then Return False
        Dim IndexPage As Page = _bot.Getpage(PageToArchive.Title & WPStrings.ArchiveIndex)
        Dim ArchiveCfg As ArchiveTemplateData = GetArchiveTemplateData(PageToArchive, ArchiveTemplateName)

        If Not ValidPage(PageToArchive, ArchiveCfg) Then Return False

        Dim ArchivePages As New List(Of String)
        Dim pageThreads As String() = Utils.GetPageThreads(PageToArchive.Content)

        Dim notify As Boolean
        Dim strategy As String = String.Empty
        Dim useBox As Boolean
        Dim pageDest As String = String.Empty
        Dim maxDays As Integer = 0
        If Not PageConfig(ArchiveCfg, pageDest, maxDays, strategy, useBox, notify, PageToArchive.Title) Then Return False

        If pageThreads.Count = 1 Then
            EventLogger.Log(String.Format(BotMessages.OneThreadPage, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        'Revisar hilos y archivar si corresponde
        Dim archiveResults As ThreadsArchiveResult =
            CheckAndArchiveThreads(PageToArchive.Title, pageThreads, PageToArchive.Content,
                                   strategy, pageDest, maxDays,
                                   DoNotArchiveTemplateName, ProgrammedArchiveTemplateName, ExcludingRegexPattern)

        Dim ArchivedList As SortedList(Of String, String) = archiveResults.ArchiveList
        Dim Newpagetext As String = archiveResults.UpdatedPageText
        Dim ArchivedThreads As Integer = archiveResults.ArchivedThreadsCount

        If ArchivedThreads > 0 Then

            'Guardar pagina principal
            If Not String.IsNullOrEmpty(Newpagetext) Then
                'Si debe tener caja de archivos...
                If useBox Then
                    If Not Regex.Match(Newpagetext, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                        Dim Archivetemplate As String = Utils.GetTemplate(PageToArchive.Content, ArchiveTemplateName, True).Text
                        Newpagetext = Newpagetext.Replace(Archivetemplate, Archivetemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
                    End If
                End If
                Dim isminor As Boolean = Not notify
                Dim Summary As String = String.Format(BotMessages.ArchivedThreadSumm, ArchivedThreads, maxDays.ToString)
                If ArchivedThreads > 1 Then
                    Summary = String.Format(BotMessages.ArchivedThreadsSumm, ArchivedThreads, maxDays.ToString)
                End If
                If Not PageToArchive.Save(Newpagetext, Summary, isminor, True, True) = EditResults.Edit_successful Then Return False
            End If

            'Guardar los hilos en los archivos correspondientes por fecha
            For Each k As KeyValuePair(Of String, String) In ArchivedList
                Dim isminor As Boolean = Not notify
                Dim Archivepage As String = k.Key
                Dim ThreadText As String = Environment.NewLine & k.Value
                Dim threadcount As Integer = Utils.GetPageThreads(Environment.NewLine & ThreadText).Count
                Dim ArchPage As Page = _bot.Getpage(Archivepage)
                Dim ArchivePageText As String = ArchPage.Content
                ArchivePages.Add(Archivepage)

                'Verificar si la página de archivado está en el mismo espacio de nombres
                If Not ArchPage.PageNamespace = PageToArchive.PageNamespace Then
                    EventLogger.Log(String.Format(BotMessages.InvalidNamespace, ArchPage.Title, ArchPage.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                    Return False
                End If

                'Verificar si la página de archivado es una subpágina de la raiz
                If Not ArchPage.Title.StartsWith(PageToArchive.RootPage) Then
                    EventLogger.Log(String.Format(BotMessages.NotASubPage, ArchPage.Title, PageToArchive.RootPage), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                End If

                'Anadir los hilos al texto
                ArchivePageText &= ThreadText

                'Añadir la plantilla de archivo
                If Not Utils.IsTemplatePresent(ArchivePageText, ArchiveMessageTemplateName) Then
                    Dim tarchivemessage As String = ArchiveMessageTemplateName
                    If tarchivemessage.Contains(":"c) Then
                        tarchivemessage = tarchivemessage.Split(":"c)(1).Trim
                    End If
                    ArchivePageText = "{{" & tarchivemessage & "}}" & Environment.NewLine & ArchivePageText
                End If

                'Si se usa la caja de archivos
                If useBox Then
                    'Verificar si contiene la plantilla de indice
                    If Not Regex.Match(ArchivePageText, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                        ArchivePageText = "{{" & IndexPage.Title & "}}" & Environment.NewLine & ArchivePageText
                    End If
                End If

                'Texto de resumen de edicion
                Dim PageToArchivePermanentLink As String = "[[Especial:EnlacePermanente/" & PageToArchive.ParentRevId & "|" & PageToArchive.Title & "]]"
                Dim SummaryText As String = String.Format(BotMessages.ArchivedThreadDestSumm, threadcount, maxDays.ToString(), PageToArchivePermanentLink)
                If threadcount > 1 Then
                    SummaryText = String.Format(BotMessages.ArchivedThreadsDestSumm, threadcount, maxDays.ToString(), PageToArchivePermanentLink)
                End If

                'Guardar
                Dim result As EditResults = ArchPage.Save(ArchivePageText, SummaryText, isminor, True, True)
                EventLogger.Log(String.Format(BotMessages.EditResultMessage, PageToArchive.Title, result.ToString()), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Next

            'Actualizar caja si corresponde
            If useBox Then
                UpdateBox(IndexPage, ArchivePages, ArchiveBoxTemplateName)
            End If

        Else
            EventLogger.Log(String.Format(BotMessages.NothingToArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        End If
        EventLogger.Log(String.Format(BotMessages.AutoArchiveDone, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        Return True
    End Function



    Private Function CheckAndArchiveThreads(ByVal Pagename As String, ByVal threads As String(), pagetext As String, strategy As String,
                                            ConfigDest As String, maxDays As Integer, DoNotArchiveTemplateNames As String(),
                                            ProgrammedArchiveTemplateName As String, ExcludingRegexPattern As String) As ThreadsArchiveResult
        Dim archiveList As New SortedList(Of String, String)
        Dim newText As String = pagetext
        Dim maxDate As Date = Date.UtcNow.AddDays(-maxDays)
        Dim archivedThreads As Integer = 0

        For i As Integer = 0 To threads.Count - 1
            If archivedThreads >= threads.Count - 1 Then Continue For 'Dejar al menos un hilo!
            Dim thread As String = threads(i)
            Try
                Dim tDate As Date
                If strategy = WPStrings.MostRecentSignature Then
                    tDate = Utils.MostRecentDate(thread)
                ElseIf strategy = WPStrings.LastPSignature Then
                    tDate = Utils.LastParagraphDateTime(thread)
                Else
                    Continue For
                End If
                Dim threadresult As ThreadArchiveResult =
                    CheckAndArchiveThread(thread, tDate, maxDate, newText, ConfigDest,
                                          DoNotArchiveTemplateNames,
                                          ProgrammedArchiveTemplateName, ExcludingRegexPattern)

                If threadresult Is Nothing Then Continue For
                archivedThreads += 1
                Dim ArchivePageName As String = threadresult.DestinationPage
                Dim ArchiveThreadText As String = threadresult.ThreadText
                newText = threadresult.UpdatedThreadText
                If archiveList.ContainsKey(ArchivePageName) Then
                    archiveList(ArchivePageName) = archiveList(ArchivePageName) & ArchiveThreadText
                Else
                    archiveList.Add(ArchivePageName, ArchiveThreadText)
                End If
            Catch ex As Exception When Not Debugger.IsAttached
                EventLogger.EX_Log(String.Format(BotMessages.WikiThreadError, Pagename, i.ToString, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            End Try
        Next
        Return New ThreadsArchiveResult(archiveList, newText, archivedThreads)
    End Function

    Private Function CheckAndArchiveThread(
    ByVal threadtext As String,
    threaddate As Date,
    limitdate As Date,
    pagetext As String,
    ConfigDestination As String,
    DoNotArchiveTemplateNames As String(),
    ProgrammedArchiveTemplateName As String,
    ExcludingRegexPattern As String) As ThreadArchiveResult

        ' Check if thread should not be archived
        Dim doNotArchive As Boolean = DoNotArchiveTemplateNames.Any(Function(t) Utils.IsTemplatePresent(threadtext, t)) OrElse
                                 Regex.Match(threadtext, ExcludingRegexPattern).Success
        If doNotArchive Then
            Return Nothing
        End If

        ' Common archiving logic
        Dim shouldArchive As Boolean = False
        Dim destination As String = ReplaceDatePlaceholders(threaddate, ConfigDestination)

        ' Programmed archive check
        If Utils.IsTemplatePresent(threadtext, ProgrammedArchiveTemplateName) Then
            Dim programmedTemplate As Template = Utils.GetTemplate(threadtext, ProgrammedArchiveTemplateName, True)
            Dim fechaStr As String = programmedTemplate.Parameters _
            .FirstOrDefault(Function(p) p.Item1?.Trim().ToLower() = "fecha" OrElse p.Item1?.Trim().ToLower() = "1")?.Item2?.Trim()

            If Not String.IsNullOrEmpty(fechaStr) Then
                Try
                    ' Normalize date string
                    fechaStr = $" {fechaStr} ".Replace(" 1-", "01-").Replace(" 2-", "02-").Replace(" 3-", "03-").Replace(" 4-", "04-") _
                    .Replace(" 5-", "05-").Replace(" 6-", "06-").Replace(" 7-", "07-").Replace(" 8-", "08-").Replace(" 9-", "09-") _
                    .Replace("-1-", "-01-").Replace("-2-", "-02-").Replace("-3-", "-03-").Replace("-4-", "-04-").Replace("-5-", "-05-") _
                    .Replace("-6-", "-06-").Replace("-7-", "-07-").Replace("-8-", "-08-").Replace("-9-", "-09-").Trim()
                    fechaStr = Utils.RemoveAllAlphas(fechaStr)

                    Dim fecha As DateTime = DateTime.ParseExact(fechaStr, "ddMMyyyy", System.Globalization.CultureInfo.InvariantCulture)
                    shouldArchive = DateTime.Now > fecha.AddDays(1)
                Catch ex As FormatException
                    EventLogger.Debug_Log($"Invalid date format in programmed archive template: {fechaStr}", "CheckAndArchiveThread", _bot.UserName)
                    Return Nothing
                End Try
            End If
        Else
            ' Normal archive check
            shouldArchive = threaddate < limitdate
        End If

        ' Perform archiving if applicable
        If shouldArchive Then
            Dim updatedPageText As String = pagetext.Replace(threadtext, "")
            Return New ThreadArchiveResult(destination, threadtext, updatedPageText)
        End If

        Return Nothing
    End Function


    ''' <summary>
    ''' Obtiene los datos de una plantilla de archivado y retorna estos como ArchiveTemplateData.
    ''' </summary>
    ''' <param name="PageToArchive">Página desde donde se busca la plantilla.</param>
    ''' <returns></returns>
    Function GetArchiveTemplateData(PageToArchive As Page, ArchiveTemplateName As String) As ArchiveTemplateData
        If PageToArchive Is Nothing OrElse String.IsNullOrEmpty(ArchiveTemplateName) Then
            Return New ArchiveTemplateData()
        End If

        Dim archiveTemplate As Template = Utils.GetTemplate(PageToArchive.Content, ArchiveTemplateName, True)
        If String.IsNullOrEmpty(archiveTemplate?.Name) Then
            Return New ArchiveTemplateData()
        End If

        Dim destination As String = ""
        Dim days As String = "7"
        Dim notify As String = "No"
        Dim strategy As String = "FirmaEnÚltimoPárrafo"
        Dim useBox As String = "No"

        For Each param As Tuple(Of String, String) In archiveTemplate.Parameters
            Dim key As String = param.Item1?.Trim().ToLower()
            Dim value As String = param.Item2?.Trim()

            Select Case key
                Case WPStrings.ArchiveDestiny.ToLower()
                    If Not String.IsNullOrEmpty(value) Then
                        If value.Contains(":"c) Then
                            Dim parts As String() = value.Split(":"c, 2)
                            Dim destNamespace As String = Utils.UppercaseFirstCharacter(parts(0).Trim().ToLower())
                            Dim destPagename As String = Utils.UppercaseFirstCharacter(parts(1).Trim())
                            destination = $"{destNamespace}:{destPagename}"
                        Else
                            destination = value
                        End If
                    End If

                Case WPStrings.DaysTokeep.ToLower()
                    Dim cleanedDays As String = Utils.RemoveAllAlphas(value)
                    If Integer.TryParse(cleanedDays, Nothing) AndAlso Integer.Parse(cleanedDays) >= 7 Then
                        days = cleanedDays
                    ElseIf PageToArchive.PageNamespace = 4 Then
                        days = cleanedDays
                    End If

                Case WPStrings.WarnArchiving.ToLower()
                    notify = value

                Case WPStrings.Strategy.ToLower()
                    strategy = value

                Case WPStrings.KeepFileBox.ToLower()
                    useBox = value
            End Select
        Next

        Return New ArchiveTemplateData(destination, days, notify, strategy, useBox)
    End Function

    Private Function ReplaceDatePlaceholders(ByVal threaddate As Date, destination As String) As String
        Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
        Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
        Dim ThreadMonth2 As String = Utils.UppercaseFirstCharacter(threaddate.ToString("MMMM", New System.Globalization.CultureInfo("es-ES")))
        Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
        Dim ThreadSem As Integer
        Dim ThreadTrim As Integer

        If threaddate.Month < 7 Then
            ThreadSem = 1
        Else
            ThreadSem = 2
        End If

        Select Case True
            Case threaddate.Month <= 3
                ThreadTrim = 1
            Case threaddate.Month <= 6
                ThreadTrim = 2
            Case threaddate.Month <= 9
                ThreadTrim = 3
            Case threaddate.Month <= 12
                ThreadTrim = 4
        End Select


        Dim PageDestination As String = destination.Replace("AAAA", Threadyear).Replace("MMMM", ThreadMonth2).Replace("MM", ThreadMonth) _
                      .Replace("DD", ThreadDay).Replace("SEM", ThreadSem.ToString).Replace("TRIM", ThreadTrim.ToString)

        Return PageDestination
    End Function

    Private Function FixArchiveBox(ByVal pagetext As String) As String
        Return Regex.Replace(pagetext, "{{ *[Cc]aja (de)* *archivos", "{{Caja de archivos")
    End Function

    Private Function UpdateBox(Indexpage As Page, ArchivePages As IEnumerable(Of String), ArchiveBoxTemplateName As String) As Boolean
        Dim boxstring As String = WPStrings.BoxMessage
        If ArchiveBoxTemplateName.Contains(":"c) Then
            ArchiveBoxTemplateName = ArchiveBoxTemplateName.Split(":"c)(1).Trim
        End If
        Try
            'Verificar si está creada la página de archivo, si no, la crea.
            If Not Indexpage.Exists Then
                Dim newtext As String = boxstring & Environment.NewLine & "{{" & ArchiveBoxTemplateName & "|" & Environment.NewLine

                For Each p As String In ArchivePages
                    If Not newtext.Contains(p) Then
                        Dim ArchiveBoxLink As String = "[[" & p & "]]"
                        Dim Archivename As Match = Regex.Match(p, "\/.+")

                        If Archivename.Success Then
                            Dim Subpagename As String() = p.Split("/"c)
                            ArchiveBoxLink = "[[" & p & "|/" & Subpagename.Last & "]]"
                        End If
                        newtext = newtext & "<center>" & ArchiveBoxLink & "</center>" & Environment.NewLine
                    End If
                Next
                newtext &= "}}"
                Indexpage.Save(newtext, BotMessages.CreatingBoxSumm)

            Else
                Dim FixedPageContent As String = FixArchiveBox(Indexpage.Content)

                EventLogger.Debug_Log(BotMessages.UpdatingArchiveBox, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                If Utils.IsTemplatePresent(FixedPageContent, ArchiveBoxTemplateName) Then
                    Dim ArchiveBoxtext As String = Utils.GetTemplate(FixedPageContent, ArchiveBoxTemplateName, True).Text
                    Dim temptxt As String = ArchiveBoxtext
                    Dim temp As New Template(ArchiveBoxtext, False)
                    For Each t As Tuple(Of String, String) In temp.Parameters
                        'Buscar el item 1 de la plantilla de caja de archivos
                        If t.Item1 = "1" Then                                'Generar links en caja de archivos:
                            For Each p As String In ArchivePages
                                If Not temptxt.Contains(p) Then
                                    Dim ArchiveBoxLink As String = "[[" & p & "]]"
                                    Dim Archivename As Match = Regex.Match(p, "\/.+")
                                    If Archivename.Success Then
                                        Dim Subpagename As String() = p.Split("/"c)
                                        ArchiveBoxLink = "[[" & p & "|/" & Subpagename.Last & "]]"
                                    End If
                                    ArchiveBoxLink = "<center>" & ArchiveBoxLink & "</center>"
                                    temptxt = temptxt.Replace(t.Item2, t.Item2.TrimEnd(CType(Environment.NewLine, Char())) & Environment.NewLine & ArchiveBoxLink & Environment.NewLine)
                                End If
                            Next
                            Exit For
                        End If
                    Next
                    Dim newtext As String = FixedPageContent.Replace(ArchiveBoxtext, temptxt)
                    Indexpage.Save(newtext, BotMessages.UpdatingBoxSumm, True, True)

                Else 'No contiene una plantilla de caja de archivo, en ese caso se crea una nueva por sobre el contenido de la pagina

                    Dim newtext As String = boxstring & Environment.NewLine & "{{" & ArchiveBoxTemplateName & "|" & Environment.NewLine
                    For Each p As String In ArchivePages
                        If Not newtext.Contains(p) Then
                            Dim ArchiveBoxLink As String = "[[" & p & "]]"
                            Dim Archivename As Match = Regex.Match(p, "\/.+")

                            If Archivename.Success Then
                                Dim Subpagename As String() = p.Split("/"c)
                                ArchiveBoxLink = "[[" & p & "|/" & Subpagename.Last & "]]"
                            End If
                            newtext = newtext & "<center>" & ArchiveBoxLink & "</center>" & Environment.NewLine
                        End If
                    Next
                    newtext = newtext & "}}" & Environment.NewLine
                    Indexpage.Save(newtext & FixedPageContent, BotMessages.OverwritingBoxSumm)
                End If
            End If

        Catch ex As Exception When Not Debugger.IsAttached
            EventLogger.EX_Log(String.Format(BotMessages.UpdateBoxEx, Indexpage.Title, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End Try
        Return True
    End Function


    Private Function PageConfig(
    ByVal Params As ArchiveTemplateData,
    ByRef destination As String,
    ByRef maxDays As Integer,
    ByRef strategy As String,
    ByRef useBox As Boolean,
    ByRef notify As Boolean,
    sourcePageName As String) As Boolean

        ' Validate ArchiveTemplateData
        If Params Is Nothing OrElse Not Params.Valid Then
            EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        ' Destination
        destination = Params.Destination?.Trim()
        If String.IsNullOrEmpty(destination) Then
            EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        ' Days to keep
        Dim daysStr As String = Params.Days?.Trim()
        If Not Integer.TryParse(daysStr, maxDays) OrElse maxDays < 7 Then
            maxDays = Math.Max(maxDays, 7)
            If String.IsNullOrEmpty(daysStr) Then
                EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
        End If

        ' Notify
        Dim notifyStr As String = Params.Notify?.Trim().ToLower()
        notify = String.IsNullOrEmpty(notifyStr) OrElse notifyStr.Contains(WPStrings.YES) OrElse notifyStr.Contains(WPStrings.YES2)

        ' Strategy
        Dim validStrategies As New HashSet(Of String) From {
        WPStrings.LastPSignature,
        WPStrings.MostRecentSignature}

        Dim strategyInput As String = Params.Strategy?.Trim()
        strategy = If(validStrategies.Contains(strategyInput), strategyInput, WPStrings.LastPSignature)

        ' Use archive box
        Dim useBoxStr As String = Params.UseBox?.Trim().ToLower()
        useBox = Not String.IsNullOrEmpty(useBoxStr) AndAlso
             (useBoxStr.Contains(WPStrings.YES) OrElse useBoxStr.Contains(WPStrings.YES2))

        Return True
    End Function

    Function ValidPage(ByVal PageToArchive As Page, ByRef ArchiveCfg As ArchiveTemplateData) As Boolean
        'Verificar el espacio de nombres de la página se archiva
        If Not ValidNamespace(PageToArchive) Then
            EventLogger.Debug_Log(String.Format(BotMessages.InvalidNamespace, PageToArchive.Title, PageToArchive.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        'En caso de que el parámetro esté vacío o nulo
        If (String.IsNullOrWhiteSpace(ArchiveCfg.Destination)) Then
            EventLogger.Debug_Log(String.Format(BotMessages.InvalidPageName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        'Verificar si es una discusión de usuario.
        If PageToArchive.PageNamespace = 3 Then

            Dim Username As String = PageToArchive.Title.Split(":"c)(1)
            'si es una subpágina
            If Username.Contains("/") Then
                Username = Username.Split("/"c)(0)
            End If
            'Cargar usuario
            Dim User As New WikiUser(_bot, Username)
            'Validar usuario
            If Not ValidUser(User) Then
                EventLogger.Debug_Log(String.Format(BotMessages.InvalidUserArchive, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
            'Validar que destino de archivado sea una subpágina del usuario.
            If Not ArchiveCfg.Destination.StartsWith(PageToArchive.Title) Then
                EventLogger.Log(String.Format(BotMessages.NotASubPage, ArchiveCfg.Destination, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
        End If

        'Validar que los espacios de nombres sean iguales
        Dim ArchiveSubpagePrefix As Page = _bot.Getpage(ArchiveCfg.Destination)
        If Not ArchiveSubpagePrefix.PageNamespace = PageToArchive.PageNamespace Then
            EventLogger.Log(String.Format(BotMessages.InvalidNamespace, ArchiveSubpagePrefix.Title, ArchiveSubpagePrefix.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        ArchiveCfg.Valid = True

        Return True
    End Function

    ''' <summary>
    ''' Verifica si la página forma parte de un espacio de nombres válido
    ''' </summary>
    ''' <param name="pageToCheck"></param>
    ''' <returns></returns>
    Private Function ValidNamespace(pageToCheck As Page) As Boolean
        Dim validNamespaces As Integer() = {1, 3, 4, 5, 9, 11, 13, 15, 101, 102, 103, 105, 829}
        If Not validNamespaces.Contains(pageToCheck.PageNamespace) Then
            EventLogger.Debug_Log(String.Format(BotMessages.InvalidNamespace, pageToCheck.Title, pageToCheck.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Verifica si el usuario que se le pase cumple con los requisitos para archivar su discusión
    ''' </summary>
    ''' <param name="user">Usuario de Wiki</param>
    ''' <returns></returns>
    Public Shared Function ValidUser(ByVal user As WikiUser, bot As Bot) As Boolean
        EventLogger.Debug_Log(String.Format(BotMessages.CheckingUser, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, bot.UserName)
        'Verificar si el usuario existe
        If Not user.Exists Then
            EventLogger.Debug_Log(String.Format(BotMessages.UserInexistent, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, bot.UserName)
            Return False
        End If

        'Verificar si el usuario está bloqueado.
        If user.Blocked Then
            EventLogger.Debug_Log(String.Format(BotMessages.UserBlocked, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, bot.UserName)
            Return False
        End If

        'Verificar si el usuario editó hace al menos 4 días.
        If Date.Now.Subtract(user.LastEdit).Days >= 4 Then

            EventLogger.Debug_Log(String.Format(BotMessages.UserInactive, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, bot.UserName)
            Return False
        End If
        Return True
    End Function

    Private Function ValidUser(ByVal user As WikiUser) As Boolean
        Return ValidUser(user, _bot)
    End Function

    'Class that holds an archive template data
    Public Class ArchiveTemplateData
        Public Destination As String
        Public Days As String
        Public Notify As String
        Public Strategy As String
        Public UseBox As String
        Public Valid As Boolean
        Sub New(T_Destination As String, T_Days As String, T_Notify As String, T_Strategy As String, T_UseBox As String)
            Destination = T_Destination
            Days = T_Days
            Notify = T_Notify
            Strategy = T_Strategy
            UseBox = T_UseBox
            Valid = True
        End Sub
        Sub New()
            Valid = False
        End Sub
    End Class

    ' Class that holds the result of archiving multiple threads
    Public Class ThreadsArchiveResult
        Public Property ArchiveList As SortedList(Of String, String)
        Public Property UpdatedPageText As String
        Public Property ArchivedThreadsCount As Integer

        Public ReadOnly Property HasArchives As Boolean
            Get
                Return ArchiveList IsNot Nothing AndAlso ArchiveList.Count > 0
            End Get
        End Property

        Public Sub New(archives As SortedList(Of String, String), updatedText As String, archivedCount As Integer)
            ArchiveList = If(archives, New SortedList(Of String, String))
            UpdatedPageText = updatedText
            ArchivedThreadsCount = archivedCount
        End Sub
    End Class

    ' Class that holds the archive result
    Public Class ThreadArchiveResult
        Public Property DestinationPage As String
        Public Property ThreadText As String
        Public Property UpdatedThreadText As String

        Public ReadOnly Property HasArchive As Boolean
            Get
                Return Not String.IsNullOrEmpty(DestinationPage) AndAlso Not String.IsNullOrEmpty(ThreadText)
            End Get
        End Property

        Public Sub New(destPage As String, text As String, updatedText As String)
            DestinationPage = destPage
            ThreadText = text
            UpdatedThreadText = updatedText
        End Sub
    End Class


End Class

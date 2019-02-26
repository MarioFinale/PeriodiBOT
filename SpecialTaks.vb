Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.My.Resources
Imports MWBot.net.My.Resources
Imports MWBot.net.WikiBot
Imports MWBot.net

Class SpecialTaks
    Private _bot As Bot

    Sub New(ByRef WorkerBot As Bot)
        _bot = WorkerBot
    End Sub

    ''' <summary>
    ''' Verifica si el usuario que se le pase cumple con los requisitos para archivar su discusión
    ''' </summary>
    ''' <param name="user">Usuario de Wiki</param>
    ''' <returns></returns>
    Private Function ValidUser(ByVal user As WikiUser) As Boolean
        Utils.EventLogger.Debug_Log(String.Format(BotMessages.CheckingUser, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        'Verificar si el usuario existe
        If Not user.Exists Then
            Utils.EventLogger.Debug_Log(String.Format(BotMessages.UserInexistent, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        'Verificar si el usuario está bloqueado.
        If user.Blocked Then
            Utils.EventLogger.Log(String.Format(BotMessages.UserBlocked, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        'Verificar si el usuario editó hace al menos 4 días.
        If Date.Now.Subtract(user.LastEdit).Days >= 4 Then

            Utils.EventLogger.Debug_Log(String.Format(BotMessages.UserInactive, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If
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
            Utils.EventLogger.Debug_Log(String.Format(BotMessages.InvalidNamespace, pageToCheck.Title, pageToCheck.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If
        Return True
    End Function

    Private Function PageConfig(ByVal Params As String(), ByRef destination As String, ByRef maxDays As Integer, ByRef strategy As String, ByRef useBox As Boolean, ByRef notify As Boolean, sourcePageName As String) As Boolean
        If Not Params.Count >= 4 Then Return False
        'Destino
        If String.IsNullOrEmpty(Params(0)) Then
            Utils.EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        Else
            destination = Params(0)
        End If
        'Dias a mantener
        If String.IsNullOrEmpty(Params(1)) Then
            Utils.EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        Else
            maxDays = Integer.Parse(Params(1))
        End If
        'Avisar al archivar
        If String.IsNullOrEmpty(Params(2)) Then
            notify = True
        Else
            If Params(2).ToLower.Contains("si") Or Params(2).ToLower.Contains("sí") Then
                notify = True
            Else
                notify = False
            End If
        End If
        'Estrategia
        If String.IsNullOrEmpty(Params(3)) Then
            strategy = WPStrings.LastPSignature
        Else
            If Params(3) = WPStrings.LastPSignature Then
                strategy = WPStrings.LastPSignature
            ElseIf Params(3) = WPStrings.MostRecentSignature Then
                strategy = WPStrings.MostRecentSignature
            Else
                strategy = WPStrings.LastPSignature
            End If
        End If
        'Usar caja de archivos
        If String.IsNullOrEmpty(Params(4)) Then
            useBox = False
        Else
            If Params(4).ToLower.Contains(WPStrings.YES) Or Params(4).ToLower.Contains(WPStrings.YES2) Then
                useBox = True
            Else
                useBox = False
            End If
        End If
        Return True
    End Function


    Function ValidPage(ByVal PageToArchive As Page, ByVal ArchiveCfg As String()) As Boolean

        'Verificar el espacio de nombres de la página se archiva
        If Not ValidNamespace(PageToArchive) Then
            Utils.EventLogger.Debug_Log(String.Format(BotMessages.InvalidNamespace, PageToArchive.Title, PageToArchive.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
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
                Utils.EventLogger.Debug_Log(String.Format(BotMessages.InvalidUserArchive, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
            'Validar que destino de archivado sea una subpágina del usuario.
            If Not ArchiveCfg(0).StartsWith(PageToArchive.Title) Then
                Utils.EventLogger.Log(String.Format(BotMessages.NotASubPage, ArchiveCfg(0), PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
        End If
        Return True
    End Function

    ''' <summary>
    ''' Realiza un archivado siguiendo una lógica similar a la que seguía Grillitus.
    ''' </summary>
    ''' <param name="PageToArchive">Página a archivar.</param>
    ''' <param name="ArchiveTemplateName">Plantilla de archivado.</param>
    ''' <param name="DoNotArchiveTemplateName">Plantilla de "No archivar".</param>
    ''' <param name="ProgrammedArchiveTemplateName">Plantilla de archivo programado.</param>
    ''' <param name="ArchiveBoxTemplateName">Plantilla de caja de archivos.</param>
    ''' <param name="ArchiveMessageTemplateName">Plantilla de mensaje de archivo.</param>
    ''' <returns></returns>
    Function AutoArchive(ByVal PageToArchive As Page, ArchiveTemplateName As String, DoNotArchiveTemplateName As String,
                          ProgrammedArchiveTemplateName As String, ArchiveBoxTemplateName As String, ArchiveMessageTemplateName As String) As Boolean

        Utils.EventLogger.Log(String.Format(BotMessages.AutoArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        If PageToArchive Is Nothing Then Return False
        Dim IndexPage As Page = _bot.Getpage(PageToArchive.Title & WPStrings.ArchiveIndex)
        Dim ArchiveCfg As String() = GetArchiveTemplateData(PageToArchive, ArchiveTemplateName)

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
            Utils.EventLogger.Log(String.Format(BotMessages.OneThreadPage, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End If

        'Revisar hilos y archivar si corresponde
        Dim archiveResults As Tuple(Of SortedList(Of String, String), String, Integer) = CheckAndArchiveThreads(PageToArchive.Title, pageThreads, PageToArchive.Content,
                                                                                                                strategy, pageDest, maxDays, ArchiveTemplateName,
                                                                                                                DoNotArchiveTemplateName, ProgrammedArchiveTemplateName,
                                                                                                                ArchiveBoxTemplateName, ArchiveMessageTemplateName)
        Dim ArchivedList As SortedList(Of String, String) = archiveResults.Item1
        Dim Newpagetext As String = archiveResults.Item2
        Dim ArchivedThreads As Integer = archiveResults.Item3

        If ArchivedThreads > 0 Then
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
                    Utils.EventLogger.Log(String.Format(BotMessages.InvalidNamespace, ArchPage.Title, ArchPage.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                    Return False
                End If

                'Verificar si la página de archivado es una subpágina de la raiz
                If Not ArchPage.Title.StartsWith(PageToArchive.RootPage) Then
                    Utils.EventLogger.Log(String.Format(BotMessages.NotASubPage, ArchPage.Title, PageToArchive.RootPage), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                End If

                'Anadir los hilos al texto
                ArchivePageText = ArchivePageText & ThreadText

                'Añadir la plantilla de archivo
                If Not SimpleTemplateNoParamIsPresent(ArchivePageText, ArchiveMessageTemplateName) Then
                    Dim tarchivemessage As String = ArchiveMessageTemplateName
                    If tarchivemessage.Contains(":"c) Then
                        tarchivemessage = tarchivemessage.Split(":"c)(1).Trim
                    End If
                    ArchivePageText = tarchivemessage & Environment.NewLine & ArchivePageText
                End If

                'Si se usa la caja de archivos
                If useBox Then
                    'Verificar si contiene la plantilla de indice
                    If Not Regex.Match(ArchivePageText, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                        ArchivePageText = "{{" & IndexPage.Title & "}}" & Environment.NewLine & ArchivePageText
                    End If
                End If

                'Texto de resumen de edicion
                Dim SummaryText As String = String.Format(BotMessages.ArchivedThreadDestSumm, threadcount, maxDays.ToString, PageToArchive.Title)
                If threadcount > 1 Then
                    SummaryText = String.Format(BotMessages.ArchivedThreadsDestSumm, threadcount, maxDays.ToString, PageToArchive.Title)
                End If

                'Guardar
                ArchPage.Save(ArchivePageText, SummaryText, isminor, True)
            Next

            'Actualizar caja si corresponde
            If useBox Then
                UpdateBox(IndexPage, ArchivePages, ArchiveBoxTemplateName)
            End If

            'Guardar pagina principal
            If Not String.IsNullOrEmpty(Newpagetext) Then
                'Si debe tener caja de archivos...
                If useBox Then
                    If Not Regex.Match(Newpagetext, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                        Dim Archivetemplate As String = GetSimpleTemplate(PageToArchive.Content, ArchiveTemplateName)
                        Newpagetext = Newpagetext.Replace(Archivetemplate, Archivetemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
                    End If
                End If

                Dim isminor As Boolean = Not notify
                Dim Summary As String = String.Format(BotMessages.ArchivedThreadSumm, ArchivedThreads, maxDays.ToString)
                If ArchivedThreads > 1 Then
                    Summary = String.Format(BotMessages.ArchivedThreadsSumm, ArchivedThreads, maxDays.ToString)
                End If
                PageToArchive.Save(Newpagetext, Summary, isminor, True)
            End If
        Else
            Utils.EventLogger.Log(String.Format(BotMessages.NothingToArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        End If

        Utils.EventLogger.Log(String.Format(BotMessages.AutoArchiveDone, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        Return True
    End Function

    Private Function CheckAndArchiveThreads(ByVal Pagename As String, ByVal threads As String(), pagetext As String, strategy As String,
                                            ConfigDest As String, maxDays As Integer, ArchiveTemplateName As String, DoNotArchiveTemplateName As String,
                                            ProgrammedArchiveTemplateName As String, ArchiveBoxTemplateName As String,
                                            ArchiveMessageTemplateName As String) As Tuple(Of SortedList(Of String, String), String, Integer)

        Dim archiveList As New SortedList(Of String, String)
        Dim newText As String = pagetext
        Dim maxDate As Date = Date.UtcNow.AddDays(-maxDays)
        Dim archivedThreads As Integer = 0

        For i As Integer = 0 To threads.Count - 1
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
                Dim threadresult As Tuple(Of Tuple(Of String, String), String) = CheckAndArchiveThread(thread, tDate, maxDate, newText, ConfigDest,
                                                                                                       ArchiveTemplateName, DoNotArchiveTemplateName,
                                                                                                       ProgrammedArchiveTemplateName, ArchiveBoxTemplateName,
                                                                                                       ArchiveMessageTemplateName)
                If threadresult Is Nothing Then Continue For
                archivedThreads += 1
                Dim ArchivedThreadInfo As Tuple(Of String, String) = threadresult.Item1
                Dim ArchivePageName As String = ArchivedThreadInfo.Item1
                Dim ArchiveThreadText As String = ArchivedThreadInfo.Item2
                newText = threadresult.Item2
                If archiveList.ContainsKey(ArchivePageName) Then
                    archiveList(ArchivePageName) = archiveList(ArchivePageName) & ArchiveThreadText
                Else
                    archiveList.Add(ArchivePageName, ArchiveThreadText)
                End If
            Catch ex As Exception
                Utils.EventLogger.EX_Log(String.Format(BotMessages.WikiThreadError, Pagename, i.ToString, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            End Try
        Next
        Return New Tuple(Of SortedList(Of String, String), String, Integer)(archiveList, newText, archivedThreads)
    End Function

    Private Function SimpleTemplateNoParamIsPresent(ByVal text As String, templatename As String) As Boolean
        Dim PageNameWithoutNamespace As String = templatename.Replace("{{", "").Replace("}}", "")
        If templatename.Contains(":"c) Then
            PageNameWithoutNamespace = templatename.Split(":"c)(1).Trim
        End If
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0, 1).ToUpper & PageNameWithoutNamespace.Substring(0, 1).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & " *}}"
        Dim IsPresent As Boolean = Regex.Match(text, templateregex).Success
        Return IsPresent
    End Function

    Private Function GetSimpleTemplateNoParam(ByVal text As String, templatename As String) As String
        Dim PageNameWithoutNamespace As String = templatename.Split(":"c)(1).Trim
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0, 1).ToUpper & PageNameWithoutNamespace.Substring(0, 1).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & " *}}"
        Dim tTemplate As String = Regex.Match(text, templateregex).Value
        Return tTemplate
    End Function

    Private Function GetSimpleTemplate(ByVal text As String, templatename As String) As String
        Dim PageNameWithoutNamespace As String = templatename.Split(":"c)(1).Trim
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0, 1).ToUpper & PageNameWithoutNamespace.Substring(0, 1).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & "[\s\S]+?}}"
        Dim tTemplate As String = Regex.Match(text, templateregex).Value
        Return tTemplate
    End Function

    Private Function SimpleTemplatePresent(ByVal text As String, templatename As String) As Boolean
        Dim PageNameWithoutNamespace As String = templatename.Split(":"c)(1).Trim
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0, 1).ToUpper & PageNameWithoutNamespace.Substring(0, 1).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & "[\s\S]+?}}"
        Dim ispresent As Boolean = Regex.Match(text, templateregex).Success
        Return ispresent
    End Function

    Function GetTemplate(ByVal text As String, templatename As String, removenamespace As Boolean) As Template
        If removenamespace Then
            If templatename.Contains(":"c) Then
                templatename = templatename.Split(":"c)(1)
            End If
        End If
        Return GetTemplate(text, templatename)
    End Function
    Function GetTemplate(ByVal text As String, templatename As String) As Template
        Dim tlist As List(Of Template) = Template.GetTemplates(text)
        For Each t As Template In tlist
            If (t.Name.Trim.Substring(0, 1).ToUpper & t.Name.Trim.Substring(1).ToLower) = (templatename.Trim.Substring(0, 1).ToUpper & templatename.Trim.Substring(1).ToLower) Then
                Return t
            End If
        Next
        Return New Template
    End Function

    ''' <summary>
    ''' Entrega el comienzo de la plantilla (sin su espacio de nombres) si se encuentra en el texto
    ''' </summary>
    ''' <param name="text">Texto a analizar</param>
    ''' <param name="PageName">Nombre de la plantilla (con su espacio de nombres, para funcionar correctamente debe estar en el espacio de nombres "Template" o su equivalente en la wiki.</param>
    ''' <returns></returns>
    Function GetTemplateBeggining(ByVal text As String, PageName As String) As String
        Dim templatelist As List(Of Template) = Template.GetTemplates(text)
        For Each temp As Template In templatelist
            Dim PageNameWithoutNamespace As String = PageName.Split(":"c)(1).Trim
            Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0, 1).ToUpper & PageNameWithoutNamespace.Substring(0, 1).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
            Dim templateregex As String = "{{ *" & PageNameRegex & " *"
            Dim IsPresent As Boolean = Regex.Match(temp.Text, templateregex).Success
            If IsPresent Then
                Return Regex.Match(temp.Text, templateregex).Value
            End If
        Next
        Return String.Empty
    End Function

    Private Function CheckAndArchiveThread(ByVal threadtext As String, threaddate As Date, limitdate As Date,
                                           pagetext As String, ConfigDestination As String, ArchiveTemplateName As String,
                                           DoNotArchiveTemplateName As String, ProgrammedArchiveTemplateName As String, ArchiveBoxTemplateName As String,
                                           ArchiveMessageTemplateName As String) As Tuple(Of Tuple(Of String, String), String)

        Dim ProgrammedTemplate As String = GetSimpleTemplateNoParam(threadtext, ProgrammedArchiveTemplateName)
        Dim DoNotArchive As Boolean = SimpleTemplateNoParamIsPresent(threadtext, DoNotArchiveTemplateName)

        If Not DoNotArchive Then

            'Archivado programado
            If Not String.IsNullOrWhiteSpace(ProgrammedTemplate) Then
                Dim fechastr As String = Utils.TextInBetween(threadtext, ProgrammedTemplate, "}}")(0)
                fechastr = " " & fechastr & " "
                fechastr = fechastr.Replace(" 1-", "01-").Replace(" 2-", "02-").Replace(" 3-", "03-").Replace(" 4-", "04-") _
                    .Replace(" 5-", "05-").Replace(" 6-", "06-").Replace(" 7-", "07-").Replace(" 8-", "08-").Replace(" 9-", "09-") _
                    .Replace("-1-", "-01-").Replace("-2-", "-02-").Replace("-3-", "-03-").Replace("-4-", "-04-").Replace("-5-", "-05-") _
                    .Replace("-6-", "-06-").Replace("-7-", "-07-").Replace("-8-", "-08-").Replace("-9-", "-09-").Trim()
                fechastr = Utils.RemoveAllAlphas(fechastr)

                Dim fecha As DateTime = DateTime.ParseExact(fechastr, "ddMMyyyy", System.Globalization.CultureInfo.InvariantCulture)

                If DateTime.Now > fecha.AddDays(1) Then
                    pagetext = pagetext.Replace(threadtext, "")
                    Dim destination As String = SetPageDestination(threaddate, ConfigDestination)
                    Dim tdest As New Tuple(Of String, String)(destination, threadtext)
                    Return New Tuple(Of Tuple(Of String, String), String)(tdest, pagetext)
                End If
            Else
                'Archivado normal
                If threaddate < limitdate Then
                    pagetext = pagetext.Replace(threadtext, "")
                    Dim destination As String = SetPageDestination(threaddate, ConfigDestination)
                    Dim tdest As New Tuple(Of String, String)(destination, threadtext)
                    Return New Tuple(Of Tuple(Of String, String), String)(tdest, pagetext)
                End If
            End If
        End If
        Return Nothing
    End Function



    Private Function SetPageDestination(ByVal threaddate As Date, destination As String) As String
        Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
        Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
        Dim ThreadMonth2 As String = Utils.UppercaseFirstCharacter(threaddate.ToString("MMMM", New System.Globalization.CultureInfo("es-ES")))
        Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
        Dim Threadhyear As Integer

        If threaddate.Month < 7 Then
            Threadhyear = 1
        Else
            Threadhyear = 2
        End If
        Dim PageDestination As String = destination.Replace("AAAA", Threadyear).Replace("MMMM", ThreadMonth2).Replace("MM", ThreadMonth) _
                      .Replace("DD", ThreadDay).Replace("SEM", Threadhyear.ToString)

        Return PageDestination
    End Function


    Private Function FixArchiveBox(ByVal pagetext As String) As String
        Return Regex.Replace(pagetext, "{{ *[Cc]aja (de)* *archivos", "{{Caja de archivos")
    End Function



    Private Function UpdateBox(Indexpage As Page, ArchivePages As IEnumerable(Of String), ArchiveBoxTemplateName As String) As Boolean
        Dim boxstring As String = WPStrings.BoxMessage
        If ArchiveBoxTemplateName.Contains(":"c) Then
            ArchiveBoxTemplateName = ArchiveBoxTemplateName.Split(":"c)(1)
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
                newtext = newtext & "}}"
                Indexpage.Save(newtext, BotMessages.CreatingBoxSumm)

            Else
                Dim FixedPageContent As String = FixArchiveBox(Indexpage.Content)

                Utils.EventLogger.Debug_Log(BotMessages.UpdatingArchiveBox, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                If SimpleTemplatePresent(FixedPageContent, ArchiveBoxTemplateName) Then
                    Dim ArchiveBoxtext As String = GetSimpleTemplate(FixedPageContent, ArchiveBoxTemplateName)
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
                                        ArchiveBoxLink = "[[" & p & "|" & Archivename.Value & "]]"
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

        Catch ex As Exception
            Utils.EventLogger.EX_Log(String.Format(BotMessages.UpdateBoxEx, Indexpage.Title, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' Obtiene los datos de una plantilla de archivado y retorna estos como array.
    ''' </summary>
    ''' <param name="PageToArchive">Página desde donde se busca la plantilla.</param>
    ''' <returns></returns>
    Function GetArchiveTemplateData(PageToArchive As Page, ArchiveTemplateName As String) As String()
        Dim ArchiveTemplate As Template = GetTemplate(PageToArchive.Content, ArchiveTemplateName, True)
        If String.IsNullOrEmpty(ArchiveTemplate.Name) Then
            Return {"", "", "", "", ""}
        End If
        Dim Destination As String = String.Empty
        Dim Days As String = "30"
        Dim Notify As String = String.Empty
        Dim Strategy As String = String.Empty
        Dim UseBox As String = String.Empty

        For Each tup As Tuple(Of String, String) In ArchiveTemplate.Parameters
            If tup.Item1 = WPStrings.ArchiveDestiny Then
                Destination = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim()
                If Destination.Contains(":") Then
                    Dim destNamespace As String = Destination.Split(":"c)(0)
                    Dim destPagename As String = Utils.ReplaceFirst(Destination, destNamespace & ":", "")
                    Dim destParsedNamespace As String = Utils.UppercaseFirstCharacter(destNamespace.ToLower)
                    Dim destParsedPagename As String = Utils.UppercaseFirstCharacter(destPagename)
                    Destination = destParsedNamespace & ":" & destParsedPagename
                End If
            End If
            If tup.Item1 = WPStrings.DaysTokeep Then
                Days = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
                Days = Utils.RemoveAllAlphas(Days)
                If Integer.Parse(Days) < 7 Then
                    Days = "7"
                End If
            End If
            If tup.Item1 = WPStrings.WarnArchiving Then
                Notify = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
            End If
            If tup.Item1 = WPStrings.Strategy Then
                Strategy = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
            End If
            If tup.Item1 = WPStrings.KeepFileBox Then
                UseBox = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
            End If
        Next

        Return {Destination, Days, Notify, Strategy, UseBox}
    End Function

    ''' <summary>
    ''' Actualiza todas las paginas que incluyan la plantilla de archivado automático.
    ''' </summary>
    ''' <returns></returns>
    Function ArchiveAllInclusions(ByVal ArchiveTemplateName As String, DoNotArchiveTemplateName As String, ProgrammedArchiveTemplateName As String,
                                  ArchiveBoxTemplateName As String, ArchiveMessageTemplateName As String) As Boolean
        Dim includedpages As String() = _bot.GetallInclusions(ArchiveTemplateName)
        Utils.EventLogger.Log(String.Format(BotMessages.ArchivingInclusions, ArchiveTemplateName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        For Each pa As String In includedpages
            Dim _Page As Page = _bot.Getpage(pa)
            If _Page.Exists Then
                Try
                    AutoArchive(_Page, ArchiveTemplateName, DoNotArchiveTemplateName, ProgrammedArchiveTemplateName,
                                ArchiveBoxTemplateName, ArchiveMessageTemplateName)
                Catch ex As Exception
                    Utils.EventLogger.EX_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                End Try
            End If
        Next
        Return True
    End Function

    ''' <summary>
    ''' Obtiene la primera aparición de la plantilla de firmado en la página pasada como parámetro 
    ''' </summary>
    ''' <param name="PageToGet">Pagina de la cual se busca la plantilla de firmado</param>
    ''' <returns></returns>
    Function GetSignTemplate(ByVal PageToGet As Page) As Template
        Dim templist As List(Of Template) = Template.GetTemplates(Template.GetTemplateTextArray(PageToGet.Content))
        Dim signtemp As New Template
        For Each t As Template In templist
            If Regex.Match(t.Name, WPStrings.AutoSignatureTemplateInsideRegex).Success Then
                signtemp = t
                Exit For
            End If
        Next
        Return signtemp
    End Function

    ''' <summary>
    ''' Comprueba firmas faltantes en las páginas que contengan la plantilla de firma automática.
    ''' </summary>
    ''' <param name="AutoSignatureTemplateName">Plantilla de firma automática.</param>
    ''' <returns></returns>
    Function SignAllInclusions(AutoSignatureTemplateName As String) As Boolean
        Dim includedpages As String() = _bot.GetallInclusions(AutoSignatureTemplateName)
        For Each pa As String In includedpages
            Try
                Utils.EventLogger.Debug_Log("Checking page " & pa, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Dim _Page As Page = _bot.Getpage(pa)
                If _Page.Exists Then
                    If Not ValidNamespace(_Page) Then Continue For
                    If (Date.UtcNow - _Page.LastEdit) < (New TimeSpan(0, 15, 0)) Then Continue For
                    Dim SignTemplate As Template = GetSignTemplate(_Page)
                    Dim minor As Boolean = True
                    Dim newthreads As Boolean = False
                    For Each tup As Tuple(Of String, String) In SignTemplate.Parameters
                        If tup.Item1 = "Avisar al completar firma" Then
                            minor = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char())).ToLower = "no"
                        End If
                        If tup.Item1 = "Estrategia" Then
                            newthreads = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char())).ToLower = "NuevaSecciónSinFirmar"
                        End If
                    Next
                    If AddMissingSignature(_Page, newthreads, minor) Then
                        Utils.EventLogger.Log("SignAllInclusions: Page """ & pa & """", Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                    End If
                End If
            Catch ex As Exception
                Utils.EventLogger.EX_Log("SignAllInclusions: Page """ & pa & """ EX: " & ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            End Try
        Next
        Return True
    End Function



    ''' <summary>
    ''' Revisa todas las páginas que llamen a la página indicada y las retorna como sortedlist.
    ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
    ''' el último usuario que la editó y el segundo el título real de la página.
    ''' </summary>
    Function GetAllRequestedpages(pageName As String) As SortedList(Of String, String())
        Dim plist As New SortedList(Of String, String())
        For Each s As String In _bot.GetallInclusions(pageName)
            Dim Pag As Page = _bot.Getpage(s)
            Dim pagetext As String = Pag.Content
            For Each s2 As String In Utils.TextInBetween(pagetext, "{{" & pageName & "|", "}}")
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
    Function GetResumeRequests(ByVal pageName As String) As SortedList(Of String, String())
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
                    End If
                End If
            Catch ex As IndexOutOfRangeException
            End Try
        Next
        Return Reqlist
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
        Utils.EventLogger.Debug_Log(String.Format(BotMessages.LoadingOldExtracts, ResumeTemplate.Parameters.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        Dim PageNames As New List(Of String)

        For Each PageResume As Tuple(Of String, String) In ResumeTemplate.Parameters
            PageNames.Add(PageResume.Item1)
            OldResumes.Add(PageResume.Item1, "|" & PageResume.Item1 & "=" & PageResume.Item2)
        Next

        For Each p As KeyValuePair(Of String, String()) In GetResumeRequests(pageName)
            PageNames.Add(p.Key)
            NewPages += 1
        Next
        PageNames.Sort()
        Dim IDLIST As SortedList(Of String, Integer) = _bot.GetLastRevIds(PageNames.ToArray)

        Utils.EventLogger.Debug_Log(String.Format(BotMessages.LoadingNewExtracts, PageNames.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        '============================================================================================
        ' Adding New resumes to list
        Dim Page_Resume_pair As SortedList(Of String, String) = _bot.GetWikiExtractFromPageNames(PageNames.ToArray, 660)
        Dim Page_Image_pair As SortedList(Of String, String) = _bot.GetImagesExtract(PageNames.ToArray)

        For Each Page As String In Page_Resume_pair.Keys
            Dim containsimage As Boolean = False
            If Page_Image_pair.Keys.Contains(Page) Then
                If Not Page_Image_pair.Item(Page) = String.Empty Then
                    containsimage = True
                End If
            End If
            If containsimage Then
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

        Dim EditScoreList As SortedList(Of Integer, Double()) = _bot.GetORESScores(IDLIST.Values.ToArray)

        If Not Utils.BotSettings.Contains("ORESBFThreshold") Then
            Utils.BotSettings.NewVal("ORESBFThreshold", 51)
        End If
        Dim ORESBFThreshold As Integer = CInt(Utils.BotSettings.Get("ORESBFThreshold"))

        '==========================================================================================
        'Choose between a old resume and a new resume depending if new resume is safe to use
        Utils.EventLogger.Debug_Log(BotMessages.RecreatingText, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        For Each s As String In PageNames.ToArray
            Try
                Dim bfscore As Double = EditScoreList(IDLIST(s))(0)
                If (bfscore < ORESBFThreshold) And (Utils.CountCharacter(NewResumes(s), CType("[", Char)) = Utils.CountCharacter(NewResumes(s), CType("]", Char))) Then
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
        Utils.EventLogger.Debug_Log(String.Format(BotMessages.TryingToSave, ResumePage.Title), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)

        Try
            Dim EditSummary As String = String.Format(BotMessages.UpdatedExtracts, Safepages.ToString)

            If NewPages > 0 Then
                Dim NewPageText As String = String.Format(BotMessages.AddedExtract, NewPages.ToString)
                If NewPages > 1 Then
                    NewPageText = String.Format(BotMessages.AddedExtracts, NewPages.ToString)
                End If
                EditSummary = EditSummary & NewPageText
            End If

            If NotSafepages > 0 Then
                Dim NumbText As String = String.Format(BotMessages.OmittedExtract, NotSafepages.ToString)
                If NotSafepages > 1 Then
                    NumbText = String.Format(BotMessages.OmittedExtracts, NotSafepages.ToString)
                End If
                NumbText = String.Format(NumbText, NotSafepages)
                EditSummary = EditSummary & NumbText
            End If
            Dim Result As EditResults = ResumePage.Save(NewResumePageText, EditSummary, True, True)

            If Result = EditResults.Edit_successful Then
                Utils.EventLogger.Debug_Log(BotMessages.SuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return True
            Else
                Utils.EventLogger.Log(BotMessages.UnsuccessfulOperation & " (" & [Enum].GetName(GetType(EditResults), Result) & ").", Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
                Return False
            End If
        Catch ex As IndexOutOfRangeException
            Utils.EventLogger.Log(BotMessages.UnsuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Utils.EventLogger.Debug_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
            Return False
        End Try

    End Function

    Function CheckInformalMediation() As Boolean
        Dim newThreads As Boolean = False
        Dim membPage As Page = _bot.Getpage(WPStrings.InformalMediationMembers)
        Dim MedPage As Page = _bot.Getpage(WPStrings.InfMedPage)
        Dim subthreads As String() = Utils.GetPageSubThreads(membPage.Content)
        Dim uTempList As List(Of Template) = Template.GetTemplates(subthreads(0))
        Dim userList As New List(Of String)
        For Each temp As Template In uTempList
            If temp.Name = "u" Then
                userList.Add(temp.Parameters(0).Item2)
            End If
        Next

        Dim currentThreads As Integer = Utils.GetPageThreads(MedPage).Count

        If Utils.BotSettings.Contains(WPStrings.InfMedSettingsName) Then
            If Utils.BotSettings.Get(WPStrings.InfMedSettingsName).GetType Is GetType(Integer) Then
                Dim lastthreadcount As Integer = Integer.Parse(Utils.BotSettings.Get(WPStrings.InfMedSettingsName).ToString)
                If currentThreads > lastthreadcount Then
                    Utils.BotSettings.Set(WPStrings.InfMedSettingsName, currentThreads)
                    newThreads = True
                Else
                    Utils.BotSettings.Set(WPStrings.InfMedSettingsName, currentThreads) 'Si disminuye la cantidad de hilos entonces lo guarda
                End If
            End If
        Else
            Utils.BotSettings.NewVal(WPStrings.InfMedSettingsName, currentThreads)
        End If

        If newThreads Then
            For Each u As String In userList
                Dim user As New WikiUser(_bot, u)
                If user.Exists Then
                    Dim userTalkPage As Page = user.TalkPage
                    userTalkPage.AddSection(WPStrings.InfMedTitle, WPStrings.InfMedMsg, WPStrings.InfMedSumm, False)
                End If
            Next
        End If
        Return True
    End Function

    Function GetLastUnsignedSection(ByVal tpage As Page, newthreads As Boolean) As Tuple(Of String, String, Date)
        If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
        Dim oldPage As Page = _bot.Getpage(tpage.ParentRevId)
        Dim currentPage As Page = tpage

        Dim oldPageThreads As String() = oldPage.Threads
        Dim currentPageThreads As String() = currentPage.Threads

        Dim LastEdit As Date = currentPage.LastEdit
        Dim LastUser As String = currentPage.Lastuser
        Dim editedthreads As String()

        If newthreads Then
            editedthreads = Utils.GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
        Else
            If oldPageThreads.Count = currentPageThreads.Count Then
                editedthreads = Utils.GetChangedThreads(oldPageThreads, currentPageThreads)
            ElseIf oldPageThreads.Count < currentPageThreads.Count Then
                editedthreads = Utils.GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
            Else
                editedthreads = {}
            End If
        End If

        If editedthreads.Count > 0 Then
            Dim lasteditedthread As String = editedthreads.Last
            Dim lastsign As Date = Utils.LastParagraphDateTime(lasteditedthread)
            If lastsign = New DateTime(9999, 12, 31, 23, 59, 59) Then
                Return New Tuple(Of String, String, Date)(lasteditedthread, LastUser, LastEdit)
            End If
        End If
        Return Nothing
    End Function

    Function AddMissingSignature(ByVal tpage As Page, newthreads As Boolean, minor As Boolean) As Boolean
        If tpage.Lastuser = _bot.UserName Then Return False 'No completar firma en páginas en las que haya editado
        Dim LastUser As WikiUser = New WikiUser(_bot, tpage.Lastuser)
        If LastUser.IsBot Then Return False
        Dim UnsignedSectionInfo As Tuple(Of String, String, Date) = GetLastUnsignedSection(tpage, newthreads)
        If UnsignedSectionInfo Is Nothing Then Return False
        Dim pagetext As String = tpage.Content
        Dim UnsignedThread As String = UnsignedSectionInfo.Item1
        Dim Username As String = UnsignedSectionInfo.Item2
        Dim pusername As String = String.Empty
        If tpage.PageNamespace = 3 Then
            If tpage.Title.Contains(":") Then
                pusername = tpage.Title.Split(":"c)(1)
                If pusername.Contains("/") Then
                    pusername = pusername.Split("/"c)(0)
                End If
                pusername = pusername.Trim()
            End If
        End If
        If pusername = Username Then Return False
        Dim UnsignedDate As Date = UnsignedSectionInfo.Item3
        Dim dstring As String = Utils.GetSpanishTimeString(UnsignedDate)
        pagetext = pagetext.Replace(UnsignedThread, UnsignedThread & " {{sust:No firmado|" & Username & "|" & dstring & "}}")
        If tpage.Save(pagetext, String.Format(BotMessages.UnsignedSumm, Username), minor, True) = EditResults.Edit_successful Then
            Return True
        Else
            Return False
        End If
    End Function

    Sub CheckUsersActivity(ByVal templatePage As Page, ByVal pageToSave As Page)
        If pageToSave Is Nothing Then Exit Sub

        Dim ActiveUsers As New Dictionary(Of String, WikiUser)
        Dim InactiveUsers As New Dictionary(Of String, WikiUser)
        For Each p As Page In _bot.GetallInclusionsPages(templatePage)

            If (p.PageNamespace = 3) Or (p.PageNamespace = 2) Then
                Dim Username As String = p.Title.Split(":"c)(1)
                'si es una subpágina
                If Username.Contains("/") Then
                    Username = Username.Split("/"c)(0)
                End If
                'Cargar usuario
                Dim User As New WikiUser(_bot, Username)
                'Validar usuario
                If Not ValidUser(User) Then
                    Utils.EventLogger.Debug_Log(String.Format(Messages.InvalidUser, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name, _bot.UserName)
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
        pageToSave.Save(templatetext, "Bot: Actualizando lista.", True, True, False)

    End Sub

    ''' <summary>
    ''' Actualiza un contador segun los hilos en el macrohilo de la pagina principal
    ''' </summary>
    ''' <param name="tpage">Pagina a analizar.</param>
    ''' <param name="mainthreadtocheck">Pagina a actualizar.</param>
    ''' <returns></returns>
    Function UpdateBotRecuestCount(ByVal tpage As Page, pagetoupdate As Page, mainthreadtocheck As Integer) As Boolean
        Dim mthreads As String() = Utils.GetPageMainThreads(tpage.Content)
        If mthreads.Count >= mainthreadtocheck Then
            Dim tthreads As String() = Utils.GetPageThreads(mthreads(mainthreadtocheck - 1))
            Dim CountPage As Page = pagetoupdate
            Dim tcounttexts As String() = Utils.TextInBetweenInclusive(CountPage.Content, "<onlyinclude>", "</onlyinclude>")
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
            BotText = BotText & Environment.NewLine & "{{/bot|nombre=" & tbot.UserName & "|controlador=" & tcontroller.UserName & "|primera=" & If(tbot.EditCount = 0, "N/A", tbot.FirstEdit.ToString("dd-MM-yyyy")) _
            & "|última=" & If(tbot.EditCount = 0, "N/A", tbot.LastEdit.ToString("dd-MM-yyyy")) & "|días inactivo=" & If(tbot.EditCount = 0, "0", Date.UtcNow.Subtract(tbot.LastEdit).Days.ToString) & "|ediciones=" & tbot.EditCount.ToString _
            & "|flag=" & If(tbot.Exists AndAlso tbot.IsBot, "Sí", "No") & "|ficha de bot=" & If(tbot.Exists AndAlso tbot.UserPage.Content.ToLower.Contains("{{" & BotInfoBoxTemplateName.ToLower), "Sí", "No") _
            & "|bloqueo=" & If(tbot.Exists AndAlso tbot.Blocked, "Sí", "No") & "|bloqueo controlador=" & If(tcontroller.Exists AndAlso tcontroller.Blocked, "Sí", "No") & "}}"
        Next
        Dim tcontent As String = PageToUpdate.Content.Replace(Utils.TextInBetween(PageToUpdate.Content, "<!-- Marca de inicio de datos -->", "<!-- Marca de fin de datos -->")(0), BotText & Environment.NewLine)
        PageToUpdate.Save(tcontent, "Bot: Actualizando lista según [[Plantilla:Controlador|la plantilla de controladores]].", False, True, True)
        Return True
    End Function

End Class

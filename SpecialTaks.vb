﻿Option Strict On
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
        Utils.EventLogger.Debug_Log(String.Format(BotMessages.CheckingUser, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, Reflection.MethodBase.GetCurrentMethod().Name)
        'Verificar si el usuario existe
        If Not user.Exists Then
            Utils.EventLogger.Log(String.Format(BotMessages.UserInexistent, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
            Return False
        End If

        'Verificar si el usuario está bloqueado.
        If user.Blocked Then
            Utils.EventLogger.Log(String.Format(BotMessages.UserBlocked, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
            Return False
        End If

        'Verificar si el usuario editó hace al menos 4 días.
        If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
            Utils.EventLogger.Log(String.Format(BotMessages.UserInactive, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
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
        Dim validNamespaces As Integer() = {1, 3, 4, 5, 11, 15, 101, 102, 103, 105, 447, 829}
        If Not validNamespaces.Contains(pageToCheck.PageNamespace) Then
            Utils.EventLogger.Log(String.Format(BotMessages.InvalidNamespace, pageToCheck.Title, pageToCheck.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name)
            Return False
        End If
        Return True
    End Function

    Private Function PageConfig(ByVal Params As String(), ByRef destination As String, ByRef maxDays As Integer, ByRef strategy As String, ByRef useBox As Boolean, ByRef notify As Boolean, sourcePageName As String) As Boolean
        If Not Params.Count >= 4 Then Return False
        'Destino
        If String.IsNullOrEmpty(Params(0)) Then
            Utils.EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name)
            Return False
        Else
            destination = Params(0)
        End If
        'Dias a mantener
        If String.IsNullOrEmpty(Params(1)) Then
            Utils.EventLogger.Log(String.Format(BotMessages.MalformedArchiveConfig, sourcePageName), Reflection.MethodBase.GetCurrentMethod().Name)
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
            Utils.EventLogger.Log(String.Format(BotMessages.InvalidNamespace, PageToArchive.Title, PageToArchive.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name)
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
                Utils.EventLogger.Log(String.Format(BotMessages.InvalidUserArchive, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End If
            'Validar que destino de archivado sea una subpágina del usuario.
            If Not ArchiveCfg(0).StartsWith(PageToArchive.Title) Then
                Utils.EventLogger.Log(String.Format(BotMessages.NotASubPage, ArchiveCfg(0), PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End If
        End If
        Return True
    End Function

    ''' <summary>
    ''' Realiza un archivado general siguiendo una lógica similar a la de Grillitus.
    ''' </summary>
    ''' <param name="PageToArchive">Página a archivar</param>
    ''' <returns></returns>
    Function AutoArchive(ByVal PageToArchive As Page) As Boolean
        Utils.EventLogger.Log(String.Format(BotMessages.AutoArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name)
        If PageToArchive Is Nothing Then Return False
        Dim IndexPage As Page = _bot.Getpage(PageToArchive.Title & WPStrings.ArchiveIndex)
        Dim ArchiveCfg As String() = GetArchiveTemplateData(PageToArchive)

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
            Utils.EventLogger.Log(String.Format(BotMessages.OneThreadPage, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name)
            Return False
        End If

        'Revisar hilos y archivar si corresponde
        Dim archiveResults As Tuple(Of SortedList(Of String, String), String, Integer) = CheckAndArchiveThreads(PageToArchive.Title, pageThreads, PageToArchive.Content, strategy, pageDest, maxDays)
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
                    Utils.EventLogger.Log(String.Format(BotMessages.InvalidNamespace, ArchPage.Title, ArchPage.PageNamespace), Reflection.MethodBase.GetCurrentMethod().Name)
                    Return False
                End If

                'Verificar si la página de archivado es una subpágina de la raiz
                If Not ArchPage.Title.StartsWith(PageToArchive.RootPage) Then
                    Utils.EventLogger.Log(String.Format(BotMessages.NotASubPage, ArchPage.Title, PageToArchive.RootPage), Reflection.MethodBase.GetCurrentMethod().Name)
                End If

                'Anadir los hilos al texto
                ArchivePageText = ArchivePageText & ThreadText

                'Añadir la plantilla de archivo
                If Not SimpleTemplateNoParamIsPresent(ArchivePageText, _bot.ArchiveMessageTemplate) Then
                    ArchivePageText = ArchiveMessage() & Environment.NewLine & ArchivePageText
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
                UpdateBox(IndexPage, ArchivePages)
            End If

            'Guardar pagina principal
            If Not String.IsNullOrEmpty(Newpagetext) Then
                'Si debe tener caja de archivos...
                If useBox Then
                    If Not Regex.Match(Newpagetext, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                        Dim Archivetemplate As String = GetArchiveTemplate(PageToArchive.Content)
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
            Utils.EventLogger.Log(String.Format(BotMessages.NothingToArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name)
        End If

        Utils.EventLogger.Log(String.Format(BotMessages.AutoArchiveDone, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name)
        Return True
    End Function




    Private Function CheckAndArchiveThreads(ByVal Pagename As String, ByVal threads As String(), pagetext As String, strategy As String, ConfigDest As String, maxDays As Integer) As Tuple(Of SortedList(Of String, String), String, Integer)
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
                Dim threadresult As Tuple(Of Tuple(Of String, String), String) = CheckAndArchiveThread(thread, tDate, maxDate, newText, ConfigDest)
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
                Utils.EventLogger.EX_Log(String.Format(BotMessages.WikiThreadError, Pagename, i.ToString, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name)
            End Try
        Next
        Return New Tuple(Of SortedList(Of String, String), String, Integer)(archiveList, newText, archivedThreads)
    End Function


    Private Function DoNotArchiveTemplatePresent(ByVal text As String) As Boolean
        Return SimpleTemplateMatch(text, _bot.AutoArchiveDoNotArchivePageName)
    End Function
    Private Function ProgrammedArchiveTemplatePresent(ByVal text As String) As Boolean
        Return SimpleTemplateMatch(text, _bot.AutoArchiveProgrammedArchivePageName)
    End Function
    Private Function GetProgrammedArchiveTemplate(ByVal text As String) As String
        Return GetTemplate(text, _bot.AutoArchiveProgrammedArchivePageName)
    End Function
    Private Function ArchiveBoxTemplate() As String
        Return _bot.ArchiveBoxTemplate.Split(":"c)(1).Trim
    End Function

    Private Function ArchiveMessage() As String
        Return _bot.ArchiveMessageTemplate.Split(":"c)(1).Trim
    End Function

    Private Function ArchiveBoxTemplatePresent(ByVal text As String) As Boolean
        Return SimpleTemplateMatch(text, _bot.ArchiveBoxTemplate)
    End Function

    Private Function ContainsArchiveTemplate(ByVal text As String) As Boolean
        Dim TempRegex As String = "[" & _bot.AutoArchiveTemplatePageName.Split(":"c)(1).Trim.Substring(0, 1).ToUpper & _bot.AutoArchiveTemplatePageName.Split(":"c)(1).Trim.Substring(0, 1).ToLower & "]" & _bot.AutoArchiveTemplatePageName.Split(":"c)(1).Trim.Substring(1)
        Dim FullRegex As String = "{{ *" & TempRegex & "[\s\S]+?}}"
        Dim result As Boolean = Regex.Match(text, FullRegex).Success
        Return result
    End Function

    Private Function GetArchiveTemplate(ByVal text As String) As String
        Dim TempRegex As String = "[" & _bot.AutoArchiveTemplatePageName.Split(":"c)(1).Trim.Substring(0, 1).ToUpper & _bot.AutoArchiveTemplatePageName.Split(":"c)(1).Trim.Substring(0, 1).ToLower & "]" & _bot.AutoArchiveTemplatePageName.Split(":"c)(1).Trim.Substring(1)
        Dim FullRegex As String = "{{ *" & TempRegex & "[\s\S]+?}}"
        Dim result As String = Regex.Match(text, FullRegex).Value
        Return result
    End Function

    Private Function GetArchiveBoxTemplate(ByVal text As String) As String
        Dim TempRegex As String = "[" & _bot.ArchiveBoxTemplate.Split(":"c)(1).Trim.Substring(0, 1).ToUpper & _bot.ArchiveBoxTemplate.Split(":"c)(1).Trim.Substring(0, 1).ToLower & "]" & _bot.ArchiveBoxTemplate.Split(":"c)(1).Trim.Substring(1)
        Dim FullRegex As String = "{{ *" & TempRegex & "[\s\S]+?}}"
        Dim result As String = Regex.Match(text, FullRegex).Value
        Return result
    End Function

    Function SimpleTemplateNoParamIsPresent(ByVal text As String, templatename As String) As Boolean
        Dim PageNameWithoutNamespace As String = templatename.Split(":"c)(1).Trim
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0).ToUpper & PageNameWithoutNamespace.Substring(0).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & " *}}"
        Dim IsPresent As Boolean = Regex.Match(text, templateregex).Success
        Return IsPresent
    End Function

    Function GetSimpleTemplateNoParam(ByVal text As String, templatename As String) As String
        Dim PageNameWithoutNamespace As String = templatename.Split(":"c)(1).Trim
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0).ToUpper & PageNameWithoutNamespace.Substring(0).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & " *}}"
        Dim tTemplate As String = Regex.Match(text, templateregex).Value
        Return tTemplate
    End Function


    ''' <summary>
    ''' Entrega el comienzo de la plantilla (sin su espacio de nombres) si se encuentra en el texto
    ''' </summary>
    ''' <param name="text">Texto a analizar</param>
    ''' <param name="PageName">Nombre de la plantilla (con su espacio de nombres, para funcionar correctamente debe estar en "Template" o su equivalente en la wiki.</param>
    ''' <returns></returns>
    Function GetTemplate(ByVal text As String, PageName As String) As String
        Dim templatelist As List(Of Template) = Template.GetTemplates(text)
        For Each temp As Template In templatelist
            Dim PageNameWithoutNamespace As String = PageName.Split(":"c)(1).Trim
            Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0).ToUpper & PageNameWithoutNamespace.Substring(0).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
            Dim templateregex As String = "{{ *" & PageNameRegex & " *"
            Dim IsPresent As Boolean = Regex.Match(temp.Text, templateregex).Success
            If IsPresent Then
                Return Regex.Match(temp.Text, templateregex).Value
            End If
        Next
        Return String.Empty
    End Function

    ''' <summary>
    ''' Indica sólamente si la plantilla esta en el texto (consume menos recursos que analizarla)
    ''' </summary>
    ''' <param name="text">Texto a analizar</param>
    ''' <param name="PageName">Nombre de la plantilla (con su espacio de nombres, para funcionar correctamente debe estar en "Template" o su equivalente en la wiki.</param>
    ''' <returns></returns>
    Function SimpleTemplateMatch(ByVal text As String, PageName As String) As Boolean
        Dim PageNameWithoutNamespace As String = PageName.Split(":"c)(1).Trim
        Dim PageNameRegex As String = "[" & PageNameWithoutNamespace.Substring(0).ToUpper & PageNameWithoutNamespace.Substring(0).ToLower & "]" & PageNameWithoutNamespace.Substring(1)
        Dim templateregex As String = "{{ *" & PageNameRegex & " *"
        Dim IsPresent As Boolean = Regex.Match(text, templateregex).Success
        Return IsPresent
    End Function

    Private Function CheckAndArchiveThread(ByVal threadtext As String, threaddate As Date, limitdate As Date, pagetext As String, ConfigDestination As String) As Tuple(Of Tuple(Of String, String), String)

        Dim ProgrammedTemplate As String = GetProgrammedArchiveTemplate(threadtext)
        Dim DoNotArchive As Boolean = DoNotArchiveTemplatePresent(threadtext)

        If Not DoNotArchive Then

            'Archivado programado
            If Not String.IsNullOrWhiteSpace(ProgrammedTemplate) Then
                Dim fechastr As String = Utils.TextInBetween(threadtext, ProgrammedTemplate, "}}")(0)
                fechastr = " " & fechastr & " "
                fechastr = fechastr.Replace(" 1-", "01-").Replace(" 2-", "02-").Replace(" 3-", "03-").Replace(" 4-", "04-") _
                    .Replace(" 5-", "05-").Replace(" 6-", "06-").Replace(" 7-", "07-").Replace(" 8-", "08-").Replace(" 9-", "09-") _
                    .Replace("-1-", "-01-").Replace("-2-", "-02-").Replace("-3-", "-03-").Replace("-4-", "-04-").Replace("-5-", "-05-") _
                    .Replace("-6-", "-06-").Replace("-7-", "-07-").Replace("-8-", "-08-").Replace("-9-", "-09-").Trim()

                Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'MM'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)

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



    Private Function UpdateBox(Indexpage As Page, ArchivePages As IEnumerable(Of String)) As Boolean
        Dim boxstring As String = WPStrings.BoxMessage
        Try
            'Verificar si está creada la página de archivo, si no, la crea.
            If Not Indexpage.Exists Then
                Dim newtext As String = boxstring & Environment.NewLine & ArchiveBoxTemplate() & Environment.NewLine

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

                Utils.EventLogger.Debug_Log(BotMessages.UpdatingArchiveBox, Reflection.MethodBase.GetCurrentMethod().Name)
                If ArchiveBoxTemplatePresent(FixedPageContent) Then
                    Dim ArchiveBoxtext As String = GetArchiveBoxTemplate(FixedPageContent)
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

                    Dim newtext As String = boxstring & Environment.NewLine & "{{" & ArchiveBoxTemplate() & Environment.NewLine
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
            Utils.EventLogger.EX_Log(String.Format(BotMessages.UpdateBoxEx, Indexpage.Title, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' Obtiene los datos de una plantilla de archivado y retorna estos como array.
    ''' </summary>
    ''' <param name="PageToArchive">Página desde donde se busca la plantilla.</param>
    ''' <returns></returns>
    Function GetArchiveTemplateData(PageToArchive As Page) As String()

        Dim ArchiveTemplate As Template = GetArchiveTemplate(PageToArchive)
        If String.IsNullOrEmpty(ArchiveTemplate.Name) Then
            Return {"", "", "", "", ""}
        End If
        Dim Destination As String = String.Empty
        Dim Days As String = String.Empty
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
    ''' Obtiene la primera aparición de la plantilla de archivado en la página pasada como parámetro 
    ''' </summary>
    ''' <param name="PageToGet">Pagina de la cual se busca la plantilla de archivado</param>
    ''' <returns></returns>
    Function GetArchiveTemplate(ByVal PageToGet As Page) As Template
        Dim templist As List(Of Template) = Template.GetTemplates(Template.GetTemplateTextArray(PageToGet.Content))
        Dim Archtemp As New Template
        For Each t As Template In templist
            If ContainsArchiveTemplate(t.Text) Then
                Archtemp = t
                Exit For
            End If
        Next
        Return Archtemp
    End Function

    ''' <summary>
    ''' Actualiza todas las paginas que incluyan la plantilla de archivado automático.
    ''' </summary>
    ''' <returns></returns>
    Function ArchiveAllInclusions() As Boolean
        Dim templatePageName As String = _bot.AutoArchiveTemplatePageName
        Dim includedpages As String() = _bot.GetallInclusions(templatePageName)
        Utils.EventLogger.Log(String.Format(BotMessages.ArchivingInclusions, templatePageName), Reflection.MethodBase.GetCurrentMethod().Name)
        For Each pa As String In includedpages
            Dim _Page As Page = _bot.Getpage(pa)
            If _Page.Exists Then
                Try
                    AutoArchive(_Page)
                Catch ex As Exception
                    Utils.EventLogger.EX_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name)
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
    ''' Actualiza todas las paginas que incluyan la plantilla de archivado automático.
    ''' </summary>
    ''' <returns></returns>
    Function SignAllInclusions() As Boolean
        Dim includedpages As String() = _bot.GetallInclusions(_bot.AutoSignatureTemplatePageName)
        For Each pa As String In includedpages
            Try
                Utils.EventLogger.Debug_Log("Checking page " & pa, Reflection.MethodBase.GetCurrentMethod().Name)
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
                        Utils.EventLogger.Log("SignAllInclusions: Page """ & pa & """", Reflection.MethodBase.GetCurrentMethod().Name)
                    End If
                End If
            Catch ex As Exception
                Utils.EventLogger.EX_Log("SignAllInclusions: Page """ & pa & """ EX: " & ex.Message, Reflection.MethodBase.GetCurrentMethod().Name)
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
        Utils.EventLogger.Log(String.Format(BotMessages.GetPageExtract, pageName), Reflection.MethodBase.GetCurrentMethod().Name)
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
        Utils.EventLogger.Debug_Log(String.Format(BotMessages.LoadingOldExtracts, ResumeTemplate.Parameters.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name)
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

        Utils.EventLogger.Debug_Log(String.Format(BotMessages.LoadingNewExtracts, PageNames.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name)
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

        Dim EditScoreList As SortedList(Of Integer, Double()) = _bot.GetORESScores(IDLIST.Values.ToArray)

        '==========================================================================================
        'Choose between a old resume and a new resume depending if new resume is safe to use
        Utils.EventLogger.Debug_Log(BotMessages.RecreatingText, Reflection.MethodBase.GetCurrentMethod().Name)
        For Each s As String In PageNames.ToArray
            Try
                If (EditScoreList(IDLIST(s))(0) < 20) And (Utils.CountCharacter(NewResumes(s), CType("[", Char)) = Utils.CountCharacter(NewResumes(s), CType("]", Char))) Then
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
        NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "}}" & Environment.NewLine & "<noinclude>{{documentación}}</noinclude>"
        Utils.EventLogger.Debug_Log(String.Format(BotMessages.TryingToSave, ResumePage.Title), Reflection.MethodBase.GetCurrentMethod().Name)

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
                Utils.EventLogger.Log(BotMessages.SuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name)
                Return True
            Else
                Utils.EventLogger.Log(BotMessages.UnsuccessfulOperation & " (" & [Enum].GetName(GetType(EditResults), Result) & ").", Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End If
        Catch ex As IndexOutOfRangeException
            Utils.EventLogger.Log(BotMessages.UnsuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name)
            Utils.EventLogger.Debug_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name)
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
        If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
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
        Dim UnsignedDate As Date = UnsignedSectionInfo.Item3
        Dim dstring As String = Utils.GetSpanishTimeString(UnsignedDate)
        pagetext = pagetext.Replace(UnsignedThread, UnsignedThread & " {{sust:No firmado|" & Username & "|" & dstring & "}}")
        If tpage.Save(pagetext, String.Format(BotMessages.UnsignedSumm, Username), minor, True) = EditResults.Edit_successful Then
            Return True
        Else
            Return False
        End If
    End Function



End Class
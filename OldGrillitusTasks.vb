Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.My.Resources

Namespace WikiBot
    Class OldGrillitusTasks
        Private _bot As Bot

        Sub New(ByVal WorkerBot As WikiBot.Bot)
            _bot = WorkerBot
        End Sub

        ''' <summary>
        ''' Verifica si el usuario que se le pase cumple con los requisitos para archivar su discusión
        ''' </summary>
        ''' <param name="user">Usuario de Wiki</param>
        ''' <returns></returns>
        Private Function ValidUser(ByVal user As WikiUser) As Boolean
            Utils.EventLogger.Debug_Log(String.Format(Messages.CheckingUser, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name, SStrings.LocalSource)
            'Verificar si el usuario existe
            If Not user.Exists Then
                Utils.EventLogger.Log("ValidUser: User " & user.UserName & " doesn't exist", SStrings.LocalSource)
                Return False
            End If

            'Verificar si el usuario está bloqueado.
            If user.Blocked Then
                Utils.EventLogger.Log("ValidUser: User " & user.UserName & " is blocked", SStrings.LocalSource)
                Return False
            End If

            'Verificar si el usuario editó hace al menos 4 días.
            If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
                Utils.EventLogger.Log("ValidUser: User " & user.UserName & " is inactive", SStrings.LocalSource)
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
                Utils.EventLogger.Log("The page " & pageToCheck.Title & " doesn't belong to any valid namespace. (NS:" & pageToCheck.PageNamespace & ")", SStrings.LocalSource)
                Return False
            End If
            Return True
        End Function

        Private Function PageConfig(ByVal Params As String(), ByRef destination As String, ByRef maxDays As Integer, ByRef strategy As String, ByRef useBox As Boolean, ByRef notify As Boolean) As Boolean
            If Not Params.Count >= 4 Then Return False
            'Destino
            If String.IsNullOrEmpty(Params(0)) Then
                Utils.EventLogger.Log("AutoArchive: Malformed config, aborting.", SStrings.LocalSource)
                Return False
            Else
                destination = Params(0)
            End If
            'Dias a mantener
            If String.IsNullOrEmpty(Params(1)) Then
                Utils.EventLogger.Log("AutoArchive: Malformed config, aborting.", SStrings.LocalSource)
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
                strategy = "FirmaEnÚltimoPárrafo"
            Else
                If Params(3) = "FirmaEnÚltimoPárrafo" Then
                    strategy = "FirmaEnÚltimoPárrafo"
                ElseIf Params(3) = "FirmaMásRecienteEnLaSección" Then
                    strategy = "FirmaMásRecienteEnLaSección"
                Else
                    strategy = "FirmaEnÚltimoPárrafo"
                End If
            End If
            'Usar caja de archivos
            If String.IsNullOrEmpty(Params(4)) Then
                useBox = False
            Else
                If Params(4).ToLower.Contains("si") Or Params(4).ToLower.Contains("sí") Then
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
                Utils.EventLogger.Log("AutoArchive: The page" & PageToArchive.Title & " is not in a valid namespace, aborting.", SStrings.LocalSource)
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
                    Utils.EventLogger.Log("AutoArchive: """ & User.UserName & """ doesn't meet the requirements.", SStrings.LocalSource)
                    Return False
                End If
                'Validar que destino de archivado sea una subpágina del usuario.
                If Not ArchiveCfg(0).StartsWith(PageToArchive.Title) Then
                    Utils.EventLogger.Log("AutoArchive: The page" & ArchiveCfg(0) & " isn't a subpage of the same user.", SStrings.LocalSource)
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
            Utils.EventLogger.Log(String.Format(Messages.AutoArchive, PageToArchive.Title), Reflection.MethodBase.GetCurrentMethod().Name)
            If PageToArchive Is Nothing Then Return False
            Dim IndexPage As Page = _bot.Getpage(PageToArchive.Title & "/Archivo-00-índice")
            Dim ArchiveCfg As String() = GetArchiveTemplateData(PageToArchive)

            If Not ValidPage(PageToArchive, ArchiveCfg) Then Return False

            Dim ArchivePages As New List(Of String)
            Dim pageThreads As String() = Utils.GetPageThreads(PageToArchive.Text)

            Dim notify As Boolean
            Dim strategy As String = String.Empty
            Dim useBox As Boolean
            Dim pageDest As String = String.Empty
            Dim maxDays As Integer = 0
            If Not PageConfig(ArchiveCfg, pageDest, maxDays, strategy, useBox, notify) Then Return False

            If pageThreads.Count = 1 Then
                Utils.EventLogger.Log("AutoArchive: The page " & PageToArchive.Title & " only have one thread, aborting.", SStrings.LocalSource)
                Return False
            End If
            Utils.EventLogger.Debug_Log("AutoArchive: Read Threads", SStrings.LocalSource)

            'Revisar hilos y archivar si corresponde
            Dim archiveResults As Tuple(Of SortedList(Of String, String), String, Integer) = CheckAndArchiveThreads(PageToArchive.Title, pageThreads, PageToArchive.Text, strategy, pageDest, maxDays)
            Dim ArchivedList As SortedList(Of String, String) = archiveResults.Item1
            Dim Newpagetext As String = archiveResults.Item2
            Dim ArchivedThreads As Integer = archiveResults.Item3

            If ArchivedThreads > 0 Then
                'Guardar los hilos en los archivos correspondientes por fecha
                For Each k As KeyValuePair(Of String, String) In ArchivedList
                    Utils.EventLogger.Debug_Log("AutoArchive: Save pageThreads", SStrings.LocalSource)
                    Dim isminor As Boolean = Not notify
                    Dim Archivepage As String = k.Key
                    Dim ThreadText As String = Environment.NewLine & k.Value
                    Dim threadcount As Integer = Utils.GetPageThreads(Environment.NewLine & ThreadText).Count
                    Dim ArchPage As Page = _bot.Getpage(Archivepage)
                    Dim ArchivePageText As String = ArchPage.Text
                    ArchivePages.Add(Archivepage)

                    'Verificar si la página de archivado está en el mismo espacio de nombres
                    If Not ArchPage.PageNamespace = PageToArchive.PageNamespace Then
                        Utils.EventLogger.Log("AutoArchive: The page " & ArchPage.Title & " is not a in the same namespace of " & PageToArchive.Title & " aborting.", SStrings.LocalSource)
                        Return False
                    End If

                    'Verificar si la página de archivado es una subpágina de la raiz
                    If Not ArchPage.Title.StartsWith(PageToArchive.RootPage) Then
                        Utils.EventLogger.Log("AutoArchive: The page " & ArchPage.Title & " is not a subpage of " & PageToArchive.RootPage & " aborting.", SStrings.LocalSource)
                    End If

                    'Anadir los hilos al texto
                    ArchivePageText = ArchivePageText & ThreadText

                    'Añadir la plantilla de archivo
                    If Not Regex.Match(ArchivePageText, "{{ *[Aa]rchivo *}}").Success Then
                        ArchivePageText = "{{Archivo}}" & Environment.NewLine & ArchivePageText
                    End If

                    'Si se usa la caja de archivos
                    If useBox Then
                        'Verificar si contiene la plantilla de indice
                        If Not Regex.Match(ArchivePageText, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                            ArchivePageText = "{{" & IndexPage.Title & "}}" & Environment.NewLine & ArchivePageText
                        End If
                    End If

                    'Texto de resumen de edicion
                    Dim SummaryText As String = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad desde [[{2}]].", threadcount, maxDays.ToString, PageToArchive.Title)
                    If threadcount > 1 Then
                        SummaryText = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad desde [[{2}]].", threadcount, maxDays.ToString, PageToArchive.Title)
                    Else
                        SummaryText = String.Format("Bot: Archivando {0} hilo con más de {1} días de antigüedad desde [[{2}]].", threadcount, maxDays.ToString, PageToArchive.Title)
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
                    Utils.EventLogger.Debug_Log("AutoArchive: Save main page", SStrings.LocalSource)

                    'Si debe tener caja de archivos...
                    If useBox Then
                        If Not Regex.Match(Newpagetext, "{{" & IndexPage.Title & "}}", RegexOptions.IgnoreCase).Success Then
                            Dim Archivetemplate As String = Regex.Match(PageToArchive.Text, "{{ *[Aa]rchivado automático[\s\S]+?}}").Value
                            Newpagetext = Newpagetext.Replace(Archivetemplate, Archivetemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
                        End If
                    End If

                    Dim isminor As Boolean = Not notify
                    Dim Summary As String
                    If ArchivedThreads > 1 Then
                        Summary = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad.", ArchivedThreads, maxDays.ToString)
                    Else
                        Summary = String.Format("Bot: Archivando {0} hilo con más de {1} días de antigüedad.", ArchivedThreads, maxDays.ToString)
                    End If
                    PageToArchive.Save(Newpagetext, Summary, isminor, True)
                End If

            Else
                Utils.EventLogger.Log("AutoArchive: Nothing to archive on " & PageToArchive.Title, SStrings.LocalSource)
            End If

            Utils.EventLogger.Log("AutoArchive: " & PageToArchive.Title & " done.", SStrings.LocalSource)
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
                    If strategy = "FirmaMásRecienteEnLaSección" Then
                        tDate = Utils.MostRecentDate(thread)
                    ElseIf strategy = "FirmaEnÚltimoPárrafo" Then
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
                    Utils.EventLogger.EX_Log(String.Format(Messages.WikiThreadError, Pagename, i.ToString, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name)
                End Try
            Next
            Return New Tuple(Of SortedList(Of String, String), String, Integer)(archiveList, newText, archivedThreads)
        End Function


        Private Function CheckAndArchiveThread(ByVal threadtext As String, threaddate As Date, limitdate As Date, pagetext As String, ConfigDestination As String) As Tuple(Of Tuple(Of String, String), String)

            Dim ProgrammedMatch As Match = Regex.Match(threadtext, "{{ *[Aa]rchivo programado *\| *fecha\=")
            Dim DoNotArchiveMatch As Match = Regex.Match(threadtext, "{{ *[Nn]o archivar *")

            If Not DoNotArchiveMatch.Success Then

                'Archivado programado
                If ProgrammedMatch.Success Then
                    Dim fechastr As String = Utils.TextInBetween(threadtext, ProgrammedMatch.Value, "}}")(0)
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

        Private Function UpdateBox(Indexpage As Page, ArchivePages As IEnumerable(Of String)) As Boolean
            Dim boxstring As String = "<!-- Caja generada por PeriodiBOT, puedes editarla cuanto quieras, pero los nuevos enlaces siempre se añadirán al final. -->"
            Try
                'Verificar si está creada la página de archivo, si no, la crea.
                If Not Indexpage.Exists Then
                    Dim newtext As String = boxstring & Environment.NewLine & "{{caja archivos|" & Environment.NewLine

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
                    Indexpage.Save(newtext, "Bot: Creando nueva caja de archivos.")

                Else
                    Utils.EventLogger.Debug_Log("UpdateBox: Updating Box", SStrings.LocalSource)
                    Dim ArchiveBoxMatch As Match = Regex.Match(Indexpage.Text, "{{[Cc]aja (de)* *archivos[\s\S]+?}}")
                    If ArchiveBoxMatch.Success Then
                        Dim temptxt As String = ArchiveBoxMatch.Value
                        Dim temp As New Template(ArchiveBoxMatch.Value, False)
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
                        Dim newtext As String = Indexpage.Text.Replace(ArchiveBoxMatch.Value, temptxt)
                        Indexpage.Save(newtext, "Bot: Actualizando caja de archivos.", True, True)

                    Else 'No contiene una plantilla de caja de archivo, en ese caso se crea una nueva por sobre el contenido de la pagina

                        Dim newtext As String = boxstring & Environment.NewLine & "{{caja archivos|" & Environment.NewLine

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
                        Indexpage.Save(newtext & Indexpage.Text, "Bot: Actualizando caja de archivos por sobre contenido ya existente.")

                    End If
                End If

            Catch ex As Exception
                Utils.EventLogger.EX_Log("UpdateBox: " & ex.Message, SStrings.LocalSource)
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
                If tup.Item1 = "Destino" Then
                    Destination = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim()
                    If Destination.Contains(":") Then
                        Dim destNamespace As String = Destination.Split(":"c)(0)
                        Dim destPagename As String = Utils.ReplaceFirst(Destination, destNamespace & ":", "")
                        Dim destParsedNamespace As String = Utils.UppercaseFirstCharacter(destNamespace.ToLower)
                        Dim destParsedPagename As String = Utils.UppercaseFirstCharacter(destPagename)
                        Destination = destParsedNamespace & ":" & destParsedPagename
                    End If
                End If
                If tup.Item1 = "Días a mantener" Then
                    Days = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
                End If
                If tup.Item1 = "Avisar al archivar" Then
                    Notify = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
                End If
                If tup.Item1 = "Estrategia" Then
                    Strategy = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
                End If
                If tup.Item1 = "MantenerCajaDeArchivos" Then
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
            Dim templist As List(Of Template) = Template.GetTemplates(Template.GetTemplateTextArray(PageToGet.Text))
            Dim Archtemp As New Template
            For Each t As Template In templist
                If Regex.Match(t.Name, " *[Aa]rchivado automático").Success Then
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
            Dim includedpages As String() = _bot.GetallInclusions("Plantilla:Archivado automático")
            For Each pa As String In includedpages
                Utils.EventLogger.Log("ArchiveAllInclusions: Page " & pa, SStrings.LocalSource)
                Dim _Page As Page = _bot.Getpage(pa)
                If _Page.Exists Then
                    Try
                        AutoArchive(_Page)
                    Catch ex As Exception
                        Utils.EventLogger.Debug_Log("AutoArchive error, page " & _Page.Title, SStrings.LocalSource)
                        Utils.EventLogger.EX_Log(ex.Message, "ArchiveAllInclusions")
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
            Dim templist As List(Of Template) = Template.GetTemplates(Template.GetTemplateTextArray(PageToGet.Text))
            Dim signtemp As New Template
            For Each t As Template In templist
                If Regex.Match(t.Name, " *[Ff]irma automática").Success Then
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
            Dim includedpages As String() = _bot.GetallInclusions("Plantilla:Firma_automática")
            For Each pa As String In includedpages
                Try
                    Utils.EventLogger.Debug_Log("SignAllInclusions: Page " & pa, SStrings.LocalSource)
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
                        If _bot.AddMissingSignature(_Page, newthreads, minor) Then
                            Utils.EventLogger.Log("SignAllInclusions: Page """ & pa & """", SStrings.LocalSource)
                        End If
                    End If
                Catch ex As Exception
                    Utils.EventLogger.EX_Log("SignAllInclusions: Page """ & pa & """ EX: " & ex.Message, SStrings.LocalSource)
                End Try
            Next
            Return True
        End Function

    End Class
End Namespace

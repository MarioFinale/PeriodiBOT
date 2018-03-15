Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.WikiBot
Namespace WikiBot
    Class GrillitusArchive
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
            Debug_Log("ValidUser: Check user", "LOCAL", BOTName)
            'Verificar si el usuario existe
            If Not user.Exists Then
                Log("ValidUser: User " & user.UserName & " doesn't exist", "LOCAL", BOTName)
                Return False
            End If

            'Verificar si el usuario está bloqueado.
            If user.Blocked Then
                Log("ValidUser: User " & user.UserName & " is blocked", "LOCAL", BOTName)
                Return False
            End If

            'Verificar si el usuario editó hace al menos 4 días.
            If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
                Log("ValidUser: User " & user.UserName & " is inactive", "LOCAL", BOTName)
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
                Log("Archive: The page " & pageToCheck.Title & " doesn't belong to any valid namespace. (NS:" & pageToCheck.PageNamespace & ")", "LOCAL", BOTName)
                Return False
            End If
            Return True
        End Function



        Private Function PageConfig(ByVal Params As String(), ByRef destination As String, ByRef maxDays As Integer, ByRef strategy As String, ByRef useBox As Boolean, ByRef notify As Boolean) As Boolean

            If Not Params.Count >= 4 Then Return False
            Try
                'Destino
                If String.IsNullOrEmpty(Params(0)) Then
                    Log("Archive: Malformed config, aborting.", "LOCAL", BOTName)
                    Return False
                Else
                    destination = Params(0)
                End If
                'Dias a mantener
                If String.IsNullOrEmpty(Params(1)) Then
                    Log("Archive: Malformed config, aborting.", "LOCAL", BOTName)
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
            Catch ex As Exception
                Return False
            End Try

        End Function



        ''' <summary>
        ''' Realiza un archivado general siguiendo una lógica similar a la de Grillitus.
        ''' </summary>
        ''' <param name="PageToArchive">Página a archivar</param>
        ''' <returns></returns>
        Function Archive(ByVal PageToArchive As Page) As Boolean
            Log("Archive: Page " & PageToArchive.Title, "LOCAL", BOTName)
            Dim IndexPage As Page = _bot.Getpage(PageToArchive.Title & "/Archivo-00-índice")
            Dim ArchiveCfg As String() = GetArchiveTemplateData(PageToArchive)
            Dim Newpagetext As String = PageToArchive.Text

            'Verificar el espacio de nombres de la página se archiva
            If Not ValidNamespace(PageToArchive) Then
                Log("Archive: The page" & PageToArchive.Title & " is not in a valid namespace, aborting.", "LOCAL", BOTName)
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
                    Log("Archive: The user" & User.UserName & " doesn't meet the requirements.", "LOCAL", BOTName)
                    Return False
                End If
                'Validar que destino de archivado sea una subpágina del usuario.
                If Not ArchiveCfg(0).StartsWith(PageToArchive.Title) Then
                    Log("Archive: The page" & ArchiveCfg(0) & " isn't a subpage of the same user.", "LOCAL", BOTName)
                    Return False
                End If
            End If

            Dim ArchivePages As New List(Of String)

            Debug_Log("Archive: Declare tuples", "LOCAL", BOTName)
            Dim Archives As New List(Of Tuple(Of String, String))

            Debug_Log("Archive: Get threads of page " & PageToArchive.Title, "LOCAL", BOTName)
            Dim threads As String() = _bot.GetPageThreads(PageToArchive.Text)

            Dim notify As Boolean
            Dim strategy As String = String.Empty
            Dim useBox As Boolean
            Dim pageDest As String = String.Empty
            Dim maxDays As Integer = 0
            If Not PageConfig(ArchiveCfg, pageDest, maxDays, strategy, useBox, notify) Then Return False


            Dim ArchivedThreads As Integer = 0
            If threads.Count = 1 Then
                Log("Archive: The page " & PageToArchive.Title & " only have one thread, aborting.", "LOCAL", BOTName)
                Return False
            End If

            Debug_Log("Archive: Declare limit date", "LOCAL", BOTName)
            Dim LimitDate As DateTime = DateTime.Now.AddDays(-maxDays)
            Debug_Log("Archive: Read Threads", "LOCAL", BOTName)

            For Each t As String In threads
                Try
                    If ArchivedThreads = threads.Count - 1 Then
                        Exit For
                    End If
                    '-----------------------------------------------------------------------------------------------
                    'Firma mas reciente en la seccion
                    If strategy = "FirmaMásRecienteEnLaSección" Then
                        Dim threaddate As DateTime = _bot.MostRecentDate(t)

                        Dim ProgrammedMatch As Match = Regex.Match(t, "{{ *[Aa]rchivo programado *\| *fecha\=")
                        Dim DoNotArchiveMatch As Match = Regex.Match(t, "{{ *[Nn]o archivar *")

                        If Not DoNotArchiveMatch.Success Then

                            'Archivado programado
                            If ProgrammedMatch.Success Then
                                Dim fechastr As String = TextInBetween(t, ProgrammedMatch.Value, "}}")(0)
                                fechastr = " " & fechastr & " "
                                fechastr = fechastr.Replace(" 1-", "01-").Replace(" 2-", "02-").Replace(" 3-", "03-").Replace(" 4-", "04-") _
                                    .Replace(" 5-", "05-").Replace(" 6-", "06-").Replace(" 7-", "07-").Replace(" 8-", "08-").Replace(" 9-", "09-") _
                                    .Replace("-1-", "-01-").Replace("-2-", "-02-").Replace("-3-", "-03-").Replace("-4-", "-04-").Replace("-5-", "-05-") _
                                    .Replace("-6-", "-06-").Replace("-7-", "-07-").Replace("-8-", "-08-").Replace("-9-", "-09-").Trim()
                                Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'MM'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)

                                If DateTime.Now > fecha.AddDays(1) Then
                                    'Quitar el hilo de la pagina
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    Dim destination As String = SetPageDestination(threaddate, ArchiveCfg(0))
                                    Archives.Add(New Tuple(Of String, String)(destination, t))
                                    ArchivedThreads += 1
                                End If
                            Else

                                'Archivado normal
                                If threaddate < LimitDate Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    Dim destination As String = SetPageDestination(threaddate, ArchiveCfg(0))
                                    Archives.Add(New Tuple(Of String, String)(destination, t))
                                    ArchivedThreads += 1
                                End If
                            End If
                        End If

                        'Firma en el ultimo parrafo
                        '-----------------------------------------------------------------------------------------------------
                    ElseIf strategy = "FirmaEnÚltimoPárrafo" Then
                        Dim threaddate As Date = _bot.LastParagraphDateTime(t)
                        Dim ProgrammedMatch As Match = Regex.Match(t, "{{ *[Aa]rchivo programado *\| *fecha\=")
                        Dim DoNotArchiveMatch As Match = Regex.Match(t, "{{ *[Nn]o archivar *")

                        If Not DoNotArchiveMatch.Success Then

                            'Archivado programado
                            If ProgrammedMatch.Success Then
                                Dim fechastr As String = TextInBetween(t, ProgrammedMatch.Value, "}}")(0)
                                fechastr = " " & fechastr & " "
                                fechastr = fechastr.Replace(" 1-", "01-").Replace(" 2-", "02-").Replace(" 3-", "03-").Replace(" 4-", "04-") _
                                    .Replace(" 5-", "05-").Replace(" 6-", "06-").Replace(" 7-", "07-").Replace(" 8-", "08-").Replace(" 9-", "09-") _
                                    .Replace("-1-", "-01-").Replace("-2-", "-02-").Replace("-3-", "-03-").Replace("-4-", "-04-").Replace("-5-", "-05-") _
                                    .Replace("-6-", "-06-").Replace("-7-", "-07-").Replace("-8-", "-08-").Replace("-9-", "-09-").Trim()

                                Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'MM'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)

                                If DateTime.Now > fecha.AddDays(1) Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    Dim destination As String = SetPageDestination(threaddate, ArchiveCfg(0))
                                    Archives.Add(New Tuple(Of String, String)(destination, t))
                                    ArchivedThreads += 1

                                End If
                            Else
                                'Archivado normal
                                If threaddate < LimitDate Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    Dim destination As String = SetPageDestination(threaddate, ArchiveCfg(0))
                                    Archives.Add(New Tuple(Of String, String)(destination, t))
                                    ArchivedThreads += 1
                                End If
                            End If
                        End If
                    End If
                Catch ex As Exception
                    Log("Archive: Thread error on " & PageToArchive.Title, "LOCAL", BOTName)
                    EX_Log(ex.Message, "Archive", BOTName)
                End Try
            Next

            If ArchivedThreads > 0 Then
                Debug_Log("Archive: List pages", "LOCAL", BOTName)
                'Lista de Pagina de archivado e hilos a archivar
                Dim Sl As New SortedList(Of String, String)

                For Each t As Tuple(Of String, String) In Archives
                    Dim tdestiny As String = t.Item1
                    Dim Thread As String = t.Item2
                    If Not Sl.Keys.Contains(tdestiny) Then
                        Sl.Add(tdestiny, Thread)
                    Else
                        Sl.Item(tdestiny) = Sl.Item(tdestiny) & Thread
                    End If
                Next

                'Guardar los hilos en los archivos correspondientes por fecha
                For Each k As KeyValuePair(Of String, String) In Sl
                    Debug_Log("Archive: Save threads", "LOCAL", BOTName)
                    Dim isminor As Boolean = Not notify
                    Dim Archivepage As String = k.Key
                    Dim ThreadText As String = Environment.NewLine & k.Value
                    Dim threadcount As Integer = _bot.GetPageThreads(Environment.NewLine & ThreadText).Count
                    Dim ArchPage As Page = _bot.Getpage(Archivepage)
                    Dim ArchivePageText As String = ArchPage.Text
                    ArchivePages.Add(Archivepage)

                    'Verificar si la página de archivado está en el mismo espacio de nombres
                    If Not ArchPage.PageNamespace = PageToArchive.PageNamespace Then
                        Log("Archive: The page " & ArchPage.Title & " is not a in the same namespace of " & PageToArchive.Title & " aborting.", "LOCAL", BOTName)
                        Return False
                    End If

                    'Verificar si la página de archivado es una subpágina de la raiz
                    If Not ArchPage.Title.StartsWith(PageToArchive.RootPage) Then
                        Log("Archive: The page " & ArchPage.Title & " is not a subpage of " & PageToArchive.RootPage & " aborting.", "LOCAL", BOTName)
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
                    If ArchivedThreads > 1 Then
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
                    Debug_Log("Archive: Save main page", "LOCAL", BOTName)

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
                Log("Archive: Nothing to archive on " & PageToArchive.Title, "LOCAL", BOTName)
            End If


            Log("Archive: " & PageToArchive.Title & " done.", "LOCAL", BOTName)
            Return True
        End Function


        Private Function SetPageDestination(ByVal threaddate As Date, destination As String) As String
            Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
            Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
            Dim ThreadMonth2 As String = UppercaseFirstCharacter(threaddate.ToString("MMMM", New System.Globalization.CultureInfo("es-ES")))
            Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
            Dim Threadhyear As Integer

            If threaddate.Month < 6 Then
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
                    Debug_Log("UpdateBox: Updating Box", "LOCAL", BOTName)
                    Dim ArchiveBoxMatch As Match = Regex.Match(Indexpage.Text, "{{[Cc]aja (de)* *archivos[\s\S]+?}}")
                    Dim Newbox As String = String.Empty
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
                EX_Log("UpdateBox: " & ex.Message, "LOCAL", BOTName)
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
                    Destination = tup.Item2.Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))
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
            Dim templist As List(Of Template) = GetTemplates(GetTemplateTextArray(PageToGet.Text))
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
        Function ArchiveAllInclusions(ByVal IRC As Boolean) As Boolean
            If IRC Then
                BotIRC.Sendmessage(ColoredText("Archivando todas las páginas...", "04"))
            End If
            Dim includedpages As String() = _bot.GetallInclusions("Plantilla:Archivado automático")
            For Each pa As String In includedpages
                Log("ArchiveAllInclusions: Page " & pa, "LOCAL", BOTName)
                Dim _Page As Page = _bot.Getpage(pa)
                If _Page.Exists Then
                    Try
                        Archive(_Page)
                    Catch ex As Exception
                        Debug_Log("Archive error, page " & _Page.Title, "LOCAL", BOTName)
                        EX_Log(ex.Message, "ArchiveAllInclusions", BOTName)
                    End Try

                End If
            Next
            If IRC Then
                BotIRC.Sendmessage(ColoredText("Archivado completo...", "04"))
            End If
            Return True
        End Function
    End Class
End Namespace

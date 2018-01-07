Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions


Class GrillitusArchive
    Private Bot As WikiBot.Bot

    Sub New(ByVal WorkerBot As WikiBot.Bot)
        Bot = WorkerBot
    End Sub

    ''' <summary>
    ''' Realiza un archivado general siguiendo una lógica similar a la de Grillitus.
    ''' </summary>
    ''' <param name="PageToArchive">Página a archivar</param>
    ''' <returns></returns>
    Function Archive(ByVal PageToArchive As Page) As Boolean
        Log("Archive: Page " & PageToArchive.Title, "LOCAL", BOTName)


        'Verificar el espacio de nombres de la página se archiva
        If Not (PageToArchive.PageNamespace = 1 Or PageToArchive.PageNamespace = 3 _
            Or PageToArchive.PageNamespace = 4 Or PageToArchive.PageNamespace = 5 _
            Or PageToArchive.PageNamespace = 11 Or PageToArchive.PageNamespace = 13 _
            Or PageToArchive.PageNamespace = 15 Or PageToArchive.PageNamespace = 101 _
            Or PageToArchive.PageNamespace = 102 Or PageToArchive.PageNamespace = 103 _
            Or PageToArchive.PageNamespace = 105 Or PageToArchive.PageNamespace = 447 _
            Or PageToArchive.PageNamespace = 829) Then
            Log("Archive: The page " & PageToArchive.Title & " doesn't belong to any valid namespace. (NS:" & PageToArchive.PageNamespace & ")", "LOCAL", BOTName)
            Return False
        End If

        'Verificar si es una discusión de usuario.
        If PageToArchive.PageNamespace = 3 Then
            Dim Username As String = PageToArchive.Title.Split(CType(":", Char()))(1)
            'Verificar si el usuario está bloqueado.
            If UserIsBlocked(Username) Then
                Return False
            End If
            'Verificar si el usuario editó hace al menos 4 días.
            If Date.Now.Subtract(Bot.GetLastEditTimestampUser(Username)).Days >= 4 Then
                Return False
            End If

        End If



        Dim ArchiveCfg As String() = GetArchiveTemplateData(PageToArchive)

        Dim IndexPage As Page = Bot.Getpage(PageToArchive.Title & "/Archivo-00-índice")

        Dim PageTitle As String = PageToArchive.Title
        Dim pagetext As String = PageToArchive.Text

        Dim Newpagetext As String = pagetext

        Dim ArchivePages As New List(Of String)
        Dim Archives As New List(Of Tuple(Of String, String))

        Dim threads As String() = Bot.GetPageThreads(pagetext)

        Dim Notify As Boolean = False
        Dim Strategy As String = String.Empty
        Dim UseBox As Boolean = False
        Dim ArchivePageName As String = ArchiveCfg(0)

        Dim MaxDays As Integer = 0
        Dim ArchivedThreads As Integer = 0

        If threads.Count = 1 Then
            Return False
        End If

        If String.IsNullOrEmpty(ArchiveCfg(0)) Then
            Return False
        End If
        If String.IsNullOrEmpty(ArchiveCfg(1)) Then
            Return False
        Else
            MaxDays = Integer.Parse(ArchiveCfg(1))
        End If
        If String.IsNullOrEmpty(ArchiveCfg(2)) Then
            Notify = False
        Else
            If ArchiveCfg(2).ToLower.Contains("si") Or ArchiveCfg(2).ToLower.Contains("sí") Then
                Notify = True
            Else
                Notify = False
            End If
        End If

        If String.IsNullOrEmpty(ArchiveCfg(3)) Then
            Strategy = "FirmaEnÚltimoPárrafo"
        Else
            If ArchiveCfg(3) = "FirmaEnÚltimoPárrafo" Then
                Strategy = "FirmaEnÚltimoPárrafo"
            ElseIf ArchiveCfg(3) = "FirmaMásRecienteEnLaSección" Then
                Strategy = "FirmaMásRecienteEnLaSección"
            Else
                Strategy = "FirmaEnÚltimoPárrafo"
            End If
        End If

        If String.IsNullOrEmpty(ArchiveCfg(4)) Then
            UseBox = False
        Else
            If ArchiveCfg(4).ToLower.Contains("si") Or ArchiveCfg(4).ToLower.Contains("sí") Then
                UseBox = True
            Else
                UseBox = False
            End If
        End If



        Dim LimitDate As DateTime = DateTime.Now.AddDays(-MaxDays)

        For Each t As String In threads
            Try
                If ArchivedThreads = threads.Count - 1 Then
                    Exit For
                End If
                '-----------------------------------------------------------------------------------------------
                'Firma mas reciente en la seccion
                If Strategy = "FirmaMásRecienteEnLaSección" Then
                    Dim threaddate As DateTime = Bot.MostRecentDate(t)

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
                            Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'mm'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)

                            If DateTime.Now >= fecha Then
                                'Quitar el hilo de la pagina
                                Newpagetext = Newpagetext.Replace(t, "")


                                Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
                                Dim Threadhyear As Integer = CInt((threaddate.Month - 1) / 6 + 1)

                                Dim destiny As String = ArchiveCfg(0).Replace("AAAA", Threadyear).Replace("MM", ThreadMonth) _
                                                       .Replace("DD", ThreadDay).Replace("SEM", Threadhyear.ToString)


                                Archives.Add(New Tuple(Of String, String)(destiny, t))

                                ArchivedThreads += 1
                            End If
                        Else

                            'Archivado normal
                            If threaddate < LimitDate Then
                                Newpagetext = Newpagetext.Replace(t, "")

                                Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
                                Dim Threadhyear As Integer = CInt((threaddate.Month - 1) / 6 + 1)

                                Dim destiny As String = ArchiveCfg(0).Replace("AAAA", Threadyear).Replace("MM", ThreadMonth) _
                                                       .Replace("DD", ThreadDay).Replace("SEM", Threadhyear.ToString)


                                Archives.Add(New Tuple(Of String, String)(destiny, t))

                                ArchivedThreads += 1
                            End If
                        End If
                    End If

                    'Firma en el ultimo parrafo
                    '-----------------------------------------------------------------------------------------------------
                ElseIf Strategy = "FirmaEnÚltimoPárrafo" Then
                    Dim threaddate As DateTime = Bot.LastParagraphDateTime(t)
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

                            If DateTime.Now >= fecha Then
                                Newpagetext = Newpagetext.Replace(t, "")

                                Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
                                Dim Threadhyear As Integer = CInt((threaddate.Month - 1) / 6 + 1)

                                Dim destiny As String = ArchiveCfg(0).Replace("AAAA", Threadyear).Replace("MM", ThreadMonth) _
                                                       .Replace("DD", ThreadDay).Replace("SEM", Threadhyear.ToString)


                                Archives.Add(New Tuple(Of String, String)(destiny, t))

                                ArchivedThreads += 1

                            End If
                        Else
                            'Archivado normal
                            If threaddate < LimitDate Then
                                Newpagetext = Newpagetext.Replace(t, "")

                                Dim Threadyear As String = threaddate.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadMonth As String = threaddate.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
                                Dim ThreadDay As String = threaddate.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)
                                Dim Threadhyear As Integer = CInt((threaddate.Month - 1) / 6 + 1)

                                Dim destiny As String = ArchiveCfg(0).Replace("AAAA", Threadyear).Replace("MM", ThreadMonth) _
                                                       .Replace("DD", ThreadDay).Replace("SEM", Threadhyear.ToString)

                                Archives.Add(New Tuple(Of String, String)(destiny, t))

                                ArchivedThreads += 1
                            End If
                        End If


                    End If
                End If
            Catch ex As Exception
                Log("Archive: Thread error on " & PageToArchive.Title, "LOCAL", BOTName)
            End Try
        Next


        If ArchivedThreads > 0 Then


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

                Dim isminor As Boolean = Not Notify
                Dim Archivepage As String = k.Key
                Dim ThreadText As String = Environment.NewLine & k.Value
                Dim threadcount As Integer = Bot.GetPageThreads(Environment.NewLine & ThreadText).Count
                Dim ArchPage As Page = Bot.Getpage(Archivepage)
                Dim ArchivePageText As String = ArchPage.Text
                ArchivePages.Add(Archivepage)
                'Verificar si la página de archivado está en el mismo espacio de nombres
                If Not ArchPage.PageNamespace = PageToArchive.PageNamespace Then
                    Return False
                End If

                'Verificar si la página de archivado es una subpágina de la principal
                If Not ArchPage.Title.Contains(PageToArchive.Title) Then
                    Return False
                End If

                'Anadir los hilos al texto
                ArchivePageText = ArchivePageText & ThreadText

                'Si se usa la caja de archivos
                If UseBox Then

                    'Verificar si contiene la plantilla de indice
                    If Not ArchivePageText.Contains("{{" & IndexPage.Title & "}}") Then
                        ArchivePageText = "{{" & IndexPage.Title & "}}" & Environment.NewLine & ArchivePageText
                    End If
                End If

                'Texto de resumen de edicion
                Dim SummaryText As String = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad desde [[{2}]].", threadcount, MaxDays.ToString, PageTitle)
                'Guardar
                ArchPage.Save(ArchivePageText, SummaryText, isminor, True)
            Next




            'Actualizar caja si corresponde
            If UseBox Then
                Dim ArchiveBoxMatch As Match = Regex.Match(IndexPage.Text, "{{caja archivos\|[\s\S]+?}}")
                Dim Newbox As String = String.Empty
                Dim IndexPageText As String = IndexPage.Text
                If ArchiveBoxMatch.Success Then
                    Newbox = ArchiveBoxMatch.Value.Replace("{{caja archivos|", "").Replace("}}", "")

                    'Generar links en caja de archivos:
                    For Each p As String In ArchivePages
                        If Not Newbox.Contains(p) Then
                            Dim ArchiveBoxLink As String = "[[" & p & "]]"
                            Dim Archivename As Match = Regex.Match(p, "\/.+")

                            If Archivename.Success Then
                                ArchiveBoxLink = "[[" & p & "|" & Archivename.Value & "]]"
                            End If

                            Newbox = Newbox & "<center>" & ArchiveBoxLink & "</center>" & Environment.NewLine

                        End If
                    Next
                    Newbox = "{{caja archivos|" & Newbox & "}}"
                    IndexPageText = IndexPageText.Replace(ArchiveBoxMatch.Value, Newbox)
                    IndexPage.Save(IndexPageText, "Bot: Actualizando caja de archivos.", True, True)
                End If
            End If




            'Guardar pagina principal
            If Not String.IsNullOrEmpty(Newpagetext) Then

                'Si debe tener caja de archivos...
                If UseBox Then
                    If Not Newpagetext.Contains("{{" & IndexPage.Title & "}}") Then
                        Dim Archivetemplate As String = Regex.Match(pagetext, "{{ *[Aa]rchivado automático[\s\S]+?}}").Value
                        Newpagetext = Newpagetext.Replace(Archivetemplate, Archivetemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
                    End If
                End If

                Dim isminor As Boolean = Not Notify
                Dim Summary As String
                If ArchivedThreads > 1 Then
                    Summary = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad.", ArchivedThreads, MaxDays.ToString)
                Else
                    Summary = String.Format("Bot: Archivando {0} hilo con más de {1} días de antigüedad.", ArchivedThreads, MaxDays.ToString)
                End If
                PageToArchive.Save(Newpagetext, Summary, isminor, True)
            End If

        Else
            Log("Archive: Nothing to archive on " & PageToArchive.Title, "LOCAL", BOTName)
        End If


        Log("Archive: " & PageToArchive.Title & " done.", "LOCAL", BOTName)
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
        Dim includedpages As String() = Bot.GetallInclusions("Plantilla:Archivado automático")
        For Each pa As String In includedpages
            Log("ArchiveAllInclusions: Page " & pa, "LOCAL", BOTName)
            Dim _Page As Page = Bot.Getpage(pa)
            If _Page.Exists Then
                Archive(_Page)
            End If
        Next
        If IRC Then
            BotIRC.Sendmessage(ColoredText("Archivado completo...", "04"))
        End If
        Return True
    End Function




End Class

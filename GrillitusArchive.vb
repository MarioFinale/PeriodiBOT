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

        'Verificar si es una discusión de usuario
        If PageToArchive.PageNamespace = 3 Then
            Dim Username As String = PageToArchive.Title.Split(CType(":", Char()))(1)
            'Verificar si el usuario está bloqueado
            If UserIsBlocked(Username) Then
                Return False
            End If
        End If


        Dim hyear As Integer = CInt((DateTime.Now.Month - 1) / 6 + 1)
        Dim Currentyear As String = DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
        Dim CurrentMonth As String = DateTime.Now.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
        Dim CurrentMonthStr As String = DateTime.Now.ToString("MMMM", New Globalization.CultureInfo("es-ES"))
        Dim CurrentDay As String = DateTime.Now.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)

        Dim ArchiveCfg As String() = GetArchiveTemplateData(PageToArchive)

        Dim IndexPage As Page = Bot.Getpage(PageToArchive.Title & "/Archivo-00-índice")
        Dim IndexpageText As String = IndexPage.Text

        Dim PageTitle As String = PageToArchive.Title
        Dim pagetext As String = PageToArchive.Text

        Dim Newpagetext As String = pagetext
        Dim ArchivePageText As String = String.Empty

        Dim threads As String() = Bot.GetPageThreads(pagetext)

        Dim Notify As Boolean = False
        Dim Strategy As String = String.Empty
        Dim UseBox As Boolean = False
        Dim ArchivePageTitle As String = ArchiveCfg(0)

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

        'Construir el nopmbre de la página de archivado
        ArchivePageTitle = ArchivePageTitle.Replace("AAAA", Currentyear).Replace("MM", CurrentMonth) _
        .Replace("DD", CurrentDay).Replace("SEM", hyear.ToString)

        'Crear el elemento Page con el nombre de la página de archivado
        Dim ArchivePage As Page = Bot.Getpage(ArchivePageTitle)

        'Verificar si la página de archivado está en el mismo espacio de nombres
        If Not ArchivePage.PageNamespace = PageToArchive.PageNamespace Then
            Return False
        End If
        'Verificar si la página de archivado es una subpágina de la principal
        If Not ArchivePage.Title.Contains(PageToArchive.Title) Then
            Return False
        End If

        Dim ArchiveBoxLink As String = "[[" & ArchivePageTitle & "]]"
        Dim Archivename As Match = Regex.Match(ArchivePageTitle, "\/.+")

        If Archivename.Success Then
            ArchiveBoxLink = "[[" & ArchivePageTitle & "|" & Archivename.Value & "]]"
        End If

        Dim LimitDate As DateTime = DateTime.Now.AddDays(-MaxDays)

        For Each t As String In threads
            Try
                If ArchivedThreads = threads.Count - 1 Then
                    Exit For
                End If
                If Strategy = "FirmaMásRecienteEnLaSección" Then
                    Dim threaddate As DateTime = Bot.MostRecentDate(t)

                    Dim ProgrammedMatch As Match = Regex.Match(t, "{{ *[Aa]rchivo programado *\| *fecha\=")
                    Dim DoNotArchiveMatch As Match = Regex.Match(t, "{{ *[Nn]o archivar *\| *fecha\=")

                    If Not DoNotArchiveMatch.Success Then

                        If ProgrammedMatch.Success Then
                            Dim fechastr As String = TextInBetween(t, ProgrammedMatch.Value, "}}")(0)
                            Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'mm'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)

                            If DateTime.Now >= fecha Then
                                Newpagetext = Newpagetext.Replace(t, "")
                                ArchivePageText = ArchivePageText & t
                                ArchivedThreads += 1
                            End If
                        Else
                            If threaddate < LimitDate Then
                                Newpagetext = Newpagetext.Replace(t, "")
                                ArchivePageText = ArchivePageText & t
                                ArchivedThreads += 1
                            End If
                        End If
                    End If


                ElseIf Strategy = "FirmaEnÚltimoPárrafo" Then
                    Dim threaddate As DateTime = Bot.LastParagraphDateTime(t)
                    Dim ProgrammedMatch As Match = Regex.Match(t, "{{ *[Aa]rchivo programado *\| *fecha\=")
                    Dim DoNotArchiveMatch As Match = Regex.Match(t, "{{ *[Nn]o archivar *\| *fecha\=")

                    If Not DoNotArchiveMatch.Success Then

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
                                ArchivePageText = ArchivePageText & t
                                ArchivedThreads += 1
                            End If
                        Else
                            If threaddate < LimitDate Then
                                Newpagetext = Newpagetext.Replace(t, "")
                                ArchivePageText = ArchivePageText & t
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
            If UseBox Then
                If IndexPage.Exists Then

                    Dim ArchiveBoxMatch As Match = Regex.Match(IndexpageText, "{{caja archivos\|[\s\S]+?}}")
                    Dim Newbox As String = String.Empty

                    If ArchiveBoxMatch.Success Then
                        Newbox = ArchiveBoxMatch.Value.Replace("{{caja archivos|", "").Replace("}}", "")

                        If Not Newbox.Contains(ArchivePageTitle) Then

                            Newbox = Newbox & "<center>" & ArchiveBoxLink & "</center>" & Environment.NewLine
                            Newbox = "{{caja archivos|" & Newbox & "}}"
                            IndexpageText = IndexpageText.Replace(ArchiveBoxMatch.Value, Newbox)

                        End If
                    Else
                        IndexpageText = "{{caja archivos|" & Environment.NewLine & ArchiveBoxLink & Environment.NewLine & "}}"
                    End If

                Else
                    IndexpageText = "<center>" & ArchiveBoxLink & "</center>"
                    IndexpageText = "{{caja archivos|" & Environment.NewLine & IndexpageText & Environment.NewLine & "}}"
                End If

                If Not String.IsNullOrEmpty(ArchivePageText) Then
                    If Not Newpagetext.Contains("{{" & IndexPage.Title & "}}") Then
                        Dim Archivetemplate As String = Regex.Match(pagetext, "{{ *[Aa]rchivado automático[\s\S]+?}}").Value
                        Newpagetext = Newpagetext.Replace(Archivetemplate, Archivetemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
                    End If

                    If Not ArchivePageText.Contains("{{" & IndexPage.Title & "}}") Then
                        ArchivePageText = "{{" & IndexPage.Title & "}}" & Environment.NewLine & ArchivePageText
                    End If
                End If
            End If
        End If

        If Not String.IsNullOrEmpty(ArchivePageText) Then
            Dim NewPage As Page = Bot.Getpage(ArchivePageTitle)
            Dim Summary As String = String.Empty

            If ArchivedThreads > 1 Then
                Summary = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad en {2}.", ArchivedThreads, MaxDays.ToString, ArchiveBoxLink)
            Else
                Summary = String.Format("Bot: Archivando {0} hilo con más de {1} días de antigüedad en {2}.", ArchivedThreads, MaxDays.ToString, ArchiveBoxLink)
            End If

            Dim ArchiveSummary As String = String.Empty
            If ArchivedThreads > 1 Then
                ArchiveSummary = String.Format("Bot: Archivando {0} hilos con más de {1} días de antigüedad desde [[{2}]].", ArchivedThreads, MaxDays.ToString, PageTitle)
            Else
                ArchiveSummary = String.Format("Bot: Archivando {0} hilo con más de {1} días de antigüedad desde [[{2}]].", ArchivedThreads, MaxDays.ToString, PageTitle)
            End If

            Dim isminor As Boolean = Not Notify

            If UseBox Then
                IndexPage.Save(IndexpageText, "Bot: Actualizando caja de archivos.", True, True)
            End If
            NewPage.Save(NewPage.Text & Environment.NewLine & ArchivePageText, ArchiveSummary, isminor, True)
            PageToArchive.Save(Newpagetext, Summary, isminor, True)

        Else
            Log("Archive: Nothing to archive ", "LOCAL", BOTName)
        End If


        Log("Archive: " & PageToArchive.Title & " done.", "LOCAL", BOTName)
        Return True
    End Function


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

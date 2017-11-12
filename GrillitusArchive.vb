Imports System.Text.RegularExpressions

Class GrillitusArchive
    Private Bot As WikiBot.Bot

    Sub New(ByVal WorkerBot As WikiBot.Bot)
        Bot = WorkerBot
    End Sub

    ''' <summary>
    ''' Busca en el texto una plantilla de archivado usada por grillitus.
    ''' De encontrar la plantilla entrega un array de tipo string con: {Destino del archivado, Días a mantener, Avisar archivado, Estrategia de archivado, mantener caja}.
    ''' Los parámetros que estén vacíos en la plantilla se entregan vacíos también.
    ''' De no encontrar los parámetros regresa un array con todos los parámetros vacíos.
    ''' </summary>
    ''' <param name="Pagetext">Texto a evaluar</param>
    ''' <returns></returns>
    Function GetGrillitusTemplateData(PageText As String) As String()
        Dim template As String = Regex.Match(PageText, "{{ *[Uu]suario *: *[Gg]rillitus\/Archivar[\s\S]+?}}").Value

        Dim Destiny As String = Regex.Match(template, "(\| *Destino *)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
        Destiny = Regex.Replace(Destiny, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))

        Dim Days As String = Regex.Match(template, "(\| *Días a mantener *)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
        Days = Regex.Replace(Days, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))

        Dim Notice As String = Regex.Match(template, "(\| *Avisar al archivar *)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
        Notice = Regex.Replace(Notice, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))

        Dim Estrategy As String = Regex.Match(template, "(\| *Estrategia *)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
        Estrategy = Regex.Replace(Estrategy, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))

        Dim Box As String = Regex.Match(template, "(\| *MantenerCajaDeArchivos *)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
        Box = Regex.Replace(Box, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char())).Trim(CType(" ", Char()))

        Return {Destiny, Days, Notice, Estrategy, Box}

    End Function

    ''' <summary>
    ''' Actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
    ''' </summary>
    ''' <returns></returns>
    Function ArchiveAllInclusions(ByVal IRC As Boolean) As Boolean
        If IRC Then
            BotIRC.Sendmessage(ColoredText("Archivando todas las discusiones...", "04"))
        End If
        Dim includedpages As String() = Bot.GetallInclusions("Usuario:Grillitus/Archivar")
        For Each pa As String In includedpages
            Log("ArchiveAllInclusions: Page " & pa, "LOCAL", BOTName)
            Dim _Page As Page = Bot.Getpage(pa)
            If _Page.Exists Then
                GrillitusArchive(_Page)
            End If
        Next
        If IRC Then
            BotIRC.Sendmessage(ColoredText("Archivado completo...", "04"))
        End If
        Return True
    End Function


    ''' <summary>
    ''' Realiza un archivado siguiendo la lógica de Grillitus.
    ''' </summary>
    ''' <param name="PageToArchive">Página a archivar</param>
    ''' <returns></returns>
    Function GrillitusArchive(ByVal PageToArchive As Page) As Boolean
        Log("GrillitusArchive: Page " & PageToArchive.Title, "LOCAL", BOTName)
        Dim IndexPage As Page = Bot.Getpage(PageToArchive.Title & "/Archivo-00-índice")
        Dim IndexpageText As String = IndexPage.Text
        Dim PageTitle As String = PageToArchive.Title
        Dim pagetext As String = PageToArchive.Text
        Dim Newpagetext As String = pagetext
        Dim ArchivePageText As String = String.Empty
        Dim threads As String() = Bot.GetPageThreads(pagetext)
        Dim GrillitusCfg As String() = GetGrillitusTemplateData(pagetext)
        Dim Notify As Boolean = False
        Dim Strategy As String = String.Empty
        Dim UseBox As Boolean = False
        Dim ArchivePageTitle As String = GrillitusCfg(0)
        Dim MaxDays As Integer = 0
        Dim ArchivedThreads As Integer = 0
        Dim quarter As Integer = CInt((DateTime.Now.Month - 1) / 3 + 1)
        Dim Currentyear As String = DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
        Dim CurrentMonth As String = DateTime.Now.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
        Dim CurrentMonthStr As String = DateTime.Now.ToString("MMMM", New Globalization.CultureInfo("es-ES"))
        Dim CurrentDay As String = DateTime.Now.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)

        If String.IsNullOrEmpty(GrillitusCfg(0)) Then
            Return False
        End If
        If String.IsNullOrEmpty(GrillitusCfg(1)) Then
            Return False
        Else
            MaxDays = Integer.Parse(GrillitusCfg(1))
        End If
        If String.IsNullOrEmpty(GrillitusCfg(2)) Then
            Notify = False
        Else
            If GrillitusCfg(2).ToLower.Contains("si") Then
                Notify = True
            Else
                Notify = False
            End If
        End If

        Notify = Not (Notify)

        If String.IsNullOrEmpty(GrillitusCfg(3)) Then
            Strategy = "FirmaEnÚltimoPárrafo"
        Else
            If GrillitusCfg(3) = "FirmaEnÚltimoPárrafo" Then
                Strategy = "FirmaEnÚltimoPárrafo"
            ElseIf GrillitusCfg(3) = "FirmaMásRecienteEnLaSección" Then
                Strategy = "FirmaMásRecienteEnLaSección"
            Else
                Strategy = "FirmaEnÚltimoPárrafo"
            End If
        End If

        If String.IsNullOrEmpty(GrillitusCfg(4)) Then
            UseBox = False
        Else
            If GrillitusCfg(4).ToLower.Contains("si") Or GrillitusCfg(4).ToLower.Contains("sí") Then
                UseBox = True
            Else
                UseBox = False
            End If
        End If


        If Not ArchivePageTitle.Contains("SEM") Then
            ArchivePageTitle = ArchivePageTitle.Replace("AAAA", Currentyear)
            ArchivePageTitle = ArchivePageTitle.Replace("MM", CurrentMonth)
            ArchivePageTitle = ArchivePageTitle.Replace("DD", CurrentDay)
        Else

            ArchivePageTitle = ArchivePageTitle.Replace("AAAA", CurrentDay)
            ArchivePageTitle = ArchivePageTitle.Replace("SEM", quarter.ToString)
        End If

        Dim LimitDate As DateTime = DateTime.Now.AddDays(-MaxDays)

        For Each t As String In threads
            Try


                If Strategy = "FirmaMásRecienteEnLaSección" Then
                    Dim threaddate As DateTime = Bot.MostRecentDate(t)

                    Dim ProgrammedMatch As Match = Regex.Match(t, "{{ *[Uu]suario *: *Grillitus\/Archivo programado *\| *fecha\=")
                    Dim DoNotArchiveMatch As Match = Regex.Match(t, "{{ *[Uu]suario *: *Grillitus\/No archivar *\| *fecha\=")

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
                    Dim ProgrammedMatch As Match = Regex.Match(t, "{{ *[Uu]suario *: *Grillitus\/Archivo programado *\| *fecha\=")
                    Dim DoNotArchiveMatch As Match = Regex.Match(t, "{{ *[Uu]suario *: *Grillitus\/No archivar *\| *fecha\=")

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
                Log("GrillitusArchive: Error in one thread on " & PageToArchive.Title, "LOCAL", BOTName)
            End Try
        Next

        If ArchivedThreads > 0 Then

            If UseBox Then

                If IndexPage.Exists Then

                    Dim ArchiveBoxMatch As Match = Regex.Match(IndexpageText, "{{caja archivos\|[\s\S]+?}}")
                    Dim Newbox As String = String.Empty
                    If ArchiveBoxMatch.Success Then

                        Newbox = ArchiveBoxMatch.Value.Replace("{{caja archivos|", "").Replace("}}", "")

                        If Not Newbox.ToLower.Contains(ArchivePageTitle) Then
                            Dim newlink As String = String.Empty

                            If GrillitusCfg(0).Contains("SEM") Then
                                newlink = "[[" & ArchivePageTitle & "|Archivo " & quarter.ToString & "]]"
                            Else

                                If GrillitusCfg(0).Contains("DD") Then
                                    newlink = "[[" & ArchivePageTitle & "|Archivo del " & CurrentDay & "/" & CurrentMonth & "/" & Currentyear & "]]"
                                ElseIf GrillitusCfg(0).Contains("MM") Then
                                    newlink = "[[" & ArchivePageTitle & "|Archivo de " & CurrentMonthStr & " de " & Currentyear & "]]"
                                ElseIf GrillitusCfg(0).Contains("AAAA") Then
                                    newlink = "[[" & ArchivePageTitle & "|Archivo del " & Currentyear & "]]"
                                Else

                                End If

                            End If

                            If Not Newbox.Contains(newlink) Then
                                Newbox = Newbox & "<center>" & newlink & "</center>" & "<br />" & Environment.NewLine
                                Newbox = "{{caja archivos|" & Newbox & "}}"
                                IndexpageText = IndexpageText.Replace(ArchiveBoxMatch.Value, Newbox)
                            End If

                        End If

                    Else

                        Dim newlink As String = String.Empty
                        If GrillitusCfg(0).Contains("SEM") Then
                            newlink = "<center>[[" & ArchivePageTitle & "|Archivo " & quarter.ToString & "]]"

                        Else
                            If GrillitusCfg(0).Contains("DD") Then
                                newlink = "<center>[[" & ArchivePageTitle & "|Archivo del " & CurrentDay & "/" & CurrentMonth & "/" & Currentyear & "]]"
                            ElseIf GrillitusCfg(0).Contains("MM") Then
                                newlink = "<center>[[" & ArchivePageTitle & "|Archivo de " & CurrentMonthStr & " de " & Currentyear & "]]"
                            ElseIf GrillitusCfg(0).Contains("AAAA") Then
                                newlink = "<center>[[" & ArchivePageTitle & "|Archivo del " & Currentyear & "]]"
                            Else

                            End If

                        End If
                        IndexpageText = "{{caja archivos|" & Environment.NewLine & newlink & "<br />" & Environment.NewLine & "}}"
                    End If


                Else


                    If GrillitusCfg(0).Contains("SEM") Then
                        IndexpageText = "[[" & ArchivePageTitle & "|Archivo " & quarter.ToString & "]]<br>"
                    Else
                        If GrillitusCfg(0).Contains("DD") Then
                            IndexpageText = "[[" & ArchivePageTitle & "|Archivo del " & CurrentDay & "/" & CurrentMonth & "/" & Currentyear & "]]"
                        ElseIf GrillitusCfg(0).Contains("MM") Then
                            IndexpageText = "[[" & ArchivePageTitle & "|Archivo de " & CurrentMonthStr & " de " & Currentyear & "]]"
                        ElseIf GrillitusCfg(0).Contains("AAAA") Then
                            IndexpageText = "[[" & ArchivePageTitle & "|Archivo del " & Currentyear & "]]"
                        Else
                            Return False
                        End If

                    End If
                    IndexpageText = "<center>" & IndexpageText & "</center>" & "<br />"
                    IndexpageText = "{{caja archivos|" & Environment.NewLine & IndexpageText & Environment.NewLine & "}}"


                End If


                If Not String.IsNullOrEmpty(ArchivePageText) Then
                    If Not Newpagetext.Contains("{{" & IndexPage.Title & "}}") Then
                        Dim grillitustemplate As String = Regex.Match(pagetext, "{{Usuario:Grillitus\/Archivar[\s\S]+?}}").Value
                        Newpagetext = Newpagetext.Replace(grillitustemplate, grillitustemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
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
                Summary = String.Format("Archivando {0} hilos con más de {1} días de antiguedad en [[{2}]].", ArchivedThreads, MaxDays.ToString, ArchivePageTitle)
            Else
                Summary = String.Format("Archivando {0} hilo con más de {1} días de antiguedad en [[{2}]].", ArchivedThreads, MaxDays.ToString, ArchivePageTitle)
            End If

            Dim ArchiveSummary As String = String.Empty
            If ArchivedThreads > 1 Then
                ArchiveSummary = String.Format("Archivando {0} hilos con más de {1} días de antiguedad desde [[{2}]].", ArchivedThreads, MaxDays.ToString, PageTitle)
            Else
                ArchiveSummary = String.Format("Archivando {0} hilo con más de {1} días de antiguedad desde [[{2}]].", ArchivedThreads, MaxDays.ToString, PageTitle)
            End If
            IndexPage.Save(IndexpageText, "Actualizando caja de archivos", True)
            NewPage.Save(NewPage.Text & Environment.NewLine & ArchivePageText, ArchiveSummary, Notify)
            PageToArchive.Save(Newpagetext, Summary, Notify)

        Else
            Log("GrillitusArchive: Nothing to archive ", "LOCAL", BOTName)
        End If


        Log("GrillitusArchive: " & PageToArchive.Title & " done.", "LOCAL", BOTName)
        Return True
    End Function


End Class

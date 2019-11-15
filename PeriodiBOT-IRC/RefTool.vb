Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports MWBot.net
Imports MWBot.net.WikiBot
Imports Utils.Utils

Public Class RefTool

    Private _bot As Bot

    Sub New(ByRef workerbot As Bot)
        _bot = workerbot
    End Sub

    Private Function RedirectionAllowed(ByVal turi As Uri) As Boolean
        Dim exceptions As String() = {"fishbase.org", "blogspot.com.ar", "blogspot.com.pe", "blogspot.com.bo", "blogspot.com.co", "parliament.uk"}
        For Each exception As String In exceptions
            If turi.Authority.Contains(exception) Then
                Return True
            End If
        Next
        Return False
    End Function


    Function FixRefs(ByVal tpage As Page) As Boolean
        Dim newtext As String = tpage.Content
        Dim irrecoverable As Integer = 0
        Dim recovered As Integer = 0
        Dim duplicatesCount As Integer = 0
        Dim cref As Integer = 0
        Dim bref As Integer = 0
        Dim removedDuplicates As Tuple(Of String, Integer) = RemoveDuplicates(newtext)
        If Not removedDuplicates Is Nothing Then newtext = removedDuplicates.Item1
        If Not removedDuplicates Is Nothing Then duplicatesCount = removedDuplicates.Item2
        Dim refmatches As MatchCollection = Regex.Matches(newtext, "(<ref>|<ref name *=[^\/]+?>)([\s\S]+?)(<\/ref>)", RegexOptions.IgnoreCase)
        Dim tmatches As String() = FilterRefs(refmatches)
        If tmatches.Count <= 0 Then Return False
        Dim turist As New List(Of Tuple(Of String, Uri, String)) 'ref original, uri de la ref, nombre de la ref
        For Each ref As String In tmatches
            If Regex.Match(ref, "\[.+\]").Success Then
                Dim tref As String = Regex.Match(ref, "\[.+\]").Value
                tref = tref.Substring(1, tref.Length - 1).Trim()
                tref = ReplaceFirst(tref, "]", "")
                Dim refinfo As String() = {tref.Split(" "c)(0), tref.Replace(tref.Split(" "c)(0), "").Trim()}
                Dim tur As New Uri("http://x.z") : Uri.TryCreate(refinfo(0).Trim(), UriKind.Absolute, tur)
                Dim refdesc As String = Regex.Replace(ref, "(<ref( name *=.+?)*>|<\/ref>)", "", RegexOptions.IgnoreCase)
                refdesc = ReplaceFirst(refdesc, "[", "").Replace(tur.OriginalString, "").Trim()
                If refdesc(refdesc.Length - 1) = "]"c Then
                    refdesc = refdesc.Substring(0, refdesc.Length - 1)
                Else
                    refdesc = ReplaceFirst(refdesc, "]", "")
                End If
                refdesc = ReplaceFirst(refdesc, "]", "-").Trim()
                If tur Is Nothing Then Continue For
                turist.Add(New Tuple(Of String, Uri, String)(ref, tur, refdesc))
            Else
                Dim tref As New Uri("http://x.z") : Uri.TryCreate(Regex.Replace(ref, "(<ref( name *=.+?)*>|<\/ref>)", "", RegexOptions.IgnoreCase), UriKind.Absolute, tref)
                If tref Is Nothing Then Continue For
                turist.Add(New Tuple(Of String, Uri, String)(ref, tref, ""))
            End If
        Next
        If turist.Count <= 0 And duplicatesCount <= 0 Then Return False

        Dim pageslist As New List(Of String)
        Dim UrlAndDateList As New HashSet(Of Tuple(Of Uri, Date, String, String))
        Dim temppage = tpage
        Dim lasttimestamp As Date = temppage.EditDate

        While True
            If temppage.ParentRevId <= 0 Then
                For Each tref As Tuple(Of String, Uri, String) In turist
                    UrlAndDateList.Add(New Tuple(Of Uri, Date, String, String)(tref.Item2, lasttimestamp, tref.Item3, tref.Item1))
                Next
                Exit While
            End If
            temppage = _bot.Getpage(temppage.ParentRevId)
            Dim tempuris As Tuple(Of String, Uri, String)() = turist.ToArray
            Dim Allmissing As Boolean = True
            For Each tref As Tuple(Of String, Uri, String) In tempuris
                If temppage.Content.Contains(tref.Item2.OriginalString) Then
                    lasttimestamp = temppage.EditDate
                    Allmissing = False
                Else
                    UrlAndDateList.Add(New Tuple(Of Uri, Date, String, String)(tref.Item2, lasttimestamp, tref.Item3, tref.Item1))
                    turist.Remove(tref)
                End If
            Next
            If Allmissing Then Exit While
        End While

        Dim datlist As New List(Of WikiWebReference)


        For Each tup As Tuple(Of Uri, Date, String, String) In UrlAndDateList
            Dim tp As String
            Try
                tp = _bot.GET(tup.Item1)
                Try
                    Dim request As Net.HttpWebRequest = DirectCast(HttpWebRequest.Create(tup.Item1), HttpWebRequest)
                    request.Timeout = 30000
                    request.Method = "GET"
                    request.UserAgent = _bot.BotApiHandler.UserAgent
                    request.ContentType = "application/x-www-form-urlencoded"
                    Dim location As String = ""
                    Using response As Net.WebResponse = request.GetResponse
                        location = response.ResponseUri.OriginalString
                    End Using
                    If Not location = tup.Item1.OriginalString Then
                        If Not RedirectionAllowed(tup.Item1) Then
                            If tup.Item1.OriginalString.Substring(0, 5) = location.Substring(0, 5) Then
                                tp = ""
                            End If
                        End If
                    End If
                Catch ex As Exception
                    tp = ""
                End Try
            Catch ex As MaxRetriesExeption
                tp = ""
            End Try
            Dim pageDown As Boolean = String.IsNullOrWhiteSpace(tp)
            Dim pagename As String = Regex.Replace(Regex.Replace(Regex.Match(tp, "<title>[\s\S]+?<\/title>", RegexOptions.IgnoreCase).Value.Trim(), "(<title>|<\/title>)", "", RegexOptions.IgnoreCase), "[\n\r]", "").Trim()
            If Not String.IsNullOrWhiteSpace(tup.Item3) Then pagename = Regex.Replace(tup.Item3.Trim(), "[\n\r]", "")
            If String.IsNullOrWhiteSpace(pagename) Then
                If Regex.IsMatch(tup.Item1.OriginalString, "\/[^\/\s]+\.pdf", RegexOptions.IgnoreCase) Then
                    pagename = Regex.Match(tup.Item1.OriginalString, "\/[^\/\s]+\.pdf", RegexOptions.IgnoreCase).Value
                    pagename = UrlWebDecode(pagename.Replace("/", "").Replace(".pdf", ""))
                Else
                    pagename = tup.Item1.Authority
                End If
            End If
            Dim lang As String = ""
            If Regex.IsMatch(tp, "<html[^\>]+?lang=""\w{2}""", RegexOptions.IgnoreCase) Then
                lang = TextInBetween(Regex.Match(tp, "<html[^\>]+?lang=""\w{2}""", RegexOptions.IgnoreCase).Value, "lang=""", """").DefaultIfEmpty("").FirstOrDefault()
            End If

            Dim langpattern As String = "(\(en )([\wñ]{2,}(es|és|ano|an|án|ino|so|nio|ol))(\))"
            Dim tmatch As Match = Regex.Match(pagename, langpattern, RegexOptions.IgnoreCase)
            If tmatch.Success Then
                Dim gr As Group = tmatch.Groups(2)
                lang = gr.Value
                pagename = Regex.Replace(pagename, langpattern, "", RegexOptions.IgnoreCase)
            End If

            pagename = pagename.Replace("|", "-").Replace(" .", ".")
            pagename = Regex.Replace(pagename, "<.+?>", "")
            pagename = RemoveExcessOfSpaces(pagename)
            pagename = UppercaseFirstCharacter(pagename).Trim()
            Dim pageroot As String = tup.Item1.Authority
            Dim tdate As String = tup.Item2.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
            Dim Reference As New WikiWebReference With {
                .PageUrl = tup.Item1.OriginalString.Trim().Split(" "c)(0),
                .PageName = pagename,
                .SpokenDate = tdate,
                .PageRoot = pageroot,
                .OriginalRef = tup.Item4,
                .PageIsDown = pageDown,
                .Language = lang,
                .RefDate = tup.Item2
            }

            Dim i As Integer = 1


            datlist.Add(Reference)
        Next
        Dim tlist As New HashSet(Of Tuple(Of String, String))

        For Each nref As WikiWebReference In datlist
            If nref.PageIsDown = True Then 'Ok se que es redundante pero igual
                'Verificar si está disponible en Internet Archive
                Dim datestring As String = nref.RefDate.ToString("yyyyMMddHHmmss", New Globalization.CultureInfo("es-ES"))
                Dim WayBackResponse As String = _bot.GET(New Uri("http://archive.org/wayback/available?" & "timestamp=" & datestring & "&url=" & nref.PageUrl))
                Dim Unavaliable As Boolean = WayBackResponse.Contains("""archived_snapshots"": {}")
                Dim tstring As String = ""
                If Unavaliable Then
                    tstring = String.Format("{{{{Enlace roto |1={0} |2={1} |fechaacceso={2} |bot={3} }}}}", nref.PageName, nref.PageUrl, nref.SpokenDate, _bot.UserName)
                    tstring &= " <small>enlace irrecuperable</small>"
                    irrecoverable += 1
                Else
                    Dim timestamp As String = RemoveAllAlphas(TextInBetween(WayBackResponse, """timestamp"": """, """,").DefaultIfEmpty(datestring).LastOrDefault())
                    Dim archivedate As Date = New Date(Integer.Parse(timestamp.Substring(0, 4)), Integer.Parse(timestamp.Substring(4, 2)), Integer.Parse(timestamp.Substring(6, 2)))
                    Dim archiveSpokenDate As String = archivedate.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
                    Dim archiveuri As String = {Regex.Match(WayBackResponse, "(?:""url"": "")(http:\/\/web\.archive\.org\/web\/.+?)(?:"")").Groups(1).Value}.DefaultIfEmpty("https://web.archive.org/web/*/" & nref.PageUrl).FirstOrDefault
                    tstring = String.Format("{{{{Cita web |url={0} |título={1} |fechaacceso={2} |sitioweb={3}{4} |urlarchivo={5}|fechaarchivo={6}}}}}",
                                                      nref.PageUrl, nref.PageName, nref.SpokenDate, nref.PageRoot, If(Not String.IsNullOrWhiteSpace(nref.Language), " |idioma=" & nref.Language, ""),
                                                      archiveuri, archiveSpokenDate)
                    recovered += 1
                End If
                tlist.Add(New Tuple(Of String, String)(nref.OriginalRef, tstring))
                bref += 1
            Else
                _bot.POST(New Uri("https://pragma.archivelab.org"), "{""url"": """ & nref.PageUrl & """, ""annotation"": {""id"": ""lst-ib"", ""message"": """ _
                          & "Respaldo automático de referencia para el artículo '" & tpage.Title & "' de Wikipedia en español por PeriodiBOT (https://es.wikipedia.org/wiki/Usuario:PeriodiBOT).") 'Aprovechar de respaldarla en internet archive

                Dim tstring As String = String.Format("{{{{Cita web |url={0} |título={1} |fechaacceso={2} |sitioweb={3}{4}}}}}",
                                                      nref.PageUrl, nref.PageName, nref.SpokenDate, nref.PageRoot, If(Not String.IsNullOrWhiteSpace(nref.Language), " |idioma=" & nref.Language, ""))

                tlist.Add(New Tuple(Of String, String)(nref.OriginalRef, tstring))
                cref += 1
            End If
        Next

        For Each ref As Tuple(Of String, String) In tlist
            Dim reftags As MatchCollection = Regex.Matches(ref.Item1, "(<ref( name *=.+?)*>)|(<\/ref>)", RegexOptions.IgnoreCase)
            Dim newref As String = reftags(0).Value & ref.Item2 & reftags(1).Value
            newtext = newtext.Replace(ref.Item1, newref)
        Next

        Dim normalizedDates As Tuple(Of String, Integer) = NormalizeDates(newtext)
        newtext = normalizedDates.Item1
        cref += normalizedDates.Item2

        Dim summary As String = GenSummary(cref, bref, duplicatesCount, irrecoverable, recovered)
        If cref > 0 OrElse bref > 0 OrElse duplicatesCount > 0 Then
            Return (tpage.Save(newtext, summary, False, True) = EditResults.Edit_successful)
        End If

        Return False
    End Function

    Function NormalizeDates(ByVal pagetext As String) As Tuple(Of String, Integer)
        Dim matches As MatchCollection = Regex.Matches(pagetext, "(\|[\s]*?[fF]echa)(acceso)?( *=)([\s]*?)(\d{1,2} ?- ?\d{1,2} ?- ?(?:\d{2}){1,2})([\s]*?)(\|)")
        Dim modifiedDates As Integer = 0
        If matches.Count > 0 Then
            For Each match As Match In matches
                Dim mdate As String() = match.Groups(5).Value.Split("-"c).Select(Function(s As String)
                                                                                     s = s.Replace(" "c, "")
                                                                                     s = s.Trim()
                                                                                     Dim i As Integer = Integer.Parse(s)
                                                                                     s = i.ToString("00")
                                                                                     Return s
                                                                                 End Function).ToArray
                If Integer.Parse(mdate(1)) > 12 Then
                    Dim ns As String() = {mdate(1), mdate(0), mdate(2)}
                    mdate = ns
                End If
                If Integer.Parse(mdate(2)) < 1800 Then
                    Dim year As Integer = Integer.Parse(mdate(2))
                    Dim realyear As Integer = 0
                    If mdate(2).Length = 2 Then
                        If year < 22 Then
                            realyear = Integer.Parse("20" & mdate(2))
                        Else
                            realyear = Integer.Parse("19" & mdate(2))
                        End If
                    Else
                        Continue For
                    End If
                    Dim ns As String() = {mdate(1), mdate(0), realyear.ToString}
                    mdate = ns
                End If

                Dim newdate As String = New Date(Integer.Parse(mdate(2)), Integer.Parse(mdate(1)), Integer.Parse(mdate(0))).ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
                pagetext = pagetext.Replace(match.Value, match.Groups(1).Value & match.Groups(2).Value _
                                             & match.Groups(3).Value & match.Groups(4).Value & newdate _
                                             & match.Groups(6).Value & match.Groups(7).Value)
                modifiedDates += 1
            Next
            Return New Tuple(Of String, Integer)(pagetext, modifiedDates)
        Else
            Return New Tuple(Of String, Integer)(pagetext, 0)
        End If


    End Function


    Private Function RemoveDuplicates(ByVal pagetext As String) As Tuple(Of String, Integer)
        Dim refmatches As MatchCollection = Regex.Matches(pagetext, "(<ref>|<ref name *=[^\/]+?>)([\s\S]+?)(<\/ref>)", RegexOptions.IgnoreCase)
        Dim tmatches As String() = FilterRefs(refmatches)
        Dim refs As New HashSet(Of String)
        Dim duplicatesCount As Integer = 0
        If Not (tmatches.Distinct().Count = tmatches.Count) Then
            Dim duplicates As String() = tmatches.GroupBy(Function(p) p).Where(Function(g) g.Count > 1).Select(Function(g) g.Key).ToArray
            duplicatesCount = duplicates.Count
            If duplicatesCount = 0 Then Return Nothing
            For Each ref As String In duplicates
                Dim refname As String = "<ref name=AutoGen-1>"
                Dim refnameindex As Integer = 1
                If Regex.Match(ref, "<ref name *=.+?>", RegexOptions.IgnoreCase).Success Then
                    refname = Regex.Match(ref, "<ref name *=.+?>", RegexOptions.IgnoreCase).Value
                Else
                    While True
                        If Not Regex.Match(ref, "<ref name *=AutoGen-" & refnameindex & ">").Success Then Exit While
                        refnameindex += 1
                    End While
                    refname = "<ref name=AutoGen-" & refnameindex & ">"
                End If
                Dim actualRefName As String = Regex.Match(ref, "(<ref>|<ref name *=[^\/]+?>)").Value
                Dim newMainRef As String = ReplaceFirst(ref, actualRefName, refname)
                Dim newSecondaryRef As String = ReplaceLast(refname, ">", "/>")
                pagetext = ReplaceFirst(pagetext, ref, newMainRef)
                If refname.Contains("ref name=AutoGen") Then
                    pagetext = pagetext.Replace(ref, newSecondaryRef)
                Else
                    pagetext = ReplaceEveryOneButFirst(pagetext, ref, newSecondaryRef)
                End If

            Next
        End If
        Return New Tuple(Of String, Integer)(pagetext, duplicatesCount)
    End Function


    Private Function GenSummary(ByVal fixedCount As Integer, ByVal brokenCount As Integer, ByVal duplicatesCount As Integer, ByVal irrecoverable As Integer, ByVal recovered As Integer) As String
        Dim ChangedSumm As String = String.Format("Completando {0} referencia{1}", If(fixedCount = 1, "una", fixedCount.ToString), If(fixedCount > 1, "s", ""))
        Dim BrokenSumm As String = String.Format("arcando {0} referencia{1} como rota{1}", If(brokenCount = 1, "una", brokenCount.ToString), If(brokenCount > 1, "s", ""))
        Dim DuplicatesSumm As String = String.Format("niendo {0} referencia{1} repetida{1}", If(duplicatesCount = 1, "una", duplicatesCount.ToString), If(duplicatesCount > 1, "s", ""))
        Dim RecoveredSumm As String = String.Format("{0} recuperada{1}", If(recovered = 1, "una", recovered.ToString), If(recovered > 1, "s", ""))
        Dim IrrecoverableSumm As String = String.Format("{0} irrecuperable{1}", If(irrecoverable = 1, "una", irrecoverable.ToString), If(irrecoverable > 1, "s", ""))
        Dim TestPfx As String = String.Format(". (TEST) #PeriodiBOT {0}", Initializer.BotVersion)
        Dim summary As String = "Bot: "

        Select Case True
            Case fixedCount > 0 And brokenCount = 0 And duplicatesCount = 0
                summary &= ChangedSumm
                '========================
            Case fixedCount = 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = 0 And recovered = 1
                summary &= "M" & BrokenSumm & " (recuperada)"
            Case fixedCount = 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = 0 And recovered = brokenCount
                summary &= "M" & BrokenSumm & " (recuperadas)"
            Case fixedCount = 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = 1 And recovered = 0
                summary &= "M" & BrokenSumm & " (irrecuperable)"
            Case fixedCount = 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = brokenCount And recovered = 0
                summary &= "M" & BrokenSumm & " (irrecuperables)"
            Case fixedCount = 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable > 0 And recovered > 0
                summary &= "M" & BrokenSumm & " (" & RecoveredSumm & " y " & IrrecoverableSumm & ")"
                '========================
            Case fixedCount > 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = 0 And recovered = 1
                summary &= ChangedSumm & " y m" & BrokenSumm & " (recuperada)"
            Case fixedCount > 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = 0 And recovered = brokenCount
                summary &= ChangedSumm & " y m" & BrokenSumm & " (recuperadas)"
            Case fixedCount > 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = 1 And recovered = 0
                summary &= ChangedSumm & " y m" & BrokenSumm & " (irrecuperable)"
            Case fixedCount > 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable = brokenCount And recovered = 0
                summary &= ChangedSumm & " y m" & BrokenSumm & " (irrecuperables)"
            Case fixedCount > 0 And duplicatesCount = 0 And brokenCount > 0 And irrecoverable > 0 And recovered > 0
                summary &= ChangedSumm & " y m" & BrokenSumm & " (" & RecoveredSumm & " y " & IrrecoverableSumm & ")"
                '========================
            Case fixedCount > 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = 0 And recovered = 1
                summary &= ChangedSumm & ", " & "u" & DuplicatesSumm & " y m" & BrokenSumm & " (recuperada)"
            Case fixedCount > 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = 0 And recovered = brokenCount
                summary &= ChangedSumm & ", " & "u" & DuplicatesSumm & " y m" & BrokenSumm & " (recuperadas)"
            Case fixedCount > 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = 1 And recovered = 0
                summary &= ChangedSumm & ", " & "u" & DuplicatesSumm & " y m" & BrokenSumm & " (irrecuperable)"
            Case fixedCount > 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = brokenCount And recovered = 0
                summary &= ChangedSumm & ", " & "u" & DuplicatesSumm & " y m" & BrokenSumm & " (irrecuperables)"
            Case fixedCount > 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable > 0 And recovered > 0
                summary &= ChangedSumm & ", " & "u" & DuplicatesSumm & " y m" & BrokenSumm & " (" & RecoveredSumm & " y " & IrrecoverableSumm & ")"
                '========================
            Case fixedCount = 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = 0 And recovered = 1
                summary &= "U" & DuplicatesSumm & " y m" & BrokenSumm & " (recuperada)"
            Case fixedCount = 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = 0 And recovered = brokenCount
                summary &= "U" & DuplicatesSumm & " y m" & BrokenSumm & " (recuperadas)"
            Case fixedCount = 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = 1 And recovered = 0
                summary &= "U" & DuplicatesSumm & " y m" & BrokenSumm & " (irrecuperable)"
            Case fixedCount = 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable = brokenCount And recovered = 0
                summary &= "U" & DuplicatesSumm & " y m" & BrokenSumm & " (irrecuperables)"
            Case fixedCount = 0 And duplicatesCount > 0 And brokenCount > 0 And irrecoverable > 0 And recovered > 0
                summary &= "U" & DuplicatesSumm & " y m" & BrokenSumm & " (" & RecoveredSumm & " y " & IrrecoverableSumm & ")"
                '========================
            Case Else
                summary &= "Ajustando " & fixedCount + brokenCount + duplicatesCount & " referencias"
        End Select

        summary &= TestPfx
        Return summary
    End Function



    Private Function FilterRefs(ByVal matc As MatchCollection) As String()
        Return matc.OfType(Of Match)().Select(Function(x) x.Value).Where(Function(x)
                                                                             Return (Not x.Contains("{{") _
                                                                             AndAlso Not x.Contains("]]") _
                                                                             AndAlso Not x.ToLower.Contains("<cite") _
                                                                             AndAlso Not x.ToLower.Contains("consultado el") _
                                                                             AndAlso Not x.ToLower.Contains("consultada el") _
                                                                             AndAlso Not Regex.IsMatch(x, "acces(o|sed) \w{1,2}", RegexOptions.IgnoreCase) _
                                                                             AndAlso Not Regex.IsMatch(x, "retrieved \w{1,2}", RegexOptions.IgnoreCase) _
                                                                             AndAlso Not Regex.IsMatch(x, "(\[.+?\]).+?(\[.+?\])") _
                                                                             AndAlso Not CountCharacter(x, "]"c) > 1)
                                                                         End Function).ToArray()
    End Function



End Class

Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports MWBot.net
Imports MWBot.net.WikiBot
Imports MWBot.net.Utility.Utils
Imports System.Net.Http
Imports System.Net.Http.Headers

Public Class RefTool

    Private Property WorkerBot As Bot

    Sub New(ByRef workerbot As Bot)
        Me.WorkerBot = workerbot
    End Sub

    Private Function RedirectionAllowed(ByVal turi As Uri) As Boolean
        Dim exceptions As String() = {"fishbase.org", "fishbase.de", "fishbase.in", "blogspot.com.ar", "blogspot.com.pe", "blogspot.com.bo", "blogspot.com.co", "parliament.uk"}
        For Each exception As String In exceptions
            If turi.Authority.Contains(exception) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function GetRefs(ByVal pagetext As String) As WikiRef()
        Dim matches As MatchCollection = Regex.Matches(pagetext, "((?:< *ref *)(?:[^/]*?>))([\s\S]+?)(< *\/ref>)", RegexOptions.IgnoreCase)
        Dim refList As New List(Of WikiRef)

        For Each m As Match In matches
            Dim openingTag As String = m.Groups(1).Value
            Dim content As String = m.Groups(2).Value
            Dim closingTag As String = m.Groups(3).Value
            Dim refName As String = String.Empty

            Dim nameMatch As Match = Regex.Match(openingTag, "(name *= *"")([\s\S]+?)("")", RegexOptions.IgnoreCase)
            If nameMatch.Success Then
                refName = nameMatch.Groups(2).Value
            End If

            Dim ref As New WikiRef With {
            .OpeningTag = openingTag,
            .Content = content,
            .ClosingTag = closingTag,
            .Name = refName,
            .OriginalString = m.Value
            }
            refList.Add(ref)
        Next

        Return refList.ToArray()
    End Function

    Private Function IsWebRef(ByVal ref As WikiRef) As Boolean
        Dim isWeb As Boolean = Regex.Match(ref.Content, "(\|[\s]*url[\s]*)=(.+?)(\|)|(\[.+\])", RegexOptions.IgnoreCase).Success
        If isWeb Then Return isWeb
        Return Uri.IsWellFormedUriString(ref.Content.Split(" "c)(0).Trim, UriKind.RelativeOrAbsolute)
    End Function

    Private Function GetWebRefs(ByVal refs As WikiRef()) As WikiWebReference()
        Dim refList As New List(Of WikiWebReference)
        For Each ref As WikiRef In refs
            If Not IsWebRef(ref) Then Continue For
            Dim refName As String = ref.Name
            Dim refString As String = ref.OriginalString
            Dim refOpeningTag As String = ref.OpeningTag
            Dim refClosingTag As String = ref.ClosingTag
            Dim refPageUrl As String = Regex.Match(ref.Content, "((https*:\/\/)|(([A-z]+?\.){1,3}([A-z]+?)\/))([^\s\|\}\{\[\]]+)", RegexOptions.IgnoreCase).Value
            Dim refPageRoot As String = New Uri(refPageUrl).Authority
            Dim refPageName As String = String.Empty

            Dim externalLink As Match = Regex.Match(ref.Content, "([^\[]\[(?!\[))(.+?)(\])")
            Dim refIsExternalLink As Boolean = externalLink.Success
            Dim refIsAlreadyArchived As Boolean = Regex.Match(ref.Content, "(web\.archive\.org|\{\{ *Wayback)", RegexOptions.IgnoreCase).Success
            Dim refIsCiteWeb As Boolean = Regex.Match(ref.Content, "(\{\{ *cita web)", RegexOptions.IgnoreCase).Success

            Dim templatesInref As List(Of Template) = Template.GetTemplates(ref.Content)
            Dim noTemplates As Boolean = True

            For Each t As Template In templatesInref
                Dim urlFound As Boolean = False
                Dim nameFound As Boolean = False
                If t.Name.ToLower.Contains("cita ") Then
                    noTemplates = False
                    For Each parameter As Tuple(Of String, String) In t.Parameters
                        If parameter.Item1.ToLower = "url" Then
                            refPageUrl = parameter.Item2
                            urlFound = True
                        End If

                        If parameter.Item1.ToLower = "título" Then
                            refPageName = parameter.Item2
                            nameFound = True
                        End If

                        If urlFound And nameFound Then Exit For
                    Next
                End If
            Next

            If refIsExternalLink And noTemplates Then
                Dim extLinkText As String = externalLink.Groups(2).Value
                refPageName = ref.Content.Replace(externalLink.Value, extLinkText.Replace(extLinkText.Split(" "c)(0), "").Trim())
            End If

            Dim tWebref As New WikiWebReference With {
                .AlreadyArchived = refIsAlreadyArchived,
                .CiteWeb = refIsCiteWeb,
                .ClosingTag = refClosingTag,
                .Complete = False,
                .Content = ref.Content,
                .Language = Nothing,
                .Name = refName,
                .OpeningTag = refOpeningTag,
                .OriginalString = ref.OriginalString,
                .PageIsDown = Nothing,
                .PageName = refPageName,
                .PageRoot = refPageRoot,
                .PageUrl = refPageUrl,
                .RefDate = Nothing,
                .SpokenDate = Nothing,
                .Valid = False}

            refList.Add(tWebref)

        Next

        Return refList.ToArray()
    End Function

    ''' <summary>
    ''' Verifica e intenta cargar un recurso web. Si se trata de un archivo FTP, solo retorna la cadena de texto "FTP FILE".
    ''' </summary>
    ''' <param name="weburi"></param>
    ''' <returns></returns>
    Function CheckAndTryLoadWebResource(ByVal weburi As Uri) As String
        Dim tp As String
        If weburi.Scheme.ToLower = "ftp" Or weburi.Scheme.ToLower = "sftp" Then
#Disable Warning SYSLIB0014 ' We need FTP support
            Dim request As WebRequest = WebRequest.Create(weburi)
#Enable Warning SYSLIB0014 '
            request.Method = WebRequestMethods.Ftp.GetFileSize
            Try
                request.GetResponse()
                tp = "FTP FILE"
            Catch e As WebException
                Dim response As FtpWebResponse = CType(e.Response, FtpWebResponse)

                If response.StatusCode = FtpStatusCode.ActionNotTakenFileUnavailable Then
                    tp = ""
                Else
                    tp = ""
                End If
            End Try
        Else
            Try
                tp = WorkerBot.GET(weburi)
                Try
                    Dim request As HttpWebRequest = DirectCast(WebRequest.Create(weburi), HttpWebRequest)
                    request.Timeout = 60000
                    request.Method = "GET"
                    request.UserAgent = WorkerBot.BotApiHandler.UserAgent
                    request.ContentType = "application/x-www-form-urlencoded"
                    Dim location As String = ""
                    Using response As Net.WebResponse = request.GetResponse
                        location = response.ResponseUri.OriginalString
                    End Using
                    If Not location = weburi.OriginalString Then
                        If Not RedirectionAllowed(weburi) Then
                            If weburi.OriginalString.Substring(0, 5) = location.Substring(0, 5) Then
                                tp = ""
                            End If
                        End If
                    End If

                    'Carguemos la página 404 del sitio y verifiquemos si la cabecera <title> es igual. Si lo es, marcar como rota. 
                    'Algunas páginas ocultan los 404 respondiendo siempre con 302 y entregando contenido genérico como "últimas noticias", etc...
                    Try

                        Dim turi2 As New Uri(weburi.GetLeftPart(UriPartial.Authority) & "/404")

                        Dim request2 As HttpWebRequest = DirectCast(WebRequest.Create(turi2), HttpWebRequest)
                        request2.Timeout = 30000
                        request2.Method = "GET"
                        request2.UserAgent = WorkerBot.BotApiHandler.UserAgent
                        request2.ContentType = "application/x-www-form-urlencoded"
                        Dim isError As Boolean = False
                        Try
                            Dim response2 As Net.WebResponse = request2.GetResponse
                            response2.Close()
                        Catch exres As WebException
                            isError = True
                        End Try
                        Dim tpage2 As String = WorkerBot.GET(turi2)
                        Dim originalTitleMatch As Match = Regex.Match(tp, "\<title\b[^>]*\>\s*([\s\S]*?)\<\/title\>", RegexOptions.IgnoreCase)
                        Dim originalPageTitle As String = ""
                        If originalTitleMatch.Success Then originalPageTitle = originalTitleMatch.Groups(1).Value

                        Dim titleMatch As Match = Regex.Match(tpage2, "\<title\b[^>]*\>\s*([\s\S]*?)\<\/title\>", RegexOptions.IgnoreCase)
                        Dim PageTitle As String = ""
                        If titleMatch.Success Then PageTitle = titleMatch.Groups(1).Value

                        If PageTitle.ToLower.Equals(originalPageTitle.ToLower) And (Not isError) Then
                            tp = "" 'El <title> es igual al 404! Marcar como caida.
                        End If

                    Catch ex As Exception
                    End Try

                Catch ex As Exception
                    tp = ""
                End Try

            Catch ex As MaxRetriesExeption
                tp = ""
            End Try
        End If
        Return tp
    End Function

    Private Function GetRefsNameAndUris(ByVal refs As String()) As List(Of Tuple(Of String, Uri, String))
        Dim refsList As New List(Of Tuple(Of String, Uri, String))
        For Each ref As String In refs
            If Regex.Match(ref, "\[.+\]").Success Then
                Dim tref As String = Regex.Match(ref, "\[.+\]").Value
                tref = tref.Substring(1, tref.Length - 1).Trim()
                tref = ReplaceFirst(tref, "]", "")
                Dim refinfo As String() = {tref.Split(" "c)(0), tref.Replace(tref.Split(" "c)(0), "").Trim()}
                Dim tur As New Uri("http://x.z") : Uri.TryCreate(refinfo(0).Trim(), UriKind.Absolute, tur)
                If tur Is Nothing Then Continue For
                Dim refdesc As String = Regex.Replace(ref, "(<ref( name *=.+?)*>|<\/ref>)", "", RegexOptions.IgnoreCase)
                refdesc = ReplaceFirst(refdesc, "[", "").Replace(tur.OriginalString, "").Trim()
                If refdesc(refdesc.Length - 1) = "]"c Then
                    refdesc = refdesc.Substring(0, refdesc.Length - 1)
                Else
                    refdesc = ReplaceFirst(refdesc, "]", "")
                End If
                refdesc = ReplaceFirst(refdesc, "]", "-").Trim()
                refsList.Add(New Tuple(Of String, Uri, String)(ref, tur, refdesc))
            Else
                Dim tref As New Uri("http://x.z")
                Uri.TryCreate(Regex.Replace(ref, "(<ref( name *=.+?)*>|<\/ref>)", "", RegexOptions.IgnoreCase), UriKind.Absolute, tref)
                If tref IsNot Nothing AndAlso (tref.Scheme Is Uri.UriSchemeHttp OrElse tref.Scheme Is Uri.UriSchemeHttps OrElse tref.Scheme = Uri.UriSchemeFtp) Then
                    refsList.Add(New Tuple(Of String, Uri, String)(ref, tref, ""))
                End If
            End If
        Next
        Return refsList
    End Function

    Private Function GetDateOfRefs(ByVal sourcePage As Page, ByVal refSet As HashSet(Of Tuple(Of String, Uri, String))) As HashSet(Of Tuple(Of Uri, Date, String, String))
        Dim UrlAndDateList As New HashSet(Of Tuple(Of Uri, Date, String, String))
        Dim temppage As Page = sourcePage
        Dim lasttimestamp As Date = temppage.LastEdit
        While True
            If temppage.ParentRevId <= 0 Then
                For Each ref As Tuple(Of String, Uri, String) In refSet
                    UrlAndDateList.Add(New Tuple(Of Uri, Date, String, String)(ref.Item2, lasttimestamp, ref.Item3, ref.Item1))
                Next
                Exit While
            End If

            temppage = WorkerBot.Getpage(temppage.ParentRevId)
            Dim allmissing As Boolean = True
            Dim refsToCheck As Tuple(Of String, Uri, String)() = refSet.ToArray

            For Each ref As Tuple(Of String, Uri, String) In refsToCheck
                If temppage.Content.Contains(ref.Item2.OriginalString) Then
                    lasttimestamp = temppage.LastEdit
                    allmissing = False
                Else
                    UrlAndDateList.Add(New Tuple(Of Uri, Date, String, String)(ref.Item2, lasttimestamp, ref.Item3, ref.Item1))
                    refSet.Remove(ref)
                End If
            Next

            If allmissing Then Exit While
        End While
        Return UrlAndDateList
    End Function

    Function FixRefs(ByVal tpage As Page) As Boolean
        Dim newtext As String = tpage.Content
        Dim irrecoverable As Integer = 0
        Dim recovered As Integer = 0
        Dim cref As Integer = 0
        Dim bref As Integer = 0
        Dim emptyRefsSimplified As Tuple(Of String, Integer) = SimplifyEmptyRefs(newtext)
        newtext = emptyRefsSimplified.Item1

        Dim duplicatesCount As Integer = emptyRefsSimplified.Item2
        Dim removedDuplicates As Tuple(Of String, Integer) = RemoveDuplicates(newtext)
        If Not removedDuplicates Is Nothing Then newtext = removedDuplicates.Item1
        If Not removedDuplicates Is Nothing Then duplicatesCount = removedDuplicates.Item2
        Dim refmatches As MatchCollection = Regex.Matches(newtext, "(<ref>|<ref name *=[^\/]+?>)([\s\S]+?)(<\/ref>)", RegexOptions.IgnoreCase)
        Dim tmatches As String() = FilterRefs(refmatches)
        If tmatches.Count <= 0 Then
            EventLogger.Log("Nothing to fix in " + tpage.Title, "FixRefs")
            Return False
        End If
        Dim turist As List(Of Tuple(Of String, Uri, String)) = GetRefsNameAndUris(tmatches) 'ref original, uri de la ref, nombre de la ref

        If turist.Count <= 0 And duplicatesCount <= 0 Then
            EventLogger.Log("Nothing to fix in " + tpage.Title, "FixRefs")
            Return False
        End If

        Dim pageslist As New List(Of String)
        Dim UrlAndDateList As New HashSet(Of Tuple(Of Uri, Date, String, String))
        Dim temppage = tpage
        Dim lasttimestamp As Date = temppage.LastEdit

        While True
            If temppage.ParentRevId <= 0 Then
                For Each tref As Tuple(Of String, Uri, String) In turist
                    UrlAndDateList.Add(New Tuple(Of Uri, Date, String, String)(tref.Item2, lasttimestamp, tref.Item3, tref.Item1))
                Next
                Exit While
            End If
            temppage = WorkerBot.Getpage(temppage.ParentRevId)
            Dim tempuris As Tuple(Of String, Uri, String)() = turist.ToArray
            Dim Allmissing As Boolean = True
            For Each tref As Tuple(Of String, Uri, String) In tempuris
                If temppage.Content.Contains(tref.Item2.OriginalString) Then
                    lasttimestamp = temppage.LastEdit
                    Allmissing = False
                Else
                    UrlAndDateList.Add(New Tuple(Of Uri, Date, String, String)(tref.Item2, lasttimestamp, tref.Item3, tref.Item1))
                    turist.Remove(tref)
                End If
            Next
            If Allmissing Then Exit While
        End While

        Dim datlist As New List(Of WikiWebReference)


        For Each tup As Tuple(Of Uri, Date, String, String) In UrlAndDateList 'uri, fecha, nombre, ref original
            Dim referenceWebsiteContent As String = CheckAndTryLoadWebResource(tup.Item1)

            Dim pageDown As Boolean = String.IsNullOrWhiteSpace(referenceWebsiteContent)
            Dim pagename As String = TryGetPagenameFromContent(tup.Item1, referenceWebsiteContent)
            Dim originalRefHasName As Boolean = (Not String.IsNullOrWhiteSpace(tup.Item3))

            If originalRefHasName Then 'Descartar nombre auto-generado y usar el que ya está en la referencia
                pagename = Regex.Replace(tup.Item3.Trim(), "[\n\r]", "")
                pagename = pagename.Replace("|", "-").Replace(" .", ".")
                pagename = Regex.Replace(pagename, "<.+?>", "")
                pagename = RemoveExcessOfSpaces(pagename)
                pagename = UppercaseFirstCharacter(pagename).Trim()
            End If

            Dim tryGetLangResult As Tuple(Of String, String) = TryGetLanguageFromContent(tup.Item1, referenceWebsiteContent, pagename)
            Dim lang As String = tryGetLangResult.Item1
            If originalRefHasName Then
                pagename = tryGetLangResult.Item2
            End If

            Dim pageroot As String = tup.Item1.Authority
            Dim tdate As String = tup.Item2.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
            Dim Reference As New WikiWebReference With {
                .PageUrl = tup.Item1.OriginalString.Trim().Split(" "c)(0),
                .PageName = pagename,
                .SpokenDate = tdate,
                .PageRoot = pageroot,
                .OriginalString = tup.Item4,
                .PageIsDown = pageDown,
                .Language = lang,
                .RefDate = tup.Item2
            }

            datlist.Add(Reference)
        Next
        Dim tlist As New HashSet(Of Tuple(Of String, String))

        For Each nref As WikiWebReference In datlist

            Dim generatedRef As GenerateReferenceTextResult = GenerateReferenceText(nref, tpage, True)
            Select Case generatedRef.result
                Case GenerateReferenceTextResult.Results.Generation_Sucessful
                    cref += 1
                Case GenerateReferenceTextResult.Results.Alternative_Prefered
                    cref += 1
                Case GenerateReferenceTextResult.Results.Broken_Alternative_Source
                    recovered += 1
                    bref += 1
                Case GenerateReferenceTextResult.Results.Broken_WebArchive_Recovered
                    recovered += 1
                    bref += 1
                Case GenerateReferenceTextResult.Results.Broken_Unrecoverable
                    irrecoverable += 1
                    bref += 1
            End Select
            tlist.Add(New Tuple(Of String, String)(nref.OriginalString, generatedRef.text))
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

        EventLogger.Log("No changes in " + tpage.Title, "FixRefs")
        Return False
    End Function


    Function GenerateReferenceText(ByRef reference As WikiWebReference, ByRef page As Page, ByVal tryGetAlternativeSources As Boolean) As GenerateReferenceTextResult
        Dim referenceText As String
        Dim result As GenerateReferenceTextResult

        Dim newPageurl As Tuple(Of String, String) = HandleSpecialWebsites(New Uri(reference.PageUrl), reference)
        If (Not String.IsNullOrWhiteSpace(newPageurl.Item1) AndAlso tryGetAlternativeSources) Then
            SendUrlToWaybackMachine(reference.PageUrl, page.Title)
            Dim newDate As Date = Date.Now()
            Dim newSpokenDate As String = newDate.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
            Dim newRefUri As New Uri(newPageurl.Item1)

            If (newRefUri.Authority.Contains("web.archive.org")) Then
                Dim originalRefUrl As String = Regex.Match(reference.PageUrl, "(https?\:\/\/web\.archive\.org\/web\/(\d{1,14})\/)(.+)", RegexOptions.IgnoreCase).Groups(3).Value
                Dim originalRefUri As New Uri(originalRefUrl)
                Dim originalRefArchiveDateString As String = Regex.Match(reference.PageUrl, "(https?\:\/\/web\.archive\.org\/web\/(\d{1,14})\/)(.+)", RegexOptions.IgnoreCase).Groups(2).Value
                Dim originalRefArchiveDateInts As Integer() = {Integer.Parse(originalRefArchiveDateString.Substring(0, 4)), Integer.Parse(originalRefArchiveDateString.Substring(4, 2)), Integer.Parse(originalRefArchiveDateString.Substring(6, 2))}
                Dim archiveDate As Date = New Date(originalRefArchiveDateInts(0), originalRefArchiveDateInts(1), originalRefArchiveDateInts(2))
                Dim archiveSpokenDate As String = archiveDate.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
                referenceText = GenerateWaybackReferenceText(originalRefUrl, newPageurl.Item2, reference.SpokenDate, originalRefUri.Authority, reference.Language, newPageurl.Item1, archiveSpokenDate)

            Else
                referenceText = GenerateNormalReferenceText(newPageurl.Item1, newPageurl.Item2, newSpokenDate, newRefUri.Authority, reference.Language)
                referenceText &= String.Format(" <!-- PeriodiBOT ha reemplazado el enlace original de la referencia en {0} con uno actualizado de {1} -->",
                                           reference.PageRoot, newRefUri.Authority)
            End If

            result = New GenerateReferenceTextResult(referenceText, GenerateReferenceTextResult.Results.Alternative_Prefered)
                Return result
            End If


            If (reference.PageIsDown) Then

            'Verificar si está disponible en Internet Archive
            Dim datestring As String = reference.RefDate.ToString("yyyyMMddHHmmss", New Globalization.CultureInfo("es-ES"))
            Dim WayBackResponse As String = WorkerBot.GET(New Uri("https://archive.org/wayback/available?" & "timestamp=" & datestring & "&url=" & reference.PageUrl))
            Dim Unavaliable As Boolean = WayBackResponse.Contains("""archived_snapshots"": {}") ''No need to parse

            If Unavaliable Then
                Dim pageName As String = reference.PageName
                SendUrlToWaybackMachine(reference.PageUrl, page.Title)
                Dim newDate As Date = Date.Now()
                Dim newSpokenDate As String = newDate.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
                Dim newRefUri As New Uri(reference.PageUrl)
                referenceText = GenerateBrokenReferenceText(reference.PageUrl, pageName, newSpokenDate, newRefUri.Authority, reference.Language)
                result = New GenerateReferenceTextResult(referenceText, GenerateReferenceTextResult.Results.Broken_Unrecoverable)

            Else
                Dim timestamp As String = RemoveAllAlphas(TextInBetween(WayBackResponse, """timestamp"": """, """,").DefaultIfEmpty(datestring).LastOrDefault())
                Dim archivedate As Date = New Date(Integer.Parse(timestamp.Substring(0, 4)), Integer.Parse(timestamp.Substring(4, 2)), Integer.Parse(timestamp.Substring(6, 2)))
                Dim archiveSpokenDate As String = archivedate.ToString("d 'de' MMMM 'de' yyyy", New Globalization.CultureInfo("es-ES"))
                Dim archiveuri As String = {Regex.Match(WayBackResponse, "(?:""url"": "")(http:\/\/web\.archive\.org\/web\/.+?)(?:"")").Groups(1).Value}.DefaultIfEmpty("https://web.archive.org/web/*/" & reference.PageUrl).FirstOrDefault
                If archiveuri.StartsWith("http://") Then archiveuri = ReplaceFirst(archiveuri, "http://", "https://")
                Dim pageName As String = reference.PageName
                pageName = Regex.Replace(pageName, "(consultado +(en|el) +\w{2,10} +(de|del) +\d{2,4}|consultado +(el) +(\d{1,2}|\w{3,20}) +de +\w{2,10} +(de|del) +\d{2,4})", "", RegexOptions.IgnoreCase)
                pageName = RemoveExcessOfSpaces(pageName)
                referenceText = GenerateWaybackReferenceText(reference.PageUrl, pageName, reference.SpokenDate, reference.PageRoot, reference.Language, archiveuri, archiveSpokenDate)
                result = New GenerateReferenceTextResult(referenceText, GenerateReferenceTextResult.Results.Broken_WebArchive_Recovered)
            End If

            Return result
        End If


        ''Sitio ok, regenerar referencia
        SendUrlToWaybackMachine(reference.PageUrl, page.Title)
        Dim tpageName As String = reference.PageName
        tpageName = Regex.Replace(tpageName, "(consultado +(en|el) +\w{2,10} +(de|del) +\d{2,4}|consultado +(el) +(\d{1,2}|\w{3,20}) +de +\w{2,10} +(de|del) +\d{2,4})", "", RegexOptions.IgnoreCase)
        tpageName = RemoveExcessOfSpaces(tpageName)
        referenceText = GenerateNormalReferenceText(reference.PageUrl, tpageName, reference.SpokenDate, reference.PageRoot, reference.Language)
        result = New GenerateReferenceTextResult(referenceText, GenerateReferenceTextResult.Results.Generation_Sucessful)
        Return result

    End Function

    Private Function GenerateNormalReferenceText(ByVal url As String, title As String, accessDate As String, websiteRoot As String, websiteLang As String) As String
        Return String.Format("{{{{Cita web |url={0} |título={1} |fechaacceso={2} |sitioweb={3}{4}}}}}",
                                              url, title, accessDate, websiteRoot, If(Not String.IsNullOrWhiteSpace(websiteLang), " |idioma=" & websiteLang, ""))
    End Function

    Private Function GenerateBrokenReferenceText(ByVal url As String, title As String, accessDate As String, websiteRoot As String, websiteLang As String) As String
        Return String.Format("{{{{Enlace roto |1={{{{Cita web |url={0} |título={1} |fechaacceso={2} |sitioweb={3}{4}}}}} |2={0} }}}}",
                              url, title, accessDate, websiteRoot, If(Not String.IsNullOrWhiteSpace(websiteLang), " |idioma=" & websiteLang, "")) &
                              String.Format("<small>enlace irrecuperable</small> <!-- El enlace original de la referencia en {0} es irrecuperable (Sitio caído y no hay snapshots en Internet Archive). -->",
                                       websiteRoot)
    End Function

    Private Function GenerateWaybackReferenceText(ByVal url As String, title As String, accessDate As String, websiteRoot As String, websiteLang As String, archiveUrl As String, archiveDate As String) As String
        Return String.Format("{{{{Cita web |url={0} |título={1} |fechaacceso={2} |sitioweb={3}{4} |urlarchivo={5} |fechaarchivo={6}}}}}",
                                      url, title, accessDate, websiteRoot, If(Not String.IsNullOrWhiteSpace(websiteLang), " |idioma=" & websiteLang, ""),
                                      archiveUrl, archiveDate)
    End Function


    Private Function HandleSpecialWebsites(ByVal siteUri As Uri, ByRef reference As WikiWebReference) As Tuple(Of String, String) 'Retornamos una cadena de texto vacía si la página no aplica
        If siteUri.Authority.Contains("aob.oxfordjournals.org") Then ''Nuevo sitio para Annals of Botany
            Dim newUrl As String = GetRedirectUrl(siteUri) 'Obtenemos la url del redirect
            Return New Tuple(Of String, String)(newUrl, reference.PageName)
        End If

        If siteUri.Authority.Contains("www.tropicos.org") Then ''Los sinónimos y nombres de tropico redireccionan a la página legacy. Es un redirect Javascript pero conocemos el formato...
            Dim synonymsMatch As Match = Regex.Match(siteUri.OriginalString, "https?:\/\/www\.tropicos\.org\/namesynonyms\.aspx\?nameid=\d{7,9}", RegexOptions.IgnoreCase)
            If synonymsMatch.Success Then
                Dim newUrl As String = "http://legacy.tropicos.org/Name/" + synonymsMatch.Groups(1).Value + "?tab=synonyms" 'El certificado SSL de tropico no es válido, usemos http
                Return New Tuple(Of String, String)(newUrl, reference.PageName)
            End If
            Dim namesMatch As Match = Regex.Match(siteUri.OriginalString, "https?:\/\/www\.tropicos\.org\/name\/(\d{6,9})", RegexOptions.IgnoreCase)
            If namesMatch.Success Then
                Dim newUrl As String = "http://legacy.tropicos.org/Name/" + namesMatch.Groups(1).Value 'El certificado SSL de tropico no es válido, usemos http
                Return New Tuple(Of String, String)(newUrl, reference.PageName)
            End If
            Return New Tuple(Of String, String)(String.Empty, String.Empty)
        End If

        If siteUri.Authority.Contains("theplantlist.org") Then 'Plantlist es obsoleto, en su lugar debe usarse World Flora Online
            Dim newUrl As String
            Dim newTitle As String
            Dim searchMatch As Match = Regex.Match(reference.PageUrl, "(https*:\/\/.+?search\?q=)(\w+)", RegexOptions.IgnoreCase)
            If Not searchMatch.Success Then
                Dim recordMatch As Match = Regex.Match(reference.PageUrl, "(https*:\/\/.+?)(record\/)(.+)", RegexOptions.IgnoreCase)
                If Not recordMatch.Success Then Return New Tuple(Of String, String)(String.Empty, String.Empty)
                newUrl = "http://www.worldfloraonline.org/tpl/" + recordMatch.Groups(3).Value.Trim 'WFO compat TPL link
                newTitle = "The World Flora Online. Search: " & recordMatch.Groups(3).Value.Trim
                Try
                    Dim content As String = WorkerBot.GET(New Uri(newUrl))
                    Dim titleMatch As Match = Regex.Match(content, "<title>(.+?)<\/title>")
                    Dim idMatch As Match = Regex.Match(content, "<div itemid=""(.+?)"" itemtype=""")
                    Dim wfoid As String = idMatch.Groups(1).Value()
                    Dim wfoName As String = titleMatch.Groups(1).Value.Trim()
                    newTitle = "WFO (" & Date.UtcNow().Year.ToString() & "): " & wfoName
                    newUrl = "http://www.worldfloraonline.org/taxon/" & wfoid 'WFO no tiene sitio en https
                Catch ex As Exception
                End Try
            Else
                newUrl = "http://www.worldfloraonline.org/search?query=" + searchMatch.Groups(2).Value.Trim 'WFO no tiene sitio en https
                newTitle = "The World Flora Online. Search: " & searchMatch.Groups(2).Value.Trim
            End If
            Return New Tuple(Of String, String)(newUrl, newTitle)
        End If

        If siteUri.Authority.Contains("web.archive.org") Then 'Referencias obtenidas desde web archive
            Dim searchMatch As Match = Regex.Match(reference.PageUrl, "(https?\:\/\/web\.archive\.org\/web\/\d{1,14}\/)(.+)", RegexOptions.IgnoreCase)
            If searchMatch.Success Then
                Return New Tuple(Of String, String)(searchMatch.Value, reference.PageName)
            End If
        End If


        If siteUri.Authority.Contains("apps.kew.org") Then 'Sinónimos de KEW no funcionan, usemos WFO
            Dim kewIDMatch As Match = Regex.Match(reference.PageUrl, "synonomy.+accepted_id=(\d{1,10})|name_id=(\d{1,10})", RegexOptions.IgnoreCase)
            If kewIDMatch.Success Then
                Dim id As String = If(kewIDMatch.Groups(1).Value, kewIDMatch.Groups(2).Value)
                Dim newUrl As String = "http://www.worldfloraonline.org/tpl/kew-" + id + "" 'WFO compat TPL link
                Dim newTitle As String = "Sinónimos en The World Flora Online."
                Try
                    Dim content As String = WorkerBot.GET(New Uri(newUrl))
                    Dim titleMatch As Match = Regex.Match(content, "<title>(.+?)<\/title>")
                    Dim idMatch As Match = Regex.Match(content, "<div itemid=""(.+?)"" itemtype=""")
                    Dim wfoid As String = idMatch.Groups(1).Value()
                    Dim wfoName As String = titleMatch.Groups(1).Value.Trim()
                    newTitle = "WFO (" & Date.UtcNow().Year.ToString() & "): Sinónimos de " & wfoName
                    newUrl = "http://www.worldfloraonline.org/taxon/" & wfoid + "#synonyms" 'WFO no tiene sitio en https
                Catch ex As Exception
                End Try
                Return New Tuple(Of String, String)(newUrl, newTitle)
            End If
        End If

        Return New Tuple(Of String, String)(String.Empty, String.Empty)
    End Function

    Function GetRedirectUrl(ByVal siteUri As Uri) As String
        Dim handler As New HttpClientHandler()
        handler.AllowAutoRedirect = False
        Dim redirectedUrl As String = String.Empty
        Using client As HttpClient = New HttpClient(handler)
            Using response As HttpResponseMessage = client.GetAsync(siteUri).Result()
                Using content As HttpContent = response.Content
                    If (response.StatusCode = HttpStatusCode.Found Or response.StatusCode = HttpStatusCode.Moved Or response.StatusCode = HttpStatusCode.MovedPermanently) Then
                        Dim headers As HttpResponseHeaders = response.Headers
                        If (headers.Location IsNot Nothing AndAlso headers.Location IsNot Nothing) Then
                            redirectedUrl = headers.Location.AbsoluteUri
                        End If
                    End If
                End Using
            End Using
        End Using
        Return redirectedUrl
    End Function

    Class GenerateReferenceTextResult
        Public ReadOnly Property text As String
        Public ReadOnly Property result As Results
        Enum Results
            Generation_Sucessful
            Alternative_Prefered
            Broken_Unrecoverable
            Broken_WebArchive_Recovered
            Broken_Alternative_Source
            Broken_New_Site_Used
        End Enum
        Public Sub New(ByVal referenceText As String, operationResult As Results)
            text = referenceText
            result = operationResult
        End Sub
    End Class

    ''' <summary>
    ''' Envá la URL a Archive.org para que guarde un snapshot. Genera la operación en otro hilo porque tarda muuucho tiempo.
    ''' </summary>
    ''' <param name="ref"></param>
    Private Sub SendUrlToWaybackMachine(ByVal ref As String, ByVal ArticleName As String)

        Dim urlRefFunc As New Func(Of Boolean)(Function()
                                                   Try
                                                       Dim saveuristring = "https://web.archive.org/save/" & ref
                                                       WorkerBot.GET(New Uri(saveuristring))
                                                   Catch ex As Exception
                                                       Return False
                                                   End Try
                                                   Return True
                                               End Function)
        Initializer.TaskAdm.NewTask("Archivar referencia utilicada en '" & ArticleName & "' en Archive.org", WorkerBot.UserName, urlRefFunc, 0, False)
    End Sub

    Private Function TryGetPagenameFromContent(ByVal webUri As Uri, websiteContent As String) As String
        Dim pageDown As Boolean = String.IsNullOrWhiteSpace(websiteContent)
        Dim pagename As String = String.Empty
        If Not String.IsNullOrWhiteSpace(websiteContent) Then pagename = Regex.Replace(Regex.Replace(Regex.Match(websiteContent, "<title>[\s\S]+?<\/title>", RegexOptions.IgnoreCase).Value.Trim(), "(<title>|<\/title>)", "", RegexOptions.IgnoreCase), "[\n\r]", "").Trim()

        If String.IsNullOrWhiteSpace(pagename) Then
            If Regex.IsMatch(webUri.OriginalString, "\/[^\/\s]+\.pdf", RegexOptions.IgnoreCase) Then
                pagename = Regex.Match(webUri.OriginalString, "\/[^\/\s]+\.pdf", RegexOptions.IgnoreCase).Value
                pagename = UrlWebDecode(pagename.Replace("/", "").Replace(".pdf", ""))
            Else
                pagename = webUri.Authority
            End If
        End If
        pagename = pagename.Replace("|", "-").Replace(" .", ".")
        pagename = Regex.Replace(pagename, "<.+?>", "")
        pagename = RemoveExcessOfSpaces(pagename)
        pagename = UppercaseFirstCharacter(pagename).Trim()
        Return pagename
    End Function

    ''' <summary>
    ''' Entrega el idioma de la referencia si es posible y elimina el texto que indica el idioma de la referencia entregado en el parámetro wikiContent.
    ''' </summary>
    ''' <param name="webUri"></param>
    ''' <param name="websiteContent"></param>
    ''' <param name="wikiContent"></param>
    ''' <returns></returns>
    Private Function TryGetLanguageFromContent(ByVal webUri As Uri, websiteContent As String, wikiContent As String) As Tuple(Of String, String)
        Dim lang As String = ""
        If (Not String.IsNullOrWhiteSpace(websiteContent)) AndAlso Regex.IsMatch(websiteContent, "<html[^\>]+?lang=""\w{2}""", RegexOptions.IgnoreCase) Then
            lang = TextInBetween(Regex.Match(websiteContent, "<html[^\>]+?lang=""\w{2}""", RegexOptions.IgnoreCase).Value, "lang=""", """").DefaultIfEmpty("").FirstOrDefault()
        End If

        Dim langpattern As String = "(\(en )([\wñ]{2,}(es|és|ano|an|án|ino|so|nio|ol))(\))"
        Dim tmatch As Match = Regex.Match(wikiContent, langpattern, RegexOptions.IgnoreCase)
        If tmatch.Success Then
            Dim gr As Group = tmatch.Groups(2)
            lang = gr.Value
            wikiContent = Regex.Replace(wikiContent, langpattern, "", RegexOptions.IgnoreCase)
            wikiContent = RemoveExcessOfSpaces(wikiContent).Trim()
        End If
        Return New Tuple(Of String, String)(lang, wikiContent)
    End Function



    Private Function NormalizeDates(ByVal pagetext As String) As Tuple(Of String, Integer)
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

    Private Function SimplifyEmptyRefs(ByVal pagetext As String) As Tuple(Of String, Integer)
        Dim refmatches As MatchCollection = Regex.Matches(pagetext, "(<ref>|<ref name *=[^\/]+?>)[\s]+(<\/ref>)", RegexOptions.IgnoreCase)
        Dim changedCount As Integer = 0
        For Each m As Match In refmatches
            Dim simplifiedRef As String = m.Groups(1).Value.TrimEnd(">"c) & "/>"
            pagetext = pagetext.Replace(m.Value, simplifiedRef)
            changedCount += 1
        Next
        Return New Tuple(Of String, Integer)(pagetext, changedCount)
    End Function


    Private Function RemoveDuplicates(ByVal pagetext As String) As Tuple(Of String, Integer)
        Dim refmatches As MatchCollection = Regex.Matches(pagetext, "(<ref>|<ref name *=[^\/]+?>)([\s\S]+?)(<\/ref>)", RegexOptions.IgnoreCase)
        Dim tmatches As String() = FilterRefs(refmatches)
        Dim refs As New HashSet(Of String)
        Dim changedCount As Integer = 0
        If Not (tmatches.Distinct().Count = tmatches.Count) Then
            Dim duplicates As String() = tmatches.GroupBy(Function(p) p).Where(Function(g) g.Count > 1).Select(Function(g) g.Key).ToArray
            changedCount = duplicates.Count
            If changedCount = 0 Then Return Nothing
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

        Dim shortenFunc As Tuple(Of String, Integer) = ShortenEmptyRefs(pagetext)
        pagetext = shortenFunc.Item1
        changedCount += shortenFunc.Item2
        Dim repFunc As Tuple(Of String, Integer) = RemoveRefWithSameNameButDifferentContent(pagetext)
        pagetext = repFunc.Item1
        changedCount += repFunc.Item2
        Return New Tuple(Of String, Integer)(pagetext, changedCount)
    End Function

    Private Function ShortenEmptyRefs(ByVal pagetext As String) As Tuple(Of String, Integer)
        Dim refmatches As MatchCollection = Regex.Matches(pagetext, "(<ref>|<ref name *=[^\/]+?>)(<\/ref>)", RegexOptions.IgnoreCase)
        Dim matches As String() = refmatches.OfType(Of Match)().Select(Function(x) x.Value).ToArray()
        Dim refList As New Dictionary(Of String, String)
        Dim replacements As Integer = 0
        Dim newPageText As String = pagetext
        For Each m As String In matches
            If Not refList.ContainsKey(m) Then
                Dim newref As String = Regex.Match(m, "(<ref>|<ref name *=[^\/]+?>)(<\/ref>)").Groups(1).Value.Replace(">", "/>")
                refList.Add(m, newref)
            End If
        Next

        For Each r As String In refList.Keys
            newPageText = newPageText.Replace(r, refList(r))
            replacements += 1
        Next
        Return New Tuple(Of String, Integer)(newPageText, replacements)
    End Function

    Private Function RemoveRefWithSameNameButDifferentContent(ByVal pagetext As String) As Tuple(Of String, Integer)
        Dim refmatches As MatchCollection = Regex.Matches(pagetext, "(<ref>|<ref name *=[^\/]+?>)([\s\S]*?)(<\/ref>)", RegexOptions.IgnoreCase)
        Dim matches As String() = refmatches.OfType(Of Match)().Select(Function(x) x.Value).ToArray()
        Dim refList As New Dictionary(Of String, HashSet(Of String))

        For Each m As String In matches
            Dim refmatch As Match = Regex.Match(m, "(<ref name *=)([^\/]+?)(>)")
            If Not refmatch.Success Then Continue For
            Dim refname As String = refmatch.Groups(2).Value
            If Not refList.ContainsKey(refname) Then
                refList.Add(refname, New HashSet(Of String))
            End If
            refList(refname).Add(m)
        Next

        Dim replaceList As New List(Of Tuple(Of String, String))

        For Each m As String In refList.Keys
            Dim refs As String() = refList(m).ToArray
            If refs.Count <= 1 Then Continue For
            Dim count As Integer = 1
            For Each s As String In refs
                replaceList.Add(New Tuple(Of String, String)(s, s.Replace(m, m.TrimEnd(""""c) & "_" & count & """")))
                count += 1
            Next
        Next
        Dim replacements As Integer = 0
        For Each replaceTup As Tuple(Of String, String) In replaceList
            pagetext = pagetext.Replace(replaceTup.Item1, replaceTup.Item2)
            replacements += 1
        Next
        Return New Tuple(Of String, Integer)(pagetext, replacements)
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

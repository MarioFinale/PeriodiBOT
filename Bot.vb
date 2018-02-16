Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Namespace WikiBot
    Public Class Bot

        Private BotCookies As CookieContainer
        Private _userName As String = String.Empty
        Private _siteurl As String = String.Empty
        Private Api As ApiSession
#Region "Properties"
        Public ReadOnly Property Bot As Boolean
            Get
                Return IsBot()
            End Get
        End Property

        Public ReadOnly Property Siteurl As String
            Get
                Return _siteurl
            End Get
        End Property

        Public ReadOnly Property Username As String
            Get
                Return _userName
            End Get
        End Property
#End Region

        Sub New(ByVal botUserName As String, botPassword As String, pageURL As String)
            Api = New ApiSession(botUserName, botPassword, pageURL)
            _userName = Api.UserName
            _siteurl = Api.Siteurl
        End Sub

        Function POSTQUERY(ByVal postdata As String) As String
            Return Api.POSTQUERY(postdata)
        End Function

        Function GETQUERY(ByVal getdata As String) As String
            Return Api.GETQUERY(getdata)
        End Function

        Function [GET](ByVal url As String) As String
            Return Api.GET(url)
        End Function

        Function IsBot() As Boolean
            Dim postdata As String = "action=query&assert=bot&format=json"
            Dim postresponse As String = POSTQUERY(postdata)
            If postresponse.Contains("assertbotfailed") Then
                Return False
            Else
                Return True
            End If
        End Function

        Function IsLoggedIn() As Boolean
            Dim postdata As String = "action=query&assert=user&format=json"
            Dim postresponse As String = POSTQUERY(postdata)
            If postresponse.Contains("assertuserfailed") Then
                Return False
            Else
                Return True
            End If
        End Function

        ''' <summary>
        ''' Retorna el ultimo REVID (como integer) de las paginas indicadas como SortedList (con el formato {Pagename,Revid}), las paginas deben ser distintas. 
        ''' En caso de no existir la pagina, retorna -1 como REVID.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de paginas unicos.</param>
        ''' <remarks></remarks>
        Function GetLastRevIds(ByVal pageNames As String()) As SortedList(Of String, Integer)
            Debug_Log("GetLastRevIDs: Get Wikipedia last RevisionID of """ & pageNames.Count.ToString & """ pages.", "LOCAL", BOTName)
            Dim PageNamesList As List(Of String) = pageNames.ToList
            PageNamesList.Sort()
            Dim PageList As List(Of List(Of String)) = SplitStringArrayIntoChunks(PageNamesList.ToArray, 50)
            Dim PagenameAndLastId As New SortedList(Of String, Integer)
            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty
                For Each s As String In ListInList
                    s = UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))

                Dim QueryResponse As String = GETQUERY(("?action=query&prop=revisions&format=json&titles=" & Qstring))
                Dim ResponseArray As String() = TextInBetweenInclusive(QueryResponse, ",""title"":", "}]")

                For Each s As String In ResponseArray

                    Dim pagetitle As String = TextInBetween(s, ",""title"":""", """,""")(0)

                    If s.Contains(",""missing"":") Then
                        If Not PagenameAndLastId.ContainsKey(pagetitle) Then
                            PagenameAndLastId.Add(UrlWebDecode(NormalizeUnicodetext(pagetitle)).Replace(" ", "_"), -1)
                        End If
                    Else
                        If Not PagenameAndLastId.ContainsKey(pagetitle) Then
                            Dim TemplateTitle As String = String.Empty
                            Dim Revid As String = TextInBetween(s, pagetitle & """,""revisions"":[{""revid"":", ",""parentid"":")(0)
                            Dim Revid_ToInt As Integer = CType(Revid, Integer)

                            Dim modlist As New List(Of String)

                            For Each tx As String In PageNamesList.ToArray
                                Dim tmp As String = tx.ToLower.Replace("_", " ")
                                modlist.Add(tmp)
                            Next

                            Dim normtext As String = NormalizeUnicodetext(pagetitle)
                            normtext = normtext.ToLower.Replace("_", " ")
                            Dim ItemIndex As Integer = modlist.IndexOf(normtext)
                            TemplateTitle = PageNamesList(ItemIndex)
                            PagenameAndLastId.Add(TemplateTitle, Revid_ToInt)

                        End If
                    End If
                Next
            Next
            Debug_Log("GetLastRevIDs: Done """ & PagenameAndLastId.Count.ToString & """ pages returned.", "LOCAL", BOTName)
            Return PagenameAndLastId
        End Function

        ''' <summary>
        ''' Entrega el título de la primera página que coincida remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, entrega una cadena de texto vacía.
        ''' </summary>
        ''' <param name="Text">Título aproximado o similar al de una página</param>
        ''' <returns></returns>
        Function TitleFirstGuess(Text As String) As String
            Dim titles As String() = GetTitlesFromQueryText(GETQUERY("action=query&format=json&list=search&utf8=1&srsearch="))
            If titles.Count >= 1 Then
                Return titles(0)
            Else
                Return String.Empty
            End If
        End Function

        ''' <summary>
        ''' Retorna el valor ORES (en %) de los EDIT ID (eswiki) indicados como SortedList (con el formato {ID,Score}), los EDIT ID deben ser distintos. 
        ''' En caso de no existir el EDIT ID, retorna 0.
        ''' </summary>
        ''' <param name="revids">Array con EDIT ID's unicos.</param>
        ''' <remarks>Los EDIT ID deben ser distintos</remarks>
        Function GetORESScores(ByVal revids As Integer()) As SortedList(Of Integer, Double())
            Dim Revlist As List(Of List(Of Integer)) = SplitIntegerArrayIntoChunks(revids, 50)
            Dim EditAndScoreList As New SortedList(Of Integer, Double())
            For Each ListOfList As List(Of Integer) In Revlist

                Dim Qstring As String = String.Empty
                For Each n As Integer In ListOfList
                    Qstring = Qstring & n.ToString & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))
                Try
                    Dim s As String = Api.GET(("https://ores.wikimedia.org/v3/scores/eswiki/?models=damaging|goodfaith&format=json&revids=" & UrlWebEncode(Qstring)))

                    For Each m As Match In Regex.Matches(s, "({|, )(""[0-9]+"":).+?(}}}})")

                        Dim EditID_str As String = Regex.Match(m.Value, """[0-9]+""").Value
                        EditID_str = EditID_str.Trim(CType("""", Char()))
                        EditID_str = RemoveAllAlphas(EditID_str)
                        Dim EditID As Integer = Integer.Parse(EditID_str)

                        If m.Value.Contains("error") Then

                            Debug_Log("GetORESScore: Server error in query of ORES score from revid " & EditID_str & " (invalid diff?)", "LOCAL", BOTName)
                            EditAndScoreList.Add(EditID, {0, 0})
                        Else
                            Try
                                Dim DMGScore_str As String = TextInBetween(TextInBetweenInclusive(s, "{""damaging"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)
                                Dim GoodFaithScore_str As String = TextInBetween(TextInBetweenInclusive(s, """goodfaith"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)

                                Debug_Log("GetORESScore: Query of ORES score from revid done, Strings: GF: " & GoodFaithScore_str & " DMG:" & DMGScore_str, "LOCAL", BOTName)

                                Dim DMGScore As Double = Double.Parse(DMGScore_str) * 100
                                Dim GoodFaithScore As Double = Double.Parse(GoodFaithScore_str) * 100

                                Debug_Log("GetORESScore: Query of ORES score from revid done, Double: GF: " & GoodFaithScore.ToString & " DMG:" & DMGScore.ToString, "LOCAL", BOTName)

                                EditAndScoreList.Add(EditID, {DMGScore, GoodFaithScore})
                            Catch ex As IndexOutOfRangeException
                                Debug_Log("GetORESScore: IndexOutOfRange EX in ORES score from revid " & EditID_str & " EX: " & ex.Message, "LOCAL", BOTName)
                                EditAndScoreList.Add(EditID, {0, 0})
                            Catch ex2 As Exception
                                Debug_Log("GetORESScore: EX in ORES score from revid " & EditID_str & " EX: " & ex2.Message, "LOCAL", BOTName)
                                EditAndScoreList.Add(EditID, {0, 0})
                            End Try
                        End If

                    Next
                Catch ex As Exception
                    Debug_Log("GetORESScore: EX obttaining ORES scores EX: " & ex.Message, "LOCAL", BOTName)
                End Try
            Next

            Return EditAndScoreList

        End Function




    End Class






    Class ApiSession

        Private ApiCookies As CookieContainer
        Private _userName As String = String.Empty

        Private _botusername As String = String.Empty
        Private _botpass As String = String.Empty
        Private _siteurl As String = String.Empty
        Private UserAgent As String = BOTName & " (http://es.wikipedia.org/wiki/Usuario_discusión:MarioFinale)"

#Region "Properties"
        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public ReadOnly Property Siteurl As String
            Get
                Return _siteurl
            End Get
        End Property
#End Region

        ''' <summary>
        ''' Inicializa una nueva instancia del BOT.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="PageURL">Nombre exacto de la página</param>
        Sub New(ByVal botUserName As String, botPassword As String, pageURL As String)
            If String.IsNullOrWhiteSpace(botUserName) Then
                Throw New ArgumentException("No username")
            End If
            If String.IsNullOrWhiteSpace(botPassword) Then
                Throw New ArgumentException("No BotPassword")
            End If
            If String.IsNullOrWhiteSpace(pageURL) Then
                Throw New ArgumentException("No PageURL")
            End If
            _botusername = botUserName
            _botpass = botPassword
            _siteurl = pageURL
            ApiCookies = New CookieContainer
            LogOn()
            _userName = botUserName.Split("@"c)(0).Trim()
        End Sub

        ''' <summary>
        ''' Obtiene un Token y cookies de ingreso, establece las cookies de la clase y retorna el token como string.
        ''' </summary>
        Private Function GetWikiToken() As String
            Log("Obtaining token...", "LOCAL", BOTName)
            Dim postdata As String = "action=query&meta=tokens&type=login&format=json"
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, ApiCookies)
            Dim token As String = TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Log("Token obtained!", "LOCAL", BOTName)
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Public Function LogOn() As String
            Log("Signing in...", "LOCAL", BOTName)
            Dim token As String = String.Empty
            Dim url As String = _siteurl
            Dim postdata As String = String.Empty
            Dim postresponse As String = String.Empty
            Dim lresult As String = String.Empty
            Dim exitloop As Boolean = False

            Do Until exitloop
                Try
                    token = GetWikiToken()
                    postdata = "action=login&format=json&lgname=" & _botusername & "&lgpassword=" & _botpass & "&lgdomain=" & "&lgtoken=" & UrlWebEncode(token)
                    postresponse = PostDataAndGetResult(url, postdata, True, ApiCookies)
                    lresult = TextInBetween(postresponse, "{""result"":""", """,")(0)
                    Log("Login result: " & lresult, "LOCAL", BOTName)
                    Dim lUserID As String = TextInBetween(postresponse, """lguserid"":", ",")(0)
                    Log("UserID: " & lUserID, "LOCAL", BOTName)
                    Dim lUsername As String = TextInBetween(postresponse, """lgusername"":""", """}")(0)
                    Log("Username: " & lUsername, "LOCAL", BOTName)
                    Return lresult
                Catch ex As IndexOutOfRangeException
                    Log("Logon error", "LOCAL", BOTName)
                    If lresult.ToLower = "failed" Then
                        Dim reason As String = TextInBetween(postresponse, """reason"":""", """")(0)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.WriteLine("Login Failed")
                        Console.WriteLine("Reason: " & reason)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.Write("Press any key to exit...")
                        Console.ReadKey()
                        ExitProgram()
                        Return lresult
                    End If
                    Return lresult
                Catch ex2 As System.Net.WebException
                    Log("Network error", "LOCAL", BOTName)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Dim reason As String = ex2.Message
                    Console.WriteLine("Login Failed (Network error)")
                    Console.WriteLine(reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                Catch ex3 As Exception
                    Log("Logon error", "LOCAL", BOTName)
                    Debug_Log("Logon error: " & ex3.Message, "LOCAL", BOTName)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Dim reason As String = ex3.Message
                    Console.WriteLine("Login Failed")
                    Console.WriteLine("Reason: " & reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                End Try
                exitloop = PressKeyTimeout()
            Loop
            ExitProgram()
            Return lresult
        End Function

        Public Function POSTQUERY(ByVal postdata As String) As String
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, ApiCookies)
            Return postresponse
        End Function

        Public Function GETQUERY(ByVal getdata As String) As String
            Dim getresponse As String = GetDataAndResult(_siteurl & getdata, True, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal urlstring As String) As String
            If String.IsNullOrWhiteSpace(urlstring) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(urlstring, True, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal url As Uri) As String
            If String.IsNullOrWhiteSpace(url.ToString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(url.ToString, True, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function POST(ByVal urlstring As String, ByVal postdata As String) As String
            If String.IsNullOrWhiteSpace(urlstring) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postdata) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(urlstring, postdata, True, ApiCookies)
            Return postresponse
        End Function

        Public Overloads Function POST(ByVal url As Uri, ByVal postdata As String) As String
            If String.IsNullOrWhiteSpace(url.ToString) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postdata) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, ApiCookies)
            Return postresponse
        End Function


        ''' <summary>
        ''' Limpia todas las cookies, retorna "true" si finaliza correctamente.
        ''' </summary>
        Public Function CleanCookies() As Boolean
            Try
                ApiCookies = New CookieContainer
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function




        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
        Public Function GetDataAndResult(ByVal pageURL As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String
            If Cookies Is Nothing Then
                Cookies = New CookieContainer
            End If

            Dim tempcookies As CookieContainer = Cookies

            Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageURL), HttpWebRequest)
            postreq.Method = "GET"
            postreq.KeepAlive = True
            postreq.CookieContainer = tempcookies
            postreq.UserAgent = UserAgent
            postreq.ContentType = "application/x-www-form-urlencoded"
            Dim postresponse As HttpWebResponse
            postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
            tempcookies.Add(postresponse.Cookies)
            If getCookies Then
                Cookies = tempcookies
            End If
            Dim postreqreader As New StreamReader(postresponse.GetResponseStream())
            Return postreqreader.ReadToEnd

        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
        Public Function PostDataAndGetResult(pageURI As Uri, postData As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String
            Return PostDataAndGetResult(pageURI.ToString, postData, getCookies, Cookies)
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
        Public Function PostDataAndGetResult(pageURL As String, postData As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String

            If String.IsNullOrEmpty(pageURL) Then
                Throw New ArgumentNullException("pageURL", "No URL specified.")
            End If

            If Cookies Is Nothing Then
                Cookies = New CookieContainer
            End If

            Dim tempcookies As CookieContainer = Cookies

            Dim encoding As New UTF8Encoding
            Dim byteData As Byte() = encoding.GetBytes(postData)
            Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageURL), HttpWebRequest)

            postreq.Method = "POST"
            postreq.KeepAlive = True
            postreq.CookieContainer = tempcookies
            postreq.UserAgent = UserAgent
            postreq.ContentType = "application/x-www-form-urlencoded"
            postreq.ContentLength = byteData.Length

            Dim postreqstream As Stream = postreq.GetRequestStream()
            postreqstream.Write(byteData, 0, byteData.Length)
            postreqstream.Close()

            Dim postresponse As HttpWebResponse
            postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
            tempcookies.Add(postresponse.Cookies)
            If getCookies Then
                Cookies = tempcookies
            End If
            Dim postreqreader As New StreamReader(postresponse.GetResponseStream())
            Return postreqreader.ReadToEnd


        End Function



    End Class






End Namespace
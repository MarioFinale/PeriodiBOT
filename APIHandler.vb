Imports System.IO
Imports System.Net

Namespace WikiBot

    Public Class APIHandler

        Private ApiCookies As CookieContainer
        Private _userName As String = String.Empty

        Private _botusername As String = String.Empty
        Private _botpass As String = String.Empty
        Private _apiURL As String = String.Empty
        Private _userAgent As String = "PeriodiBOT/" & Version & " (http://es.wikipedia.org/wiki/Usuario_discusión:MarioFinale) .NET/MONO"

#Region "Properties"
        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public ReadOnly Property ApiURL As String
            Get
                Return _apiURL
            End Get
        End Property

        Public Property UserAgent As String
            Get
                Return _userAgent
            End Get
            Set(value As String)
                _userAgent = value
            End Set
        End Property
#End Region

        ''' <summary>
        ''' Inicializa una nueva instancia del handler.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="ApiURL">Direccion de la API</param>
        Sub New(ByVal botUserName As String, botPassword As String, apiUrl As String)
            If String.IsNullOrWhiteSpace(botUserName) Then
                Throw New ArgumentException("No username")
            End If
            If String.IsNullOrWhiteSpace(botPassword) Then
                Throw New ArgumentException("No BotPassword")
            End If
            If String.IsNullOrWhiteSpace(apiUrl) Then
                Throw New ArgumentException("No PageURL")
            End If
            _botusername = botUserName
            _botpass = botPassword
            _apiURL = apiUrl
            ApiCookies = New CookieContainer
            LogOn()
            _userName = botUserName.Split("@"c)(0).Trim()
        End Sub

        ''' <summary>
        ''' Obtiene un Token y cookies de ingreso, establece las cookies de la clase y retorna el token como string.
        ''' </summary>
        Private Function GetWikiToken() As String
            Utils.EventLogger.Log("Obtaining token...", "LOCAL")
            Dim postdata As String = "action=query&meta=tokens&type=login&format=json"
            Dim postresponse As String = PostDataAndGetResult(_apiURL, postdata, ApiCookies)
            Dim token As String = Utils.TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Utils.EventLogger.Log("Token obtained!", "LOCAL")
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Public Function LogOn() As String
            Utils.EventLogger.Log("Signing in...", "LOCAL")
            Dim token As String = String.Empty
            Dim url As String = _apiURL
            Dim postdata As String = String.Empty
            Dim postresponse As String = String.Empty
            Dim lresult As String = String.Empty
            Dim exitloop As Boolean = False

            Do Until exitloop
                Try
                    token = GetWikiToken()
                    postdata = "action=login&format=json&lgname=" & _botusername & "&lgpassword=" & _botpass & "&lgdomain=" & "&lgtoken=" & Utils.UrlWebEncode(token)
                    postresponse = PostDataAndGetResult(url, postdata, ApiCookies)
                    lresult = Utils.TextInBetween(postresponse, "{""result"":""", """,")(0)
                    Utils.EventLogger.Log("Login result: " & lresult, "LOCAL")
                    Dim lUserID As String = Utils.TextInBetween(postresponse, """lguserid"":", ",")(0)
                    Utils.EventLogger.Log("UserID: " & lUserID, "LOCAL")
                    Dim lUsername As String = Utils.TextInBetween(postresponse, """lgusername"":""", """}")(0)
                    Utils.EventLogger.Log("Username: " & lUsername, "LOCAL")
                    Return lresult
                Catch ex As IndexOutOfRangeException
                    Utils.EventLogger.Log("Logon error", "LOCAL")
                    If lresult.ToLower = "failed" Then
                        Dim reason As String = Utils.TextInBetween(postresponse, """reason"":""", """")(0)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.WriteLine("Login Failed")
                        Console.WriteLine("Reason: " & reason)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.Write("Press any key to exit...")
                        Console.ReadKey()
                        Utils.ExitProgram()
                        Return lresult
                    End If
                    Return lresult
                Catch ex2 As System.Net.WebException
                    Utils.EventLogger.Log("Network error", "LOCAL")
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Dim reason As String = ex2.Message
                    Console.WriteLine("Login Failed (Network error)")
                    Console.WriteLine(reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                Catch ex3 As Exception
                    Utils.EventLogger.Log("Logon error", "LOCAL")
                    Utils.EventLogger.Debug_Log("Logon error: " & ex3.Message, "LOCAL")
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Dim reason As String = ex3.Message
                    Console.WriteLine("Login Failed")
                    Console.WriteLine("Reason: " & reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                End Try
                exitloop = Utils.PressKeyTimeout()
            Loop
            Utils.ExitProgram()
            Return lresult
        End Function

        Public Function POSTQUERY(ByVal postData As String) As String
            Dim postresponse As String = PostDataAndGetResult(_apiURL, postData, ApiCookies)
            Return postresponse
        End Function

        Public Function GETQUERY(ByVal getData As String) As String
            Dim getresponse As String = GetDataAndResult(_apiURL & "?" & getData, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal urlString As String) As String
            If String.IsNullOrWhiteSpace(urlString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(urlString, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal pageUri As Uri) As String
            If String.IsNullOrWhiteSpace(pageUri.ToString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(pageUri.ToString, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function POST(ByVal urlString As String, ByVal postData As String) As String
            If String.IsNullOrWhiteSpace(urlString) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postData) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(urlString, postData, ApiCookies)
            Return postresponse
        End Function

        Public Overloads Function POST(ByVal pageUri As Uri, ByVal postData As String) As String
            If String.IsNullOrWhiteSpace(pageUri.ToString) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postData) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(pageUri, postData, ApiCookies)
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
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        Public Function GetDataAndResult(ByVal pageUri As Uri) As String
            Return GetDataAndResult(pageUri.ToString, New CookieContainer)
        End Function
        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        Public Function GetDataAndResult(ByVal pageUri As Uri, ByRef cookies As CookieContainer) As String
            Return GetDataAndResult(pageUri.ToString, cookies)
        End Function

        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        Public Function GetDataAndResult(ByVal pageUrl As String) As String
            Return GetDataAndResult(pageUrl, New CookieContainer)
        End Function

        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="Cookies">Cookies sobre los que se trabaja.</param>
        Public Function GetDataAndResult(ByVal pageUrl As String, ByRef cookies As CookieContainer) As String
            Dim tryCount As Integer = 0

            Do Until tryCount = MaxRetry
                Try
                    If cookies Is Nothing Then
                        cookies = New CookieContainer
                    End If
                    Dim tempcookies As CookieContainer = cookies
                    Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageUrl), HttpWebRequest)
                    postreq.Method = "GET"
                    postreq.KeepAlive = True
                    postreq.Timeout = 60000
                    postreq.CookieContainer = tempcookies
                    postreq.UserAgent = _userAgent
                    postreq.ContentType = "application/x-www-form-urlencoded"
                    Dim postresponse As HttpWebResponse
                    postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
                    tempcookies.Add(postresponse.Cookies)
                    Dim postreqreader As New StreamReader(postresponse.GetResponseStream())
                    cookies = tempcookies
                    Return postreqreader.ReadToEnd
                Catch ex As ProtocolViolationException 'Catch para los headers erróneos que a veces entrega la API de MediaWiki
                    Utils.EventLogger.EX_Log(ex.Message, ex.TargetSite.Name)
                    Utils.EventLogger.Debug_Log("EX STACK TRACE: " & ex.StackTrace, ex.Source)
                    tryCount += 1
                End Try
            Loop
            Throw New MaxRetriesExceededExeption
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        Public Function PostDataAndGetResult(pageUri As Uri, postData As String) As String
            Return PostDataAndGetResult(pageUri.ToString, postData, New CookieContainer)
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        Public Function PostDataAndGetResult(pageUri As Uri, postData As String, ByRef cookies As CookieContainer) As String
            Return PostDataAndGetResult(pageUri.ToString, postData, cookies)
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        Public Function PostDataAndGetResult(pageUrl As String, postData As String) As String
            Return PostDataAndGetResult(pageUrl, postData, New CookieContainer)
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        Public Function PostDataAndGetResult(pageUrl As String, postData As String, ByRef cookies As CookieContainer) As String

            If String.IsNullOrEmpty(pageUrl) Then
                Throw New ArgumentNullException("PostDataAndGetResult", "No URL specified.")
            End If

            If cookies Is Nothing Then
                cookies = New CookieContainer
            End If

            Dim tempcookies As CookieContainer = cookies

            Dim encoding As New Text.UTF8Encoding
            Dim byteData As Byte() = encoding.GetBytes(postData)
            Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageUrl), HttpWebRequest)

            postreq.Method = "POST"
            postreq.KeepAlive = True
            postreq.CookieContainer = tempcookies
            postreq.UserAgent = _userAgent
            postreq.ContentType = "application/x-www-form-urlencoded"
            postreq.ContentLength = byteData.Length

            Dim postreqstream As Stream = postreq.GetRequestStream()
            postreqstream.Write(byteData, 0, byteData.Length)
            postreqstream.Close()

            Dim postresponse As HttpWebResponse
            postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
            tempcookies.Add(postresponse.Cookies)
            Dim postreqreader As New StreamReader(postresponse.GetResponseStream())
            cookies = tempcookies
            Return postreqreader.ReadToEnd


        End Function



    End Class

End Namespace
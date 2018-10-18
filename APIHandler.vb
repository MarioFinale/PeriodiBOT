Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Resources
Imports System.Threading

Namespace WikiBot

    Public Class ApiHandler
        Private ApiCookies As CookieContainer
        Private _userName As String = String.Empty

        Private _botUsername As String = String.Empty
        Private _botPassword As String = String.Empty
        Private _apiUri As Uri
        Private _userAgent As String = "PeriodiBOT/" & Version & " (http://es.wikipedia.org/wiki/Usuario_discusión:MarioFinale) .NET/MONO"

#Region "Properties"
        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public ReadOnly Property ApiUri As Uri
            Get
                Return _apiUri
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
        ''' Inicializa una nueva instancia del bot.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="tUri">Direccion de la API</param>
        Sub New(ByVal botUserName As String, botPassword As String, tUri As Uri)
            If String.IsNullOrWhiteSpace(botUserName) Then
                Throw New ArgumentException("No username")
            End If
            If String.IsNullOrWhiteSpace(botPassword) Then
                Throw New ArgumentException("No BotPassword")
            End If
            If tUri Is Nothing Then
                Throw New ArgumentException("No Api Uri")
            End If
            Init(botUserName, botPassword, tUri)
        End Sub


        Private Sub Init(ByVal botUserName As String, botPassword As String, apiUri As Uri)
            _botUsername = botUserName
            _botPassword = botPassword
            _apiUri = apiUri
            ApiCookies = New CookieContainer
            LogOn()
            _userName = botUserName.Split("@"c)(0).Trim()
        End Sub

        ''' <summary>
        ''' Obtiene un Token y cookies de ingreso, establece las cookies de la clase y retorna el token como string.
        ''' </summary>
        Private Function GetWikiToken() As String
            Utils.EventLogger.Log(My.Resources.Messages.RequestingToken, My.Resources.StaticVars.LocalSource)
            Dim postdata As String = "action=query&meta=tokens&type=login&format=json"
            Dim postresponse As String = PostDataAndGetResult(_apiUri, postdata, ApiCookies)
            Dim token As String = Utils.TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Utils.EventLogger.Log(My.Resources.Messages.TokenObtained, My.Resources.StaticVars.LocalSource)
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Public Function LogOn() As String
            Utils.EventLogger.Log(My.Resources.Messages.SigninIn, My.Resources.StaticVars.LocalSource)
            Dim token As String = String.Empty
            Dim turi As Uri = _apiUri
            Dim postdata As String = String.Empty
            Dim postresponse As String = String.Empty
            Dim lresult As String = String.Empty
            Dim exitloop As Boolean = False

            Do Until exitloop
                Try
                    token = GetWikiToken()
                    postdata = "action=login&format=json&lgname=" & _botUsername & "&lgpassword=" & _botPassword & "&lgdomain=" & "&lgtoken=" & Utils.UrlWebEncode(token)
                    postresponse = PostDataAndGetResult(turi, postdata, ApiCookies)
                    lresult = Utils.TextInBetween(postresponse, "{""result"":""", """,")(0)
                    Utils.EventLogger.Log(My.Resources.Messages.LoginResult & lresult, My.Resources.StaticVars.LocalSource)
                    Dim lUserID As String = Utils.TextInBetween(postresponse, """lguserid"":", ",")(0)
                    Utils.EventLogger.Log(My.Resources.Messages.LoginID & lUserID, My.Resources.StaticVars.LocalSource)
                    Dim lUsername As String = Utils.TextInBetween(postresponse, """lgusername"":""", """}")(0)
                    Utils.EventLogger.Log(My.Resources.Messages.Username & lUsername, My.Resources.StaticVars.LocalSource)
                    Return lresult
                Catch ex As IndexOutOfRangeException
                    Utils.EventLogger.Log(My.Resources.Messages.LoginError, My.Resources.StaticVars.LocalSource)
                    If lresult.ToLower(Globalization.CultureInfo.InvariantCulture) = "failed" Then
                        Dim reason As String = Utils.TextInBetween(postresponse, """reason"":""", """")(0)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.WriteLine(My.Resources.Messages.Reason & reason)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.Write(My.Resources.Messages.PressKey)
                        Console.ReadKey()
                        Utils.ExitProgram()
                        Return lresult
                    End If
                    Return lresult
                Catch ex2 As System.Net.WebException
                    Utils.EventLogger.Log(My.Resources.Messages.NetworkError & ex2.Message, My.Resources.StaticVars.LocalSource)
                End Try
                Console.WriteLine(Environment.NewLine)
                exitloop = Utils.PressKeyTimeout(5)
            Loop
            Utils.ExitProgram()
            Return lresult
        End Function

        ''' <summary>
        ''' Envía un POST a la url de la API con los datos indicados
        ''' </summary>
        ''' <param name="postData"></param>
        ''' <returns></returns>
        Public Function Postquery(ByVal postData As String) As String
            Dim postresponse As String = PostDataAndGetResult(_apiUri, postData, ApiCookies)
            Return postresponse
        End Function

        ''' <summary>
        ''' Envía una solicitud GET a la api con los datos solicitados (luego del "?")
        ''' </summary>
        ''' <param name="getData"></param>
        ''' <returns></returns>
        Public Function Getquery(ByVal getData As String) As String
            Dim turi As Uri = New Uri(_apiUri.OriginalString & "?" & getData)
            Dim getresponse As String = GetDataAndResult(turi, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal urlString As String) As String
            If String.IsNullOrWhiteSpace(urlString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(New Uri(urlString), ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal pageUri As Uri) As String
            If String.IsNullOrWhiteSpace(pageUri.ToString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(pageUri, ApiCookies)
            Return getresponse
        End Function


        ''' <summary>
        ''' Envía una solicitud POST a la uri indicada
        ''' </summary>
        ''' <param name="pageUri">Recurso al cual enviar la solicitud</param>
        ''' <param name="postData">Datos en la solicitud POST como application/x-www-form-urlencoded</param>
        ''' <returns></returns>
        Public Overloads Function POST(ByVal pageUri As Uri, ByVal postData As String) As String
            If pageUri Is Nothing Then
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
            ApiCookies = New CookieContainer
            Return True
        End Function

        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        Public Function GetDataAndResult(ByVal pageUri As Uri) As String
            Return GetDataAndResult(pageUri, New CookieContainer)
        End Function

        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageUri">URL absoluta del recurso web.</param>
        ''' <param name="Cookies">Cookies sobre los que se trabaja.</param>
        Public Function GetDataAndResult(ByVal pageUri As Uri, ByRef cookies As CookieContainer) As String
            Dim tryCount As Integer = 0

            Do Until tryCount = MaxRetry
                Try
                    If cookies Is Nothing Then
                        cookies = New CookieContainer
                    End If
                    Dim tempcookies As CookieContainer = cookies
                    Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageUri), HttpWebRequest)
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
                    Utils.EventLogger.Debug_Log(ex.StackTrace, ex.Source)
                    tryCount += 1
                End Try
            Loop
            Throw New MaxRetriesExeption
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        Public Function PostDataAndGetResult(pageUri As Uri, postData As String) As String
            Return PostDataAndGetResult(pageUri, postData, New CookieContainer)
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageUri">URL absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        Public Function PostDataAndGetResult(pageUri As Uri, postData As String, ByRef cookies As CookieContainer) As String

            If pageUri Is Nothing Then
                Throw New ArgumentNullException("pageUri", "Empty uri.")
            End If

            If cookies Is Nothing Then
                cookies = New CookieContainer
            End If

            Dim tempcookies As CookieContainer = cookies

            Dim encoding As New Text.UTF8Encoding
            Dim byteData As Byte() = encoding.GetBytes(postData)
            Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageUri), HttpWebRequest)

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
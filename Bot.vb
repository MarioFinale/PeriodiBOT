Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions

Namespace WikiBot
    Public Class Bot

        Private BotCookies As CookieContainer
        Private _userName As String = String.Empty

        Private _botusername As String = String.Empty
        Private _botpass As String = String.Empty
        Private _siteurl As String = String.Empty

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

        Public Function POSTQUERY(ByVal postdata As String) As String
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, BotCookies)
            Return postresponse
        End Function

        Public Function GETQUERY(ByVal getdata As String) As String
            Dim getresponse As String = GetDataAndResult(_siteurl & getdata, True, BotCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal urlstring As String) As String
            If String.IsNullOrWhiteSpace(urlstring) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(urlstring, True, BotCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal url As Uri) As String
            If String.IsNullOrWhiteSpace(url.ToString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(url.ToString, True, BotCookies)
            Return getresponse
        End Function

        Public Overloads Function POST(ByVal urlstring As String, ByVal postdata As String) As String
            If String.IsNullOrWhiteSpace(urlstring) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postdata) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(urlstring, postdata, True, BotCookies)
            Return postresponse
        End Function

        Public Overloads Function POST(ByVal url As Uri, ByVal postdata As String) As String
            If String.IsNullOrWhiteSpace(url.ToString) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postdata) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, BotCookies)
            Return postresponse
        End Function


        ''' <summary>
        ''' Inicializa una nueva instancia del BOT.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="PageURL">Nombre exacto de la página</param>
        Sub New(ByVal botUserName As String, botPassword As String, pageURL As Uri)
            If String.IsNullOrWhiteSpace(botUserName) Then
                Throw New ArgumentException("No username")
            End If
            If String.IsNullOrWhiteSpace(botPassword) Then
                Throw New ArgumentException("No BotPassword")
            End If
            If String.IsNullOrWhiteSpace(pageURL.ToString) Then
                Throw New ArgumentException("No PageURL")
            End If

            _botusername = botUserName
            _botpass = botPassword
            _siteurl = pageURL.ToString
            BotCookies = New CookieContainer
            WikiLogOn()
            _userName = botUserName.Split("@"c)(0).Trim()
        End Sub

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
            BotCookies = New CookieContainer
            WikiLogOn()
            _userName = botUserName.Split("@"c)(0).Trim()
        End Sub

        Function IsBot() As Boolean
            Dim postdata As String = "action=query&assert=bot&format=json"
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, BotCookies)
            If postresponse.Contains("assertbotfailed") Then
                Return False
            Else
                Return True
            End If
        End Function

        Function IsLoggedIn() As Boolean
            Dim postdata As String = "action=query&assert=user&format=json"
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, BotCookies)
            If postresponse.Contains("assertuserfailed") Then
                Return False
            Else
                Return True
            End If
        End Function

        ''' <summary>
        ''' Limpia todas las cookies, retorna "true" si finaliza correctamente.
        ''' </summary>
        Function CleanCookies() As Boolean
            Try
                BotCookies = New CookieContainer
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function


        ''' <summary>
        ''' Obtiene un Token y cookies de ingreso, establece las cookies de la clase y retorna el token como string.
        ''' </summary>
        Private Function GetWikiToken() As String
            Log("Obtaining token...", "LOCAL", BOTName)
            Dim postdata As String = "action=query&meta=tokens&type=login&format=json"
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, BotCookies)
            Dim token As String = TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Log("Token obtained!", "LOCAL", BOTName)
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Function WikiLogOn() As String
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
                    postresponse = PostDataAndGetResult(url, postdata, True, BotCookies)
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

    End Class






End Namespace
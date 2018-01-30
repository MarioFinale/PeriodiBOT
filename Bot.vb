Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions

Namespace WikiBot
    Public Class Bot

        Public BotCookies As CookieContainer
        Private Username As String = String.Empty
        Private _botusername As String = String.Empty
        Private _botpass As String = String.Empty
        Private _siteurl As String = String.Empty


        Public ReadOnly Property BotFlag As Boolean
            Get
                Return HasBotFlag()
            End Get
        End Property


        ''' <summary>
        ''' Inicializa una nueva instancia del BOT.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="PageURL">Nombre exacto de la página</param>
        Sub New(ByVal BotUsername As String, BotPassword As String, PageURL As String)
            _botusername = BotUsername
            _botpass = BotPassword
            _siteurl = PageURL
            BotCookies = New CookieContainer
            WikiLogOn()
            Username = BotUsername.Split("@"c)(0).Trim()
        End Sub

        Function HasBotFlag() As Boolean
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
        ''' Retorna un elemento Page coincidente al nombre entregado como parámetro.
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la página</param>
        Function Getpage(ByVal PageName As String) As Page
            Return New Page(PageName, _siteurl, Me, Username)
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


        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y realiza un archivado siguiendo una lógica similar a la de Grillitus.
        ''' </summary>
        ''' <param name="PageToArchive">Página a archivar</param>
        ''' <returns></returns>
        Function Archive(ByVal PageToArchive As Page) As Boolean
            Dim ArchiveFcn As New GrillitusArchive(Me)
            Return ArchiveFcn.Archive(PageToArchive)
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
        ''' </summary>
        ''' <returns></returns>
        Function ArchiveAllInclusions(ByVal IRC As Boolean) As Boolean
            Dim Archive As New GrillitusArchive(Me)
            Return Archive.ArchiveAllInclusions(IRC)
        End Function

    End Class






End Namespace
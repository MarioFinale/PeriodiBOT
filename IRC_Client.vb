Option Strict On
Imports System.IO
Imports System.Net.Sockets
Imports System.Threading
Imports PeriodiBOT_IRC.IRC_Comands
Public Class IRC_Client
    Private _sServer As String = String.Empty 'Server
    Private _sChannel As String = String.Empty 'channel
    Private _sNickName As String = String.Empty 'nickname
    Private _sPass As String = String.Empty 'irc password for nickserv auth
    Private _lPort As Int32 = 6667 'port 6667 is default
    Private _bInvisible As Boolean = False 'invisible
    Private _sRealName As String = String.Empty 'realname
    Private _sUserName As String = String.Empty 'Unique irc name

    Private _tcpclientConnection As TcpClient = Nothing 'IRC network TCPclient.
    Private _networkStream As NetworkStream = Nothing 'break that connection down to a network stream.
    Private _streamWriter As StreamWriter = Nothing 'to provide a convenient access to writing commands.
    Private _streamReader As StreamReader = Nothing 'to provide a convenient access to reading commands.

    Private Command As New IRC_Comands

    Private lastmessage As DateTime

    Public Sub New(ByVal server As String, ByVal channel As String, ByVal nickname As String, ByVal port As Int32,
                          ByVal invisible As Boolean, ByVal pass As String, ByVal realname As String, ByVal username As String)
        Initialize(server, channel, nickname, port, invisible, pass, realname, username)
    End Sub

    Public Sub New(ByVal server As String, ByVal channel As String, ByVal nickname As String, ByVal port As Int32,
                          ByVal invisible As Boolean, ByVal pass As String)
        Initialize(server, channel, nickname, port, invisible, pass, nickname, nickname)
    End Sub

    Public Sub New(ByVal server As String, ByVal channel As String, ByVal nickname As String, ByVal port As Int32,
                          ByVal invisible As Boolean)
        Initialize(server, channel, nickname, port, invisible, String.Empty, nickname, nickname)
    End Sub

    Public Sub Initialize(ByVal server As String, ByVal channel As String, ByVal nickname As String, ByVal port As Int32,
                          ByVal invisible As Boolean, ByVal pass As String, ByVal realname As String, ByVal username As String)

        _sServer = server
        _sChannel = channel

        If Not String.IsNullOrEmpty(username) Then
            _sUserName = username
        Else
            _sUserName = nickname
        End If
        If Not String.IsNullOrEmpty(realname) Then
            _sRealName = realname
        Else
            _sRealName = nickname
        End If

        _sNickName = nickname
        _sPass = pass
        _lPort = port
        _bInvisible = invisible
    End Sub


    Public Async Sub Connect()

        Log("Starting IRCclient", "IRC", _sNickName)
        Dim sIsInvisible As String = String.Empty
        Dim sCommand As String = String.Empty 'commands to process from the room.
        Dim HasExited As Boolean = False

        Dim Lastdate As DateTime = DateTime.Now

        Do Until HasExited

            Try
                'Start the main connection to the IRC server.
                Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & "**Creating Connection**")
                _tcpclientConnection = New TcpClient(_sServer, _lPort)
                With _tcpclientConnection
                    .ReceiveTimeout = 300000
                    .SendTimeout = 300000
                End With

                _networkStream = _tcpclientConnection.GetStream
                _streamReader = New StreamReader(_networkStream)
                _streamWriter = New StreamWriter(_networkStream)

                'If is invisible then...
                If _bInvisible Then
                    sIsInvisible = "8"
                Else
                    sIsInvisible = "0"
                End If

                'Attempt nickserv auth (freenode server pass method)
                If Not String.IsNullOrEmpty(_sPass) Then
                    Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & "**Attempting nickserv auth**")
                    _streamWriter.WriteLine(String.Format("PASS {0}:{1}", _sNickName, _sPass))
                    _streamWriter.Flush()
                End If

                'Create nickname.
                Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & "**Setting Nickname**")
                _streamWriter.WriteLine(String.Format(String.Format("NICK {0}", _sNickName)))
                _streamWriter.Flush()

                'Send in information
                Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & "**Setting up name**")
                _streamWriter.WriteLine(String.Format("USER {0} {1} * :{2}", _sUserName, sIsInvisible, _sRealName))
                _streamWriter.Flush()

                'Connect to a specific room.
                Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & "**Joining Room**")
                _streamWriter.WriteLine(String.Format("JOIN {0}", _sChannel))
                _streamWriter.Flush()

                Dim CheckUsersFunc As New Func(Of String())(AddressOf CheckUsers)
                Dim CheckUsersIRCTask As New IRCTask(Me, 300000, CheckUsersFunc)
                CheckUsersIRCTask.Run()

                Await Task.Run(Sub()

                                   Try
                                       While True

                                           sCommand = _streamReader.ReadLine
                                           lastmessage = DateTime.Now
                                           Dim sCommandParts As String() = sCommand.Split(CType(" ", Char()))
                                           Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & sCommand)
                                           Dim response As String = Command.ResolveCommand(sCommand, HasExited, _sNickName)

                                           If Not response Is Nothing Then
                                               _streamWriter.WriteLine(response)
                                               _streamWriter.Flush()
                                               Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & response)

                                           End If

                                           If Not _tcpclientConnection.Connected Then
                                               Debug_Log("IRC: DISCONNECTED", "IRC", BOTName)
                                               Exit While
                                           End If

                                           If sCommandParts(0).Contains("PING") Then  'Ping response
                                               _streamWriter.WriteLine(sCommand.Replace("PING", "PONG"))
                                               Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & sCommand.Replace("PING", "PONG"))
                                               _streamWriter.Flush()
                                           End If


                                           If HasExited Then
                                               Exit While
                                           End If

                                       End While

                                   Catch IOEX As System.IO.IOException
                                       Log("IRC: Error Connecting: " + IOEX.Message, "IRC", _sNickName)
                                   Catch OtherEx As Exception
                                       Log("IRC: Error Connecting: " + OtherEx.Message, "IRC", _sNickName)
                                   End Try
                               End Sub)

            Catch ex As SocketException

                'No connection, catch and retry
                Debug_Log("IRC: Error Connecting: " + ex.Message, "IRC", _sNickName)
                Try
                    'close connections
                    _streamReader.Dispose()
                    _streamWriter.Dispose()
                    _networkStream.Dispose()
                Catch exex As Exception
                End Try
            Catch ex As Exception

                'In case of something goes wrong
                Debug_Log("IRC: Error: " + ex.Message, "IRC", _sNickName)
                Try
                    _streamWriter.WriteLine("QUIT :FATAL ERROR.")
                    _streamWriter.Flush()
                    'close connections
                    _streamReader.Dispose()
                    _streamWriter.Dispose()
                    _networkStream.Dispose()
                Catch ex2 As Exception
                    'In case of something really bad happens
                    Debug_Log("IRC: Error ex2: " + ex2.Message, "IRC", BOTName)
                End Try

            End Try
            If HasExited Then
                ExitProgram()
            End If

            Log("Lost connection, retrying on 5 seconds...", "IRC", _sNickName)
            System.Threading.Thread.Sleep(5000)
        Loop

    End Sub

   
    Function Sendmessage(ByVal message As String, ByVal Channel As String) As Boolean
        _streamWriter.WriteLine(String.Format("PRIVMSG {0} : {1}", _sChannel, message))
        _streamWriter.Flush()
        Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & String.Format("PRIVMSG {0} : {1}", Channel, message))
        Return True
    End Function

    Function Sendmessage(ByVal message As String) As Boolean
        _streamWriter.WriteLine(String.Format("PRIVMSG {0} : {1}", _sChannel, message))
        _streamWriter.Flush()
        Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & String.Format("PRIVMSG {0} : {1}", _sChannel, message))
        Return True
    End Function

    Function SendText(ByVal Text As String) As Boolean
        _streamWriter.WriteLine(Text)
        _streamWriter.Flush()
        Console.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & " | " & Text)
        Return True
    End Function



End Class
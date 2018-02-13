Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net.Sockets
Imports System.Threading
Imports PeriodiBOT_IRC.IRC_Comands
Public Class IRC_Client
    Private _sServer As String = String.Empty 'Server
    Private _sChannel As String = String.Empty 'canal
    Private _sNickName As String = String.Empty 'nickname
    Private _sPass As String = String.Empty 'contrasena de irc para nickserv auth
    Private _lPort As Int32 = 6667 'puerto 6667 por defecto
    Private _bInvisible As Boolean = False 'invisible
    Private _sRealName As String = String.Empty 'realname
    Private _sUserName As String = String.Empty 'nombre irc unico

    Private _tcpclientConnection As TcpClient = Nothing 'IRC network TCPclient.
    Private _networkStream As NetworkStream = Nothing 'conexion a un network stream.
    Private _streamWriter As StreamWriter = Nothing 'escribir en el stream.
    Private _streamReader As StreamReader = Nothing 'leer desde el stream.

    Private Command As New IRC_Comands

    Private lastmessage As New IRCMessage("", {""})

    Private HasExited As Boolean = False

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
        Dim sCommand As String = String.Empty 'linea recibida
        Dim Lastdate As DateTime = DateTime.Now


        Do Until HasExited

            Try
                'Start the main connection to the IRC server.
                WriteLine("INFO", "IRC", "**Creating Connection**")
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
                    WriteLine("INFO", "IRC", "**Attempting nickserv auth**")
                    _streamWriter.WriteLine(String.Format("PASS {0}:{1}", _sNickName, _sPass))
                    _streamWriter.Flush()
                End If

                'Create nickname.
                WriteLine("INFO", "IRC", "**Setting Nickname**")
                _streamWriter.WriteLine(String.Format(String.Format("NICK {0}", _sNickName)))
                _streamWriter.Flush()

                'Send in information
                WriteLine("INFO", "IRC", "**Setting up name**")
                _streamWriter.WriteLine(String.Format("USER {0} {1} * :{2}", _sUserName, sIsInvisible, _sRealName))
                _streamWriter.Flush()

                'Connect to a specific room.
                WriteLine("INFO", "IRC", "**Joining Room**")
                _streamWriter.WriteLine(String.Format("JOIN {0}", _sChannel))
                _streamWriter.Flush()


                Await Task.Run(Sub()

                                   Try
                                       While True

                                           sCommand = _streamReader.ReadLine
                                           Dim sCommandParts As String() = sCommand.Split(CType(" ", Char()))

                                           Dim CommandFunc As New Func(Of IRCMessage())(Function()
                                                                                            Return {Command.ResolveCommand(sCommand, HasExited, _sNickName, Me, Mainwikibot)}
                                                                                        End Function)
                                           Dim IRCResponseTask As New IRCTask(Me, 0, False, CommandFunc, "ResolveCommand")

                                           Debug_Log("Run irc response", "LOCAL", BOTName)
                                           IRCResponseTask.Run()


                                           If Not _tcpclientConnection.Connected Then
                                               Debug_Log("IRC: DISCONNECTED", "IRC", BOTName)
                                               Exit While
                                           End If

                                           If sCommandParts(0).Contains("PING") Then  'Ping response
                                               _streamWriter.WriteLine(sCommand.Replace("PING", "PONG"))
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
        _streamWriter.WriteLine(String.Format("PRIVMSG {0} : {1}", Channel, message))
        _streamWriter.Flush()
        WriteLine("MSG", "IRC", Channel & " " & _sNickName & ": " & message)
        Return True
    End Function

    Sub Quit(ByVal message As String)
        HasExited = True
        SendText("QUIT: " & message)
        _streamReader.Dispose()
        _streamWriter.Dispose()
        _networkStream.Dispose()
    End Sub


    Function Sendmessage(ByVal message As IRCMessage) As Boolean
        SyncLock (lastmessage)
            If message.Text(0) = lastmessage.Text(0) Then
                Return False
            End If
        End SyncLock

        For Each s As String In message.Text
            _streamWriter.WriteLine(String.Format("{2} {0} : {1}", message.Source, s, message.Command))
            _streamWriter.Flush()
            WriteLine("MSG", "IRC", message.Source & " " & _sNickName & ": " & s)
        Next
        lastmessage = message
        Return True
    End Function

    Function Sendmessage(ByVal message As String) As Boolean
        _streamWriter.WriteLine(String.Format("PRIVMSG {0} : {1}", _sChannel, message))
        _streamWriter.Flush()
        WriteLine("MSG", "IRC", _sChannel & " " & _sNickName & ": " & message)
        Return True
    End Function

    Function SendText(ByVal Text As String) As Boolean
        _streamWriter.WriteLine(Text)
        _streamWriter.Flush()
        WriteLine("RAW TEXT", "IRC", Text)
        Return True
    End Function



End Class
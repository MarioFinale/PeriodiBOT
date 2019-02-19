Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net.Sockets
Imports MWBot.net
Imports MWBot.net.GlobalVars
Imports MWBot.net.My.Resources
Imports PeriodiBOT_IRC.Initializer

Namespace IRC
    Public Class IRC_Client

        Property HasExited As Boolean = False
        Property FloodDelay As Integer = 700
        Property ReconTime As Integer = 5

        Private _sServer As String = String.Empty
        Private _sChannels As String()
        Private _sNickName As String = String.Empty
        Private _sPass As String = String.Empty
        Private _lPort As Int32 = 6667
        Private _bInvisible As Boolean = False
        Private _sRealName As String = String.Empty
        Private _sUserName As String = String.Empty

        Private _tcpClient As TcpClient = Nothing
        Private _networkStream As NetworkStream = Nothing
        Private _streamWriter As StreamWriter = Nothing
        Private _streamReader As StreamReader = Nothing
        Private _opFile As ConfigFile
        Private _configFile As ConfigFile
        Private Commands As New IRCCommandResolver

        Private lastmessage As New IRCMessage("", {""})

        Private _workerbot As WikiBot.Bot


#Region "Properties"
        Public ReadOnly Property NickName As String
            Get
                Return _sNickName
            End Get
        End Property

        Public ReadOnly Property Server As String
            Get
                Return _sServer
            End Get
        End Property
#End Region

        Public Sub New(ByVal Cfile As ConfigFile, ByVal port As Int32, ByVal opFile As ConfigFile)
            Initialize(Cfile, port, opFile)
        End Sub

        Public Sub New(ByVal Cfile As ConfigFile, ByVal port As Int32, ByVal opFile As ConfigFile, ByRef bot As WikiBot.Bot)
            _workerbot = bot
            Initialize(Cfile, port, opFile)
        End Sub

        Public Sub Initialize(ByVal Cfile As ConfigFile, ByVal port As Int32, ByVal opFile As ConfigFile)
            Dim tserver As String
            Dim tname As String
            Dim tpass As String
            Dim tchannels As String()

            If Not Cfile.Params.Count = 4 Then
                IO.File.Delete(Cfile.Path)
                Console.Clear()
                Console.WriteLine(BotMessages.NoIrcConfigFile)
                Console.WriteLine(BotMessages.NewIrcNetworkAdress)
                tserver = Console.ReadLine
                Console.WriteLine(BotMessages.NewIrcNetworkNickName)
                tname = Console.ReadLine
                Console.WriteLine(BotMessages.NewIrcNetworkPass)
                tpass = Console.ReadLine
                Console.WriteLine(BotMessages.NewIrcNetworkChannels)
                tchannels = Console.ReadLine.Split("|"c)
                IO.File.AppendAllLines(Cfile.Path, {tserver, tname, tpass, String.Join("|"c, tchannels)})
            Else
                tserver = Cfile.Params(0)
                tname = Cfile.Params(1)
                tpass = Cfile.Params(2)
                tchannels = Cfile.Params(3).Trim.Split("|"c)
            End If
            _configFile = Cfile
            _opFile = opFile
            LoadConfig()

            _sServer = tserver
            _sChannels = tchannels
            _sUserName = tname
            _sRealName = tname
            _sNickName = tname
            _sPass = tpass
            _lPort = port
            _bInvisible = False
        End Sub


        Public Async Sub StartClient()
            Utils.EventLogger.Log(BotMessages.StartingIRCClient, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
            Dim sIsInvisible As String = String.Empty
            Dim sCommand As String = String.Empty 'linea recibida
            Dim Lastdate As DateTime = DateTime.Now

            Dim MsgQueue As New Queue(Of Tuple(Of String, String, IRC_Client, WikiBot.Bot))
            Dim ResolveMessages As New Func(Of Boolean)(Function()
                                                            Return SendMessagequeue(MsgQueue)
                                                        End Function)
            TaskAdm.NewTask("IRC client message resolver", BotCodename, ResolveMessages, 10, True, True)

            Do Until HasExited
                Try
                    'Start the main connection to the IRC server.
                    Utils.EventLogger.Log(BotMessages.CreatingConnection, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                    _tcpClient = New TcpClient(_sServer, _lPort)
                    With _tcpClient
                        .ReceiveTimeout = 300000
                        .SendTimeout = 300000
                    End With

                    _networkStream = _tcpClient.GetStream
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
                        Utils.EventLogger.Log(BotMessages.NickervAuth, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                        _streamWriter.WriteLine(String.Format("PASS {0}:{1}", _sNickName, _sPass))
                        _streamWriter.Flush()
                    End If

                    'Create nickname.
                    Utils.EventLogger.Log(BotMessages.SetNick, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                    _streamWriter.WriteLine(String.Format(String.Format("NICK {0}", _sNickName)))
                    _streamWriter.Flush()

                    'Set name and status
                    Utils.EventLogger.Log(BotMessages.SetName, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                    _streamWriter.WriteLine(String.Format("USER {0} {1} * :{2}", _sUserName, sIsInvisible, _sRealName))
                    _streamWriter.Flush()

                    'Connect to the channels.
                    For Each chan As String In _sChannels
                        Utils.EventLogger.Log(String.Format(BotMessages.JoiningChannel, chan), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                        _streamWriter.WriteLine(String.Format("JOIN {0}", chan))
                        _streamWriter.Flush()
                    Next

                    Await Task.Run(Sub()

                                       Try
                                           While True

                                               sCommand = _streamReader.ReadLine


                                               Dim sCommandParts As String() = sCommand.Split(CType(" ", Char()))

                                               SyncLock (MsgQueue)
                                                   MsgQueue.Enqueue(New Tuple(Of String, String, IRC_Client, WikiBot.Bot)(sCommand, _sNickName, Me, _workerbot))
                                               End SyncLock

                                               If Not _tcpClient.Connected Then
                                                   Utils.EventLogger.Debug_Log(BotMessages.NotConnected, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
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
                                           Utils.EventLogger.Log(String.Format(BotMessages.ConnectionError, IOEX.Message), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                                       Catch OtherEx As Exception
                                           Utils.EventLogger.Log(String.Format(BotMessages.UnexpectedEX, OtherEx.Message), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                                       End Try

                                   End Sub)

                Catch ex As SocketException
                    'No connection, catch and retry
                    Utils.EventLogger.EX_Log(String.Format(BotMessages.ConnectionError, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                    Try
                        'close connections
                        _streamReader.Dispose()
                        _streamWriter.Dispose()
                        _networkStream.Dispose()
                    Catch exex As Exception
                    End Try
                Catch ex As Exception
                    'In case of something goes wrong
                    Utils.EventLogger.Log(String.Format(BotMessages.UnexpectedEX, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                    Try
                        _streamWriter.WriteLine("QUIT :FATAL ERROR.")
                        _streamWriter.Flush()
                        'close connections
                        _streamReader.Dispose()
                        _streamWriter.Dispose()
                        _networkStream.Dispose()
                    Catch ex2 As Exception
                        'In case of something really bad happens
                        Utils.EventLogger.Log(String.Format(BotMessages.UnexpectedEX, ex2.Message), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                    End Try
                End Try
                If HasExited Then
                    Utils.ExitProgram()
                End If
                Utils.EventLogger.Log(String.Format(BotMessages.LostConnectionRET, ReconTime), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                System.Threading.Thread.Sleep(ReconTime * 1000)
            Loop

        End Sub

        Function Sendmessage(ByVal message As String, ByVal channel As String) As Boolean
            _streamWriter.WriteLine(String.Format("PRIVMSG {0} : {1}", channel, message))
            _streamWriter.Flush()
            Utils.WriteLine("MSG", "IRC", channel & " " & _sNickName & ": " & message)
            Return True
        End Function

        Sub Quit(ByVal message As String)
            SendText("QUIT :" & message)
            HasExited = True
            _streamReader.Dispose()
            _streamWriter.Dispose()
            _networkStream.Dispose()
        End Sub

        Function Sendmessage(ByVal message As IRCMessage) As Boolean
            If message Is Nothing Then
                Throw New ArgumentException("No message")
            End If
            SyncLock (lastmessage)
                If message.Text(0) = lastmessage.Text(0) Then
                    Return False
                End If
            End SyncLock
            For Each s As String In message.Text
                _streamWriter.WriteLine(String.Format("{2} {0} : {1}", message.Source, s, message.Command))
                _streamWriter.Flush()
                Utils.WriteLine("MSG", "IRC", message.Source & " " & _sNickName & ": " & s)
                Threading.Thread.Sleep(FloodDelay) 'Prevent flooding
            Next
            lastmessage = message
            Return True
        End Function

        Function Sendmessage(ByVal message As IRCMessage()) As Boolean
            If message Is Nothing Then Return False
            For Each s As IRCMessage In message
                If Not String.IsNullOrEmpty(s.Text(0)) Then
                    Sendmessage(s)
                    Threading.Thread.Sleep(FloodDelay) 'Prevent flooding
                End If
            Next
            Return True
        End Function

        Function SendText(ByVal text As String) As Boolean
            _streamWriter.WriteLine(text)
            _streamWriter.Flush()
            Utils.WriteLine("RAW TEXT", "IRC", text)
            Return True
        End Function


        Private OPlist As List(Of String)

        Sub LoadConfig()
            OPlist = New List(Of String)
            If File.Exists(_opFile.Path) Then
                Utils.EventLogger.Log(BotMessages.LoadingOPs, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                Dim opstr As String() = File.ReadAllLines(_opFile.Path)
                Try
                    For Each op As String In opstr
                        OPlist.Add(op)
                    Next
                Catch ex As IndexOutOfRangeException
                    Utils.EventLogger.Log(BotMessages.MalformedOPs, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                End Try
            Else
                Utils.EventLogger.Log(BotMessages.NoOpsFile, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                Try
                    File.Create(_opFile.Path).Close()
                Catch ex As IOException
                    Utils.EventLogger.Log(String.Format(BotMessages.FileCreateErr, _opFile), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                End Try

            End If

            If OPlist.Count = 0 Then
                Utils.EventLogger.Log(BotMessages.NoOp, Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                Console.WriteLine(BotMessages.NewOp)
                Dim MainOp As String = Console.ReadLine
                Try
                    File.WriteAllText(_opFile.Path, MainOp)
                    OPlist.Add(MainOp)
                Catch ex As IOException
                    Utils.EventLogger.Log(String.Format(BotMessages.FileSaveErr, _opFile), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                End Try
            End If

        End Sub

        ''' <summary>
        ''' Verifica un mensaje en IRC y si el que lo envió es un operador, añade un operador a la lista.
        ''' </summary>
        ''' <param name="message">Línea completa del mensaje</param>
        ''' <param name="Source">Origen del mensaje</param>
        ''' <param name="user">Usuario que crea el evento</param>
        ''' <returns></returns>
        Function AddOP(ByVal message As String, ByVal source As String, ByVal user As String) As Boolean
            If message Is Nothing Then Return False
            Dim CommandParts As String() = message.Split(CType(" ", Char()))
            Dim Param As String = CommandParts(4)
            If Not OPlist.Contains(Param) Then
                If IsOp(message, source, user) Then
                    OPlist.Add(Param)
                    Try
                        File.WriteAllLines(_opFile.Path, OPlist.ToArray)
                        Return True
                    Catch ex As IOException
                        Utils.EventLogger.Log(String.Format(BotMessages.FileSaveErr, _opFile), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                        Return False
                    End Try
                Else
                    Return False
                End If
            Else
                Return False
            End If

        End Function

        ''' <summary>
        ''' Verifica un mensaje en IRC y si el que lo envió es un operador elimina el operador indicado en el mensaje.
        ''' </summary>
        ''' <param name="message">Línea completa del mensaje</param>
        ''' <param name="Source">Origen del mensaje</param>
        ''' <param name="user">Usuario que crea el evento</param>
        ''' <returns></returns>
        Function DelOP(ByVal message As String, ByVal source As String, ByVal user As String) As Boolean
            If message Is Nothing Then Return False
            Dim CommandParts As String() = message.Split(CType(" ", Char()))
            Dim Param As String = CommandParts(4)
            If IsOp(message, source, user) Then

                If OPlist.Contains(Param) Then
                    OPlist.Remove(Param)
                    Try
                        File.WriteAllLines(_opFile.Path, OPlist.ToArray)
                        Return True
                    Catch ex As System.IO.IOException
                        Utils.EventLogger.Log(String.Format(BotMessages.FileSaveErr, _opFile), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                        Return False
                    End Try
                Else
                    Return False
                End If
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Verifica un mensaje en IRC y retorna verdadero si el que lo envió es un operador.
        ''' </summary>
        ''' <param name="message">Línea completa del mensaje</param>
        ''' <param name="Source">Origen del mensaje</param>
        ''' <param name="user">Usuario que crea el evento</param>
        ''' <returns></returns>
        Function IsOp(ByVal message As String, source As String, user As String) As Boolean
            If message Is Nothing Then Return False
            Try
                Dim Scommand0 As String = message.Split(" "c)(0)
                Dim Nickname As String = Utils.GetUserFromChatresponse(message)
                Dim Hostname As String = Scommand0.Split(CType("@", Char()))(1)
                Utils.EventLogger.Log(String.Format(BotMessages.CheckOp, Nickname, Hostname), source, user)
                Dim OpString As String = Nickname & "!" & Hostname
                If OPlist.Contains(OpString) Then
                    Utils.EventLogger.Log(String.Format(BotMessages.UserOP, Nickname, Hostname), source, user)
                    Return True
                Else
                    Utils.EventLogger.Log(String.Format(BotMessages.UserNotOp, Nickname, Hostname), source, user)
                    Return False
                End If
            Catch ex As IndexOutOfRangeException
                Utils.EventLogger.Log(String.Format(BotMessages.UnexpectedEX, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name, _sNickName)
                Return False
            End Try
        End Function

        Function SendMessagequeue(ByRef queuedat As Queue(Of Tuple(Of String, String, IRC_Client, WikiBot.Bot))) As Boolean
            If queuedat Is Nothing Then Return False
            If queuedat.Count >= 1 Then
                SyncLock queuedat
                    Dim queuedmsg As Tuple(Of String, String, IRC_Client, WikiBot.Bot) = queuedat.Dequeue()
                    Dim MsgResponse As IRCMessage = Commands.ResolveCommand(queuedmsg.Item1, queuedmsg.Item2, queuedmsg.Item3, queuedmsg.Item4)
                    If Not MsgResponse Is Nothing Then
                        queuedmsg.Item3.Sendmessage(MsgResponse)
                    End If
                End SyncLock
                Return True
            Else
                Return False
            End If
        End Function

    End Class
End Namespace
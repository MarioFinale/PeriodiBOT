Option Strict on
Option Explicit On
Module Config

    Private OPlist As List(Of String)

    ''' <summary>
    ''' Inicializa las configuraciones genereales del programa desde el archivo de configuración.
    ''' Si no existe el archivo, solicita datos al usuario y lo genera.
    ''' </summary>
    ''' <returns></returns>
    Function LoadConfig() As Boolean
        Dim MainBotName As String = String.Empty
        Dim WPSite As String = String.Empty
        Dim WPAPI As String = String.Empty
        Dim WPBotUserName As String = String.Empty
        Dim WPBotPassword As String = String.Empty
        Dim IRCBotNickName As String = String.Empty
        Dim IRCBotPassword As String = String.Empty
        Dim MainIRCNetwork As String = String.Empty
        Dim MainIRCChannel As String = String.Empty
        Dim ConfigOK As Boolean = False

        If System.IO.File.Exists(ConfigFilePath) Then
            Log("Loading config", "LOCAL", "Undefined")
            Dim Configstr As String = System.IO.File.ReadAllText(ConfigFilePath)
            Try
                MainBotName = TextInBetween(Configstr, "BOTName=""", """")(0)
                WPBotUserName = TextInBetween(Configstr, "WPUserName=""", """")(0)
                WPSite = TextInBetween(Configstr, "PageURL=""", """")(0)
                WPBotPassword = TextInBetween(Configstr, "WPBotPassword=""", """")(0)
                WPAPI = TextInBetween(Configstr, "ApiURL=""", """")(0)
                MainIRCNetwork = TextInBetween(Configstr, "IRCNetwork=""", """")(0)
                IRCBotNickName = TextInBetween(Configstr, "IRCBotNickName=""", """")(0)
                IRCBotPassword = TextInBetween(Configstr, "IRCBotPassword=""", """")(0)
                MainIRCChannel = TextInBetween(Configstr, "IRCChannel=""", """")(0)
                ConfigOK = True
            Catch ex As IndexOutOfRangeException
                Log("Malformed config", "LOCAL", "Undefined")
            End Try
        Else
            Log("No config file", "LOCAL", "Undefined")
            Try
                System.IO.File.Create(ConfigFilePath).Close()
            Catch ex As System.IO.IOException
                Log("Error creating new config file", "LOCAL", "Undefined")
            End Try

        End If

        If Not ConfigOK Then
            Console.Clear()
            Console.WriteLine("No config file, please fill the data or close the program and create a config file.")
            Console.WriteLine("Bot Name: ")
            MainBotName = Console.ReadLine
            Console.WriteLine("Wikipedia Username: ")
            WPBotUserName = Console.ReadLine
            Console.WriteLine("Wikipedia bot password: ")
            WPBotPassword = Console.ReadLine
            Console.WriteLine("Wikipedia main URL: ")
            WPSite = Console.ReadLine
            Console.WriteLine("Wikipedia API URL: ")
            WPAPI = Console.ReadLine
            Console.WriteLine("IRC Network: ")
            MainIRCNetwork = Console.ReadLine
            Console.WriteLine("IRC NickName: ")
            IRCBotNickName = Console.ReadLine
            Console.WriteLine("IRC nickserv/server password (press enter if not password is set): ")
            IRCBotPassword = Console.ReadLine
            Console.WriteLine("IRC main Channel: ")
            MainIRCChannel = Console.ReadLine

            Dim configstr As String = String.Format("======================CONFIG======================
BOTName=""{0}""
WPUserName=""{1}""
WPBotPassword=""{2}""
PageURL=""{3}""
ApiURL=""{4}""
IRCNetwork=""{5}""
IRCBotNickName=""{6}""
IRCBotPassword=""{7}""
IRCChannel=""{8}""", MainBotName, WPBotUserName, WPBotPassword, WPSite, WPAPI, MainIRCNetwork, IRCBotNickName, IRCBotPassword, MainIRCChannel)

            Try
                System.IO.File.WriteAllText(ConfigFilePath, configstr)
            Catch ex As System.IO.IOException
                Log("Error saving config file", "LOCAL", "Undefined")
            End Try

        End If
        BOTName = MainBotName
        WPUserName = WPBotUserName
        BOTPassword = WPBotPassword
        site = WPSite
        ApiURL = WPAPI
        IRCNetwork = MainIRCNetwork
        BOTIRCName = IRCBotNickName
        IRCPassword = IRCBotPassword
        IRCChannel = MainIRCChannel

        OPlist = New List(Of String)
        If System.IO.File.Exists(OpFilePath) Then
            Log("Loading operators", "LOCAL", BOTName)
            Dim opstr As String() = System.IO.File.ReadAllLines(OpFilePath)
            Try
                For Each op As String In opstr
                    OPlist.Add(op)
                Next
            Catch ex As IndexOutOfRangeException
                Log("Malformed OpList", "LOCAL", BOTName)
            End Try
        Else
            Log("No Ops file", "LOCAL", BOTName)
            Try
                System.IO.File.Create(OpFilePath).Close()
            Catch ex As System.IO.IOException
                Log("Error creating ops file", "LOCAL", BOTName)
            End Try

        End If

        If OPlist.Count = 0 Then
            Log("Warning: No Ops defined!", "LOCAL", BOTName)
            Console.WriteLine("IRC OP (Nickname!hostname): ")
            Dim MainOp As String = Console.ReadLine
            Try
                System.IO.File.WriteAllText(OpFilePath, MainOp)
            Catch ex As System.IO.IOException
                Log("Error saving ops file", "LOCAL", BOTName)
            End Try
        End If

        Return True
    End Function

    ''' <summary>
    ''' Verifica un mensaje en IRC y si el que lo envió es un operador, añade un operador a la lista.
    ''' </summary>
    ''' <param name="message">Línea completa del mensaje</param>
    ''' <param name="Source">Origen del mensaje</param>
    ''' <param name="user">Usuario que crea el evento</param>
    ''' <returns></returns>
    Function AddOP(ByVal message As String, ByVal Source As String, ByVal user As String) As Boolean
        Dim CommandParts As String() = message.Split(CType(" ", Char()))
        Dim Param As String = CommandParts(4)
        If Not OPlist.Contains(Param) Then
            If IsOp(message, Source, user) Then
                OPlist.Add(Param)
                Try
                    System.IO.File.WriteAllLines(OpFilePath, OPlist.ToArray)
                    Return True
                Catch ex As System.IO.IOException
                    Log("Error saving ops file", "LOCAL", BOTName)
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
    Function DelOP(ByVal message As String, ByVal Source As String, ByVal user As String) As Boolean
        Dim CommandParts As String() = message.Split(CType(" ", Char()))
        Dim Param As String = CommandParts(4)

        If IsOp(message, Source, user) Then

            If OPlist.Contains(Param) Then
                OPlist.Remove(Param)
                Try
                    System.IO.File.WriteAllLines(OpFilePath, OPlist.ToArray)
                    Return True
                Catch ex As System.IO.IOException
                    Log("Error saving ops file", "LOCAL", BOTName)
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
    Function IsOp(ByVal message As String, Source As String, user As String) As Boolean
        Try
            Dim Scommand0 As String = message.Split(CType(" ", Char()))(0)
            Dim Nickname As String = GetUserFromChatresponse(message)
            Dim Hostname As String = Scommand0.Split(CType("@", Char()))(1)

            Log(String.Format("Checking if user {0} on host {1} is OP", Nickname, Hostname), Source, user)

            Dim OpString As String = Nickname & "!" & Hostname
            If OPlist.Contains(OpString) Then
                Log(String.Format("User {0} on host {1} is OP", Nickname, Hostname), Source, user)
                Return True
            Else
                Log(String.Format("User {0} on host {1} is not OP", Nickname, Hostname), Source, user)
                Return False
            End If
        Catch ex As IndexOutOfRangeException
            Log("EX Checking if user is OP : " & ex.Message, Source, user)
            Return False
        End Try
    End Function


End Module

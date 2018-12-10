Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.Utils

Module GlobalVars
    Public Const BotCodename As String = "PeriodiBOT"
    Public Exepath As String = AppDomain.CurrentDomain.BaseDirectory
    Public DirSeparator As String = IO.Path.DirectorySeparatorChar
    ''' <summary>
    ''' El separador de decimales varia segun SO y configuracion regional, eso puede afectar los calculos.
    ''' </summary>
    Public DecimalSeparator As String = String.Format(CType(1.1, String)).Substring(1, 1)

    Public OS As String = My.Computer.Info.OSFullName & " " & My.Computer.Info.OSVersion
    Public Version As String = Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString
    Public User_Filepath As String = Exepath & "Users.psv"
    Public Log_Filepath As String = Exepath & "Log.psv"
    Public ConfigFilePath As String = Exepath & "Config.cfg"
    Public IrcOpPath As String = Exepath & "OPs.cfg"
    Public SettingsPath As String = Exepath & "Settings.psv"
    Public Const MaxRetry As Integer = 3
    Public Uptime As DateTime
    Public ESWikiBOT As Bot
    'Public WikidataBOT As Bot
    Public BotIRC As IRC_Client

End Module

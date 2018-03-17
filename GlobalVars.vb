Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC
Module GlobalVars


    Public BOTName As String
    Public WPUserName As String
    Public BOTPassword As String
    Public site As String
    Public ApiURL As String
    Public IRCNetwork As String
    Public BOTIRCName As String
    Public IRCPassword As String
    Public IRCChannel As String
    Public Exepath As String = AppDomain.CurrentDomain.BaseDirectory

    ''' <summary>
    ''' El separador de decimales varia segun SO y configuracion regional, eso puede afectar los calculos.
    ''' </summary>
    Public DecimalSeparator As String = String.Format(CType(1.1, String)).Substring(1, 1)

    Public OS As String = My.Computer.Info.OSFullName & " Version:" & My.Computer.Info.OSVersion
    Public Version As String = Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString
    Public User_Filepath As String = Exepath & "Users.psv"
    Public Log_Filepath As String = Exepath & "Log.psv"
    Public ConfigFilePath As String = Exepath & "Config.cfg"
    Public OpFilePath As String = Exepath & "OPs.cfg"

    Public Const ResumePageName As String = "Usuario:PeriodiBOT/Resumen página"
    Public Const DidYouKnowPageName As String = "PeriodiBOT/Sabias que"
    Public Const TopicPageName As String = "Wikipedia:Café por tema"
    Public Const TopicTemplate As String = "Plantilla:Tema"
    Public Const TopicGroupsPage As String = "Wikipedia:Café por tema/Grupos"
    Public Const MaxRetry As Integer = 3

    Public LogC As New LogEngine(Log_Filepath, User_Filepath)
    Public Userdata As List(Of String()) = LogC.LogUserData
    Public Uptime As DateTime


    Public ESWikiBOT As Bot
    'Public WikidataBOT As Bot
    Public BotIRC As IRC_Client


End Module

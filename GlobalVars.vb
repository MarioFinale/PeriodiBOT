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

    Public OS As String = My.Computer.Info.OSFullName & " Version:" & My.Computer.Info.OSVersion
    Public Version As String = Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString
    Public User_Filepath As String = Exepath & "Users.psv"
    Public Log_Filepath As String = Exepath & "Log.psv"
    Public ConfigFilePath As String = Exepath & "Config.cfg"
    Public IrcOpPath As String = Exepath & "OPs.cfg"
    Public SettingsPath As String = Exepath & "Settings.psv"

    Public Const ResumePageName As String = "Usuario:PeriodiBOT/Resumen página"
    Public Const DidYouKnowPageName As String = "PeriodiBOT/Sabias que"
    Public Const TopicPageName As String = "Wikipedia:Café por tema"
    Public Const TopicTemplate As String = "Plantilla:Tema"
    Public Const TopicGroupsPage As String = "Wikipedia:Café por tema/Grupos"
    Public Const InformalMediationMembers As String = "Wikipedia:Mediación informal/Participantes/Lista"
    Public Const InfMedPage As String = "Wikipedia:Mediación informal/Solicitudes"
    Public Const MaxRetry As Integer = 3
    Public Uptime As DateTime
    Public ESWikiBOT As Bot
    'Public WikidataBOT As Bot
    Public BotIRC As IRC_Client

    Public InfMedMessage As String = "¡Hola! Este mensaje es un aviso automático a todos los miembros activos de [[Wikipedia:Mediación informal/Participantes/Lista|Mediación informal]] para informar de una [[Wikipedia:Mediación informal/Solicitudes|nueva solicitud]]. Por favor, considera participar en la discusión.

¡Gracias por tu atención! ~~~~"






End Module

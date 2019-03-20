Option Strict On
Option Explicit On
Imports Utils.Utils
Imports LogEngine
Module Vars
    Public BotName As String = "PeriodiBOT"
    Public ConfigFilePath As String = Exepath & "Config.cfg"
    Public LogPath As String = Exepath & "Log.psv"
    Public UserPath As String = Exepath & "Users.psv"
    Public SettingsPath As String = Exepath & "Settings.psv"
    Public EventLogger As New LogEngine.LogEngine(LogPath, UserPath, BotName)
    Public SettingsProvider As New Settings(SettingsPath)



End Module

﻿Option Strict On
Option Explicit On
Imports MWBot.net.Utility
Imports MWBot.net.Utility.Utils
Module Vars
    Public BotName As String = "PeriodiBOT"
    Public ConfigFilePath As String = Exepath & "Config.cfg"
    Public LogPath As String = Exepath & "Log.psv"
    Public UserPath As String = Exepath & "Users.psv"
    Public SettingsPath As String = Exepath & "Settings.psv"
    Public Verbose As Boolean = True
    Public EventLogger As New SimpleLogger(LogPath, UserPath, BotName, Verbose)
    Public SettingsProvider As New Settings(SettingsPath)
End Module

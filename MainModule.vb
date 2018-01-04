Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PeriodiBOT_IRC.WikiBot

Module MainModule

    Sub Main()
        Uptime = DateTime.Now
        LoadConfig()
        Log("Starting...", "LOCAL", BOTName)
        Mainwikibot = New Bot(WPUserName, BOTPassword, ApiURL)
        BotIRC = New IRC_Client(IRCNetwork, IRCChannel, BOTIRCName, 6667, False, IRCPassword)
        BotIRC.Connect()


        Do
            Dim command As String = Console.ReadLine()
            BotIRC.Sendmessage(command)

            ' Declaración sin utilidad. Solo para efectos de debug.
            Dim a As Integer = 1
            Thread.Sleep(500)
        Loop

    End Sub









End Module

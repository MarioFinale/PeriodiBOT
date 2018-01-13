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

        Dim UpdateExtractFunc As New Func(Of IRCMessage())(Function()
                                                               Mainwikibot.UpdatePageExtracts(True)
                                                               Return {New IRCMessage(BOTName, "")}
                                                           End Function)
        Dim UpdateExtractTask As New IRCTask(BotIRC, 43200000, True, UpdateExtractFunc)
        UpdateExtractTask.Run()


        Dim ArchiveAllFunc As New Func(Of IRCMessage())(Function()
                                                            Mainwikibot.ArchiveAllInclusions(True)
                                                            Return {New IRCMessage(BOTName, "")}
                                                        End Function)
        Dim ArchiveAllTask As New IRCTask(BotIRC, 43200000, True, ArchiveAllFunc)
        ArchiveAllTask.Run()



        Do
            Dim command As String = Console.ReadLine()
            If Not String.IsNullOrWhiteSpace(command) Then
                BotIRC.Sendmessage(command)
            End If
            Thread.Sleep(500)
        Loop

    End Sub









End Module

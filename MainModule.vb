Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC

Module MainModule

    Sub Main()
        Uptime = DateTime.Now
        LoadConfig()
        Log("Starting...", "LOCAL", BOTName)
        ESWikiBOT = New Bot(WPUserName, BOTPassword, ApiURL)
        BotIRC = New IRC_Client(IRCNetwork, IRCChannel, BOTIRCName, 6667, False, IRCPassword)
        BotIRC.Start()

        'Tarea para avisar inactividad de usuario en IRC
        Dim CheckUsersFunc As New Func(Of IRCMessage())(AddressOf CheckUsers)
        Dim CheckUsersIRCTask As New IRCTask(BotIRC, 300000, True, CheckUsersFunc, "CheckUsers")
        CheckUsersIRCTask.Run()

        'Tarea para actualizar el café temático
        Dim TopicFunc As New Func(Of IRCMessage())(Function()
                                                       UpdateTopics()
                                                       Return {New IRCMessage(IRCChannel, ColoredText("¡Temas actualizados!", "04"))}
                                                   End Function)
        Dim TopicTask As New IRCTask(BotIRC, 86400000, True, TopicFunc, "TopicUpdate")
        TopicTask.Run()

        'Tarea para actualizar extractos
        Dim UpdateExtractFunc As New Func(Of IRCMessage())(Function()
                                                               UpdatePageExtracts(True)
                                                               Return {New IRCMessage(BOTName, " ")}
                                                           End Function)
        Dim UpdateExtractTask As New IRCTask(BotIRC, 43200000, True, UpdateExtractFunc, "UpdateExtracts")
        UpdateExtractTask.Run()

        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of IRCMessage())(Function()
                                                            ArchiveAllInclusions(True)
                                                            Return {New IRCMessage(BOTName, " ")}
                                                        End Function)
        Dim ArchiveAllTask As New IRCTask(BotIRC, 86400000, True, ArchiveAllFunc, "ArchiveAll")
        ArchiveAllTask.Run()


        Do
            Dim command As String = Console.ReadLine()
            If Not String.IsNullOrWhiteSpace(command) Then
                'BotIRC.Sendmessage(command)
            End If
            Thread.Sleep(500)
        Loop

    End Sub

End Module

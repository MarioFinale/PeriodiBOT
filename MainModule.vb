﻿Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.CommFunctions


Module MainModule

    Sub Main()

        Uptime = DateTime.Now
        EventLogger.Log("Starting...", "LOCAL")
        ESWikiBOT = New Bot(New ConfigFile(ConfigFilePath))

        BotIRC = New IRC_Client(ESWikiBOT.IrcUrl, ESWikiBOT.IrcChannel, ESWikiBOT.IrcNickName, 6667, False, ESWikiBOT.IrcPassword, New ConfigFile(IrcOpPath))
        BotIRC.Start()

        'Tarea para revisar si hay solicitudes en mediacion informal
        Dim InfMedFunc As New Func(Of IRCMessage())(Function()
                                                        Dim p As Page = ESWikiBOT.Getpage("Plantilla:Estado usuario")
                                                        Dim actus As New ActiveUsers(ESWikiBOT)
                                                        actus.CheckUsersActivity(p, p)
                                                        Return {New IRCMessage(ESWikiBOT.IrcNickName, "")}
                                                    End Function)
        Dim InfMedTask As New IRCTask(BotIRC, 600000, True, InfMedFunc, "InfMedFunc")
        InfMedTask.Run()

        'Tarea para actualizar plantilla de usuario conectado
        Dim UserStatusFunc As New Func(Of IRCMessage())(Function()
                                                            Dim p As Page = ESWikiBOT.Getpage("Plantilla:Estado usuario")
                                                            Dim actus As New ActiveUsers(ESWikiBOT)
                                                            actus.CheckUsersActivity(p, p)
                                                            Return {New IRCMessage(ESWikiBOT.IrcNickName, "")}
                                                        End Function)
        Dim UserStatusTask As New IRCTask(BotIRC, 600000, True, UserStatusFunc, "UserStatus")
        UserStatusTask.Run()

        'Tarea para avisar inactividad de usuario en IRC
        Dim CheckUsersFunc As New Func(Of IRCMessage())(AddressOf CheckUsers)
        Dim CheckUsersIRCTask As New IRCTask(BotIRC, 600000, True, CheckUsersFunc, "CheckUsers")
        CheckUsersIRCTask.Run()

        'Tarea para actualizar el café temático
        Dim TopicFunc As New Func(Of IRCMessage())(Function()
                                                       UpdateTopics()
                                                       Return {New IRCMessage(ESWikiBOT.IrcNickName, ColoredText("¡Temas actualizados!", "04"))}
                                                   End Function)
        Dim TopicTask As New IRCTask(BotIRC, 21600000, True, TopicFunc, "TopicUpdate")
        TopicTask.Run()

        'Tarea para actualizar extractos
        Dim UpdateExtractFunc As New Func(Of IRCMessage())(Function()
                                                               UpdatePageExtracts(True)
                                                               Return {New IRCMessage(ESWikiBOT.IrcNickName, " ")}
                                                           End Function)
        Dim UpdateExtractTask As New IRCTask(BotIRC, 3600000, True, UpdateExtractFunc, "UpdateExtracts")
        UpdateExtractTask.Run()

        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of IRCMessage())(Function()
                                                            ArchiveAllInclusions(True)
                                                            Return {New IRCMessage(ESWikiBOT.IrcNickName, " ")}
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

﻿Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.My.Resources
Imports MWBot.net
Imports MWBot.net.WikiBot
Imports MWBot.net.GlobalVars

Public NotInheritable Class Initializer
    Private Sub New()
    End Sub

    Public Shared TaskAdm As New TaskAdmin()
    Public Shared BotVersion As String = Reflection.Assembly.GetCallingAssembly.GetName.Version.ToString
    Public Shared IrcOpPath As String = Exepath & "OPs.cfg"
    Public Shared IrcConfigPath As String = Exepath & "IRC.cfg"

    Public Shared Sub Init()
        Dim BotIRC As IRC_Client
        Dim ESWikiBOT As Bot
        Uptime = DateTime.Now
        ESWikiBOT = New Bot(New ConfigFile(ConfigFilePath))

        BotIRC = New IRC_Client(New ConfigFile(IrcConfigPath), 6667, New ConfigFile(IrcOpPath), ESWikiBOT)
        BotIRC.StartClient()

        'Tarea para generar video de efemérides
        Dim efevidfunc As New Func(Of Boolean)(Function()
                                                   Dim igen As New VideoGen(ESWikiBOT)
                                                   Return igen.CheckEfe
                                               End Function)
        TaskAdm.NewTask("Generar video con las efemérides del día", ESWikiBOT.UserName, efevidfunc, New TimeSpan(15, 0, 0), True)


        'Tarea para revisar si hay solicitudes en mediacion informal
        Dim InfMedFunc As New Func(Of Boolean)(Function()
                                                   Dim sptask As New SpecialTaks(ESWikiBOT)
                                                   Return sptask.CheckInformalMediation()
                                               End Function)
        TaskAdm.NewTask("Verificar solicitudes en mediacion informal", BotCodename, InfMedFunc, 600000, True)

        'Tarea para actualizar plantilla de usuario conectado
        Dim UserStatusFunc As New Func(Of Boolean)(Function()
                                                       Dim p As Page = ESWikiBOT.Getpage("Plantilla:Estado usuario")
                                                       ESWikiBOT.CheckUsersActivity(p, p)
                                                       Return True
                                                   End Function)
        TaskAdm.NewTask("Actualizar plantilla de usuario conectado", BotCodename, UserStatusFunc, 600000, True)


        'Tarea para actualizar el café temático
        Dim TopicFunc As New Func(Of Boolean)(Function()
                                                  Dim topicw As New WikiTopicList(ESWikiBOT)
                                                  Return topicw.UpdateTopics
                                              End Function)
        TaskAdm.NewTask("Actualizar el café temático", BotCodename, TopicFunc, New TimeSpan(12, 0, 0), True)

        'Tarea para actualizar el café temático
        Dim BiggestThreadsFunc As New Func(Of Boolean)(Function()
                                                           Dim topicw As New WikiTopicList(ESWikiBOT)
                                                           Return topicw.BiggestThreadsEver()
                                                       End Function)
        TaskAdm.NewTask("Actualizar la lista con los hilos más grandes del café.", BotCodename, BiggestThreadsFunc, New TimeSpan(9, 0, 0), True)

        'Tarea para actualizar extractos
        Dim UpdateExtractFunc As New Func(Of Boolean)(Function()
                                                          Dim sptask As New SpecialTaks(ESWikiBOT)
                                                          Return sptask.UpdatePageExtracts(WPStrings.ResumePageName)
                                                      End Function)
        TaskAdm.NewTask("Actualizar extractos", BotCodename, UpdateExtractFunc, 3600000, True)

        'Tarea para completar firmas
        Dim SignAllFunc As New Func(Of Boolean)(Function()
                                                    Dim signtask As New SpecialTaks(ESWikiBOT)
                                                    Return signtask.SignAllInclusions()
                                                End Function)
        TaskAdm.NewTask("Completar firmas", BotCodename, SignAllFunc, 240000, True)

        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of Boolean)(Function()
                                                       Dim signtask As New SpecialTaks(ESWikiBOT)
                                                       Return signtask.ArchiveAllInclusions()
                                                   End Function)
        TaskAdm.NewTask("Archivado automático", BotCodename, ArchiveAllFunc, New TimeSpan(0, 0, 0), True)

    End Sub




End Class

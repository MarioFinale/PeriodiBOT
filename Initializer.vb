Option Strict On
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
    Public Shared ArchiveTemplateName As String = "Plantilla:Archivado automático"
    Public Shared DoNotArchiveTemplateName As String = "Plantilla:No archivar"
    Public Shared ProgrammedArchiveTemplateName As String = "Plantilla:Archivo programado"
    Public Shared ArchiveBoxTemplateName As String = "Plantilla:Caja de archivos"
    Public Shared ArchiveMessageTemplateName As String = "Plantilla:Archivo"
    Public Shared AutoSignatureTemplateName As String = "Plantilla:Firma automática"



    Public Shared Sub Init()
        Dim BotIRC As IRC_Client
        Dim ESWikiBOT As Bot
        Uptime = DateTime.Now
        ESWikiBOT = New Bot(New ConfigFile(ConfigFilePath))

        BotIRC = New IRC_Client(New ConfigFile(IrcConfigPath), 6667, New ConfigFile(IrcOpPath), ESWikiBOT)
        BotIRC.StartClient()

        If Utils.BotSettings.Contains("ArchiveTemplateName") Then
            ArchiveTemplateName = Utils.BotSettings.Get("ArchiveTemplateName").ToString() : Else
            Utils.BotSettings.NewVal("ArchiveTemplateName", "Plantilla:Archivado automático")
        End If
        If Utils.BotSettings.Contains("DoNotArchiveTemplateName") Then
            DoNotArchiveTemplateName = Utils.BotSettings.Get("DoNotArchiveTemplateName").ToString() : Else
            Utils.BotSettings.NewVal("DoNotArchiveTemplateName", "Plantilla:No archivar")
        End If
        If Utils.BotSettings.Contains("ProgrammedArchiveTemplateName") Then
            ProgrammedArchiveTemplateName = Utils.BotSettings.Get("ProgrammedArchiveTemplateName").ToString() : Else
            Utils.BotSettings.NewVal("ProgrammedArchiveTemplateName", "Plantilla:Archivo programado")
        End If
        If Utils.BotSettings.Contains("ArchiveBoxTemplateName") Then
            ArchiveBoxTemplateName = Utils.BotSettings.Get("ArchiveBoxTemplateName").ToString() : Else
            Utils.BotSettings.NewVal("ArchiveBoxTemplateName", "Plantilla:Caja de archivos")
        End If
        If Utils.BotSettings.Contains("ArchiveMessageTemplateName") Then
            ArchiveMessageTemplateName = Utils.BotSettings.Get("ArchiveMessageTemplateName").ToString() : Else
            Utils.BotSettings.NewVal("ArchiveMessageTemplateName", "Plantilla:Archivo")
        End If
        If Utils.BotSettings.Contains("AutoSignatureTemplateName") Then
            AutoSignatureTemplateName = Utils.BotSettings.Get("AutoSignatureTemplateName").ToString() : Else
            Utils.BotSettings.NewVal("AutoSignatureTemplateName", "Plantilla:Firma automática")
        End If

        'Tarea para actualizar el contador de solicitudes de autorizaciones de bots
        Dim BotCountFunc As New Func(Of Boolean)(Function()
                                                     Dim sp As New SpecialTaks(ESWikiBOT)
                                                     Return sp.UpdateBotRecuestCount(ESWikiBOT.Getpage("Wikipedia:Bot/Autorizaciones"), ESWikiBOT.Getpage("Wikipedia:Bot/Autorizaciones/número"), 1)
                                                 End Function)
        TaskAdm.NewTask("Actualizar el contador de solicitudes de autorizaciones de bots", ESWikiBOT.UserName, BotCountFunc, 300000, True)


        'Tarea para revisar si hay solicitudes en mediacion informal
        Dim InfMedFunc As New Func(Of Boolean)(Function()
                                                   Dim sptask As New SpecialTaks(ESWikiBOT)
                                                   Return sptask.CheckInformalMediation()
                                               End Function)
        TaskAdm.NewTask("Verificar solicitudes en mediacion informal", ESWikiBOT.UserName, InfMedFunc, 600000, True)

        'Tarea para actualizar plantilla de usuario conectado
        Dim UserStatusFunc As New Func(Of Boolean)(Function()
                                                       Dim sptask As New SpecialTaks(ESWikiBOT)
                                                       Dim p As Page = ESWikiBOT.Getpage("Plantilla:Estado usuario")
                                                       sptask.CheckUsersActivity(p, p)
                                                       Return True
                                                   End Function)
        TaskAdm.NewTask("Actualizar plantilla de usuario conectado", ESWikiBOT.UserName, UserStatusFunc, 600000, True)


        'Tarea para actualizar el café temático
        Dim TopicFunc As New Func(Of Boolean)(Function()
                                                  Dim topicw As New WikiTopicList(ESWikiBOT)
                                                  Return topicw.UpdateTopics
                                              End Function)
        TaskAdm.NewTask("Actualizar el café temático", ESWikiBOT.UserName, TopicFunc, New TimeSpan(12, 0, 0), True)

        'Tarea para actualizar la lista de los hilos mas largos
        Dim BiggestThreadsFunc As New Func(Of Boolean)(Function()
                                                           Dim topicw As New WikiTopicList(ESWikiBOT)
                                                           Return topicw.BiggestThreadsEver()
                                                       End Function)
        TaskAdm.NewTask("Actualizar la lista con los hilos más grandes del café.", ESWikiBOT.UserName, BiggestThreadsFunc, New TimeSpan(9, 0, 0), True)

        'Tarea para actualizar extractos
        Dim UpdateExtractFunc As New Func(Of Boolean)(Function()
                                                          Dim sptask As New SpecialTaks(ESWikiBOT)
                                                          Return sptask.UpdatePageExtracts(WPStrings.ResumePageName)
                                                      End Function)
        TaskAdm.NewTask("Actualizar extractos", ESWikiBOT.UserName, UpdateExtractFunc, 300000, True)

        'Tarea para completar firmas
        Dim SignAllFunc As New Func(Of Boolean)(Function()
                                                    Dim signtask As New SpecialTaks(ESWikiBOT)
                                                    Return signtask.SignAllInclusions(AutoSignatureTemplateName)
                                                End Function)
        TaskAdm.NewTask("Completar firmas", ESWikiBOT.UserName, SignAllFunc, 240000, True)


        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of Boolean) _
            (Function()

                 Dim signtask As New SpecialTaks(ESWikiBOT)
                 Return signtask.ArchiveAllInclusions(ArchiveTemplateName, DoNotArchiveTemplateName,
                                                       ProgrammedArchiveTemplateName, ArchiveBoxTemplateName, ArchiveMessageTemplateName)
             End Function)

        TaskAdm.NewTask("Archivado automático", ESWikiBOT.UserName, ArchiveAllFunc, New TimeSpan(0, 0, 0), True)

    End Sub




End Class

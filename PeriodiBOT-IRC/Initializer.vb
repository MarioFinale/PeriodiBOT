Option Strict On
Option Explicit On

Imports MWBot.net
Imports MWBot.net.WikiBot
Imports Utils.Utils
Imports IRCCLIENT.IRC
Imports PeriodiBOT_IRC.My.Resources

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
    Public Shared Uptime As Date

    Public Shared Sub Init()
        Uptime = Date.Now
        LoadSettings()
        Dim ESWikiBOT As New Bot(ConfigFilePath, EventLogger)
        Dim BotIRC As New IRC_Client(IrcConfigPath, 6667, IrcOpPath, ESWikiBOT, TaskAdm, BotVersion, BotName, {"%", "%%", "pepino:"}, EventLogger)
        Dim tPatroller As New SignPatroller(ESWikiBOT)
        BotIRC.StartClient()
        tPatroller.StartPatroller()

        'Tarea para actualizar el contador de solicitudes de autorizaciones de bots
        Dim BotCountFunc As New Func(Of Boolean) _
            (Function()
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
        Dim UserStatusFunc As New Func(Of Boolean) _
            (Function()

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
        Dim BiggestThreadsFunc As New Func(Of Boolean) _
            (Function()
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

        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of Boolean) _
            (Function()

                 Dim signtask As New SpecialTaks(ESWikiBOT)
                 Return signtask.ArchiveAllInclusions(ArchiveTemplateName, DoNotArchiveTemplateName,
                                                       ProgrammedArchiveTemplateName, ArchiveBoxTemplateName, ArchiveMessageTemplateName)
             End Function)
        TaskAdm.NewTask("Archivado automático", ESWikiBOT.UserName, ArchiveAllFunc, New TimeSpan(0, 0, 0), True)

        'Tarea para actualizar lista de bots
        Dim UpdateBotsListFunc As New Func(Of Boolean) _
            (Function()
                 Dim signtask As New SpecialTaks(ESWikiBOT)
                 Return signtask.UpdateBotList(ESWikiBOT.Getpage("Plantilla:Controlador"), ESWikiBOT.Getpage("Wikipedia:Bot/Bots activos"), "ficha de bot")
             End Function)
        TaskAdm.NewTask("Actualizar Wikipedia:Bot/Bots activos", ESWikiBOT.UserName, UpdateBotsListFunc, 3600000, True)

        ''Tarea para reparar referencias
        'Dim FixRefFunc As New Func(Of Boolean)(Function()
        '                                           Dim sptask As New RefTool(ESWikiBOT)
        '                                           Dim rpage As Page = ESWikiBOT.GetRandomPage()
        '                                           Return sptask.FixRefs(rpage)
        '                                       End Function)
        'TaskAdm.NewTask("Completar referencias", ESWikiBOT.UserName, FixRefFunc, 1000, True)

    End Sub


    Public Shared Sub LoadSettings()
        If SettingsProvider.Contains("ArchiveTemplateName") Then
            ArchiveTemplateName = SettingsProvider.Get("ArchiveTemplateName").ToString() : Else
            SettingsProvider.NewVal("ArchiveTemplateName", "Plantilla:Archivado automático")
        End If
        If SettingsProvider.Contains("DoNotArchiveTemplateName") Then
            DoNotArchiveTemplateName = SettingsProvider.Get("DoNotArchiveTemplateName").ToString() : Else
            SettingsProvider.NewVal("DoNotArchiveTemplateName", "Plantilla:No archivar")
        End If
        If SettingsProvider.Contains("ProgrammedArchiveTemplateName") Then
            ProgrammedArchiveTemplateName = SettingsProvider.Get("ProgrammedArchiveTemplateName").ToString() : Else
            SettingsProvider.NewVal("ProgrammedArchiveTemplateName", "Plantilla:Archivo programado")
        End If
        If SettingsProvider.Contains("ArchiveBoxTemplateName") Then
            ArchiveBoxTemplateName = SettingsProvider.Get("ArchiveBoxTemplateName").ToString() : Else
            SettingsProvider.NewVal("ArchiveBoxTemplateName", "Plantilla:Caja de archivos")
        End If
        If SettingsProvider.Contains("ArchiveMessageTemplateName") Then
            ArchiveMessageTemplateName = SettingsProvider.Get("ArchiveMessageTemplateName").ToString() : Else
            SettingsProvider.NewVal("ArchiveMessageTemplateName", "Plantilla:Archivo")
        End If
        If SettingsProvider.Contains("AutoSignatureTemplateName") Then
            AutoSignatureTemplateName = SettingsProvider.Get("AutoSignatureTemplateName").ToString() : Else
            SettingsProvider.NewVal("AutoSignatureTemplateName", "Plantilla:Firma automática")
        End If
    End Sub



End Class

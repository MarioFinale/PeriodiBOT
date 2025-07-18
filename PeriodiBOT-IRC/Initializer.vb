﻿Option Strict On
Option Explicit On

Imports MWBot.net.WikiBot
Imports MWBot.net.Utility
Imports MWBot.net.Utility.Utils
Imports IRCCLIENT.IRC
Imports PeriodiBOT_IRC.My.Resources
Imports System.Text.RegularExpressions

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

        '================================  Tareas Activas =====================================

        'Bot IRC
        Dim BotIRC As New IRC_Client(IrcConfigPath, 6667, IrcOpPath, ESWikiBOT, TaskAdm, BotVersion, BotName, {"%", "%%", "pepino:"}, EventLogger)
        BotIRC.UseSASL = True
        BotIRC.StartClient()

        'Patrulla de cambios recientes
        Dim tPatroller As New SignPatroller(ESWikiBOT)
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
        TaskAdm.NewTask("Actualizar extractos generados por PeriodiBOT", ESWikiBOT.UserName, UpdateExtractFunc, 300000, True)

        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of Boolean) _
            (Function()
                 Dim ExcludingPatern As String = "{{[\s]*Admintab[\s]*(\|[\s]*\$0)*[\s]*}}" 'Excepción admintab sin parámetro o parámetro $0
                 Dim Archiver As New Archiver(ESWikiBOT)
                 Return Archiver.ArchiveAllInclusions(ArchiveTemplateName, {DoNotArchiveTemplateName},
                                                       ProgrammedArchiveTemplateName, ArchiveBoxTemplateName, ArchiveMessageTemplateName, ExcludingPatern)
             End Function)
        TaskAdm.NewTask("Archivado automático de discusiones", ESWikiBOT.UserName, ArchiveAllFunc, New TimeSpan(0, 0, 0), True)

        'Tarea para archivar la cartelera de acontecimientos
        Dim ArchiveBillboard As New Func(Of Boolean)(Function()
                                                         Dim BillArchived As New BillboardArchiver(ESWikiBOT)
                                                         Return BillArchived.ArchivePage("Wikipedia:Cartelera de acontecimientos")
                                                     End Function)
        TaskAdm.NewTask("Archivado de la cartelera de acontecimientos", ESWikiBOT.UserName, ArchiveBillboard, New TimeSpan(10, 0, 0), True)

        'Tarea para actualizar lista de bots
        Dim UpdateBotsListFunc As New Func(Of Boolean) _
            (Function()
                 Dim signtask As New SpecialTaks(ESWikiBOT)
                 Return signtask.UpdateBotList(ESWikiBOT.Getpage("Plantilla:Controlador"), ESWikiBOT.Getpage("Wikipedia:Bot/Bots activos"), "ficha de bot")
             End Function)
        TaskAdm.NewTask("Actualizar Wikipedia:Bot/Bots activos", ESWikiBOT.UserName, UpdateBotsListFunc, New TimeSpan(12, 0, 0), True)



        ''================================  Tareas Inactivas =====================================

        ''Tarea para quitar bandera de europa (inexistente)
        'Dim sp As New SpecialTaks(ESWikiBOT)
        'Dim pages As String() = ESWikiBOT.SearchPagesForText("hastemplate:Bandera insource:/[Bb]andera *\| *Europa/", 500)
        'pages = pages.Concat(ESWikiBOT.SearchPagesForText("hastemplate:Bandera insource:/[Bb]andera2 *\| *Europa/", 500)).Distinct().ToArray()

        'For Each p As String In pages
        '    Dim pageToChange As Page = ESWikiBOT.Getpage(p)
        '    Dim content As String = pageToChange.Content

        '    content = Text.RegularExpressions.Regex.Replace(content, "\{\{ *[Bb]andera *\| *[Ee]uropa *\}\}", String.Empty)
        '    content = Text.RegularExpressions.Regex.Replace(content, "\{\{ *[Bb]andera2 *\| *[Ee]uropa *\}\}", "[[Europa]]")

        '    pageToChange.Save(content, "(Bot) Quitando bandera inexistente.", True, True, True)

        'Next



        ''================================  TEST =====================================

        'Tarea para reparar referencias
        'Dim FixRefFunc As New Func(Of Boolean)(Function()
        '                                           Dim sptask As New RefTool(ESWikiBOT)
        '                                           Dim rpage As Page = ESWikiBOT.GetRandomPage()
        '                                           Return sptask.FixRefs(rpage)
        '                                       End Function)
        'TaskAdm.NewTask("Completar referencias", ESWikiBOT.UserName, FixRefFunc, 1000, True)


        'Dim sptask As New RefTool(ESWikiBOT)
        'Dim rpage As Page = ESWikiBOT.Getpage("Usuario:PeriodiBOT/Test")
        'rpage.Save(rpage.Content & Environment.NewLine & "http://eaeaq.info", "Spam-blacklist test", True, True, True)


        'Dim Archiver As New Archiver(ESWikiBOT)
        'Dim testpage As Page = ESWikiBOT.Getpage("Usuario_discusión:RageJam")
        'Dim ExcludingPatern As String = "{{[\s]*Admintab[\s]*(\|[\s]*\$0)*[\s]*}}" 'Excepción admintab sin parámetro o parámetro $0
        'Archiver.AutoArchive(testpage, ArchiveTemplateName, {DoNotArchiveTemplateName},
        'ProgrammedArchiveTemplateName, ArchiveBoxTemplateName, ArchiveMessageTemplateName, ExcludingPatern)


        'Dim ExcludingPatern As String = "{{[\s]*Admintab[\s]*(\|[\s]*\$0)*[\s]*}}" 'Excepción admintab sin parámetro o parámetro $0
        'Dim Archiver As New Archiver(ESWikiBOT)
        'Archiver.ArchiveAllInclusions(ArchiveTemplateName, {DoNotArchiveTemplateName},
        '                                      ProgrammedArchiveTemplateName, ArchiveBoxTemplateName, ArchiveMessageTemplateName, ExcludingPatern)
        'Dim pause As Integer = 0



        ''================================  TEST =====================================

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

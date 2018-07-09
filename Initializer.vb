Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.CommFunctions
Public Class Initializer

    Private Sub New()
    End Sub

    Public Shared Sub Init()
        Uptime = DateTime.Now
        EventLogger.Log("Starting...", "LOCAL")
        ESWikiBOT = New Bot(New ConfigFile(ConfigFilePath))

        Dim testp As Page = ESWikiBOT.Getpage("Usuario discusión:MarioFinale")

        Dim diff As List(Of Tuple(Of String, String)) = ESWikiBOT.GetLastDiff(testp)
        Dim a As Integer = 1


        'Tarea para revisar si hay solicitudes en mediacion informal
        Dim InfMedFunc As New Func(Of Boolean)(Function() CheckInformalMediation(ESWikiBOT))
        NewThread("Verificar solicitudes en mediacion informal", BotCodename, InfMedFunc, 600000, True)

        'Tarea para actualizar plantilla de usuario conectado
        Dim UserStatusFunc As New Func(Of Boolean)(Function()
                                                       Dim p As Page = ESWikiBOT.Getpage("Plantilla:Estado usuario")
                                                       CheckUsersActivity(p, p)
                                                       Return True
                                                   End Function)
        NewThread("Actualizar plantilla de usuario conectado", BotCodename, UserStatusFunc, 600000, True)

        'Tarea para avisar inactividad de usuario en IRC
        Dim CheckUsersFunc As New Func(Of Boolean)(Function()
                                                       BotIRC.Sendmessage(CheckUsers)
                                                       Return True
                                                   End Function)
        NewThread("Avisar inactividad de usuario en IRC", BotCodename, CheckUsersFunc, 6000, True)


        'Tarea para actualizar el café temático
        Dim TopicFunc As New Func(Of Boolean)(Function()
                                                  UpdateTopics()
                                                  BotIRC.Sendmessage(New IRCMessage(ESWikiBOT.IrcNickName, ColoredText("¡Temas actualizados!", 4)))
                                                  Return True
                                              End Function)
        NewThread("Actualizar el café temático", BotCodename, TopicFunc, New TimeSpan(12, 0, 0), True)

        'Tarea para actualizar extractos
        Dim UpdateExtractFunc As New Func(Of Boolean)(Function() UpdatePageExtracts(True))
        NewThread("Actualizar extractos", BotCodename, UpdateExtractFunc, 3600000, True)

        'Tarea para archivar todo
        Dim ArchiveAllFunc As New Func(Of Boolean)(Function() ArchiveAllInclusions(True))
        NewThread("Archivado automático", BotCodename, ArchiveAllFunc, New TimeSpan(0, 0, 0), True)

    End Sub
End Class

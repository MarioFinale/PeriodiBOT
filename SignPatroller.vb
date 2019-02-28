Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports MWBot.net
Imports MWBot.net.WikiBot
Imports PeriodiBOT_IRC.Initializer

Public Class SignPatroller
    Public Async Sub StartPatroller(ByVal workerbot As Bot)
        Dim editsqueue As New Queue(Of Tuple(Of String, String, Date))
        Dim QueueResolver As New Func(Of Boolean)(Function()
                                                      Dim tsing As New SignPatroller
                                                      Return tsing.ResolveQueue(editsqueue, workerbot)
                                                  End Function)
        TaskAdm.NewTask("Patrullar ediciones sin firma en discusiones", workerbot.UserName, QueueResolver, 250, True, False)

        Await Task.Run(Sub()
                           Try
                               While True
                                   Dim tclient As WebClient = Nothing
                                   Dim tstream As Stream = Nothing
                                   Dim tstreamreader As StreamReader = Nothing
                                   Try
                                       tclient = New WebClient()
                                       tstream = tclient.OpenRead(New Uri("https://stream.wikimedia.org/v2/stream/recentchange"))
                                       tstreamreader = New StreamReader(tstream)
                                   Catch ex As IOException
                                       Utils.EventLogger.EX_Log(ex.Message, "AutoSignPatrol", workerbot.UserName)
                                       Continue While
                                   Catch ex2 As WebException
                                       Utils.EventLogger.EX_Log(ex2.Message, "AutoSignPatrol", workerbot.UserName)
                                       Continue While
                                   End Try
                                   While True
                                       Try
                                           Dim tline As String = tstreamreader.ReadLine
                                           If Not tline.Contains("""wiki"":""eswiki""") Then Continue While
                                           If tline.Contains("""bot"":true,") Then Continue While
                                           If Not tline.Contains(",""type"":""edit"",") Then Continue While
                                           If Not (Regex.Match(tline, """namespace"":(1|3|9|11|15|101|103|105),").Success) Then Continue While
                                           Dim tusername As String = If(Utils.TextInBetween(tline, ",""user"":""", """,").Count >= 1, Utils.TextInBetween(tline, ",""user"":""", """,")(0), "")
                                           Dim tpagename As String = If(Utils.TextInBetween(tline, ",""title"":""", """,").Count >= 1, Utils.TextInBetween(tline, ",""title"":""", """,")(0), "")
                                           Dim tdate As Date = Date.UtcNow
                                           SyncLock editsqueue
                                               editsqueue.Enqueue(New Tuple(Of String, String, Date)(tusername, tpagename, tdate))
                                           End SyncLock
                                           Utils.EventLogger.Debug_Log("Edición en '" & tpagename & "'" & " por '" & tusername & "'.", "RecentChanges watcher")
                                       Catch ex As IOException
                                           Utils.EventLogger.EX_Log(ex.Message, "AutoSignPatrol", workerbot.UserName)
                                           Exit While
                                       Catch ex2 As WebException
                                           Utils.EventLogger.EX_Log(ex2.Message, "AutoSignPatrol", workerbot.UserName)
                                           Exit While
                                       End Try
                                   End While
                               End While
                           Catch ex As Exception
                               Utils.EventLogger.EX_Log("FATAL EX: " & ex.Message, "AutoSignPatrol", workerbot.UserName)
                           End Try
                       End Sub)
    End Sub

    Private Function ResolveQueue(ByRef editsqueue As Queue(Of Tuple(Of String, String, Date)), ByVal workerbot As Bot) As Boolean
        Dim tedit As Tuple(Of String, String, Date)
        If editsqueue.Count = 0 Then Return False
        SyncLock editsqueue
            tedit = editsqueue.Dequeue
        End SyncLock
        If Not Date.UtcNow.Subtract(tedit.Item3).Minutes > 4 Then
            editsqueue.Enqueue(tedit)
            Return False
        End If
        Dim tpagename As String = tedit.Item2

        Dim expagethreads As String() = workerbot.Getpage("Usuario:PeriodiBOT/Paginas exentas de firma").Threads
        Dim expageslist As String() = (From tmatch In Regex.Matches(If(expagethreads.Count >= 1, expagethreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray
        If expageslist.Contains(tpagename) Then Return False

        Dim exuserthreads As String() = workerbot.Getpage("Usuario:PeriodiBOT/Exentos firma").Threads
        Dim exuserlist As String() = (From tmatch In Regex.Matches(If(exuserthreads.Count >= 1, exuserthreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray

        Dim ackeckpagethreads As String() = workerbot.Getpage("Usuario:PeriodiBOT/Comprobar siempre firma").Threads
        Dim achecklist As String() = (From tmatch In Regex.Matches(If(ackeckpagethreads.Count >= 1, ackeckpagethreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray
        Dim tuser As WikiUser = New WikiUser(workerbot, tedit.Item1)

        If (tuser.EditCount < 500 And (Not exuserlist.Contains(tuser.UserName)) Or achecklist.Contains(tuser.UserName)) Then
            Dim tpage As Page = workerbot.Getpage(tedit.Item2)
            If Date.UtcNow.Subtract(tpage.LastEdit).Minutes < 4 Then Return False
            Dim spt As SpecialTaks = New SpecialTaks(workerbot)
            spt.AddMissingSignature2(tpage, False, True, "TEST (" & GlobalVars.BotCodename & " " & GlobalVars.MwBotVersion & "/" & workerbot.UserName & " " & Initializer.BotVersion & "):")
        End If
        Return True
    End Function




End Class
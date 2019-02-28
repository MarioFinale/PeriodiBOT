Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text.RegularExpressions
Imports MWBot.net
Imports MWBot.net.WikiBot

Public Class Test



    Public Async Sub Main(ByVal workerbot As Bot)
        Dim tclient As WebClient = New WebClient()
        Dim tstream As Stream = tclient.OpenRead(New Uri("https://stream.wikimedia.org/v2/stream/recentchange"))
        Dim tstreamreader As StreamReader = New StreamReader(tstream)
        Dim editsqueue As New Queue(Of Tuple(Of String, String, Date))




        Await Task.Run(Sub()
                           Task.Run(Sub()
                                        DoSing(editsqueue, workerbot)
                                    End Sub)

                           While True
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
                               Utils.EventLogger.Log("Edición en '" & tpagename & "'" & " por '" & tusername & "'.", "RecentChanges watcher")
                           End While
                       End Sub)



    End Sub



    Sub DoSing(ByRef editsqueue As Queue(Of Tuple(Of String, String, Date)), ByVal workerbot As Bot)
        While True
            System.Threading.Thread.Sleep(100)
            Dim tedit As Tuple(Of String, String, Date)
            If editsqueue.Count = 0 Then Continue While
            SyncLock editsqueue
                tedit = editsqueue.Dequeue
            End SyncLock
            Dim tuser As WikiUser = New WikiUser(workerbot, tedit.Item1)
            If tuser.EditCount < 500 Then
                If Not Date.UtcNow.Subtract(tedit.Item3).Minutes > 5 Then
                    editsqueue.Enqueue(tedit)
                    Continue While
                End If
                Dim tpage As Page = workerbot.Getpage(tedit.Item2)
                If Date.UtcNow.Subtract(tpage.LastEdit).Minutes < 5 Then Continue While
                Dim spt As SpecialTaks = New SpecialTaks(workerbot)
                spt.AddMissingSignature2(tpage, False, True, "TEST (" & GlobalVars.BotCodename & " " & GlobalVars.MwBotVersion & "/" & workerbot.UserName & " " & Initializer.BotVersion & "):", True)
            End If
        End While
    End Sub

End Class

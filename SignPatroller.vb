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
                                           If Not (Regex.Match(tline, """namespace"":(1|3|9|11|13|15|101|103|105|829),").Success) Then Continue While
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
        If expageslist.Contains(tpagename) Then : Utils.EventLogger.Log(String.Format(BotMessages.NotSigned, tpagename) & " INFO: EXPLIST=" & expageslist.Contains(tpagename).ToString, "ResolveQueue") : Return False :End if

        Dim exuserthreads As String() = workerbot.Getpage("Usuario:PeriodiBOT/Exentos firma").Threads
        Dim exuserlist As String() = (From tmatch In Regex.Matches(If(exuserthreads.Count >= 1, exuserthreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray

        Dim ackeckpagethreads As String() = workerbot.Getpage("Usuario:PeriodiBOT/Comprobar siempre firma").Threads
        Dim achecklist As String() = (From tmatch In Regex.Matches(If(ackeckpagethreads.Count >= 1, ackeckpagethreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray
        Dim tuser As WikiUser = New WikiUser(workerbot, tedit.Item1)
        Dim tpage As Page = workerbot.Getpage(tedit.Item2)

        If (tuser.EditCount < 500 And (Not exuserlist.Contains(tuser.UserName)) Or achecklist.Contains(tuser.UserName)) Then
            If Date.UtcNow.Subtract(tpage.LastEdit).Minutes < 4 Then Return False
            Return AddMissingSignature2(tpage, False, True, "TEST (" & GlobalVars.BotCodename & " " & GlobalVars.MwBotVersion & "/" & workerbot.UserName & " " & Initializer.BotVersion & "):", workerbot, tuser.UserName)
        End If
        Utils.EventLogger.Log(String.Format(BotMessages.NotSigned, tpage.Title) & " INFO: EC=" & tuser.EditCount _
                              & " EXULIST=" & exuserlist.Contains(tuser.UserName).ToString & " ACHECK=" & achecklist.Contains(tuser.UserName).ToString & " EXPLIST=" & expageslist.Contains(tpagename).ToString, "ResolveQueue")
        Return False
    End Function

    Function AddMissingSignature2(ByVal tpage As Page, newthreads As Boolean, minor As Boolean, addmsg As String, workerbot As Bot, lastusername As String) As Boolean
        If tpage.Lastuser = workerbot.UserName Then Return False 'No completar firma en páginas en las que haya editado
        Dim LastUser As WikiUser = New WikiUser(workerbot, tpage.Lastuser)
        If LastUser.IsBot Then Return False
        Dim UnsignedSectionInfo As Tuple(Of String, String, Date) = GetLastUnsignedSection2(tpage, newthreads, workerbot)
        If UnsignedSectionInfo Is Nothing Then Return False
        Dim pagetext As String = tpage.Content
        Dim UnsignedThread As String = UnsignedSectionInfo.Item1
        Dim lastparagraph As String = Regex.Match(UnsignedThread.TrimEnd, ".+(?=\n+==[^=].+==[^=]|$|\n+$)").Value
        If String.IsNullOrWhiteSpace(lastparagraph) Then Return False
        If Regex.Match(lastparagraph, "\[\[(:\w{2,7}:)*(user|usuario):.+?\]\]", RegexOptions.IgnoreCase).Success Then Return False
        If Regex.Match(lastparagraph, Utils.signpattern).Success Then Return False
        Dim Username As String = UnsignedSectionInfo.Item2
        If Not Username = lastusername Then Return False
        Dim pusername As String = String.Empty
        If tpage.PageNamespace = 3 Then
            If tpage.Title.Contains(":") Then
                pusername = tpage.Title.Split(":"c)(1)
                If pusername.Contains("/") Then
                    pusername = pusername.Split("/"c)(0)
                End If
                pusername = pusername.Trim()
            End If
        End If
        If pusername = Username Then Return False
        Utils.EventLogger.Log(String.Format(BotMessages.UnsignedMessageDetected, tpage.Title), "AddMissingSignature2")
        Dim UnsignedDate As Date = UnsignedSectionInfo.Item3
        Dim dstring As String = Utils.GetSpanishTimeString(UnsignedDate)
        pagetext = pagetext.Replace(UnsignedThread, UnsignedThread.TrimEnd & " {{sust:No firmado|" & Username & "|" & dstring & "}}" & Environment.NewLine)
        Dim scores As Double() = tpage.ORESScores
        If scores(0) > 0.97R Then : Utils.EventLogger.Log(String.Format(BotMessages.NotSigned, tpage.Title) & " INFO: ORES(0)=" & scores(0).ToString, "AddMissingSignature2") : Return False : End If
        If tpage.Save(pagetext, addmsg & String.Format(BotMessages.UnsignedSumm, Username), minor, True) = EditResults.Edit_successful Then Return True
        Utils.EventLogger.Log(String.Format(BotMessages.NotSigned, tpage.Title), "AddMissingSignature2")
        Return False
    End Function

    Function GetLastUnsignedSection2(ByVal tpage As Page, newthreads As Boolean, workerbot As Bot) As Tuple(Of String, String, Date)
        If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name, workerbot.UserName)
        Dim oldPage As Page = workerbot.Getpage(tpage.ParentRevId)
        Dim currentPage As Page = tpage

        Dim oldPageThreads As String() = oldPage.Threads
        Dim currentPageThreads As String() = currentPage.Threads

        Dim LastEdit As Date = currentPage.LastEdit
        Dim LastUser As String = currentPage.Lastuser
        Dim editedthreads As String()

        If newthreads Then
            editedthreads = Utils.GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
        Else
            If oldPageThreads.Count = currentPageThreads.Count Then
                If oldPageThreads.Count = 0 Then
                    editedthreads = {currentPage.Content}
                Else
                    editedthreads = Utils.GetChangedThreads(oldPageThreads, currentPageThreads)
                End If
            ElseIf oldPageThreads.Count < currentPageThreads.Count Then
                editedthreads = Utils.GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
            Else
                editedthreads = {}
            End If
        End If

        If editedthreads.Count > 0 AndAlso (Not String.IsNullOrWhiteSpace(editedthreads.Last)) Then
            Dim lasteditedthread As String = editedthreads.Last
            Dim lastsign As Date = Lastpdt2(lasteditedthread)
            If lastsign = New DateTime(9999, 12, 31, 23, 59, 59) Then
                Return New Tuple(Of String, String, Date)(lasteditedthread, LastUser, LastEdit)
            End If
        End If
        Return Nothing
    End Function

    Function Lastpdt2(ByVal text As String) As Date
        If String.IsNullOrEmpty(text) Then
            Throw New ArgumentException("Empty parameter", "text")
        End If
        Dim lastparagraph As String = Regex.Match(text, ".+(?=\n+==.+==|$|\n+$)").Value
        Dim TheDate As Date = Utils.ESWikiDatetime(lastparagraph)
        Utils.EventLogger.Debug_Log("Returning " & TheDate.ToString, Reflection.MethodBase.GetCurrentMethod().Name)
        Return TheDate
    End Function


End Class
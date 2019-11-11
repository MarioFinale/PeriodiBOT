Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports MWBot.net
Imports MWBot.net.WikiBot
Imports PeriodiBOT_IRC.Initializer
Imports Utils.Utils

Public Class SignPatroller

    Public OresThreshold As Double = 96.8#

    ReadOnly Property WorkerBot As Bot
    Sub New(ByRef workerbot As Bot)
        _WorkerBot = workerbot
    End Sub

    Public Sub StartPatroller()
        Dim editsqueue As New Queue(Of Tuple(Of String, String, Date))

        Dim RChangesWatcher As New Func(Of Boolean) _
            (Function()
                 Try
                     While True
                         Try
                             Dim tclient As WebClient = New WebClient()
                             Dim tstream As Stream = tclient.OpenRead(New Uri("https://stream.wikimedia.org/v2/stream/recentchange"))
                             Dim tstreamreader As StreamReader = New StreamReader(tstream)
                             While True
                                 Dim currentLine As String = tstreamreader.ReadLine
                                 If Not EditIsValid(currentLine) Then Continue While
                                 Dim editInfo As Tuple(Of String, String, Date) = GetEditInfoFromStreamLine(currentLine)
                                 SyncLock editsqueue
                                     editsqueue.Enqueue(editInfo)
                                 End SyncLock
                                 EventLogger.Debug_Log("Edición en '" & editInfo.Item2 & "'" & " por '" & editInfo.Item1 & "'.", "RecentChanges watcher")
                             End While
                         Catch ex As IOException
                             EventLogger.EX_Log(ex.Message, "AutoSignPatrol", WorkerBot.UserName)
                             Exit While
                         Catch ex2 As WebException
                             EventLogger.EX_Log(ex2.Message, "AutoSignPatrol", WorkerBot.UserName)
                             Exit While
                         End Try
                     End While
                 Catch ex As Exception
                     EventLogger.EX_Log("FATAL EX: " & ex.Message, "AutoSignPatrol", WorkerBot.UserName)
                 End Try
                 Return False
             End Function)

        Dim QueueResolver As New Func(Of Boolean)(Function()
                                                      Dim tsing As New SignPatroller(WorkerBot)
                                                      Return tsing.ResolveQueue(editsqueue)
                                                  End Function)

        TaskAdm.NewTask("RecentChanges watcher", WorkerBot.UserName, RChangesWatcher, 1500, True, False)
        TaskAdm.NewTask("Patrullar ediciones sin firma en discusiones con plantilla de firma", WorkerBot.UserName, QueueResolver, 250, True, False)

    End Sub

    Function GetEditInfoFromStreamLine(ByRef tline As String) As Tuple(Of String, String, Date)
        Dim tusername As String = If(TextInBetween(tline, ",""user"":""", """,").Count >= 1, TextInBetween(tline, ",""user"":""", """,")(0), "")
        Dim tpagename As String = If(TextInBetween(tline, ",""title"":""", """,").Count >= 1, TextInBetween(tline, ",""title"":""", """,")(0), "")
        Dim tdate As Date = Date.UtcNow
        Return New Tuple(Of String, String, Date)(tusername, tpagename, tdate)
    End Function

    Function ContainsAutosignatureTemplate(ByRef pageContent As String) As Boolean
        Return Regex.IsMatch(pageContent, "(\{\{[Pp]lantilla:[Ff]irma automática)([\s\S]+?\}\})")
    End Function

    Function EditIsValid(ByRef tline As String) As Boolean
        If Not tline.Contains("""wiki"":""eswiki""") Then Return False
        If tline.Contains("""bot"":true,") Then Return False
        If Not tline.Contains(",""type"":""edit"",") Then Return False
        If Not (Regex.Match(tline, """namespace"":(1|3|9|11|13|15|101|103|105|829),").Success) Then Return False
        Return True
    End Function

    Private Function ResolveQueue(ByRef editsqueue As Queue(Of Tuple(Of String, String, Date))) As Boolean
        Dim tedit As Tuple(Of String, String, Date)
        If editsqueue.Count = 0 Then Return False
        SyncLock editsqueue
            tedit = editsqueue.Dequeue
            If Not Date.UtcNow.Subtract(tedit.Item3).Minutes > 4 Then
                editsqueue.Enqueue(tedit)
                Return False
            End If
        End SyncLock
        Dim tpagename As String = tedit.Item2

        Dim expagethreads As String() = WorkerBot.Getpage("Usuario:PeriodiBOT/Paginas exentas de firma").Threads
        Dim expageslist As String() = (From tmatch In Regex.Matches(If(expagethreads.Count >= 1, expagethreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray
        If expageslist.Contains(tpagename) Then : EventLogger.Log(String.Format(BotMessages.NotSigned, tpagename) & " INFO: EXPLIST=" & expageslist.Contains(tpagename).ToString, "ResolveQueue", WorkerBot.UserName) : Return False : End If

        Dim exuserthreads As String() = WorkerBot.Getpage("Usuario:PeriodiBOT/Exentos firma").Threads
        Dim exuserlist As String() = (From tmatch In Regex.Matches(If(exuserthreads.Count >= 1, exuserthreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray

        Dim ackeckpagethreads As String() = WorkerBot.Getpage("Usuario:PeriodiBOT/Comprobar siempre firma").Threads
        Dim achecklist As String() = (From tmatch In Regex.Matches(If(ackeckpagethreads.Count >= 1, ackeckpagethreads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray
        Dim tuser As WikiUser = New WikiUser(WorkerBot, tedit.Item1)
        Dim tpage As Page = WorkerBot.Getpage(tedit.Item2)

        If (tuser.EditCount < 500 And (Not exuserlist.Contains(tuser.UserName)) Or achecklist.Contains(tuser.UserName)) Then
            If Date.UtcNow.Subtract(tpage.LastEdit).Minutes < 4 Then Return False
            Return AddMissingSignature2(tpage, False, True, String.Empty) '"TEST (" & GlobalVars.Codename & " " & GlobalVars.MwBotVersion & "/" & BotName & " " & BotVersion & "):"
        End If
        EventLogger.Log(String.Format(BotMessages.NotSigned, tpage.Title) & " INFO: EC=" & tuser.EditCount _
                              & " EXULIST=" & exuserlist.Contains(tuser.UserName).ToString & " ACHECK=" & achecklist.Contains(tuser.UserName).ToString & " EXPLIST=" & expageslist.Contains(tpagename).ToString, "ResolveQueue", WorkerBot.UserName)
        Return False
    End Function


    Function AddMissingSignature2(ByRef tpage As Page, newthreads As Boolean, minor As Boolean, addmsg As String) As Boolean
        If Not PageIsAutoSignable(tpage) Then Return False 'Verificar si la pagina puede firmarse.

        Dim UnsignedSectionInfo As Tuple(Of String, String, Date) = GetLastUnsignedSection2(tpage, newthreads)
        If UnsignedSectionInfo Is Nothing Then Return False

        Dim UnsignedThread As String = UnsignedSectionInfo.Item1
        Dim lastparagraph As String = Regex.Match(UnsignedThread.TrimEnd, ".+(?=\n+==[^=].+==[^=]|$|\n+$)").Value
        If String.IsNullOrWhiteSpace(lastparagraph) Then Return False
        If Regex.Match(lastparagraph, "\[\[(:\w{2,7}:)*(user|usuario):.+?\]\]", RegexOptions.IgnoreCase).Success Then Return False
        If Regex.Match(lastparagraph, signpattern).Success Then Return False

        Dim Username As String = UnsignedSectionInfo.Item2
        EventLogger.Log(String.Format(BotMessages.UnsignedMessageDetected, tpage.Title), "AddMissingSignature2", WorkerBot.UserName)

        Dim UnsignedDate As Date = UnsignedSectionInfo.Item3
        Dim dstring As String = GetSpanishTimeString(UnsignedDate)
        Dim newText As String = tpage.Content.Replace(UnsignedThread, UnsignedThread.TrimEnd & " {{sust:No firmado|" & Username & "|" & dstring & "}}" & Environment.NewLine)

        If tpage.Save(newText, addmsg & String.Format(BotMessages.UnsignedSumm, Username), minor, True) = EditResults.Edit_successful Then Return True
        EventLogger.Log(String.Format(BotMessages.NotSigned, tpage.Title), "AddMissingSignature2", WorkerBot.UserName)
        Return False
    End Function

    Function PageIsAutoSignable(ByRef tpage As Page) As Boolean
        If tpage.Lastuser = WorkerBot.UserName Then Return False 'No completar firma en páginas en las que el mismo bot haya editado.
        If LastUserIsBot(tpage) Then Return False 'No firmar ediciones de bot.
        If tpage.Comment.ToLower.Contains("revertidos los cambios") Then Return False 'No firmar reversiones, nunca.
        If tpage.Comment.ToLower.Contains("deshecha la edición") Then Return False 'No firmar ediciones deshechas, nunca.
        If EditedByOwner(tpage) Then Return False 'No completar firma en páginas de usuario en las que el mismo usuario haya editado.
        If GetThreadCountDiffLastEdit(tpage) >= 2 Then Return False 'Si el usuario edita 2 o mas hilos de golpe ignorar el edit.
        If IsOverORESThreshold(tpage) Then Return False 'Si el edit tiene un puntaje ores 'damaging' sobre el limite ignorarlo.
        If Not ContainsAutosignatureTemplate(tpage.Content) Then Return False 'De momento solo firmar las páginas con la plantilla de firma automática.
        Return True
    End Function

    Function IsOverORESThreshold(ByRef tpage As Page) As Boolean
        Return tpage.ORESScores(0) < OresThreshold
    End Function

    Function EditedByOwner(ByRef tpage As Page) As Boolean
        Dim tusername As String = String.Empty
        If tpage.PageNamespace = 3 Then
            If tpage.Title.Contains(":") Then
                tusername = tpage.Title.Split(":"c)(1)
                If tusername.Contains("/") Then
                    tusername = tusername.Split("/"c)(0)
                End If
                tusername = tusername.Trim()
            End If
        End If
        If tpage.Lastuser = tusername Then Return True
        Return False
    End Function

    Function LastUserIsBot(ByRef tpage As Page) As Boolean
        Return New WikiUser(WorkerBot, tpage.Lastuser).IsBot
    End Function

    ''' <summary>
    ''' Retorna la diferencia en la cantidad de hilos entre la edicion actual y la anterior.
    ''' </summary>
    ''' <param name="tpage">Pagina a evaluar.</param>
    ''' <returns>Diferencia de hilos.</returns>
    Function GetThreadCountDiffLastEdit(ByRef tpage As Page) As Integer
        If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name, WorkerBot.UserName)
        Dim oldPage As Page = WorkerBot.Getpage(tpage.ParentRevId)
        Dim oldThreadsCount As Integer = oldPage.Threads.Count()
        Dim currentThreadsCount As Integer = tpage.Threads.Count()
        Return oldThreadsCount - currentThreadsCount
    End Function

    Function GetLastUnsignedSection2(ByRef tpage As Page, newthreads As Boolean) As Tuple(Of String, String, Date)
        If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name, WorkerBot.UserName)
        Dim oldPage As Page = WorkerBot.Getpage(tpage.ParentRevId)
        Dim currentPage As Page = tpage

        Dim oldPageThreads As String() = oldPage.Threads
        Dim currentPageThreads As String() = currentPage.Threads

        Dim LastEdit As Date = currentPage.LastEdit
        Dim LastUser As String = currentPage.Lastuser
        Dim editedthreads As String()

        If newthreads Then
            editedthreads = GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
        Else
            If oldPageThreads.Count = currentPageThreads.Count Then
                If oldPageThreads.Count = 0 Then
                    editedthreads = {currentPage.Content}
                Else
                    editedthreads = GetChangedThreads(oldPageThreads, currentPageThreads)
                End If
            ElseIf oldPageThreads.Count < currentPageThreads.Count Then
                editedthreads = GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
            Else
                editedthreads = {}
            End If
        End If

        If (editedthreads.Count > 0) AndAlso (Not String.IsNullOrWhiteSpace(editedthreads.Last)) Then
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
        Dim lastparagraph As String = String.Empty
        For count As Integer = 0 To 2
            lastparagraph = Regex.Match(text, ".+(?=\n+==.+==|$|\n+$)").Value
            If lastparagraph.Count < 10 Then
                text = Regex.Replace(text, ".+(?=\n+==.+==|$|\n+$)", "")
            Else
                Exit For
            End If
        Next
        Dim TheDate As Date = ESWikiDatetime(lastparagraph)
        EventLogger.Debug_Log("Returning " & TheDate.ToString, Reflection.MethodBase.GetCurrentMethod().Name, WorkerBot.UserName)
        Return TheDate
    End Function


End Class
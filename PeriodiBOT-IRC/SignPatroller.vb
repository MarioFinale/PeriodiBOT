﻿Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports MWBot.net.Utility.Utils
Imports MWBot.net.WikiBot
Imports PeriodiBOT_IRC.Initializer
Imports PeriodiBOT_IRC.My.Resources

Public Class SignPatroller

    Public OresThreshold As Double = 91.5#

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
#Disable Warning SYSLIB0014 ' Type or member is obsolete
                             Using tclient As New WebClient()
#Enable Warning SYSLIB0014 ' Type or member is obsolete
                                 EventLogger.Debug_Log("Connecting to WMF's recent changes stream...", "RecentChanges watcher")
                                 Dim tstream As Stream = tclient.OpenRead(New Uri("https://stream.wikimedia.org/v2/stream/recentchange"))
                                 Dim tstreamreader As StreamReader = New StreamReader(tstream)
                                 Dim StreamStartingTime As DateTime = Date.UtcNow()
                                 EventLogger.Debug_Log("Connected to WMF's recent changes stream!", "RecentChanges watcher")
                                 While True
                                     Dim timeElapsed As TimeSpan = Date.UtcNow.Subtract(StreamStartingTime)
                                     If timeElapsed >= New TimeSpan(0, 14, 58) Then 'WMF's HTTP connection termination layer enforces a connection timeout of 15 minutes.
                                         tstreamreader.Close()
                                         tstream.Close()
                                         tclient.Dispose()
                                         EventLogger.Debug_Log("Disconnecting WMF's recent changes stream before server-side timeout at 15 minutes", "RecentChanges watcher")
                                         Exit While
                                     End If
                                     Dim currentLine As String = tstreamreader.ReadLine
                                     If Not EditIsValid(currentLine) Then Continue While
                                     Dim editInfo As Tuple(Of String, String, Date) = GetEditInfoFromStreamLine(currentLine)
                                     If String.IsNullOrWhiteSpace(editInfo.Item1) Or String.IsNullOrWhiteSpace(editInfo.Item2) Then Continue While
                                     SyncLock editsqueue
                                         editsqueue.Enqueue(editInfo)
                                     End SyncLock
                                     EventLogger.Debug_Log("Edición en '" & editInfo.Item2 & "'" & " por '" & editInfo.Item1 & "'.", "RecentChanges watcher")
                                 End While
                             End Using
                         Catch ex As IOException
                             EventLogger.EX_Log(ex.Message, "AutoSignPatrol", WorkerBot.UserName)
                             Exit While
                         Catch ex2 As WebException
                             EventLogger.EX_Log(ex2.Message, "AutoSignPatrol", WorkerBot.UserName)
                             Exit While
                         End Try
                     End While
                 Catch ex As Exception When Not Debugger.IsAttached
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

    Private Function GetEditInfoFromStreamLine(ByRef tline As String) As Tuple(Of String, String, Date)
        Dim tusername As String = If(TextInBetween(tline, ",""user"":""", """,").Count >= 1, TextInBetween(tline, ",""user"":""", """,")(0), "")
        Dim tpagename As String = If(TextInBetween(tline, ",""title"":""", """,").Count >= 1, TextInBetween(tline, ",""title"":""", """,")(0), "")
        Dim tdate As Date = Date.UtcNow
        Return New Tuple(Of String, String, Date)(tusername, tpagename, tdate)
    End Function

    Private Function ContainsAutosignatureTemplate(ByRef pageContent As String) As Boolean
        Return Regex.IsMatch(pageContent, "{{ *[Ff]irma automática .*?}}")
    End Function

    Private Function EditIsValid(ByRef tline As String) As Boolean
        Return tline.Contains("""wiki"":""eswiki""") AndAlso
           Not tline.Contains("""bot"":true,") AndAlso
           tline.Contains(",""type"":""edit"",") AndAlso
           Regex.Match(tline, """namespace"":(1|3|9|11|13|15|101|103|105|829),").Success
    End Function


    Private Function ResolveQueue(ByRef editsQueue As Queue(Of Tuple(Of String, String, Date))) As Boolean
        Dim editTuple As Tuple(Of String, String, Date) = Nothing

        SyncLock editsQueue
            If editsQueue.Count = 0 Then Return False

            editTuple = editsQueue.Dequeue
            If Date.UtcNow.Subtract(editTuple.Item3).Minutes <= 1 Then
                editsQueue.Enqueue(editTuple)
                Return False
            End If
        End SyncLock

        If editTuple Is Nothing Then
            ' Handle the case where editTuple is not assigned within the SyncLock block.
            Return False
        End If

        Dim targetPageName As String = editTuple.Item2
        Dim exemptPagesList As String() = GetThreadList("Usuario:PeriodiBOT/Paginas exentas de firma")
        Dim exemptUsersList As String() = GetThreadList("Usuario:PeriodiBOT/Exentos firma")
        Dim checkAlwaysList As String() = GetThreadList("Usuario:PeriodiBOT/Comprobar siempre firma")
        Dim editingUser As WikiUser = New WikiUser(WorkerBot, editTuple.Item1)
        Dim targetPage As Page = WorkerBot.Getpage(editTuple.Item2)

        Dim logMessage As String = String.Format(BotMessages.NotSigned, targetPage.Title) & " INFO: USER=" & editingUser.UserName & "EC=" & editingUser.EditCount & " EXULIST=" & exemptUsersList.Contains(editingUser.UserName).ToString & " ACHECK=" & checkAlwaysList.Contains(editingUser.UserName).ToString & " EXPLIST=" & exemptPagesList.Contains(targetPageName).ToString
        EventLogger.Log(logMessage, "ResolveQueue", WorkerBot.UserName)

        If (Not exemptUsersList.Contains(editingUser.UserName) Or checkAlwaysList.Contains(editingUser.UserName)) Then
            If Date.UtcNow.Subtract(targetPage.LastEdit).Minutes < 1 Then
                ' Log another message for this case.
                EventLogger.Log(String.Format(BotMessages.NotSigned, targetPage.Title) & " INFO: User edited within 1 minute of the previous edit.", "ResolveQueue", WorkerBot.UserName)
                Return False
            End If
            Return AddMissingSignature2(targetPage, False, True, String.Empty)
        End If

        Return False
    End Function

    Private Function GetThreadList(threadPageTitle As String) As String()
        Dim threadPage As Page = WorkerBot.Getpage(threadPageTitle)
        Return (From tmatch In Regex.Matches(If(threadPage.Threads.Count >= 1, threadPage.Threads(0), ""), "\*.+(?=\n|$|\n+$)") Select CType(tmatch, Match).Value.Replace("* ", "").Trim).ToArray
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
        If tpage Is Nothing Then Return False 'No comprobar páginas nulas.
        If tpage.Lastuser.Equals(WorkerBot.UserName) Then Return False 'No completar firma en páginas en las que el mismo bot haya editado.
        If LastUserIsBot(tpage) Then Return False 'No firmar ediciones de bot.
        If tpage.Tags.Contains("mw-reverted") Then Return False  'No firmar reversiones (verificar el tag primero)
        If tpage.Tags.Contains("mw-rollback") Then Return False  'No firmar reversiones (verificar el tag primero)
        If tpage.Tags.Contains("mw-manual-revert") Then Return False  'No firmar reversiones manuales (verificar el tag primero)
        If tpage.Tags.Contains("mw-undo") Then Return False  'No firmar ediciones deshechas (verificar el tag primero)
        If tpage.Comment.ToLower.Contains("revertidos los cambios") Then Return False 'No firmar ediciones que dicen ser reversiones (aunque no tengan el tag).
        If tpage.Comment.ToLower.Contains("revierto") Then Return False 'No firmar ediciones que dicen ser reversiones (aunque no tengan el tag)
        If tpage.Comment.ToLower.StartsWith("rv:") Then Return False 'No firmar ediciones que dicen ser reversiones (aunque no tengan el tag)
        If tpage.Comment.ToLower.Contains("reverted") Then Return False 'No firmar ediciones que dicen ser reversiones en inglés (aunque no tengan el tag)
        If tpage.Comment.ToLower.Contains("deshecha la edición") Then Return False 'No firmar ediciones que dicen ser deshechas (aunque no tengan el tag)
        If tpage.Comment.ToLower.Contains("deshaciendo") Then Return False 'No firmar ediciones que dicen ser deshechas (aunque no tengan el tag)
        If tpage.Comment.ToLower.Contains("deshago") Then Return False 'No firmar ediciones que dicen ser deshechas (aunque no tengan el tag)
        If tpage.Threads.Count() <= 1 Then Return False 'No firmar páginas con 1 o menos hilos
        If EditedByOwner(tpage) Then Return False 'No completar firma en páginas de usuario en las que el mismo usuario haya editado.
        If GetThreadCountDiffLastEdit(tpage) >= 2 Then Return False 'Si el usuario edita 2 o mas hilos de golpe ignorar el edit.
        If IsOverORESThreshold(tpage) Then Return False 'Si el edit tiene un puntaje ores 'damaging' sobre el limite ignorarlo.
        If CheckIfLastEditAddedTemplateOnFirstLine(tpage) Then Return False 'Si la nueva edición solo añadió una plantilla al principio de la página, ignorar el edit
        If AuthorIsRevertingSignature(tpage) Then Return False

        ' If Not ContainsAutosignatureTemplate(tpage.Content) Then Return False 'De momento solo firmar las páginas con la plantilla de firma automática.
        Return True
    End Function

    ''' <summary>
    ''' Verifica si la edición anterior de la página es del bot firmando al mismo usuario que acaba de editar y si el usuario quitó contenido en la última edición.
    ''' </summary>
    ''' <param name="tpage">Página a comprobar.</param>
    ''' <returns>Retorna verdadero si la última edición es del usuario quitando contenido luego de que el bot complete una firma.</returns>
    Function AuthorIsRevertingSignature(ByVal tpage As Page) As Boolean
        Dim LastUser As String = tpage.Lastuser
        Dim CurrentPageSize As Integer = tpage.Size
        Dim PreviousEdit As Page = WorkerBot.Getpage(tpage.ParentRevId)
        Dim PreviousPageSize As Integer = PreviousEdit.Size
        Dim PreviousUser As String = PreviousEdit.Lastuser
        Dim PreviousEditRes As String = PreviousEdit.Comment
        If (PreviousUser.Equals(WorkerBot.UserName) And PreviousEditRes.Contains(LastUser) And PreviousEditRes.Contains("firma") And (PreviousPageSize > CurrentPageSize)) Then
            Return True
        End If
        Return False
    End Function

    Function IsOverORESThreshold(ByRef tpage As Page) As Boolean
        Return Not (tpage.ORESScore(1) > OresThreshold) '(0) damaging = true | (1) goodfaith = true
    End Function

    Function CheckIfLastEditAddedTemplateOnFirstLine(ByRef tpage As Page) As Boolean
        Dim currentText As String() = Regex.Split(tpage.Content, "[\r\n]+")
        Dim previousText As String() = Regex.Split(WorkerBot.Getpage(tpage.ParentRevId).Content, "[\r\n]+")

        If (previousText(0) = currentText(0)) Then Return False

        Dim previousHadTemplate As Boolean = (Template.GetTemplates(previousText(0)).Count >= 1)
        Dim currentHasTemplate As Boolean = (Template.GetTemplates(currentText(0)).Count >= 1)

        Return ((Not previousHadTemplate) And currentHasTemplate)
    End Function

    Private Function EditedByOwner(ByRef tpage As Page) As Boolean
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
        Dim username As String = tpage.Lastuser
        If String.IsNullOrWhiteSpace(username) Then Return True
        Dim user As WikiUser = New WikiUser(WorkerBot, tpage.Lastuser)
        Return user.IsBot
    End Function

    ''' <summary>
    ''' Retorna la diferencia en la cantidad de hilos entre la edicion actual y la anterior.
    ''' </summary>
    ''' <param name="tpage">Pagina a evaluar.</param>
    ''' <returns>Diferencia de hilos.</returns>
    Function GetThreadCountDiffLastEdit(ByRef tpage As Page) As Integer
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
                editedthreads = Array.Empty(Of String)
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
            Throw New ArgumentException("Empty parameter", NameOf(text))
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
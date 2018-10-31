Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC
Imports System.Text.RegularExpressions
Imports System.Text

Namespace WikiBot
    Public Class WikiTopicList
        Private _bot As Bot
        Sub New(ByVal workerbot As Bot)
            _bot = workerbot
        End Sub

        Function UpdateTopics() As Boolean
            Try
                Dim topicpage As Page = ESWikiBOT.Getpage(TopicPageName)
                Dim newtext As String = GetTopicsPageText()
                If Not newtext.Length = topicpage.Content.Length Then
                    topicpage.Save(newtext, "Bot: Actualizando temas", False, True)
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Utils.EventLogger.EX_Log(ex.Message, "UpdateTopics")
                Return False
            End Try
        End Function

        Private Function GetTopicsPageText() As String
            Dim scannedPages As Integer = 0
            Dim topics As SortedDictionary(Of String, List(Of String)) = GetTopicsText(scannedPages)
            Dim pagetext As String = "{{/Encabezado}}" & Environment.NewLine
            Dim UpdateDate As Date = Date.UtcNow
            Dim UpdateText As String = "<span style=""color:#0645AD"">►</span> Actualizado al " & Integer.Parse(UpdateDate.ToString("dd")).ToString & UpdateDate.ToString(" 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES")) _
                & " por [[Usuario:" & _bot.UserName & "|" & _bot.UserName & "]] sobre un total de " & scannedPages.ToString & " páginas de archivo." & Environment.NewLine

            pagetext = pagetext & Environment.NewLine & UpdateText
            Dim TopicGroups As SortedDictionary(Of String, List(Of String)) = GetTopicGroups()

            Dim EndList As New SortedDictionary(Of String, SortedDictionary(Of String, List(Of String))) 'Diccionario con (grupo,(tema, lineas()))

            For Each topic As String In topics.Keys 'Por cada tema
                Dim hasGroup As Boolean = False 'Tiene grupo?
                For Each group As String In TopicGroups.Keys 'Por cada grupo
                    If TopicGroups(group).Contains(topic) Then 'Si el grupo contiene el tema
                        hasGroup = True 'Si tiene grupo
                        If Not EndList.Keys.Contains(group) Then 'Si el diccionario final no contiene el grupo
                            EndList.Add(group, New SortedDictionary(Of String, List(Of String))) 'Se añade el grupo al diccionario final
                        End If
                        If Not EndList(group).Keys.Contains(topic) Then 'Si el diccionario del grupo del diccionario final no contiene el tema
                            EndList(group).Add(topic, New List(Of String)) 'Se añade el tema al diccionario del grupo en el diccionario final
                        End If
                        EndList(group)(topic).AddRange(topics(topic)) 'Añade todas la líneas del tema a la lista del tema en el diccionario del grupo en el diccionario final
                    End If
                Next
                If Not hasGroup Then 'Si ningún grupo contiene el tema
                    If Not EndList.Keys.Contains("Varios") Then 'Si el diccionario final no contiene el grupo "varios"
                        EndList.Add("Varios", New SortedDictionary(Of String, List(Of String))) 'Se añade el grupo "varios" al diccionario final
                    End If
                    If Not EndList("Varios").Keys.Contains(topic) Then 'Si el diccionario del grupo "varios" del diccionario final no contiene el tema
                        EndList("Varios").Add(topic, New List(Of String)) 'Se añade el tema al diccionario del grupo "varios" en el diccionario final
                    End If
                    EndList("Varios")(topic).AddRange(topics(topic)) 'Añade todas la líneas del tema a la lista del tema en el diccionario del grupo "varios" en el diccionario final
                End If
            Next

            For Each g As String In EndList.Keys 'Por cada grupo en la lista
                pagetext = pagetext & Environment.NewLine & Environment.NewLine & "== " & g & "==" & Environment.NewLine 'Añadir al texto el título
                For Each t As String In EndList(g).Keys 'Por cada tema
                    pagetext = pagetext & Environment.NewLine & "=== " & t & " ===" & Environment.NewLine 'Añadir el tema al texto
                    For Each l As String In EndList(g)(t) 'Por cada linea del tema
                        pagetext = pagetext & Environment.NewLine & l
                    Next
                Next
            Next
            Return pagetext
        End Function

        Private Function GetTopicGroups() As SortedDictionary(Of String, List(Of String))
            Dim GroupsPage As Page = _bot.Getpage(TopicGroupsPage) 'Inicializar página de grupos
            Dim Threads As String() = Utils.GetPageThreads(GroupsPage.Content) 'Obtener hilos de la página

            Dim Groups As New SortedDictionary(Of String, List(Of String))
            For Each t As String In Threads 'Por cada hilo...
                Dim threadTitle As String = Regex.Match(t, "(\n|^)(==.+==)").Value.Trim.Trim("="c).Trim 'Obtiene el título del hilo 
                If Not Groups.Keys.Contains(threadTitle) Then 'Si el diccionario no contiene el título del hilo...
                    Groups.Add(threadTitle, New List(Of String)) 'Añade el hilo al diccionario
                End If
                For Each l As String In Utils.GetLines(t, True) 'Por cada línea en el hilo...
                    If Not Regex.Match(l, "(\n|^)(==.+==)").Success Then 'Si no es el título del hilo
                        Dim top As String = l.Trim.Trim("*"c).Trim 'Eliminar los espacios en blanco y asteriscos al principio y al final
                        Groups(threadTitle).Add(top) 'Añadirlo a la clave en el diccionario
                    End If
                Next
            Next
            'Regresar el diccionario
            Return Groups
        End Function


        Private Function GetTopicsText(ByRef inclusions As Integer) As SortedDictionary(Of String, List(Of String))
            Dim TopicThreads As SortedList(Of String, WikiTopic) = GetAllTopicThreads(inclusions) 'Obtener los temas y la información
            Dim TopicList As New SortedDictionary(Of String, List(Of String)) 'Inicializar la lista con el texto

            For Each topic As String In TopicThreads.Keys 'Por cada tema...
                If Not TopicList.Keys.Contains(topic) Then 'Si no está en el diccionario, se añade
                    TopicList.Add(topic, New List(Of String))
                End If
                For Each thread As WikiTopicThread In TopicThreads.Item(topic).Threads 'Por cada hilo en el tema
                    Dim line As String = "* " 'Inicializar la línea con los datos
                    Dim threadDate As String = thread.FirstSignature.ToString("dd-MM-yyyy") & " " 'Fecha
                    Dim threadType As String = thread.Subsection & " " 'En que zona del café está
                    Dim threadTitle As String = thread.ThreadTitle & " " 'Título del hilo
                    Dim threadResume As String = String.Empty
                    If Not String.IsNullOrWhiteSpace(thread.ThreadResume) Then
                        threadResume = "- " & thread.ThreadResume.TrimEnd("."c) & ". " 'Resumen del hilo
                    End If
                    Dim threadLink As String = thread.ThreadLink & " " 'Enlace al hilo
                    Dim threadSize As String = "(" & Math.Ceiling(thread.ThreadBytes / 1024).ToString & "&nbsp;kB)" 'Kbytes del hilo

                    line = line & threadDate & threadType & "- " & "[[" & threadLink.Trim & "|" & threadTitle.Trim & "]] " & threadResume & threadSize 'Poner todo junto
                    TopicList.Item(topic).Add(line) 'Añadirlo a la lista de la clave en el diccionario
                Next
            Next
            'Regresar el diccionario
            Return TopicList
        End Function

        Private Function GetAllTopicThreads(ByRef inclusions As Integer) As SortedList(Of String, WikiTopic)
            Dim Topiclist As New SortedList(Of String, WikiTopic)
            Dim pages As String() = _bot.GetallInclusions(TopicTemplate) 'Paginas que incluyen la plantilla de tema.
            For Each p As String In pages 'Por cada página que incluya la plantilla tema, no se llama a GetallInclusionsPages por temas de memoria.
                Topiclist = GetTopicsOfpage(_bot.Getpage(p), Topiclist) 'Añadir nuevos hilos
            Next
            inclusions = pages.Length 'Cuantas páginas se revisaron
            Return Topiclist 'Retorna el diccionario
        End Function

        Private Function GetTopicsOfpage(ByVal sourcePage As Page, ByVal TopicList As SortedList(Of String, WikiTopic)) As SortedList(Of String, WikiTopic)
            If TopicList Is Nothing Then 'Si no está inicializado correctamente...
                TopicList = New SortedList(Of String, WikiTopic)
            End If
            Dim Threads As SortedSet(Of WikiTopicThread) = GetTopicsThreads(sourcePage)
            For Each Thread As WikiTopicThread In Threads 'Por cada hilo....
                'Añadir temas a diccionario
                For Each topic As String In Thread.Topics
                    If Not TopicList.Keys.Contains(topic) Then
                        'si no existe el tema en el diccionario, lo inicializa y añade el hilo
                        TopicList.Add(topic, New WikiTopic(topic, New SortedSet(Of WikiTopicThread)))
                        TopicList.Item(topic).Threads.Add(Thread)
                    Else
                        'si existe solo añade el hilo
                        TopicList.Item(topic).Threads.Add(Thread)
                    End If
                Next
            Next
            Return TopicList
        End Function

        Private Function GetTitleAndLink(ByVal Pagetitle As String, ByVal thread As String) As Tuple(Of String, String)
            Dim threadTitle As String = Regex.Match(thread, "(\n|^)(==.+==)").Value.Trim.Trim("="c).Trim 'Inicializa el título del hilo 
            If (Regex.Match(threadTitle, "(\[\[[^\]]+\|)").Success) Or (Regex.Match(threadTitle, "(\[{1,2}[^\|\]]+\]{1,2})").Success) Then
                If Regex.Match(threadTitle, "(\[{1,2}[^\|\]]+\]{1,2})").Success Then
                    Dim threadmatches As MatchCollection = Regex.Matches(threadTitle, "(\[{1,2}[^\|\]]+\]{1,2})")
                    For Each tm As Match In threadmatches
                        Dim threadsimplelink As String = tm.Value
                        Dim newthreadsimplelink As String = threadsimplelink.Replace("["c, "").Replace("]"c, "").Trim.TrimStart(":"c).Trim
                        threadTitle = threadTitle.Replace(threadsimplelink, newthreadsimplelink)
                    Next
                End If
                threadTitle = Regex.Replace(threadTitle, "(\[{1,2}[^\|\]]+)", "").Replace("]"c, "").Replace("|"c, "")
            End If
            threadTitle = Regex.Replace(threadTitle, "<+.+?>+", "") 'Quitar etiquetas HTML
            Dim threadTitleLink As String = Utils.UrlWebEncode(threadTitle.Trim.Replace(" ", "_").Replace("'''", "").Replace("''", ""))
            Dim threadLink As String = Pagetitle & "#" & threadTitleLink 'Generar enlace al hilo específico

            threadTitle = Regex.Replace(threadTitle, "\{{1,2}|\}{1,2}", "") 'Quitar plantillas
            Return New Tuple(Of String, String)(threadTitle, threadLink)
        End Function

        Private Function GetTopicsThreads(ByVal tPage As Page) As SortedSet(Of WikiTopicThread)
            Dim Threadlist As New SortedSet(Of WikiTopicThread)
            Dim threads As String() = tPage.Threads
            For Each t As String In threads
                Dim TopicMatch As Match = Regex.Match(t, "({{ *[Tt]ema *\|.+?}})") 'Regex para plantilla de tema
                'Si la plantilla de tema se encuentra en el hilo:
                If TopicMatch.Success Then
                    Dim TitleAndLink As Tuple(Of String, String) = GetTitleAndLink(tPage.Title, t)
                    Dim threadTitle As String = TitleAndLink.Item1
                    Dim threadLink As String = TitleAndLink.Item2
                    Dim threadResume As String = String.Empty 'Inicializa el resumen del hilo
                    Dim lastsignature As Date = Utils.FirstDate(t) 'Firma más antigua del hilo
                    If lastsignature.Year = 9999 Then 'Hilo con plantilla pero sin firma
                        Continue For 'No añadir hilo
                    End If
                    Dim threadBytes As Integer = Encoding.Unicode.GetByteCount(t) 'Bytes del hilo 
                    Dim Subsection As String = "Miscelánea" 'Subsección por defecto, aplica para cuando el café aún no era dividido por subpáginas
                    If Regex.Match(tPage.Title, "(\/Archivo\/.+?)(\/)").Success Then 'Obtener el café del archivado
                        Subsection = Regex.Match(tPage.Title, "(\/Archivo\/.+?)(\/)").Value.Trim("/"c).Split("/"c)(1) 'Café del archivado
                    End If
                    Dim TopicTemp As New Template(TopicMatch.Value, False) 'Inicializa una plantilla usando el pseudoparser entregando el texto de la plantilla como parámetro
                    Dim TopicList As New List(Of String) 'lista con los temas del hilo

                    'Obtener datos de la plantilla
                    For Each p As Tuple(Of String, String) In TopicTemp.Parameters 'por cada parámetro en la plantilla de tema...
                        If p.Item1.ToLower.Contains("resumen") Then
                            threadResume = p.Item2 'Cambia el resumen del hilo
                        Else
                            TopicList.Add(p.Item2) 'Añade el tema a la lista de temas
                        End If
                    Next
                    Dim Thread As New WikiTopicThread With {
                        .FirstSignature = lastsignature,
                        .Subsection = Subsection,
                        .ThreadBytes = threadBytes,
                        .ThreadLink = threadLink,
                        .ThreadResume = threadResume,
                        .ThreadTitle = threadTitle,
                        .Topics = TopicList
                    }
                    Threadlist.Add(Thread)
                End If
            Next
            Return Threadlist
        End Function

        Function BiggestThreadsEver() As Boolean
            Dim PageText As String = "{{/Header}}" & Environment.NewLine
            Dim threadlist As New SortedList(Of Integer, List(Of Tuple(Of String, String, Date)))
            Dim Pages As String() = _bot.PrefixSearch("Wikipedia:Café/")
            Dim TotalThreadCount As Integer = 0
            For Each CPage As String In Pages
                Dim wPage As Page = _bot.Getpage(CPage)
                For Each thread As String In wPage.Threads
                    TotalThreadCount += 1
                    Dim TitleAndLink As Tuple(Of String, String) = GetTitleAndLink(CPage, thread)
                    Dim ThreadSize As Integer = Encoding.Unicode.GetByteCount(thread) 'bytes del hilo
                    Dim ThreadDate As Date = Utils.FirstDate(thread)
                    If Not threadlist.Keys.Contains(ThreadSize) Then
                        threadlist.Add(ThreadSize, New List(Of Tuple(Of String, String, Date)))
                    End If
                    threadlist(ThreadSize).Add(New Tuple(Of String, String, Date)(TitleAndLink.Item1, TitleAndLink.Item2, ThreadDate))
                Next
            Next
            Dim threadcount As Integer = threadlist.Count
            Dim Top100 As New HashSet(Of Tuple(Of Integer, String, String, Date))
            For i As Integer = threadcount - 1 To threadcount - 101 Step -1
                For Each t As Tuple(Of String, String, Date) In threadlist(threadlist.Keys(i))
                    Top100.Add(New Tuple(Of Integer, String, String, Date)(threadlist.Keys(i), t.Item1, t.Item2, t.Item3))
                Next
            Next

            For i As Integer = 0 To 10
                PageText = PageText & "*[[" & (Top100(i).Item3) & "|" & Top100(i).Item2 & "]]. Iniciado el " & Utils.GetSpanishTimeString(Top100(i).Item4) & " con un peso de " & Utils.GetSizeAsString(Top100(i).Item1) & "." & Environment.NewLine
            Next
            PageText = PageText & Environment.NewLine & "== Más hilos grandes ==" & Environment.NewLine
            For i As Integer = 11 To Top100.Count - 1
                PageText = PageText & "*[[" & (Top100(i).Item3) & "|" & Top100(i).Item2 & "]]. Iniciado el " & Utils.GetSpanishTimeString(Top100(i).Item4) & " con un peso de " & Utils.GetSizeAsString(Top100(i).Item1) & "." & Environment.NewLine
            Next
            Dim tPage As Page = _bot.Getpage("Usuario:PeriodiBOT/Curiosidades/Hilos más largos en la historia del café")
            If tPage.Save(PageText, "Bot: Generando lista.", True, True) = EditResults.Edit_successful Then
                Return True
            Else
                Return False
            End If
        End Function
    End Class
End Namespace
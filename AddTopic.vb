Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC
Imports System.Text.RegularExpressions
Imports System.Text
Imports PeriodiBOT_IRC.CommFunctions

Namespace WikiBot
    Public Class AddTopic
        Private _bot As Bot
        Sub New(ByVal workerbot As Bot)
            _bot = workerbot
        End Sub

        Function UpdateTopics() As Boolean
            Try
                Dim topicpage As Page = ESWikiBOT.Getpage(TopicPageName)
                Dim newtext As String = GetTopicsPageText()
                If Not newtext.Length = topicpage.Text.Length Then
                    topicpage.Save(GetTopicsPageText(), "Bot: Actualizando temas", False, True)
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                EventLogger.EX_Log(ex.Message, "UpdateTopics")
                Return False
            End Try
        End Function


        Function GetTopicsPageText() As String
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


        Function GetTopicGroups() As SortedDictionary(Of String, List(Of String))
            Dim GroupsPage As Page = _bot.Getpage(TopicGroupsPage) 'Inicializar página de grupos
            Dim Threads As String() = _bot.GetPageThreads(GroupsPage.Text) 'Obtener hilos de la página
            Dim Groups As New SortedDictionary(Of String, List(Of String))
            For Each t As String In Threads 'Por cada hilo...
                Dim threadTitle As String = Regex.Match(t, "(\n|^)(==.+==)").Value.Trim.Trim("="c).Trim 'Obtiene el título del hilo 
                If Not Groups.Keys.Contains(threadTitle) Then 'Si el diccionario no contiene el título del hilo...
                    Groups.Add(threadTitle, New List(Of String)) 'Añade el hilo al diccionario
                End If
                For Each l As String In GetLines(t, True) 'Por cada línea en el hilo...
                    If Not Regex.Match(l, "(\n|^)(==.+==)").Success Then 'Si no es el título del hilo
                        Dim top As String = l.Trim.Trim("*"c).Trim 'Eliminar los espacios en blanco y asteriscos al principio y al final
                        Groups(threadTitle).Add(top) 'Añadirlo a la clave en el diccionario
                    End If
                Next
            Next
            'Regresar el diccionario
            Return Groups
        End Function


        Function GetTopicsText(Optional ByRef Inclusions As Integer = 0) As SortedDictionary(Of String, List(Of String))
            Dim TopicThreads As SortedDictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) = GetAllTopicThreads(Inclusions) 'Obtener los temas y la información
            Dim TopicList As New SortedDictionary(Of String, List(Of String)) 'Inicializar la lista con el texto

            For Each topic As String In TopicThreads.Keys 'Por cada tema...
                If Not TopicList.Keys.Contains(topic) Then 'Si no está en el diccionario, se añade
                    TopicList.Add(topic, New List(Of String))
                End If
                For Each thread As Tuple(Of String, String, String, String, Date, Integer) In TopicThreads.Item(topic) 'Por cada hilo en el tema
                    Dim line As String = "* " 'Inicializar la línea con los datos
                    Dim threadDate As String = thread.Item5.ToString("dd-MM-yyyy") & " " 'Fecha
                    Dim threadType As String = thread.Item4 & " " 'En que zona del café está
                    Dim threadTitle As String = thread.Item1 & " " 'Título del hilo
                    Dim threadResume As String = String.Empty
                    If Not String.IsNullOrWhiteSpace(thread.Item2) Then
                        threadResume = "- " & thread.Item2.TrimEnd("."c) & ". " 'Resumen del hilo
                    End If
                    Dim threadLink As String = thread.Item3 & " " 'Enlace al hilo
                    Dim threadSize As String = "(" & Math.Ceiling(thread.Item6 / 1024).ToString & "&nbsp;kB)" 'Kbytes del hilo

                    line = line & threadDate & threadType & "- " & "[[" & threadLink.Trim & "|" & threadTitle.Trim & "]] " & threadResume & threadSize 'Poner todo junto
                    TopicList.Item(topic).Add(line) 'Añadirlo a la lista de la clave en el diccionario
                Next
            Next
            'Regresar el diccionario
            Return TopicList
        End Function


        Function GetAllTopicThreads(Optional ByRef Inclusions As Integer = 0) As SortedDictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer)))
            Dim TopicAndTitleList As New SortedDictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) 'Inicializar diccionario que contiene temas e hilos
            Dim pages As String() = _bot.GetallInclusions(TopicTemplate) 'Paginas que incluyen la plantilla de tema.
            For Each p As String In pages 'Por cada página que incluya la plantilla tema, no se llama a GetallInclusionsPages por temas de memoria.
                TopicAndTitleList = GetTopicsOfpage(_bot.Getpage(p), TopicAndTitleList) 'Añadir nuevos hilos al diccionario
            Next
            Inclusions = pages.Length 'Cuantas páginas se revisaron
            Return TopicAndTitleList 'Retorna el diccionario
        End Function

        Function GetTopicsOfpage(ByVal SourcePage As Page, ByVal TopicAndTitleList As SortedDictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer)))) As SortedDictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer)))
            Dim Text As String = SourcePage.Text 'Texto de la página
            Dim PageTitle As String = SourcePage.Title 'Título de la página (con el espacio de nombres).

            If TopicAndTitleList Is Nothing Then 'Si no está inicializado correctamente...
                TopicAndTitleList = New SortedDictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) 'Diccionario: Tema,(titulo, resumen, enlace, ubicación, fecha, bytes)
            End If

            For Each t As String In _bot.GetPageThreads(Text) 'Por cada hilo en el texto....

                Dim TopicMatch As Match = Regex.Match(t, "({{ *[Tt]ema *\|.+?}})") 'Regex para plantilla de tema
                'Si la plantilla de tema se encuentra en el hilo:

                If TopicMatch.Success Then
                    Dim threadTitle As String = Regex.Match(t, "(\n|^)(==.+==)").Value.Trim.Trim("="c).Trim 'Inicializa el título del hilo 
                    'Normalizar el título del hilo si tiene enlaces
                    '----------
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
                    '----------
                    threadTitle = Regex.Replace(threadTitle, "<+.+?>+", "") 'Quitar etiquetas HTML
                    Dim threadTitleLink As String = UrlWebEncode(threadTitle.Trim.Replace(" ", "_").Replace("'''", "").Replace("''", ""))
                    Dim threadLink As String = PageTitle & "#" & threadTitleLink 'Generar enlace al hilo específico
                    threadTitle = Regex.Replace(threadTitle, "\{{1,2}|\}{1,2}", "") 'Quitar plantillas
                    Dim threadResume As String = String.Empty 'Inicializa el resumen del hilo
                    Dim threadBytes As Integer = Encoding.Unicode.GetByteCount(t) 'Bytes del hilo 
                    Dim lastsignature As Date = _bot.FirstDate(t) 'Firma más antigua del hilo
                    If lastsignature.Year = 9999 Then 'Hilo con plantilla pero sin firma
                        Continue For 'No añadir hilo
                    End If
                    Dim Subsection As String = "Miscelánea" 'Subsección por defecto, aplica para cuando el café aún no era dividido por subpáginas
                    If Regex.Match(PageTitle, "(\/Archivo\/.+?)(\/)").Success Then 'Obtener el café del archivado
                        Subsection = Regex.Match(PageTitle, "(\/Archivo\/.+?)(\/)").Value.Trim("/"c).Split("/"c)(1) 'Café del archivado
                    End If
                    Dim TopicTemp As New Template(TopicMatch.Value, False) 'Inicializa una plantilla usando el pseudoparser entregando el texto de la plantilla como parámetro
                    Dim Topics As New List(Of String) 'lista con los temas del hilo
                    'Obtener datos de la plantilla
                    For Each p As Tuple(Of String, String) In TopicTemp.Parameters 'por cada parámetro en la plantilla de tema...
                        If p.Item1.ToLower.Contains("resumen") Then
                            threadResume = p.Item2 'Cambia el resumen del hilo
                        Else
                            Topics.Add(p.Item2) 'Añade el tema a la lista de temas
                        End If
                    Next
                    'Añadir temas a diccionario
                    For Each topic As String In Topics
                        If Not TopicAndTitleList.Keys.Contains(topic) Then
                            'si no existe el tema en el diccionario, lo inicializa y añade el hilo
                            TopicAndTitleList.Add(topic, New List(Of Tuple(Of String, String, String, String, Date, Integer)))
                            TopicAndTitleList.Item(topic).Add(New Tuple(Of String, String, String, String, Date, Integer)(threadTitle, threadResume, ThreadLink, Subsection, lastsignature, threadBytes))
                            TopicAndTitleList.Item(topic) = TopicAndTitleList.Item(topic).OrderBy(Function(x) x.Item5).ToList
                        Else
                            'si existe solo añade el hilo
                            TopicAndTitleList.Item(topic).Add(New Tuple(Of String, String, String, String, Date, Integer)(threadTitle, threadResume, ThreadLink, Subsection, lastsignature, threadBytes))
                            TopicAndTitleList.Item(topic) = TopicAndTitleList.Item(topic).OrderBy(Function(x) x.Item5).ToList
                        End If
                    Next

                End If
            Next
            Return TopicAndTitleList 'Retorna el diccionario
        End Function

    End Class
End Namespace
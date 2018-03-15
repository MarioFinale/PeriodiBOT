Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC
Imports System.Text.RegularExpressions
Imports System.Text

Namespace WikiBot
    Public Class AddTopic
        Private _bot As Bot
        Sub New(ByVal workerbot As Bot)
            _bot = workerbot
        End Sub


        Function GetTopicsPageText() As String
            Dim scannedPages As Integer = 0
            Dim pagetext As String = "{{/Encabezado}}" & Environment.NewLine
            Dim UpdateDate As Date = Date.UtcNow
            Dim UpdateText As String = "<span style=""color:#0645AD"">►</span> Actualizado por " & BOTName & " al " & UpdateDate.ToString("dd 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES")) & " sobre un total de " & scannedPages.ToString & "páginas de archivo."

            Dim MainTopics As String() = {"Bloqueos y suspensiones", "Comunidad", "Edición", "Organización", "Temas técnicos", "Títulos", "Wikimedia y proyectos Wikimedia", "Varios"}










        End Function



        Function GetTopicsText(Optional ByRef Inclusions As Integer = 0) As Dictionary(Of String, List(Of String))
            Dim TopicThreads As Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) = GetAllTopicThreads(Inclusions) 'Obtener los temas y la información
            Dim TopicList As New Dictionary(Of String, List(Of String)) 'Inicializar la lista con el texto

            For Each topic As String In TopicThreads.Keys 'Por cada tema...
                If Not TopicList.Keys.Contains(topic) Then 'Si no está en el diccionario, se añade
                    TopicList.Add(topic, New List(Of String))
                End If
                For Each thread As Tuple(Of String, String, String, String, Date, Integer) In TopicThreads.Item(topic) 'Por cada hilo en el tema
                    Dim line As String = "* " 'Inicializar la línea con los datos
                    Dim threadDate As String = thread.Item5.ToString("dd-MM-yyyy") & " " 'Fecha
                    Dim threadType As String = thread.Item4 & " " 'En que zona del café está
                    Dim threadTitle As String = thread.Item1 & " " 'Título del hilo
                    Dim threadResume As String = "- " & thread.Item2 & " " 'Resumen del hilo
                    Dim threadLink As String = thread.Item3 & " " 'Enlace al hilo
                    Dim threadSize As String = "(" & Math.Ceiling(thread.Item6 / 1024).ToString & "&nbsp;kB)" 'Kbytes del hilo

                    line = line & threadDate & threadType & "- " & threadResume & "[[" & threadLink & "|" & threadTitle & "]]" & threadResume & threadSize 'Poner todo junto
                    TopicList.Item(topic).Add(line) 'Añadirlo a la lista de la clave en el diccionario
                Next
            Next
            'Regresar el diccionario
            Return TopicList
        End Function


        Function GetAllTopicThreads(Optional ByRef Inclusions As Integer = 0) As Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer)))
            Dim TopicAndTitleList As New Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) 'Inicializar diccionario que contiene temas e hilos
            Dim pages As String() = _bot.GetallInclusions(TopicTemplate) 'Paginas que incluyen la plantilla de tema.
            For Each p As String In pages 'Por cada página que incluya la plantilla tema, no se llama a GetallInclusionsPages por temas de memoria.
                TopicAndTitleList = GetTopicsOfpage(_bot.Getpage(p), TopicAndTitleList) 'Añadir nuevos hilos al diccionario
            Next
            Inclusions = pages.Length 'Cuantas páginas se revisaron
            Return TopicAndTitleList 'Retorna el diccionario
        End Function

        Function GetTopicsOfpage(ByVal SourcePage As Page, ByVal TopicAndTitleList As Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer)))) As Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer)))
            Dim Text As String = SourcePage.Text 'Texto de la página
            Dim PageTitle As String = SourcePage.Title 'Título de la página (con el espacio de nombres).

            If TopicAndTitleList Is Nothing Then 'Si no está inicializado correctamente...
                TopicAndTitleList = New Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) 'Diccionario: Tema,(titulo, resumen, enlace, ubicación, fecha, bytes)
            End If

            For Each t As String In _bot.GetPageThreads(Text) 'Por cada hilo en el texto....

                Dim TopicMatch As Match = Regex.Match(t, "({{[Tt]ema.+?}})") 'Regex para plantilla de tema
                'Si la plantilla de tema se encuentra en el hilo:

                If TopicMatch.Success Then
                    Dim threadTitle As String = Regex.Match(t, "(\n|^)(==.+==)").Value.Trim.Trim("="c).Trim 'Inicializa el título del hilo 
                    'Normalizar el título del hilo si tiene enlaces
                    '----------
                    If (Regex.Match(threadTitle, "(\[\[[^\]]+\|)").Success) Or (Regex.Match(threadTitle, "(\[{1,2}[^\|\]]+\]{1,2})").Success) Then
                        If Regex.Match(threadTitle, "(\[{1,2}[^\|\]]+\]{1,2})").Success Then
                            Dim threadsimplelink As String = Regex.Match(threadTitle, "(\[{1,2}[^\|\]]+\]{1,2})").Value
                            Dim newthreadsimplelink As String = threadsimplelink.Replace("["c, "").Replace("]"c, "")
                            threadTitle = threadTitle.Replace(threadsimplelink, newthreadsimplelink)
                        End If
                        threadTitle = Regex.Replace(threadTitle, "(\[{1,2}[^\|\]]+)", "").Replace("]"c, "").Replace("|"c, "")
                    End If
                    '----------
                    Dim ThreadLink As String = (PageTitle & "#" & threadTitle).Replace(" "c, "_"c) 'Generar enlace al hilo específico
                    Dim threadResume As String = String.Empty 'Inicializa el resumen del hilo
                    Dim threadBytes As Integer = Encoding.Unicode.GetByteCount(t) 'Bytes del hilo 
                    Dim lastsignature As Date = _bot.MostRecentDate(t) 'Firma más nueva del hilo
                    Dim Subsection As String = "Miscelánea"
                    If Regex.Match(ThreadLink, "(\/Archivo\/.+?)(\/)").Success Then
                        Subsection = Regex.Match(ThreadLink, "(\/Archivo\/.+?)(\/)").Value.Trim("/"c).Split("/"c)(1) 'Café del archivado
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
                        Else
                            'si existe solo añade el hilo
                            TopicAndTitleList.Item(topic).Add(New Tuple(Of String, String, String, String, Date, Integer)(threadTitle, threadResume, ThreadLink, Subsection, lastsignature, threadBytes))
                        End If
                    Next

                End If

            Next
            Return TopicAndTitleList 'Retorna el diccionario
        End Function



    End Class
End Namespace
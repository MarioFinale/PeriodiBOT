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


        Function GetPageText() As String
            Dim IncludedPages As Integer = 0
            Dim TopicThreads As Dictionary(Of String, List(Of Tuple(Of String, String, String, String, Date, Integer))) = GetAllTopicThreads(IncludedPages)

            Dim TopicList As New Dictionary(Of String, List(Of String))

            For Each topic As String In TopicThreads.Keys
                If Not TopicList.Keys.Contains(topic) Then
                    TopicList.Add(topic, New List(Of String))
                End If

                Dim Line As String = "* "





            Next






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
                Dim TopicMatch As Match = Regex.Match(t, "({{[Tt]ema.+?}})") 'Regex para plantilla de tema

                'Si la plantilla de tema se encuentra en el hilo:
                If TopicMatch.Success Then
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
Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC
Imports System.Text.RegularExpressions

Namespace WikiBot
    Public Class AddTopic
        Private _bot As Bot
        Sub New(ByVal workerbot As Bot)
            _bot = workerbot
        End Sub

        Function GetTopicsOfText(ByVal PageText As String) As Tuple(Of String, String())
            Dim TopicMatch As Match = Regex.Match(PageText, "({{[Tt]ema.+?}})")
            If TopicMatch.Success Then
                Dim TopicTemp As New Template(TopicMatch.Value, False)
                Dim Topics As New List(Of String)
                For Each p As Tuple(Of String, String) In TopicTemp.Parameters
                    Topics.Add(p.Item2)
                Next

            End If

        End Function



    End Class
End Namespace
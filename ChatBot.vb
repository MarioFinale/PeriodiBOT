Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.My.Resources

Namespace IRC
    Public Class ChatBot
        Private _botName As String
        Private _wBot As WikiBot.Bot

        Sub New(ByVal botName As String, ByVal workerBot As WikiBot.Bot)
            _botName = botName
            _wBot = workerBot
        End Sub

        Function GetMessage(ByVal source As String, ByVal user As String, ByVal text As String) As ChatMessage
            Return New ChatMessage(source, user, text, _wBot)
        End Function

        Function GetPossiblePage(ByVal starts As String, ByVal message As ChatMessage) As WikiBot.Page
            If String.IsNullOrWhiteSpace(starts) Or message Is Nothing Then Throw New ArgumentNullException("message")
            For Each svar As String In starts
                If message.NormalizedMessage.ToLower.StartsWith(svar.ToLower) Then
                    Dim resline As String = message.NormalizedMessage.ToLower
                    If resline.Contains(Environment.NewLine) Or resline.Contains(vbCrLf) Then
                        resline = resline.Replace(vbCrLf, Environment.NewLine)
                        resline = resline.Split(CType(Environment.NewLine, Char()))(0)
                    End If
                    resline = resline.Replace(svar.ToLower, "").Replace("?"c, "").Trim
                    If Not String.IsNullOrWhiteSpace(resline) Then
                        Return _wBot.GetSearchedPage(resline)
                    End If
                End If
            Next
            Return Nothing
        End Function


        Function IsMentioned(ByVal tline As String) As Boolean
            If String.IsNullOrWhiteSpace(tline) Then Return False
            If tline.Contains("*") Then Return False
            Dim divisor As String = _botName.ToLower
            Return tline.Contains(divisor)
        End Function

    End Class



End Namespace



Option Strict On
Option Explicit On

Public Class ChatMessage
    Property Source As String
    Property User As String
    Property Text As String
    Property NormalizedMessage As String

    Sub New(ByVal tSource As String, tUser As String, tText As String, ByVal workerBot As WikiBot.Bot)
        Source = tSource
        User = tUser
        tText = tText
        NormalizedMessage = NormalizeMessage(tText)
    End Sub

    Function NormalizeMessage(ByVal text As String) As String
        Dim newMessage As String = String.Empty
        If String.IsNullOrWhiteSpace(text) Then Return newMessage
        newMessage = text.ToLower.Trim
        If newMessage.StartsWith("¿") Then
            newMessage = newMessage.Substring(1, newMessage.Length - 2)
        End If
        Return newMessage
    End Function

End Class





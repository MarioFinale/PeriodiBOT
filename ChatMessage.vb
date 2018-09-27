Option Strict On
Option Explicit On

Public Class ChatMessage
    Property Source As String
    Property User As String
    Property Text As String

    Sub New(ByVal tSource As String, tUser As String, tText As String)
        Source = tSource
        User = tUser
        tText = tText
    End Sub
End Class





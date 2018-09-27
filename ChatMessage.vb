Option Strict On
Option Explicit On

Public Class ChatMessage
    Implements IChatMessage
    Property Source As String Implements IChatMessage.Source
    Property User As String Implements IChatMessage.User
    Property Text As String Implements IChatMessage.Text

    Sub New(ByVal tSource As String, tUser As String, tText As String)
        Source = tSource
        User = tUser
        tText = tText
    End Sub
End Class





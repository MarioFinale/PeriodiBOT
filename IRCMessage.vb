Option Strict On
Option Explicit On
Public Class IRCMessage

    Public ReadOnly Property Source As String
    Public ReadOnly Property Text As List(Of String)
    Public ReadOnly Property Command As String
    Sub New(ByVal dest As String, ParamArray message() As String)
        Text = New List(Of String)
        Source = dest
        Text.AddRange(message)
        Command = "PRIVMSG"
    End Sub


End Class

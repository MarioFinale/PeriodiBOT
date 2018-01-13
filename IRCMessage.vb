Option Strict On
Option Explicit On
Public Class IRCMessage

    Public Property Source As String
    Public Property Text As String()
    Public Property Command As String
    Sub New(ByVal Dest As String, ParamArray message() As String)
        Source = Dest
        Text = message
        Command = "PRIVMSG"
    End Sub


End Class

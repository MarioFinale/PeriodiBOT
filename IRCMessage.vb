Option Strict On
Option Explicit On
Imports System.Collections.ObjectModel

Namespace IRC
    Public Class IRCMessage
        Public ReadOnly Property Source As String
        Public ReadOnly Property Text As ReadOnlyCollection(Of String)
        Public ReadOnly Property Command As String
        Sub New(ByVal dest As String, ParamArray message() As String)
            Dim tmplist As New List(Of String)
            Source = dest
            tmplist.AddRange(message)
            Command = "PRIVMSG"
            Text = New ReadOnlyCollection(Of String)(tmplist)
        End Sub
    End Class
End Namespace
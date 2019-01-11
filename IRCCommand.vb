Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.IRC

Public Class IRCCommand
    Public ReadOnly Property Name As String
    Public ReadOnly Property Aliases As String()
    Public ReadOnly Property Description As String
    Public ReadOnly Property Usage As String
    Friend ComFunc As Func(Of IRCCommandParams, IRCMessage)

    Public Sub New(ByVal commandName As String, ByVal commandAliases As String(), ByRef commandFunc As Func(Of IRCCommandParams, IRCMessage), ByVal commandDescription As String, ByVal commandUsage As String)
        Name = commandName
        Aliases = commandAliases
        ComFunc = commandFunc
        Description = commandDescription
        Usage = commandUsage
    End Sub

    Public Function Contains(ByVal commandAlias As String) As Boolean
        Return Aliases.Contains(commandAlias)
    End Function

End Class

Imports PeriodiBOT_IRC.IRC

Public Class IRCCommand
    Public ReadOnly Property Name As String
    Public ReadOnly Property Aliases As String()
    Public ReadOnly Property Description As String
    Private ComFunc As [Delegate]
    Private Client As IRC_Client

    Public Sub New(ByVal CommandName As String, ByVal CommandAliases As String(), ByVal CommandFunc As [Delegate], ByVal CommandDescription As String, ByVal ClientResolver As IRC_Client)
        Name = CommandName
        Aliases = CommandAliases
        ComFunc = CommandFunc
        Description = CommandDescription
        Client = ClientResolver
    End Sub

    Public Function Contains(ByVal CommandAlias As String) As Boolean
        Return Aliases.Contains(CommandAlias)
    End Function

    Private Sub Resolve(ByVal Commandmethod As [Delegate], ByVal ParamArray args As Object())
        Commandmethod.DynamicInvoke(args)
    End Sub



End Class

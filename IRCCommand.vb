Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot

Public Class IRCCommand
    Public ReadOnly Property Name As String
    Public ReadOnly Property Aliases As String()
    Public ReadOnly Property Description As String
    Friend ComFunc As Func(Of CommandParams, IRCMessage)
    Private Client As IRC_Client

    Public Sub New(ByVal CommandName As String, ByVal CommandAliases As String(), ByRef CommandFunc As Func(Of CommandParams, IRCMessage), ByVal CommandDescription As String, ByVal ClientResolver As IRC_Client)
        Name = CommandName
        Aliases = CommandAliases
        ComFunc = CommandFunc
        Description = CommandDescription
        Client = ClientResolver
    End Sub

    Public Function Contains(ByVal CommandAlias As String) As Boolean
        Return Aliases.Contains(CommandAlias)
    End Function

    Private Sub Resolve(ByRef Commandmethod As Func(Of CommandParams, IRCMessage), ByVal args As CommandParams)
        Commandmethod.Invoke(args)
    End Sub
End Class


Public Class CommandParams

    Private _source As String
    Private _realname As String
    Private _totalParam As String
    Private _client As IRC_Client
    Private _prefixes As String()
    Private _imputline As String
    Private _workerbot As Bot

#Region "Properties"
    Public ReadOnly Property Source As String
        Get
            Return _source
        End Get
    End Property

    Public ReadOnly Property Realname As String
        Get
            Return _realname
        End Get
    End Property

    Public ReadOnly Property TotalParam As String
        Get
            Return _totalParam
        End Get
    End Property

    Public ReadOnly Property Client As IRC_Client
        Get
            Return _client
        End Get
    End Property

    Public Property Prefixes As String()
        Get
            Return _prefixes
        End Get
        Set(value As String())
            _prefixes = value
        End Set
    End Property

    Public Property Imputline As String
        Get
            Return _imputline
        End Get
        Set(value As String)
            _imputline = value
        End Set
    End Property


    Public ReadOnly Property OpRequest As Boolean
        Get
            Return _client.IsOp(_imputline, _source, _realname)
        End Get
    End Property

    Public Property Workerbot As Bot
        Get
            Return _workerbot
        End Get
        Set(value As Bot)
            _workerbot = value
        End Set
    End Property

#End Region

    Sub New(ByVal CommandSource As String, CommandRealName As String, CommandParams As String, CommandPrefixes As String(), line As String, CommandResolver As IRC_Client, Workerbot As Bot)
        _source = CommandSource
        _realname = CommandRealName
        _totalParam = CommandParams
        _client = CommandResolver
        _prefixes = CommandPrefixes
        _imputline = line
        _workerbot = Workerbot
    End Sub



End Class
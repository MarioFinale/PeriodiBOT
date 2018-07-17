Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.CommFunctions

Public Class IRCCommand
    Public ReadOnly Property Name As String
    Public ReadOnly Property Aliases As String()
    Public ReadOnly Property Description As String
    Public ReadOnly Property Usage As String
    Friend ComFunc As Func(Of CommandParams, IRCMessage)
    Private Client As IRC_Client

    Public Sub New(ByVal CommandName As String, ByVal CommandAliases As String(), ByRef CommandFunc As Func(Of CommandParams, IRCMessage), ByVal CommandDescription As String, ByVal CommandUsage As String, ByVal ClientResolver As IRC_Client)
        Name = CommandName
        Aliases = CommandAliases
        ComFunc = CommandFunc
        Description = CommandDescription
        Client = ClientResolver
        Usage = CommandUsage
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
    Private _cParam As String
    Private _client As IRC_Client
    Private _imputline As String
    Private _workerbot As Bot
    Private _commandName As String
    Private _messageLine As String

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

    Public ReadOnly Property CParam As String
        Get
            Return _cParam
        End Get
    End Property

    Public ReadOnly Property Client As IRC_Client
        Get
            Return _client
        End Get
    End Property

    Public ReadOnly Property Imputline As String
        Get
            Return _imputline
        End Get
    End Property

    Public ReadOnly Property IsOp As Boolean
        Get
            Return _client.IsOp(_imputline, _source, _realname)
        End Get
    End Property

    Public ReadOnly Property Workerbot As Bot
        Get
            Return _workerbot
        End Get
    End Property

    Public ReadOnly Property CommandName As String
        Get
            Return _commandName
        End Get
    End Property

    Public ReadOnly Property MessageLine As String
        Get
            Return _messageLine
        End Get
    End Property

#End Region

    Sub New(ByVal cimputline As String, CommandResolver As IRC_Client, RWorkerbot As Bot)

        _source = String.Empty
        _realname = String.Empty
        _cParam = String.Empty
        _commandName = String.Empty
        _messageLine = String.Empty

        _client = CommandResolver
        _imputline = cimputline
        _workerbot = RWorkerbot

        Dim sCommandParts As String() = Imputline.Split(CType(" ", Char()))
        If sCommandParts.Length < 4 Then Exit Sub
        Dim sPrefix As String = sCommandParts(0)
        Dim sCommand As String = sCommandParts(1)
        Dim sSource As String = sCommandParts(2)
        Dim sParam As String = GetParamString(Imputline)

        Dim sRealname As String = GetUserFromChatresponse(sPrefix)
        If Source.ToLower = CommandResolver.NickName.ToLower Then
            sSource = sRealname
        End If

        Dim sCommandText As String = String.Empty
        For i As Integer = 3 To sCommandParts.Length - 1
            sCommandText = sCommandText & " " & sCommandParts(i)
        Next
        Dim sParams As String() = GetParams(sParam)
        Dim MainParam As String = sParams(0).ToLower
        Dim commandParam As String = String.Empty
        If Not MainParam = String.Empty Then
            Dim usrarr As String() = sParam.Split(CType(" ", Char()))
            For i As Integer = 0 To usrarr.Count - 1
                If i = 0 Then
                Else
                    commandParam = commandParam & " " & usrarr(i)
                End If
            Next
            commandParam = commandParam.Trim(CType(" ", Char()))
        End If
        _source = sSource
        _realname = sRealname
        _cParam = commandParam
        _commandName = MainParam
        _messageLine = sParam


    End Sub

    Private Function GetParams(ByVal Param As String) As String()
        Return Param.Split(CType(" ", Char()))
    End Function

    Private Function GetParamString(ByVal message As String) As String
        If message.Contains(":") Then
            Try
                Dim StringToRemove As String = TextInBetweenInclusive(message, ":", " :")(0)
                Dim Paramstring As String = message.Replace(StringToRemove, String.Empty)
                Return Paramstring
            Catch ex As IndexOutOfRangeException
                Return String.Empty
            Catch ex2 As Exception
                EventLogger.EX_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex2.Message, "IRC", _client.NickName)
                Return String.Empty
            End Try
        Else
            Return String.Empty
        End If
    End Function

End Class
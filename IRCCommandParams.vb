Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot
Namespace IRC
    Public Class IRCCommandParams

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

        Sub New(ByVal cimputline As String, commandResolver As IRC_Client, rWorkerbot As Bot)
            If commandResolver Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
            If rWorkerbot Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)

            _source = String.Empty
            _realname = String.Empty
            _cParam = String.Empty
            _commandName = String.Empty
            _messageLine = String.Empty

            _client = commandResolver
            _imputline = cimputline
            _workerbot = rWorkerbot

            Dim sCommandParts As String() = Imputline.Split(CType(" ", Char()))
            If sCommandParts.Length < 4 Then Exit Sub
            Dim sPrefix As String = sCommandParts(0)
            'Dim sCommand As String = sCommandParts(1) //Useless
            Dim sSource As String = sCommandParts(2)
            Dim sParam As String = GetParamString(Imputline)

            Dim sRealname As String = Utils.GetUserFromChatresponse(sPrefix)
            If sSource.ToLower = _client.NickName.ToLower Then
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

        Private Function GetParams(ByVal param As String) As String()
            Return param.Split(CType(" ", Char()))
        End Function

        Private Function GetParamString(ByVal message As String) As String
            If message.Contains(":") Then
                Dim matchedstrings As String() = Utils.TextInBetweenInclusive(message, ":", " :")
                If matchedstrings.Count = 0 Then Return String.Empty
                Dim stringtoremove As String = matchedstrings(0)
                Dim paramstring As String = message.Replace(StringToRemove, String.Empty)
                Return paramstring
            Else
                Return String.Empty
            End If
        End Function

    End Class
End Namespace
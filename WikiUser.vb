Option Strict On
Option Explicit On

Namespace WikiBot


    Public Class WikiUser
        Private _bot As Bot

        Private _username As String
        Private _userid As Integer
        Private _editcount As Integer
        Private _registration As Date
        Private _groups As String()
        Private _genders As String()

        Private _blocked As Boolean
        Private _blockid As Integer
        Private _blockedTimestamp As String
        Private _blockedby As String
        Private _blockreason As String
        Private _blockexpiry As String

        Private _exists As Boolean

#Region "Properties"
        Public ReadOnly Property Username As String
            Get
                Return _username
            End Get
        End Property

        Public ReadOnly Property Editcount As Integer
            Get
                Return _editcount
            End Get
        End Property

        Public ReadOnly Property Registration As Date
            Get
                Return _registration
            End Get
        End Property

        Public ReadOnly Property Groups As String()
            Get
                Return _groups
            End Get
        End Property

        Public ReadOnly Property Blocked As Boolean
            Get
                Return _blocked
            End Get
        End Property

        Public ReadOnly Property Blockedby As String
            Get
                Return _blockedby
            End Get
        End Property

        Public ReadOnly Property Blockreason As String
            Get
                Return _blockreason
            End Get
        End Property

        Public ReadOnly Property BlockedTimestamp As String
            Get
                Return _blockedTimestamp
            End Get
        End Property

        Public ReadOnly Property Blockexpiry As String
            Get
                Return _blockexpiry
            End Get
        End Property

        Public ReadOnly Property Blockid As Integer
            Get
                Return _blockid
            End Get
        End Property

        Public ReadOnly Property Exists As Boolean
            Get
                Return _exists
            End Get
        End Property
#End Region


        Sub New(ByVal WikiBot As Bot, username As String)
            _username = username
            _bot = WikiBot


        End Sub

        Sub LoadInfo()
            Dim queryresponse As String = _bot.POSTQUERY("action=query&format=json&list=users&usprop=blockinfo|groups|editcount|registration|gender&ususers=" & _username)
            Try
                _username = TextInBetween(queryresponse, """name"":""", """")(0)

                If queryresponse.Contains("""missing"":""""") Then
                    _exists = False
                    Exit Sub
                End If

                _userid = Integer.Parse(TextInBetween(queryresponse, """userid"":", ",")(0))
                _editcount = Integer.Parse(TextInBetween(queryresponse, """editcount"":", ",")(0))
                _registration = Date.ParseExact(TextInBetween(queryresponse, """userid"":", ",")(0), "yyyy-MM-ddThh:mm:ssZ", Nothing)
                _groups = TextInBetween(queryresponse, """userid"":", ",")(0).Split(","c)



            Catch ex As Exception
                Log("Wikiuser LoadInfo EX: " & ex.Message, "LOCAL", BOTName)
            End Try






        End Sub



    End Class

End Namespace
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
        Private _blocked As Tuple(Of Boolean, String)

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

        Public ReadOnly Property Blocked As Tuple(Of Boolean, String)
            Get
                Return _blocked
            End Get
        End Property
#End Region


        Sub New(ByVal WikiBot As Bot, username As String)
            _username = username
            _bot = WikiBot


        End Sub

        Sub LoadInfo()
            Dim queryresponse As String = _bot.POSTQUERY("action=query&format=json&list=users&usprop=blockinfo|groups|editcount|registration|gender&ususers=" & _username)






        End Sub



    End Class

End Namespace
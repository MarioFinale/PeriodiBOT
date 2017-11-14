Public Class Template


    Private _name As String
    Private _values As New List(Of Tuple(Of String, String))
    Private _text As String

    Public Property Name As String
        Get
            Return _name
        End Get
        Set(value As String)
            _name = value
        End Set
    End Property

    Public Property Values As List(Of Tuple(Of String, String))
        Get
            Return _values
        End Get
        Set(value As List(Of Tuple(Of String, String)))
            _values = value
        End Set
    End Property

    Public Property Text As String
        Get
            Return _text
        End Get
        Set(value As String)
            _text = value
        End Set
    End Property
End Class

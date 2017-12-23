Public Class IRCTask

    Dim _client As IRC_Client
    Dim _endtask As Boolean = False



    Public WriteOnly Property Endtask As Boolean
        Set(value As Boolean)
            _endtask = value
        End Set
    End Property



    Public Sub New(ByVal Client As IRC_Client)
        _client = Client
    End Sub


End Class

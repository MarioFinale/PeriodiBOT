Public Class ThreadInfo


    Public name As String
    Public type As String
    Public author As String
    ''' <summary>
    ''' Intervalo de repetición en milisegundos.
    ''' </summary>
    Public interval As Integer
    Public scheduledTime As TimeSpan
    Public scheduledtask As Boolean
    Public infinite As Boolean
    Public status As String
    Public running As Boolean
    Public task As Func(Of Boolean)
    Public cancelled As Boolean
    Public paused As Boolean
    Public runcount As Double
    Public queueData As Object
    Public excount As Integer
    Public critical As Boolean

End Class

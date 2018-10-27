Public Class ConfigFile
    Private _path As String
    Public ReadOnly Property GetPath As String
        Get
            Return _path
        End Get
    End Property

    Public Sub New(ByVal FilePath As String)
        _path = FilePath
        If Not IO.File.Exists(_path) Then
            IO.File.Create(_path).Close()
        End If
    End Sub
End Class
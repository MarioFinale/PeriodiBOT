Public Class ConfigFile
    Private _path As String
    Public ReadOnly Property GetPath As String
        Get
            Return _path
        End Get
    End Property

    Public Sub New(ByVal FilePath As String)
        _path = FilePath
        If Not System.IO.File.Exists(_Path) Then
            System.IO.File.Create(_Path).Close()
        End If
    End Sub
End Class

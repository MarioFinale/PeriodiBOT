Public Class ConfigFile
    Private _Path As String

    Public Function GetPath() As String
        Return _Path
    End Function

    Public Sub SetPath(Filepath As String)
        _Path = Filepath
    End Sub
    Public Sub New(ByVal FilePath As String)
        SetPath(FilePath)
        If Not System.IO.File.Exists(_Path) Then
            System.IO.File.Create(_Path).Close()
        End If
    End Sub
End Class

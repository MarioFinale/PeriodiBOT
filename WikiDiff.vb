Public Class WikiDiff

    Property OldId As Integer
    Property NewId As Integer
    Property Diffs As IReadOnlyCollection(Of Tuple(Of String, String))

    Sub New(ByVal tOldid As Integer, tNewid As Integer, tDiff As ICollection(Of Tuple(Of String, String)))
        OldId = tOldid
        NewId = tNewid
        Diffs = tDiff
    End Sub

End Class

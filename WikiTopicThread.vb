Option Strict On
Option Explicit On

Namespace WikiBot
    Public Class WikiTopicThread
        Implements IComparable(Of WikiTopicThread)
        Public Property Topics As List(Of String)
        Public Property ThreadTitle As String
        Public Property ThreadResume As String
        Public Property ThreadLink As String
        Public Property Subsection As String
        Public Property FirstSignature As Date
        Public Property ThreadBytes As Integer
        Public Function CompareTo(other As WikiTopicThread) As Integer Implements IComparable(Of WikiTopicThread).CompareTo
            If other Is Nothing Then Throw New ArgumentNullException("other")
            Return Me.FirstSignature().CompareTo(other.FirstSignature())
        End Function
    End Class
End Namespace
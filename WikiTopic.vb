Option Strict On
Option Explicit On

Namespace WikiBot
    Public Class WikiTopic
        Implements IComparable(Of WikiTopic)
        Public Property Name As String
        Public Property Threads As New SortedSet(Of WikiTopicThread)
        Public Function CompareTo(other As WikiTopic) As Integer Implements IComparable(Of WikiTopic).CompareTo
            Return Me.Name().CompareTo(other.Name())
        End Function
        Sub New(ByVal TopicName As String, ThreadList As SortedSet(Of WikiTopicThread))
            Name = TopicName
            Threads = ThreadList
        End Sub
    End Class
End Namespace
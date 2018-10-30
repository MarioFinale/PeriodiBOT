Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.My.Resources

Namespace WikiBot
    Public Class WikiExtract
        Implements IComparable(Of WikiExtract)
        Property PageName As String
        Property ExtractContent As String

        Public Function CompareTo(other As WikiExtract) As Integer Implements IComparable(Of WikiExtract).CompareTo
            If other Is Nothing Then Throw New ArgumentNullException("other")
            Return Me.PageName().CompareTo(other.PageName())
        End Function
    End Class
End Namespace
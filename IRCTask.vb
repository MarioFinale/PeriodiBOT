Imports System.Runtime.InteropServices

Public Class IRCTask
    Implements IDisposable

    Dim _client As IRC_Client
    Dim _interval As Integer

    Dim disposed As Boolean = False
    Dim _task As Task
    Dim _nFunc As Func(Of String())



    Public Sub New(ByVal Client As IRC_Client, Interval As Integer, ByVal nFunc As Func(Of String()))
        _nFunc = nFunc
        _client = Client
        _interval = Interval
    End Sub

    Public Sub Run()

        _task = Task.Run(Sub()

                             Do
                                 For Each s As String In _nFunc.Invoke
                                     _client.SendText(s)
                                 Next
                                 System.Threading.Thread.Sleep(_interval)
                             Loop

                         End Sub)


    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If disposed Then Return
        If disposing Then
            _task.Dispose()
        End If
        disposed = True
    End Sub





End Class

Option Strict On
Option Explicit On
Imports System.Threading
Imports PeriodiBOT_IRC.CommFunctions

Public Module ThreadPoolAdmin
    Public ThreadList As New List(Of ThreadInfo)

    Public Sub NewThread(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), interval As Integer, infinite As Boolean)
        NewThread(name, author, task, interval, infinite, False)
    End Sub

    Public Sub NewThread(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), scheduledTime As TimeSpan, infinite As Boolean)
        NewThread(name, author, task, scheduledTime, infinite, False)
    End Sub

    Public Sub NewThread(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), interval As Integer, infinite As Boolean, critical As Boolean)
        Dim Tinfo As New ThreadInfo With {
            .Author = author,
            .Name = name,
            .Task = task,
            .Scheduledtask = False,
            .Canceled = False,
            .Paused = False,
            .Interval = interval,
            .Infinite = infinite,
            .Critical = critical
        }
        ThreadList.Add(Tinfo)
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf Timedmethod), Tinfo)
    End Sub

    Public Sub NewThread(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), scheduledTime As TimeSpan, infinite As Boolean, critical As Boolean)
        Dim Tinfo As New ThreadInfo With {
            .Author = author,
            .Name = name,
            .Task = task,
            .Scheduledtask = True,
            .Canceled = False,
            .Paused = False,
            .Interval = 2147483646,
            .Infinite = infinite,
            .ScheduledTime = scheduledTime,
            .Critical = critical
        }
        ThreadList.Add(Tinfo)
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf ScheduledMethod), Tinfo)
    End Sub

    Private Sub Timedmethod(ByVal state As Object)
        Dim tinfo As ThreadInfo = CType(state, ThreadInfo)
        Try
            Do
                If tinfo.Canceled Then
                    If Not tinfo.Critical Then
                        Exit Do
                    Else
                        EventLogger.Log("CANNOT END TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                    End If
                End If
                If Not tinfo.Paused Then
                    tinfo.Running = True
                    tinfo.Status = "Running"
                    Try
                        tinfo.Task.Invoke
                    Catch ex As Exception
                        EventLogger.EX_Log("UNHANDLED TASK EX: """ & tinfo.Name & """  EX: " & ex.Message, "THREAD", tinfo.Author)
                    End Try

                    tinfo.Runcount += 1.0F
                    tinfo.Status = "Completed"
                    If Not tinfo.Infinite Then
                        Exit Do
                    End If
                    tinfo.Status = "Waiting"
                    Thread.Sleep(tinfo.Interval)
                Else
                    If Not tinfo.Critical Then
                        tinfo.Status = "Paused"
                        Thread.Sleep(1000)
                    Else
                        EventLogger.Log("CANNOT PAUSE TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                        tinfo.Paused = False
                    End If
                End If
            Loop
        Catch ex As Exception
            tinfo.ExCount += 1
            EventLogger.EX_Log("UNHANDLED THREAD EX: """ & tinfo.Name & """  EX: " & ex.Message, "THREAD", tinfo.Author)
        End Try
        ThreadList.Remove(tinfo)
    End Sub

    Private Sub ScheduledMethod(ByVal state As Object)
        Dim tinfo As ThreadInfo = CType(state, ThreadInfo)
        Try
            Do
                If tinfo.Canceled Then
                    If Not tinfo.Critical Then
                        Exit Do
                    Else
                        EventLogger.Log("CANNOT END TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                    End If
                End If
                If Not tinfo.Paused Then
                    tinfo.Running = True
                    tinfo.Status = "Scheduled"
                    If Date.UtcNow.TimeOfDay.ToString("hh\:mm") = tinfo.ScheduledTime.ToString("hh\:mm") Then
                        tinfo.Status = "Running"
                        tinfo.Task.Invoke
                        Thread.Sleep(60000)
                        tinfo.Runcount += 1.0F
                        tinfo.Status = "Completed"
                    End If
                    If Not tinfo.Infinite Then
                        Exit Do
                    End If
                    Thread.Sleep(100)
                Else
                    If Not tinfo.Critical Then
                        tinfo.Status = "Paused"
                        Thread.Sleep(1000)
                    Else
                        EventLogger.Log("CANNOT PAUSE TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                        tinfo.Paused = False
                    End If

                End If
            Loop
        Catch ex As Exception
            tinfo.ExCount += 1
            EventLogger.EX_Log("TASK """ & tinfo.Name & """  EX: " & ex.Message, "THREAD", tinfo.Author)
        End Try
        ThreadList.Remove(tinfo)
    End Sub



End Module

Public Class ThreadInfo

    Public Property Name As String
    Public Property ThreadType As String
    Public Property Author As String
    ''' <summary>
    ''' Intervalo de repetición en milisegundos.
    ''' </summary>
    Public Property Interval As Integer
    Public Property ScheduledTime As TimeSpan
    Public Property Scheduledtask As Boolean
    Public Property Infinite As Boolean
    Public Property Status As String
    Public Property Running As Boolean
    Public Property Task As Func(Of Boolean)
    Public Property Canceled As Boolean
    Public Property Paused As Boolean
    Public Property Runcount As Double
    Public Property QueueData As Object
    Public Property ExCount As Integer
    Public Property Critical As Boolean

End Class


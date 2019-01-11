Option Strict On
Option Explicit On
Imports System.Globalization
Imports System.Threading
Imports MWBot.net

Public Class TaskAdmin
    Public TaskList As ICollection(Of TaskInfo)

    Sub New()
        TaskList = New List(Of TaskInfo)
    End Sub


    Sub NewTask(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), interval As Integer, infinite As Boolean)
        NewTask(name, author, task, interval, infinite, False)
    End Sub

    Sub NewTask(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), scheduledTime As TimeSpan, infinite As Boolean)
        NewTask(name, author, task, scheduledTime, infinite, False)
    End Sub

    Sub NewTask(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), interval As Integer, infinite As Boolean, critical As Boolean)
        Dim Tinfo As New TaskInfo With {
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
        TaskList.Add(Tinfo)
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf Timedmethod), Tinfo)
    End Sub

    Sub NewTask(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), scheduledTime As TimeSpan, infinite As Boolean, critical As Boolean)
        Dim Tinfo As New TaskInfo With {
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
        TaskList.Add(Tinfo)
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf ScheduledMethod), Tinfo)
    End Sub

    Private Sub Timedmethod(ByVal state As Object)
        Dim tinfo As TaskInfo = CType(state, TaskInfo)
        Try
            Do
                If tinfo.Canceled Then
                    If Not tinfo.Critical Then
                        Exit Do
                    Else
                        Utils.EventLogger.Log("CANNOT END TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                    End If
                End If
                If Not tinfo.Paused Then
                    tinfo.Running = True
                    tinfo.Status = "Running"
                    Try
                        tinfo.Task.Invoke
                    Catch ex As Exception
                        tinfo.ExCount += 1
                        Utils.EventLogger.EX_Log("UNHANDLED TASK EX: """ & tinfo.Name & """  EX: " & ex.Message, "THREAD", tinfo.Author)
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
                        Utils.EventLogger.Log("CANNOT PAUSE TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                        tinfo.Paused = False
                    End If
                End If
            Loop
        Catch ex As Exception
            tinfo.ExCount += 1
            Utils.EventLogger.EX_Log("UNHANDLED THREAD EX: """ & tinfo.Name & """  EX: " & ex.Message, "THREAD", tinfo.Author)
        End Try
        TaskList.Remove(tinfo)
    End Sub

    Private Sub ScheduledMethod(ByVal state As Object)
        Dim tinfo As TaskInfo = CType(state, TaskInfo)
        Try
            Do
                If tinfo.Canceled Then
                    If Not tinfo.Critical Then
                        Exit Do
                    Else
                        Utils.EventLogger.Log("CANNOT END TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                    End If
                End If
                If Not tinfo.Paused Then
                    tinfo.Running = True
                    tinfo.Status = "Scheduled"
                    If Date.UtcNow.TimeOfDay.ToString("hh\:mm", CultureInfo.InvariantCulture()) = tinfo.ScheduledTime.ToString("hh\:mm", CultureInfo.InvariantCulture()) Then
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
                        Utils.EventLogger.Log("CANNOT PAUSE TASK """ & tinfo.Name & """: CRITICAL TASK", "THREAD", tinfo.Author)
                        tinfo.Paused = False
                    End If

                End If
            Loop
        Catch ex As Exception
            tinfo.ExCount += 1
            Utils.EventLogger.EX_Log("TASK """ & tinfo.Name & """  EX: " & ex.Message, "THREAD", tinfo.Author)
        End Try
        TaskList.Remove(tinfo)
    End Sub



End Class


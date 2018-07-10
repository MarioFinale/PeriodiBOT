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
            .author = author,
            .name = name,
            .task = task,
            .scheduledtask = False,
            .cancelled = False,
            .paused = False,
            .interval = interval,
            .infinite = infinite,
            .critical = critical
        }
        ThreadList.Add(Tinfo)
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf Timedmethod), Tinfo)
    End Sub

    Public Sub NewThread(ByVal name As String, ByVal author As String, ByVal task As Func(Of Boolean), scheduledTime As TimeSpan, infinite As Boolean, critical As Boolean)
        Dim Tinfo As New ThreadInfo With {
            .author = author,
            .name = name,
            .task = task,
            .scheduledtask = True,
            .cancelled = False,
            .paused = False,
            .interval = 2147483646,
            .infinite = infinite,
            .scheduledTime = scheduledTime,
            .critical = critical
        }
        ThreadList.Add(Tinfo)
        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf ScheduledMethod), Tinfo)
    End Sub

    Private Sub Timedmethod(ByVal state As Object)
        Dim tinfo As ThreadInfo = CType(state, ThreadInfo)
        Try
            Do
                If tinfo.cancelled Then
                    If Not tinfo.critical Then
                        Exit Do
                    Else
                        EventLogger.Log("CANNOT END TASK """ & tinfo.name & """: CRITICAL TASK", "THREAD", tinfo.author)
                    End If
                End If
                If Not tinfo.paused Then
                    tinfo.running = True
                    tinfo.status = "Running"
                    tinfo.task.Invoke
                    tinfo.runcount += 1.0F
                    tinfo.status = "Completed"
                    If Not tinfo.infinite Then
                        Exit Do
                    End If
                    tinfo.status = "Waiting"
                    Thread.Sleep(tinfo.interval)
                Else
                    If Not tinfo.critical Then
                        tinfo.status = "Paused"
                        Thread.Sleep(1000)
                    Else
                        EventLogger.Log("CANNOT PAUSE TASK """ & tinfo.name & """: CRITICAL TASK", "THREAD", tinfo.author)
                        tinfo.paused = False
                    End If
                End If
            Loop
        Catch ex As Exception
            tinfo.excount += 1
            EventLogger.EX_Log("TASK """ & tinfo.name & """  EX: " & ex.Message, "THREAD", tinfo.author)
        End Try
        ThreadList.Remove(tinfo)
    End Sub

    Private Sub ScheduledMethod(ByVal state As Object)
        Dim tinfo As ThreadInfo = CType(state, ThreadInfo)
        Try
            Do
                If tinfo.cancelled Then
                    If Not tinfo.critical Then
                        Exit Do
                    Else
                        EventLogger.Log("CANNOT END TASK """ & tinfo.name & """: CRITICAL TASK", "THREAD", tinfo.author)
                    End If
                End If
                If Not tinfo.paused Then
                    tinfo.running = True
                    tinfo.status = "Scheduled"
                    If Date.UtcNow.TimeOfDay.ToString("hh\:mm") = tinfo.scheduledTime.ToString("hh\:mm") Then
                        tinfo.status = "Running"
                        tinfo.task.Invoke
                        Thread.Sleep(60000)
                        tinfo.runcount += 1.0F
                        tinfo.status = "Completed"
                    End If
                    If Not tinfo.infinite Then
                        Exit Do
                    End If
                    Thread.Sleep(100)
                Else
                    If Not tinfo.critical Then
                        tinfo.status = "Paused"
                        Thread.Sleep(1000)
                    Else
                        EventLogger.Log("CANNOT PAUSE TASK """ & tinfo.name & """: CRITICAL TASK", "THREAD", tinfo.author)
                        tinfo.paused = False
                    End If

                End If
            Loop
        Catch ex As Exception
            tinfo.excount += 1
            EventLogger.EX_Log("TASK """ & tinfo.name & """  EX: " & ex.Message, "THREAD", tinfo.author)
        End Try
        ThreadList.Remove(tinfo)
    End Sub



End Module

Option Strict On
Option Explicit On
Imports System.Globalization
Imports System.Threading

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
        ThreadList.Remove(tinfo)
    End Sub



End Module

Public Class ThreadInfo
    ''' <summary>
    ''' Nombre de la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property Name As String
    ''' <summary>
    ''' Tipo de tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property ThreadType As String
    ''' <summary>
    ''' Autor de la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property Author As String
    ''' <summary>
    ''' Intervalo de repetición en milisegundos.
    ''' </summary>
    Public Property Interval As Integer
    ''' <summary>
    ''' Hora a la que está programada la ejecución de la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property ScheduledTime As TimeSpan
    ''' <summary>
    ''' Indica si la tarea está agendada para su ejecución a una hora o es periódica.
    ''' </summary>
    ''' <returns></returns>
    Public Property Scheduledtask As Boolean
    ''' <summary>
    ''' Indica si una tarea se ejecuta infinitamente.
    ''' </summary>
    ''' <returns></returns>
    Public Property Infinite As Boolean
    ''' <summary>
    ''' Indica el estado de la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property Status As String
    ''' <summary>
    ''' Indica si la tarea está en ejecución.
    ''' </summary>
    ''' <returns></returns>
    Public Property Running As Boolean
    ''' <summary>
    ''' Función que ejecutará la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property Task As Func(Of Boolean)
    ''' <summary>
    ''' Indica si la tarea está en estado de cancelación.
    ''' </summary>
    ''' <returns></returns>
    Public Property Canceled As Boolean
    ''' <summary>
    ''' Indica ssi la tarea está pausada.
    ''' </summary>
    ''' <returns></returns>
    Public Property Paused As Boolean
    ''' <summary>
    ''' Indica cuantes veces se ha ejecutado la función en la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property Runcount As Double
    ''' <summary>
    ''' Datos a pasar a la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property QueueData As Object
    ''' <summary>
    ''' Indica cuantas excepciones ha lanzado la función en la tarea.
    ''' </summary>
    ''' <returns></returns>
    Public Property ExCount As Integer
    ''' <summary>
    ''' Indica si la tarea es críctica. Una tarea crítica no se puede pausar.
    ''' </summary>
    ''' <returns></returns>
    Public Property Critical As Boolean

End Class


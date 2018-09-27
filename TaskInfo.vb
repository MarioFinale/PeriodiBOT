Option Strict On
Option Explicit On

Public Class TaskInfo
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


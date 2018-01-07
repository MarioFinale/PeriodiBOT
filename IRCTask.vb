Imports System.Runtime.InteropServices
Imports PeriodiBOT_IRC

Public Class IRCTask
    Implements IDisposable

    Dim _client As IRC_Client
    Dim _interval As Integer
    Dim _infinite As Boolean

    Dim disposed As Boolean = False
    Dim _task As Task
    Dim _nFunc As Func(Of String())
    ''' <summary>
    ''' Crea una nueva tarea de IRC
    ''' </summary>
    ''' <param name="Client">Cliente de IRC</param>
    ''' <param name="Interval">Intervalo de repetición de la tarea en milisegundos (el tiempo de espera se ejecuta al final de cada iteración)</param>
    ''' <param name="Infinite">¿Se repite idefinidamente la tarea?.</param>
    ''' <param name="nFunc">Función (String()) a ejecutar, cada linea será escrita directamente en el streamwriter del cliente IRC.</param>
    Public Sub New(ByVal Client As IRC_Client, Interval As Integer, Infinite As Boolean, ByVal nFunc As Func(Of String()))
        _nFunc = nFunc
        _client = Client
        _interval = Interval
        _infinite = Infinite
    End Sub

    ''' <summary>
    ''' Ejecuta la tarea creada en otro thread (dispara y corre en métodos sincrónicos).
    ''' </summary>
    Public Sub Run()

        _task = Task.Run(Sub()

                             Do
                                 For Each s As String In _nFunc.Invoke
                                     If Not String.IsNullOrEmpty(s) Then
                                         _client.SendText(s)
                                     End If
                                 Next
                                 If Not _infinite Then
                                     Exit Do
                                 End If
                                 System.Threading.Thread.Sleep(_interval)
                             Loop

                         End Sub)


    End Sub
    ''' <summary>
    ''' Detiene la tarea de forma segura (si es infinita).
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' Detiene la tarea.
    ''' </summary>
    ''' <param name="disposing"></param>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If disposed Then Return
        If disposing Then
            _task.Dispose()
        End If
        disposed = True
    End Sub

End Class

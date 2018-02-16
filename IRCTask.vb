Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports PeriodiBOT_IRC
Imports System.Threading
Namespace IRC
    Public Class IRCTask
        Implements IDisposable

        Dim _client As IRC_Client
        Dim _interval As Integer
        Dim _infinite As Boolean

        Dim disposed As Boolean = False
        Dim _nFunc As Func(Of IRCMessage())
        Dim Thread As Thread
        Dim _source As String

        ''' <summary>
        ''' Crea una nueva tarea de IRC
        ''' </summary>
        ''' <param name="Client">Cliente de IRC</param>
        ''' <param name="Interval">Intervalo de repetición de la tarea en milisegundos (el tiempo de espera se ejecuta al final de cada iteración)</param>
        ''' <param name="Infinite">¿Se repite idefinidamente la tarea?.</param>
        ''' <param name="nFunc">Función (String()) a ejecutar, cada linea será escrita directamente en el streamwriter del cliente IRC.</param>
        Public Sub New(ByVal Client As IRC_Client, interval As Integer, infinite As Boolean, ByVal nFunc As Func(Of IRCMessage()), ByVal source As String)
            _nFunc = nFunc
            _client = Client
            _interval = interval
            _infinite = infinite
            _source = source
        End Sub


        ''' <summary>
        ''' Ejecuta la tarea creada en otro thread (dispara y corre en métodos sincrónicos).
        ''' </summary>
        Public Sub Run()
            Thread = New Threading.Thread(New Threading.ParameterizedThreadStart(Sub()

                                                                                     Do
                                                                                         Try

                                                                                             Dim msg As IRCMessage() = _nFunc.Invoke

                                                                                             For Each s As IRCMessage In msg

                                                                                                 If Not String.IsNullOrEmpty(s.Text(0)) Then
                                                                                                     _client.Sendmessage(s)
                                                                                                 End If
                                                                                             Next

                                                                                         Catch ex As Exception
                                                                                             Debug_Log("TASK " & _source & " EX: " & ex.Message, "THREAD", BOTName)
                                                                                         End Try

                                                                                         If Not _infinite Then
                                                                                             Exit Do
                                                                                         End If

                                                                                         System.Threading.Thread.Sleep(_interval)
                                                                                     Loop

                                                                                 End Sub))
            Thread.Start()

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
            Debug_Log("End task func", "LOCAL", BOTName)
            _infinite = False
            If disposed Then Return
            If disposing Then
                Thread.Abort()
            End If
            disposed = True
        End Sub

    End Class
End Namespace

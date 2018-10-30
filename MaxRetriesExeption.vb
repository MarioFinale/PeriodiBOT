Option Strict On
Option Explicit On
Imports System.Runtime.Serialization
''' <summary>
''' Excepción que se produce cuando se alcanza un número máximo de reintentos.
''' </summary>
Public Class MaxRetriesExeption : Inherits System.Exception
    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(ByVal message As String, ByVal innerException As System.Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.New(info, context)
    End Sub
End Class
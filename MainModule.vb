Option Strict On
Option Explicit On
Imports System.Threading


Module MainModule

    Sub Main()
        Initializer.Init()
        Do
            Dim command As String = Console.ReadLine()
            If Not String.IsNullOrWhiteSpace(command) Then
                'BotIRC.Sendmessage(command)
            End If
            Thread.Sleep(500)
        Loop
    End Sub

End Module

Option Strict On
Option Explicit On
Imports System.Threading


Module MainModule

    Sub Main()
        Initializer.Init()
        Do
            Thread.Sleep(500)
        Loop
    End Sub

End Module

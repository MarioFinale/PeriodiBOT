Option Strict On
Option Explicit On
Imports System.Threading


Module MainModule

    Sub Main()
        Thread.CurrentThread.CurrentUICulture = New Globalization.CultureInfo("es")
        Thread.CurrentThread.CurrentCulture = New Globalization.CultureInfo("es")
        Globalization.CultureInfo.CurrentCulture = New Globalization.CultureInfo("es")
        Globalization.CultureInfo.CurrentUICulture = New Globalization.CultureInfo("es")
        Globalization.CultureInfo.DefaultThreadCurrentCulture = New Globalization.CultureInfo("es")
        Initializer.Init()
        Do
            Thread.Sleep(500)
        Loop
    End Sub

End Module

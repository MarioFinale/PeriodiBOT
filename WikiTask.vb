Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions

Namespace WikiBot

    Public Class WikiTask

        Private _bot As Bot
        Private _siteurl As String

        Sub New(ByVal wBot As Bot)
            If wBot Is Nothing Then
                Throw New ArgumentException("Wbot")
            End If
            _bot = wBot
            _siteurl = wBot.Siteurl
        End Sub










    End Class

End Namespace
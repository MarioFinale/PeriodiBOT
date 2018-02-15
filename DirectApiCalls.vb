Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.WikiBot

Module DirectApiCalls

    Const ResumePageName As String = "PeriodiBOT/Resumen página"
    Const DidYouKnowPageName As String = "PeriodiBOT/Sabias que"


    ''' <summary>
    ''' Entrega el título de la primera página que coincida remotamente con el texto entregado como parámetro.
    ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
    ''' Si no hay coincidencia, entrega una cadena de texto vacía.
    ''' </summary>
    ''' <param name="Text">Título aproximado o similar al de una página</param>
    ''' <returns></returns>
    Function TitleFirstGuess(Text As String) As String
        Try
            Return GetTitlesFromQueryText(Gethtmlsource((ApiURL & "?action=query&format=json&list=search&utf8=1&srsearch=" & Text), False))(0)
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function

End Module


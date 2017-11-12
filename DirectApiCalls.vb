Option Strict On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.WikiBot

Module DirectApiCalls

    Const ResumePageName As String = "PeriodiBOT/Resumen página"
    Const DidYouKnowPageName As String = "PeriodiBOT/Sabias que"
    Const wikiurl As String = "https://es.wikipedia.org"


    Function EswikiPageExist(ByVal pagename As String) As Boolean
        Dim s As String = String.Empty
        pagename = UrlWebEncode(pagename)
        s = Gethtmlsource(wikiurl & "/w/api.php?action=query&format=json&titles=")
        If s.Contains("pages"":{""-1"":") Then
            Return False
        Else
            Return True
        End If
    End Function


    Function TitleFirstGuess(ByVal pagename As String) As String
        Return Mainwikibot.TitleFirstGuess(pagename)
    End Function


End Module


﻿Option Strict On
Option Explicit On
Imports System.Threading
Imports PeriodiBOT_IRC.WikiBot

Module MainModule

    Public Mainwikibot As Bot
    Public BotIRC As IRC_Client

    Sub Main()
        '  Uptime = DateTime.Now
        '  LoadConfig()
        '  Log("Starting...", "Local", BOTName)
        '  Mainwikibot = New Bot(WPUserName, BOTPassword, ApiURL)
        '  BotIRC = New IRC_Client(IRCNetwork, IRCChannel, BOTIRCName, 6667, False, IRCPassword) ', IRCPASS)
        '  BotIRC.Connect()
        Dim teststring As String = "{{Ficha de ejemplo1
|nombre = Escudo: {{Archivo|Escudo de Mark.jpg}}
|edad =
|nota =
}}

{{Ficha de ejemplo2
|nombre = Escudo: {{Archivo|Escudo de Mark.jpg}}
|edad = {{Archivo|Escudo de Mark.jpg}} {{Archivs|Escudo de Mark.jpg}}
|nota =
}}

{{Ficha de ejemplo3
|nombre = Escudo: {{Archivo|Escudo de Mark.jpg}} {{Archivsso|Escudo de Mark.jpg}}
|edad =
|nota =
}}

{{Ficha de ejemplo4
|nombre = Escudo: {{Archivo|{{Archivsso|Escudo de Mark.jpg}} Escudo de Mark.jpg}} 
|edad =
|nota =
}}"

        Do
            Dim command As String = Console.ReadLine()
            ' Mainwikibot.ArchiveAllInclusions()
            ' Dim p As Page = Mainwikibot.Getpage("User:PeriodiBOT")
            ' Dim pagetext As String = p.Text
            ' Mainwikibot.GrillitusArchive(p)
            Dim t As List(Of String) = GetTemplateTextArray(teststring)
            GetTemplates(t)
            Dim a As Integer = 1

            Thread.Sleep(500)
        Loop

    End Sub





    Function GetTemplateTextArray(ByVal text As String) As List(Of String)

        Dim templatetext As String = text

        Dim hasinited As Boolean = False
        Dim AbsolutelyInited As Boolean = False
        Dim templates As New List(Of String)


        Dim tmptext As String = String.Empty
        For i As Integer = 0 To templatetext.Count - 1

            tmptext = tmptext & templatetext(i)


            If AbsolutelyInited Then
                If CountCharacter(tmptext, CChar("{")) = CountCharacter(tmptext, CChar("}")) Then
                    templates.Add(tmptext)
                    AbsolutelyInited = False
                    hasinited = False
                    tmptext = String.Empty
                End If
            Else
                If hasinited Then
                    If templatetext(i) = "{" Then
                        AbsolutelyInited = True
                        tmptext = "{{"
                        Continue For
                    End If
                Else
                    If templatetext(i) = "{" Then
                        hasinited = True
                        Continue For
                    End If
                End If
            End If

        Next
        Dim innertlist As New List(Of String)

        For Each t As String In templates
            Dim newt As String = t.Substring(2, t.Length - 4)

            innertlist.AddRange(GetTemplateTextArray(newt))

        Next

        templates.AddRange(innertlist)

        Return templates

    End Function


    Function GetTemplates(ByVal templatearray As List(Of String)) As List(Of Template)
        Dim TemplateList As New List(Of Template)
        For Each t As String In templatearray
            Dim Ttext As String = t.Substring(2, t.Length - 4)

            Dim temp As New Template
            Dim tempname As String = String.Empty
            For cha As Integer = 0 To Ttext.Count - 1
                If Not (Ttext(cha) = "}" Or Ttext(cha) = "{" Or Ttext(cha) = "|") Then
                    tempname = tempname & Ttext(cha)
                Else
                    Exit For
                End If
            Next

            temp.Name = tempname.Trim(CType(" ", Char())).Trim(CType(Environment.NewLine, Char()))
            temp.Text = t
            TemplateList.Add(temp)
            Dim a As Int16 = 1


        Next


        Return TemplateList


    End Function








End Module

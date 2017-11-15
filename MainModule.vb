Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
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
        Dim teststring As String = "{{PR|Enfermedades/VIH/SIDA}}
{{Discusión:VIH/sida/Archivo-00-índice}}
{{Usuario:Grillitus/Archivar
|Destino=Discusión:VIH/sida/Archivo AAAA
|Días a mantener=150
|Avisar al archivar=si
|Estrategia=FirmaMásRecienteEnLaSección
|MantenerCajaDeArchivos=sí
}}
"

        Do
            Dim command As String = Console.ReadLine()
            ' Mainwikibot.ArchiveAllInclusions()
            ' Dim p As Page = Mainwikibot.Getpage("User:PeriodiBOT")
            ' Dim pagetext As String = p.Text
            ' Mainwikibot.GrillitusArchive(p)
            Dim t As List(Of String) = GetTemplateTextArray(teststring)
            Dim templist As List(Of Template) = GetTemplates(t)
            Dim temp As Template = templist(0)


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
            TemplateList.Add(New Template(t, False))
        Next
        Return TemplateList
    End Function












End Module

Imports System.Threading
Imports PeriodiBOT_IRC.WikiBot

Module MainModule

    Public Mainwikibot As Bot
    Public BotIRC As IRC_Client

    Sub Main()
        Uptime = DateTime.Now
        LoadConfig()
        Log("Starting...", "Local", BOTName)
        Mainwikibot = New Bot(WPUserName, BOTPassword, ApiURL)
        BotIRC = New IRC_Client(IRCNetwork, IRCChannel, BOTIRCName, 6667, False, IRCPassword) ', IRCPASS)
        BotIRC.Connect()

        Do
            Dim command As String = Console.ReadLine()
            ' Mainwikibot.ArchiveAllInclusions()
            Dim p As Page = Mainwikibot.Getpage("User:PeriodiBOT")
            ' Dim pagetext As String = p.Text
            Mainwikibot.GrillitusArchive(p)

            Thread.Sleep(500)
        Loop

    End Sub




End Module

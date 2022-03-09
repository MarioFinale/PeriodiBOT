Option Strict On
Option Explicit On

Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.My.Resources
Imports MWBot.net.WikiBot
Imports MWBot.net.Utility.Utils
Imports System.Globalization

Class BillboardArchiver

    Private Bot As Bot

    Structure BillboardEvent
        Public EventDateString As String
        Public EventDate As Date
        Public EventIcon As String
        Public EventContent As String
        Public EventOriginalText As String
    End Structure

    Class BillBoardPage
        Public PageMonth As Integer
        Public PageYear As Integer
        Public BillBoardEvents As List(Of BillboardEvent)
        Public PageContent As String

        Public Function PageName() As String
            Dim dummyDate As New Date(PageYear, PageMonth, 1)
            Dim monthString As String = dummyDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))
            Return "Wikipedia:Resumen de " + monthString + " de " + PageYear.ToString
        End Function
    End Class

    Sub New(ByRef workerBot As Bot)
        Bot = workerBot
    End Sub

    Function ArchivePage(ByVal PageName As String) As Boolean
        EventLogger.Log("Archivando cartelera de acontecimientos", "BillboardArchiver")
        Dim pageToArchive As Page = Bot.Getpage(PageName)
        Dim currentDate As Date = Date.Now
        Dim newPageText As String = pageToArchive.Content

        Dim results As MatchCollection = Regex.Matches(newPageText, "(\|\-.*[\n\r]\|.+[\n\r]\|.+[\n\r]\|.+[\n\r])")
        Dim events As New List(Of String)
        Dim dates As New List(Of String)
        Dim icons As New List(Of String)
        Dim description As New List(Of String)
        Dim eventList As New List(Of BillboardEvent)
        Dim pagesList As New Dictionary(Of Date, BillBoardPage)
        Try
            For Each m As Match In results
                events.Add(m.Groups(1).Value)
            Next

            For Each d As String In events
                Dim m As Match = Regex.Match(d, "\| (\d{1,2} +\w{1,2} +\w{4,11})")
                If m.Success Then
                    dates.Add(m.Groups(1).Value)
                Else
                    dates.Add("*")
                End If
            Next

            For Each d As String In events
                Dim m As Match = Regex.Match(d, "\| *(\[\[([Aa]rchivo|[Ff]ile|[Ii]magen*) *\:.+?\]\])")
                If m.Success Then
                    icons.Add(m.Groups(1).Value)
                Else
                    icons.Add("*")
                End If
            Next

            For Each d As String In events
                Dim m As String = GetLines(d).SkipLast(1).Last
                m = m.Trim()
                m = m.TrimStart("|"c)
                m = m.Trim()
                description.Add(m)
            Next

            For i As Integer = 0 To results.Count - 1
                Dim bEvent As BillboardEvent
                bEvent.EventDateString = dates.Item(i)
                bEvent.EventIcon = icons.Item(i)
                bEvent.EventContent = description.Item(i)
                bEvent.EventDate = GetDateFromBillboardDateString(bEvent.EventDateString, currentDate)
                If bEvent.EventDate = Nothing Then Return False
                bEvent.EventOriginalText = events.Item(i)
                eventList.Add(bEvent)
            Next


            For Each e As BillboardEvent In eventList
                Dim dummydate As New Date(e.EventDate.Year, e.EventDate.Month, 1)
                If pagesList.ContainsKey(dummydate) Then
                    pagesList(dummydate).BillBoardEvents.Add(e)
                Else
                    Dim bPage As BillBoardPage = New BillBoardPage
                    bPage.BillBoardEvents = New List(Of BillboardEvent)
                    bPage.PageMonth = e.EventDate.Month
                    bPage.PageYear = e.EventDate.Year
                    bPage.BillBoardEvents.Add(e)
                    Dim asd As String = bPage.PageName()
                    pagesList.Add(dummydate, bPage)
                End If
            Next

            Dim monthdate As New Date(currentDate.Year, currentDate.Month, 1)
            pagesList.Remove(monthdate)
            If pagesList.Count = 0 Then Return True
            For Each p As BillBoardPage In pagesList.Values
                p.PageContent = "{| class=""wikitable"" style=""width: 100%; clear: both; text-align: left;"""
                p.PageContent &= Environment.NewLine
                p.PageContent &= "|-"
                p.PageContent &= "! style=""width: 15%;"" | Fecha"
                p.PageContent &= "! &ndash;"
                p.PageContent &= "! style=""width: 80%;"" | Anuncio / Evento"

                For Each e In p.BillBoardEvents
                    newPageText = newPageText.Replace(e.EventOriginalText, "")
                    p.PageContent &= e.EventOriginalText
                Next

                p.PageContent &= "|}"
                p.PageContent &= Environment.NewLine
                p.PageContent &= "{{Wikipedia:Cartelera de acontecimientos/Archivo}}"
                p.PageContent &= Environment.NewLine
                p.PageContent &= "[[Categoría:Wikipedia:Cartelera de acontecimientos|R" & p.PageMonth.ToString("00") & "]]"

                Dim thePage As Page = Bot.Getpage(p.PageName)
                thePage.Save(p.PageContent, "(Bot) Archivando Cartelera de acontecimientos", False, True, True)
            Next

            pageToArchive.Save(newPageText, "(Bot) Archivando Cartelera de acontecimientos", False, True, True)
            EventLogger.Log("Archivado de cartelera de acontecimientos finalizado", "BillboardArchiver.ArchivePage")
            Return True

        Catch ex As Exception
            EventLogger.EX_Log(ex.Message, "BillboardArchiver.ArchivePage")
        End Try
        Return False

    End Function


    Function GetDateFromBillboardDateString(ByVal dateString As String, ByVal currentDate As Date) As Date
        Dim m As Match = Regex.Match(dateString, "(\d{1,2}) +\w{2} +(\w{4,11})")
        If Not m.Success Then
            Return Nothing
        End If
        Dim dayStr As String = m.Groups(1).Value.Trim
        Dim monthStr As String = m.Groups(2).Value.ToLower.Trim
        Dim monthInt As Integer = 0
        Dim dayInt As Integer = 0
        Dim isday As Boolean = Integer.TryParse(dayStr, dayInt)
        If Not isday Then Return Nothing
        If dayInt > 31 Then Return Nothing
        Dim currentMonthInt As Integer = currentDate.Month
        Dim yearInt As Integer = currentDate.Year
        Select Case monthStr
            Case "enero"
                monthInt = 1
            Case "febrero"
                monthInt = 2
            Case "marzo"
                monthInt = 3
            Case "abril"
                monthInt = 4
            Case "mayo"
                monthInt = 5
            Case "junio"
                monthInt = 6
            Case "julio"
                monthInt = 7
            Case "agosto"
                monthInt = 8
            Case "septiembre"
                monthInt = 9
            Case "setiembre"
                monthInt = 9
            Case "octubre"
                monthInt = 10
            Case "noviembre"
                monthInt = 11
            Case "diciembre"
                monthInt = 12
            Case Else
                monthInt = 0
        End Select
        If monthInt = 0 Then Return Nothing
        If monthInt > currentMonthInt Then
            yearInt += -1
        End If
        Dim resultDate As New Date(yearInt, monthInt, dayInt)
        Return resultDate

    End Function



End Class

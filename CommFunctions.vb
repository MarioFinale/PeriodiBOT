Option Strict On
Imports System.Text.RegularExpressions

Public Module CommFunctions

    Public Function Log(ByVal text As String, source As String, user As String) As Boolean
        Return LogC.Log(text, source, user)
    End Function
    Public Function Debug_Log(ByVal text As String, source As String, user As String) As Boolean
        Return LogC.Debug_log(text, source, user)
    End Function
    Public Function SetUserTime(ByVal UserAndTime As String()) As Boolean
        Return LogC.SetUserTime(UserAndTime)
    End Function
    Public Function SaveUsersToFile() As Boolean
        Return LogC.SaveUsersToFile()
    End Function
    Public Function LastLog(ByRef Source As String, ByVal user As String) As String()
        Return LogC.Lastlog(Source, user)
    End Function
    Public Function EndLog() As Boolean
        LogC.EndLog = True
        Return True
    End Function

    Function LoadConfig() As Boolean
        Dim MainBotName As String = String.Empty
        Dim WPSite As String = String.Empty
        Dim WPAPI As String = String.Empty
        Dim WPBotUserName As String = String.Empty
        Dim WPBotPassword As String = String.Empty
        Dim IRCBotNickName As String = String.Empty
        Dim IRCBotPassword As String = String.Empty
        Dim MainIRCNetwork As String = String.Empty
        Dim MainIRCChannel As String = String.Empty
        Dim ConfigOK As Boolean = False
        If System.IO.File.Exists(ConfigFilePath) Then
            Log("Loading config", "LOCAL", "Undefined")
            Dim Configstr As String = System.IO.File.ReadAllText(ConfigFilePath)
            Try
                MainBotName = TextInBetween(Configstr, "BOTName=""", """")(0)
                WPBotUserName = TextInBetween(Configstr, "WPUserName=""", """")(0)
                WPSite = TextInBetween(Configstr, "PageURL=""", """")(0)
                WPBotPassword = TextInBetween(Configstr, "WPBotPassword=""", """")(0)
                WPAPI = TextInBetween(Configstr, "ApiURL=""", """")(0)
                MainIRCNetwork = TextInBetween(Configstr, "IRCNetwork=""", """")(0)
                IRCBotNickName = TextInBetween(Configstr, "IRCBotNickName=""", """")(0)
                IRCBotPassword = TextInBetween(Configstr, "IRCBotPassword=""", """")(0)
                MainIRCChannel = TextInBetween(Configstr, "IRCChannel=""", """")(0)
                ConfigOK = True
            Catch ex As IndexOutOfRangeException
                Log("Malformed config", "LOCAL", "Undefined")
            End Try
        Else
            Log("No config file", "LOCAL", "Undefined")
            System.IO.File.Create(ConfigFilePath).Close()
        End If

        If Not ConfigOK Then
            Console.Clear()
            Console.WriteLine("No config file, please fill the data or close the program and create a config file.")
            Console.WriteLine("Bot Name: ")
            MainBotName = Console.ReadLine
            Console.WriteLine("Wikipedia Username: ")
            WPBotUserName = Console.ReadLine
            Console.WriteLine("Wikipedia bot password: ")
            WPBotPassword = Console.ReadLine
            Console.WriteLine("Wikipedia main URL: ")
            WPSite = Console.ReadLine
            Console.WriteLine("Wikipedia API URL: ")
            WPAPI = Console.ReadLine
            Console.WriteLine("IRC Network: ")
            MainIRCNetwork = Console.ReadLine
            Console.WriteLine("IRC NickName: ")
            IRCBotNickName = Console.ReadLine
            Console.WriteLine("IRC nickserv/server password (press enter if not password is set): ")
            IRCBotPassword = Console.ReadLine
            Console.WriteLine("IRC main Channel: ")
            MainIRCChannel = Console.ReadLine

            Dim configstr As String = String.Format("======================CONFIG======================
BOTName=""{0}""
WPUserName=""{1}""
WPBotPassword=""{2}""
PageURL=""{3}""
ApiURL=""{4}""
IRCNetwork=""{5}""
IRCBotNickName=""{6}""
IRCBotPassword=""{7}""
IRCChannel=""{8}""", MainBotName, WPBotUserName, WPBotPassword, WPSite, WPAPI, MainIRCNetwork, IRCBotNickName, IRCBotPassword, MainIRCChannel)

            System.IO.File.WriteAllText(ConfigFilePath, configstr)
        End If
        BOTName = MainBotName
        WPUserName = WPBotUserName
        BOTPassword = WPBotPassword
        site = WPSite
        ApiURL = WPAPI
        IRCNetwork = MainIRCNetwork
        BOTIRCName = IRCBotNickName
        IRCPassword = IRCBotPassword
        IRCChannel = MainIRCChannel
        Return True
    End Function



    Function GetMostRecentDateTime(ByVal comment As String) As DateTime
        Dim dattimelist As New List(Of DateTime)
        Debug_Log("Begin GetLastDateTime", "LOCAL", BOTName)

        For Each m As Match In Regex.Matches(comment, "(\ [0-9][0-9]:[0-9][0-9]\ ).*(\(UTC\))")
            Try
                Dim parsedtxt As String = m.Value.Replace(" ene ", "/01/").Replace(" feb ", "/02/") _
                .Replace(" mar ", "/03/").Replace(" abr ", "/04/").Replace(" may ", "/05/") _
                .Replace(" jun ", "/06/").Replace(" jul ", "/07/").Replace(" ago ", "/08/") _
                .Replace(" sep ", "/09/").Replace(" oct ", "/10/").Replace(" nov ", "/11/") _
                .Replace(" dic ", "/12/").Replace(vbLf, String.Empty).Replace(" (UTC)", String.Empty)

                Debug_Log("GetMostRecentDateTime: Try parse", "LOCAL", BOTName)
                dattimelist.Add(DateTime.Parse(parsedtxt))
                Debug_Log("GetMostRecentDateTime parse string: """ & parsedtxt & """", "LOCAL", BOTName)
            Catch ex As System.FormatException
                Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions", BOTName)
            End Try
        Next
        Debug_Log("GetMostRecentDateTime: Sorting", "LOCAL", BOTName)
        dattimelist.Sort()
        If Not dattimelist.Count = 0 Then
            Debug_Log("GetMostRecentDateTime: returning """ & dattimelist.Last.ToLongDateString & """", "LOCAL", BOTName)
            Return dattimelist.Last
        Else
            Debug_Log("GetMostRecentDateTime: Returning nothing ", "LOCAL", BOTName)
            Return Nothing
        End If
    End Function


    Function SQLSAFEPARSE(ByVal text As String) As String
        text = text.Replace("'", """").Replace("%27", "°27")
        Return text
    End Function

    Function IsODD(ByVal Number As Integer) As Boolean
        Return (Number Mod 2 = 0)
    End Function

    Function CountString(ByVal inputString As String, ByVal stringToSearch As String) As Integer
        Return Regex.Split(inputString, stringToSearch).Length - 1
    End Function

    Function LvlOfAppereance(ByVal Phrase As String, words As String()) As Double
        Dim PhraseString As String() = Phrase.Split(Chr(32))
        Dim NOWords As Integer = PhraseString.Count
        Dim NOAppeareances As Integer = 0
        For a As Integer = 0 To NOWords - 1
            For Each s As String In words
                If PhraseString(a).ToLower.Contains(s.ToLower) Then
                    NOAppeareances = NOAppeareances + 1
                End If
            Next
        Next
        Return ((CType(NOAppeareances, Double) * 100) / CType(NOWords, Double))

    End Function


    Public Function UnixToTime(ByVal strUnixTime As String) As Date
        UnixToTime = DateAdd(DateInterval.Second, Val(strUnixTime), #1/1/1970#)
        If UnixToTime.IsDaylightSavingTime = True Then
            UnixToTime = DateAdd(DateInterval.Hour, 1, UnixToTime)
        End If
    End Function

    Public Function TimeStringToSeconds(ByVal time As String) As Integer
        Try

            Dim Str1 As String() = time.Split(CType(":", Char))
            Dim Str2 As String() = Str1(0).Split(CType(".", Char))

            Dim Str_Days As String = Str2(0)
            Dim Str_Hours As String = Str2(1)
            Dim Str_Minutes As String = Str1(1)

            Dim Days As Integer = Convert.ToInt32(Str_Days) * 86400
            Dim Hours As Integer = Convert.ToInt32(Str_Hours) * 3600
            Dim Minutes As Integer = Convert.ToInt32(Str_Minutes) * 60

            Dim total As Integer = (Days + Hours + Minutes)
            Return total

        Catch ex As Exception
            Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "CommFuncs", BOTName)
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "CommFuncs", BOTName)
            Return 0
        End Try
    End Function

    Public Function WaitSeconds(ByVal seconds As Integer) As Boolean
        System.Threading.Thread.Sleep(seconds * 1000)
        Return True
    End Function

    Public Function TimeToUnix(ByVal dteDate As Date) As Integer
        If dteDate.IsDaylightSavingTime = True Then
            dteDate = DateAdd(DateInterval.Hour, -1, dteDate)
        End If
        TimeToUnix = CInt(DateDiff(DateInterval.Second, #1/1/1970#, dteDate))
    End Function

    ''' <summary>
    ''' Separa un array de string segun lo especificado. Retorna una lista con listas de texto.
    ''' </summary>
    ''' <param name="StrArray">Lista a partir</param>
    ''' <param name="chunkSize">En cuantos items se parte</param>
    ''' <returns></returns>
    Function SplitStringArrayIntoChunks(StrArray As String(), chunkSize As Integer) As List(Of List(Of String))
        Return StrArray.
                Select(Function(x, i) New With {Key .Index = i, Key .Value = x}).
                GroupBy(Function(x) (x.Index \ chunkSize)).
                Select(Function(x) x.Select(Function(v) v.Value).ToList()).
                ToList()
    End Function

    ''' <summary>
    ''' Separa un array de integer segun lo especificado. Retorna una lista con listas de integer.
    ''' </summary>
    ''' <param name="IntArray">Lista a partir</param>
    ''' <param name="chunkSize">En cuantos items se parte</param>
    ''' <returns></returns>
    Function SplitIntegerArrayIntoChunks(IntArray As Integer(), chunkSize As Integer) As List(Of List(Of Integer))
        Return IntArray.
                Select(Function(x, i) New With {Key .Index = i, Key .Value = x}).
                GroupBy(Function(x) (x.Index \ chunkSize)).
                Select(Function(x) x.Select(Function(v) v.Value).ToList()).
                ToList()
    End Function


    Function EsWikiDatetime(ByVal text As String) As DateTime
        Dim TheDate As DateTime = Nothing
        Dim matchc As MatchCollection = Regex.Matches(text, "([0-9]{2}):([0-9]{2}) ([0-9]{2}|[0-9]) ([Z-z]{3}) [0-9]{4} \(UTC\)")

        If matchc.Count = 0 Then
            Return DateTime.Parse("23:59 31/12/9999")
        End If

        For Each m As Match In matchc
            Try
                Dim parsedtxt As String = m.Value.ToLower.Replace(" ene ", "/01/").Replace(" feb ", "/02/") _
                .Replace(" mar ", "/03/").Replace(" abr ", "/04/").Replace(" may ", "/05/") _
                .Replace(" jun ", "/06/").Replace(" jul ", "/07/").Replace(" ago ", "/08/") _
                .Replace(" sep ", "/09/").Replace(" oct ", "/10/").Replace(" nov ", "/11/") _
                .Replace(" dic ", "/12/").Replace(vbLf, String.Empty).Replace(" (utc)", String.Empty)
                parsedtxt = parsedtxt.Replace(" 1/", " 01/").Replace(" 2/", " 02/").Replace(" 3/", " 03/").
                Replace(" 4/", " 04/").Replace(" 5/", " 05/").Replace(" 6/", " 06/").Replace(" 7/", " 07/").
                Replace(" 8/", " 08/").Replace(" 9/", " 09/")

                Debug_Log("GetLastDateTime: Try parse", "LOCAL", BOTName)
                TheDate = DateTime.ParseExact(parsedtxt, "HH:mm dd'/'MM'/'yyyy", System.Globalization.CultureInfo.InvariantCulture)

                Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """", "LOCAL", BOTName)
            Catch ex As System.FormatException
                Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions", BOTName)
            End Try

        Next
        Return TheDate

    End Function


    Function GetLastDateTime(ByVal comment As String) As DateTime
        Dim dattimelist As New List(Of DateTime)
        Debug_Log("Begin GetLastDateTime", "LOCAL", BOTName)

        For Each m As Match In Regex.Matches(comment, "(\ [0-9][0-9]:[0-9][0-9]\ ).*(\(UTC\))")
            Debug_Log("GetLastDateTime match: """ & m.Value & """", "LOCAL", BOTName)
            Try
                Dim parsedtxt As String = m.Value.ToLower.Replace(" ene ", "/01/").Replace(" feb ", "/02/") _
                .Replace(" mar ", "/03/").Replace(" abr ", "/04/").Replace(" may ", "/05/") _
                .Replace(" jun ", "/06/").Replace(" jul ", "/07/").Replace(" ago ", "/08/") _
                .Replace(" sep ", "/09/").Replace(" oct ", "/10/").Replace(" nov ", "/11/") _
                .Replace(" dic ", "/12/").Replace(vbLf, String.Empty).Replace(" (utc)", String.Empty)

                Debug_Log("GetLastDateTime: Try parse", "LOCAL", BOTName)
                dattimelist.Add(DateTime.Parse(parsedtxt))
                Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """", "LOCAL", BOTName)
            Catch ex As System.FormatException
                Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "LOCAL", BOTName)
            End Try
        Next
        If Not dattimelist.Count = 0 Then
            Debug_Log("GetMostRecentDateTime: returning """ & dattimelist.Last.ToLongDateString & """", "LOCAL", BOTName)
            Return dattimelist.Last
        Else
            Debug_Log("GetMostRecentDateTime: Returnung nothing ", "LOCAL", BOTName)
            Return Nothing
        End If

    End Function


    ''' <summary>
    ''' Verifica si un usuario programado no ha editado en el tiempo especificado.
    ''' </summary>
    ''' <returns></returns>
    Function CheckUsers() As String()
        Dim returnstring As New List(Of String)
        Try
            For Each UserdataLine As String() In Userdata
                Dim User As String = UserdataLine(1)
                Dim OP As String = UserdataLine(0)
                Dim UserDate As String = UserdataLine(2)

                Log("CheckUsers: Checking user " & User, "IRC", BOTName)
                Dim LastEdit As DateTime = Mainwikibot.GetLastEditTimestampUser(User)
                If LastEdit.Year = 1111 Then
                    Log("CheckUsers: The user " & User & " has not edited on this wiki", "IRC", BOTName)
                    Continue For
                End If
                Dim actualtime As DateTime = DateTime.UtcNow

                Dim LastEditUnix As Integer = CInt(TimeToUnix(LastEdit))
                Dim ActualTimeUnix As Integer = CInt(TimeToUnix(actualtime))


                Dim Timediff As Integer = ActualTimeUnix - LastEditUnix - 3600
                Dim TriggerTimeDiff As Long = TimeStringToSeconds(UserDate)

                Dim TimediffToHours As Integer = CInt(Timediff / 3600)
                Dim TimediffToMinutes As Integer = CInt(Timediff / 60)
                Dim TimediffToDays As Integer = CInt(Timediff / 86400)
                Dim responsestring As String = String.Empty

                Console.WriteLine("Timediff  " & User & ": " & Timediff)
                Console.WriteLine("triggertimediff " & User & ": " & TriggerTimeDiff)

                If Timediff > TriggerTimeDiff Then

                    If TimediffToMinutes <= 1 Then
                        responsestring = String.Format("¡{0} editó recién!", User)
                    Else
                        If TimediffToMinutes < 60 Then
                            responsestring = String.Format("La última edición de {0} fue hace {1} minutos", User, TimediffToMinutes)
                        Else
                            If TimediffToMinutes < 120 Then
                                responsestring = String.Format("La última edición de {0} fue hace más de {1} hora", User, TimediffToHours)
                            Else
                                If TimediffToMinutes < 1440 Then
                                    responsestring = String.Format("La última edición de {0} fue hace más de {1} horas", User, TimediffToHours)
                                Else
                                    If TimediffToMinutes < 2880 Then
                                        responsestring = String.Format("La última edición de {0} fue hace {1} día", User, TimediffToDays)
                                    Else
                                        responsestring = String.Format("La última edición de {0} fue hace más de {1} días", User, TimediffToDays)
                                    End If
                                End If
                            End If
                        End If
                    End If
                    returnstring.Add(String.Format("PRIVMSG {0} :{1}| El proximo aviso será en 5 minutos.", OP, responsestring))
                End If
            Next
        Catch ex As System.ObjectDisposedException
            Debug_Log("CheckUsers EX: " & ex.Message, "IRC", BOTName)
        End Try

        Return returnstring.ToArray

    End Function

    ''' <summary>
    ''' Retorna una lista de plantillas si se le entrega como parámetro un array de tipo string con texto en formato válido de plantilla.
    ''' Si uno de los items del array no tiene formato válido, entregará una plantilla vacia en su lugar ("{{}}").
    ''' </summary>
    ''' <param name="templatearray"></param>
    ''' <returns></returns>
    Function GetTemplates(ByVal templatearray As List(Of String)) As List(Of Template)
        Dim TemplateList As New List(Of Template)
        For Each t As String In templatearray
            TemplateList.Add(New Template(t, False))
        Next
        Return TemplateList
    End Function

    Function GetGrillitusTemplate(ByVal test As String) As Template

        Dim templist As List(Of Template) = GetTemplates(GetTemplateTextArray(test))
        Dim Grittemp As New Template
        For Each t As Template In templist
            If Regex.Match(t.Name, " *[Uu]suario *: *[Gg]rillitus\/Archivar").Success Then
                Grittemp = t
                Exit For
            End If
        Next
        Return Grittemp

    End Function


End Module

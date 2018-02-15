﻿Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.WikiBot

Public Module CommFunctions

    ''' <summary>
    ''' Registra un evento normal.
    ''' </summary>
    ''' <param name="text">Texto del evento</param>
    ''' <param name="source">origen del evento</param>
    ''' <param name="user">Usuario que origina el evento</param>
    ''' <returns></returns>
    Public Function Log(ByVal text As String, source As String, user As String) As Boolean
        Return LogC.Log(text, source, user)
    End Function
    ''' <summary>
    ''' Registra un evento de tipo debug.
    ''' </summary>
    ''' <param name="text">Texto del evento</param>
    ''' <param name="source">origen del evento</param>
    ''' <param name="user">Usuario que origina el evento</param>
    ''' <returns></returns>
    Public Function Debug_Log(ByVal text As String, source As String, user As String) As Boolean
        Return LogC.Debug_log(text, source, user)
    End Function
    ''' <summary>
    ''' Registra una excepción.
    ''' </summary>
    ''' <param name="text">Texto del evento</param>
    ''' <param name="source">origen del evento</param>
    ''' <param name="user">Usuario que origina el evento</param>
    ''' <returns></returns>
    Public Function EX_Log(ByVal text As String, source As String, user As String) As Boolean
        Return LogC.EX_Log(text, source, user)
    End Function

    ''' <summary>
    ''' Añade un usuario a la lista de aviso de inactividad.
    ''' </summary>
    ''' <param name="UserAndTime"></param>
    ''' <returns></returns>
    Public Function SetUserTime(ByVal userAndTime As String()) As Boolean
        Return LogC.SetUserTime(userAndTime)
    End Function
    ''' <summary>
    ''' Guarda los usuarios en la lista de aviso de inactividad.
    ''' </summary>
    ''' <returns></returns>
    Public Function SaveUsersToFile() As Boolean
        Return LogC.SaveUsersToFile()
    End Function
    ''' <summary>
    ''' Entrega el último evento registrado
    ''' </summary>
    ''' <param name="Source"></param>
    ''' <param name="user"></param>
    ''' <returns></returns>
    Public Function LastLog(ByRef source As String, ByVal user As String) As String()
        Return LogC.Lastlog(source, user)
    End Function
    ''' <summary>
    ''' Finaliza la instancia del motor de log
    ''' </summary>
    ''' <returns></returns>
    Public Function EndLog() As Boolean
        LogC.EndLog = True
        Return True
    End Function
    ''' <summary>
    ''' Finaliza el programa correctamente.
    ''' </summary>
    Sub ExitProgram()
        EndLog()
        Environment.Exit(0)
    End Sub

    ''' <summary>
    ''' Entrega la fecha más reciente en una fira de un comentario de wikipedia.
    ''' </summary>
    ''' <param name="comment"></param>
    ''' <returns></returns>
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

    ''' <summary>
    ''' Retorna true si un numero es par.
    ''' </summary>
    ''' <param name="Number"></param>
    ''' <returns></returns>
    Function IsODD(ByVal number As Integer) As Boolean
        Return (number Mod 2 = 0)
    End Function

    ''' <summary>
    ''' Cuenta cuantas veces se repite una cadena de texto dada dentro de otra cadena de texto.
    ''' </summary>
    ''' <param name="input">Cadena de texto donde se busca</param>
    ''' <param name="value">CAdena de texto a buscar</param>
    ''' <returns></returns>
    Function CountString(ByVal input As String, ByVal value As String) As Integer
        Return Regex.Split(input, RegexParser(value)).Length - 1
    End Function

    ''' <summary>
    ''' Entrega un valor que simboliza el nivel de aparición de las palabras indicadas
    ''' </summary>
    ''' <param name="Phrase">Frase a evaluar</param>
    ''' <param name="words">Palabras a buscar</param>
    ''' <returns></returns>
    Function LvlOfAppereance(ByVal phrase As String, words As String()) As Double
        If (phrase Is Nothing) Or (words Is Nothing) Then
            Return 0
        End If
        Dim PhraseString As String() = phrase.Split(Chr(32))
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

    ''' <summary>
    ''' Convierte una cadena de texto con una hora en formato unix a DateTime
    ''' </summary>
    ''' <param name="strUnixTime"></param>
    ''' <returns></returns>
    Public Function UnixToTime(ByVal strUnixTime As String) As Date
        UnixToTime = DateAdd(DateInterval.Second, Val(strUnixTime), #1/1/1970#)
        If UnixToTime.IsDaylightSavingTime = True Then
            UnixToTime = DateAdd(DateInterval.Hour, 1, UnixToTime)
        End If
    End Function

    ''' <summary>
    ''' Convierte una cadena de texto con formatoe special a segundos
    ''' </summary>
    ''' <param name="time"></param>
    ''' <returns></returns>
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
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "CommFuncs", BOTName)
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Establece un tiempo de espera (en segundos)
    ''' </summary>
    ''' <param name="seconds"></param>
    ''' <returns></returns>
    Public Function WaitSeconds(ByVal seconds As Integer) As Boolean
        System.Threading.Thread.Sleep(seconds * 1000)
        Return True
    End Function

    ''' <summary>
    ''' Convierte una fecha a un numero entero que representa la hora en formato unix
    ''' </summary>
    ''' <param name="dteDate"></param>
    ''' <returns></returns>
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
    Function SplitStringArrayIntoChunks(strArray As String(), chunkSize As Integer) As List(Of List(Of String))
        Return strArray.
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
    Function SplitIntegerArrayIntoChunks(intArray As Integer(), chunkSize As Integer) As List(Of List(Of Integer))
        Return intArray.
                Select(Function(x, i) New With {Key .Index = i, Key .Value = x}).
                GroupBy(Function(x) (x.Index \ chunkSize)).
                Select(Function(x) x.Select(Function(v) v.Value).ToList()).
                ToList()
    End Function

    ''' <summary>
    ''' Entrega 
    ''' </summary>
    ''' <param name="text">Entrega la primera fecha, que aparezca en un texto dado (si la fecha tiene formato de firma wikipedia).</param>
    ''' <returns></returns>
    Function ESWikiDatetime(ByVal text As String) As DateTime
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

    ''' <summary>
    ''' Retorna la última fecha en un comentario (si la fecha tiene formato de firma wikipedia).
    ''' </summary>
    ''' <param name="comment"></param>
    ''' <returns></returns>
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
    ''' Retorna una lista de plantillas si se le entrega como parámetro un array de tipo string con texto en formato válido de plantilla.
    ''' Si uno de los items del array no tiene formato válido, entregará una plantilla vacia en su lugar ("{{}}").
    ''' </summary>
    ''' <param name="templatearray"></param>
    ''' <returns></returns>
    Function GetTemplates(ByVal templatearray As List(Of String)) As List(Of Template)
        If templatearray Is Nothing Then
            Return New List(Of Template)
        End If
        Dim TemplateList As New List(Of Template)
        For Each t As String In templatearray
            TemplateList.Add(New Template(t, False))
        Next
        Return TemplateList
    End Function

    ''' <summary>
    ''' Retorna todas las plantillas que encuentre en una pagina, de no haber entregará una list vacia.
    ''' </summary>
    ''' <param name="WikiPage"></param>
    ''' <returns></returns>
    Function GetTemplates(ByVal wikiPage As Page) As List(Of Template)
        If wikiPage Is Nothing Then
            Return New List(Of Template)
        End If
        Dim TemplateList As New List(Of Template)
        Dim temps As List(Of String) = GetTemplateTextArray(wikiPage.Text)

        For Each t As String In temps
            TemplateList.Add(New Template(t, False))
        Next
        Return TemplateList
    End Function

    Function GetCurrentThreads() As Integer
        Try
            Return Process.GetCurrentProcess().Threads.Count 
        Catch ex As Exception
            Debug_Log(ex.Message, "LOCAL", BOTName)
            Return 0
        End Try

    End Function

    Function PrivateMemory() As Long
        Try
            Return CLng(Process.GetCurrentProcess().PrivateMemorySize64 / 1024)
        Catch ex As Exception
            Debug_Log(ex.Message, "LOCAL", BOTName)
            Return 0
        End Try

    End Function

    Function UsedMemory() As Long
        Try
            Return CLng(Process.GetCurrentProcess().WorkingSet64 / 1024)
        Catch ex As Exception
            Debug_Log(ex.Message, "LOCAL", BOTName)
            Return 0
        End Try

    End Function


    Sub RunDailyTask()
        ArchiveAllInclusions(True)
    End Sub

    Sub WriteLine(ByVal type As String, ByVal source As String, message As String)
        Dim msgstr As String = "[" & DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & "]" & " [" & source & " " & type & "] " & message
        Console.WriteLine(msgstr)
    End Sub

    Function PressKeyTimeout() As Boolean
        Dim exitloop As Boolean = False
        Console.WriteLine("Press any key to exit or wait 5 seconds")
        For timeout As Integer = 0 To 5
            Console.Write(".")
            If Console.KeyAvailable Then
                exitloop = True
                Exit For
            End If
            System.Threading.Thread.Sleep(1000)
        Next
        Return exitloop
    End Function


End Module

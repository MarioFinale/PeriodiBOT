Option Strict On
Option Explicit On
Imports System.Runtime.Serialization
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot

NotInheritable Class CommFunctions
#Region "Prevent init"
    Private Sub New()
    End Sub
#End Region

#Region "Text Functions"
    ''' <summary>
    ''' Elimina los tags html de una cadena de texto dada.
    ''' </summary>
    ''' <param name="html">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function StripTags(ByVal html As String) As String
        Return Regex.Replace(html, "<.*?>", String.Empty)
    End Function
    ''' <summary>
    ''' Elimina las líneas en blanco.
    ''' </summary>
    ''' <param name="Str">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function Removewhitelines(ByVal Str As String) As String
        Return Regex.Replace(Str, "^\s+$[\r\n]*", "", RegexOptions.Multiline)
    End Function
    ''' <summary>
    ''' Elimina los excesos de espacios (consecutivos) en una cadena de texto.
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function RemoveExcessOfSpaces(ByVal text As String) As String
        Return Regex.Replace(text, "\s{2,}", " ")
    End Function
    ''' <summary>
    ''' Cuenta las veces que un carácter aparece en una cadena de texto dada.
    ''' </summary>
    ''' <param name="value">Texto a evaluar.</param>
    ''' <param name="ch">Carácter a buscar.</param>
    ''' <returns></returns>
    Public Shared Function CountCharacter(ByVal value As String, ByVal ch As Char) As Integer
        Return value.Count(Function(c As Char) c = ch)
    End Function
    ''' <summary>
    ''' Regresa la misma cadena de texto, pero con la primera letra mayúscula.
    ''' </summary>
    ''' <param name="val">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function UppercaseFirstCharacter(ByVal val As String) As String
        If String.IsNullOrEmpty(val) Then
            Return val
        End If
        Dim array() As Char = val.ToCharArray
        array(0) = Char.ToUpper(array(0))
        Return New String(array)
    End Function
    ''' <summary>
    ''' Convierte el texto completo a minúsculas y luego coloca en mayúsculas la primera letra.
    ''' </summary>
    ''' <param name="text">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function NormalizeText(ByVal text As String) As String
        Dim s As String = text.ToLower
        Return UppercaseFirstCharacter(s)
    End Function
    ''' <summary>
    ''' Verifica si una cadena de texto es numérica.
    ''' </summary>
    ''' <param name="number">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function IsNumeric(ByVal number As String) As Boolean
        If Regex.IsMatch(number, "^[0-9 ]+$") Then
            Return True
        Else
            Return False
        End If
    End Function
    ''' <summary>
    ''' Une un array de texto usando el separador indicado
    ''' </summary>
    ''' <param name="arr">Array de texto a unir</param>
    ''' <param name="separator">Separador entre cada elemento del array</param>
    ''' <returns></returns>
    Public Shared Function JoinTextArray(ByVal arr As String(), separator As Char) As String
        Dim responsestring As String = String.Empty
        For Each s As String In arr
            responsestring = responsestring & s & separator
        Next
        Return responsestring.TrimEnd(separator)
    End Function

    ''' <summary>
    ''' Realiza los escapes para usar una cadena de texto dentro de una expresión regular.
    ''' </summary>
    ''' <param name="s">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function RegexParser(ByVal s As String) As String
        s = s.Replace("\", "\\")
        Return s.Replace("[", "\[").Replace("/", "\/").Replace("^", "\^").Replace("$", "\$").Replace(".", "\.") _
            .Replace("|", "\|").Replace("?", "\?").Replace("*", "\*").Replace("+", "\+").Replace("(", "\(").Replace(")", "\)") _
            .Replace("{", "\{").Replace("}", "\}")
    End Function

    ''' <summary>
    ''' Realiza los escapes para usar una cadena de texto dentro de una expresión regular, exceptuando el "pipe" (|).
    ''' </summary>
    ''' <param name="s">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function SpamListParser(ByVal s As String) As String
        s = s.Replace("\", "\\")
        Return s.Replace("[", "\[").Replace("/", "\/").Replace("^", "\^").Replace("$", "\$").Replace(".", "\.") _
            .Replace("?", "\?").Replace("*", "\*").Replace("+", "\+").Replace("(", "\(").Replace(")", "\)") _
            .Replace("{", "\{").Replace("}", "\}")
    End Function
    ''' <summary>
    ''' Entrega el título de la página en la pseudoplantilla de resúmenes de página.
    ''' </summary>
    ''' <param name="SourceString">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function GetTitlesOfTemplate(ByVal SourceString As String) As String()
        Dim mlist As New List(Of String)
        mlist = TextInBetween(SourceString, "|" & Environment.NewLine & "|", "=").ToList
        Return mlist.ToArray
    End Function

    ''' <summary>
    ''' Reeplaza la primera ocurrencia de una cadena dada en la cadena de entrada.
    ''' </summary>
    ''' <param name="text">Cadena de texto a modificar.</param>
    ''' <param name="search">Cadena texto a buscar.</param>
    ''' <param name="replace">Cadena texto de reemplazo.</param>
    ''' <returns></returns>
    Public Shared Function ReplaceFirst(ByVal text As String, ByVal search As String, ByVal replace As String) As String
        Dim pos As Integer = text.IndexOf(search)
        If pos < 0 Then
            Return text
        End If
        Return text.Substring(0, pos) & replace + text.Substring(pos + search.Length)
    End Function

    ''' <summary>
    ''' Retorna las líneas contenidas en una cadena de texto.
    ''' </summary>
    ''' <param name="text">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function GetLines(ByVal text As String) As String()
        Return GetLines(text, False)
    End Function

    ''' <summary>
    ''' Retorna las líneas contenidas en una cadena de texto.
    ''' </summary>
    ''' <param name="text">Texto a evaluar.</param>
    ''' <param name="removeemptylines">Eliminar las líneas vacías</param>
    ''' <returns></returns>
    Public Shared Function GetLines(ByVal text As String, ByVal removeEmptyLines As Boolean) As String()
        Dim thelines As List(Of String) = text.Split({vbCrLf, vbCr, vbLf, Environment.NewLine}, StringSplitOptions.None).ToList
        If removeEmptyLines Then
            thelines.RemoveAll(Function(x) String.IsNullOrWhiteSpace(x))
        End If
        Return thelines.ToArray
    End Function

    ''' <summary>
    ''' Soluciona un error de la api en los resúmenes, donde cuertas plantillas los números los entrega repetidos con varios símbolos en medio.
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Public Shared Function FixResumeNumericExp(ByVal text As String) As String
        Dim newtext As String = text
        For Each m As Match In Regex.Matches(text, "&+[0-9]+\.&+[0-9 ]+")
            Dim num As Integer = Integer.Parse(TextInBetween(m.Value, "&&&&&&&&", ".&&&&&")(0))
            Dim numsrt As String = num.ToString & " "
            newtext = newtext.Replace(m.Value, numsrt)
        Next
        Return newtext
    End Function
    ''' <summary>
    ''' Normaliza los títulos de la plantilla de resúmenes.
    ''' </summary>
    ''' <param name="SourceString">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function GetNormalizedTitlesOfTemplate(ByVal SourceString As String) As String()
        Dim mlist As New List(Of String)
        For Each m As Match In Regex.Matches(SourceString, "(\n\|)[\s\S]*?(=\n)")
            mlist.Add(NormalizeText(m.Value.Replace("|", String.Empty).Replace("=", String.Empty).Replace(vbLf, String.Empty).Replace("_", " ")))
        Next
        Return mlist.ToArray
    End Function
    ''' <summary>
    ''' Entrega un array con todas las coincidencias donde se encuentre un texto en medio de dos cadenas de texto
    ''' </summary>
    ''' <param name="SourceString">Texto a evaluar.</param>
    ''' <param name="string1">Texto a la izquierda.</param>
    ''' <param name="string2">Texto a la derecha.</param>
    ''' <returns></returns>
    Public Shared Function TextInBetween(ByVal SourceString As String, string1 As String, string2 As String) As String()
        Dim mlist As New List(Of String)
        For Each m As Match In Regex.Matches(SourceString, "(" & RegexParser(string1) & ")[\s\S]*?(" & RegexParser(string2) & ")")
            mlist.Add(m.Value.Replace(string1, String.Empty).Replace(string2, String.Empty))
        Next
        Return mlist.ToArray
    End Function
    ''' <summary>
    ''' Similar a TextInBetween pero incluye además las las cadenas a la izquierda y derecha del texto.
    ''' </summary>
    ''' <param name="SourceString">Texto a evaluar.</param>
    ''' <param name="string1">Texto a la izquierda.</param>
    ''' <param name="string2">Texto a la derecha</param>
    ''' <returns></returns>
    Public Shared Function TextInBetweenInclusive(ByVal SourceString As String, string1 As String, string2 As String) As String()
        Dim mlist As New List(Of String)
        For Each m As Match In Regex.Matches(SourceString, "(" & RegexParser(string1) & ")[\s\S]*?(" & RegexParser(string2) & ")")
            mlist.Add(m.Value)
        Next
        Return mlist.ToArray
    End Function
    ''' <summary>
    ''' Entrega un array con todos los números (integer) que estén entre comillas.
    ''' </summary>
    ''' <param name="SourceString">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function MatchQuotedIntegers(ByVal SourceString As String) As String()
        Dim mlist As New List(Of String)
        For Each m As Match In Regex.Matches(SourceString, "(""[0-9]+"")")
            mlist.Add(m.Value.Replace("""", ""))
        Next
        Return mlist.ToArray
    End Function
    ''' <summary>
    ''' Entrega los títulos en una cadena de texto con el formatod e respuesta de la Api de wikipedia.
    ''' </summary>
    ''' <param name="sourcestring">Texto a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function GetTitlesFromQueryText(ByVal sourcestring As String) As String()
        Return TextInBetween(sourcestring, ",""title"":""", """,")
    End Function

    ''' <summary>
    ''' Normaliza el texto ASCII con códigos unicodes escapados con el formato \\u(número)
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Public Shared Function NormalizeUnicodetext(ByVal text As String) As String
        Dim temptext As String = Regex.Replace(text, "\\u([\dA-Fa-f]{4})", Function(v) ChrW(Convert.ToInt32(v.Groups(1).Value, 16)))
        temptext = Regex.Replace(temptext, "(?<!\\)\\n", Environment.NewLine)
        temptext = Regex.Replace(temptext, "(?<!\\)\\t", ControlChars.Tab)
        temptext = temptext.Replace("\""", """")
        temptext = temptext.Replace("\\", "\")
        temptext = temptext.Replace("\\n", "\n")
        temptext = temptext.Replace("\\t", "\t")
        Return temptext
    End Function

    ''' <summary>
    ''' Codifica una cadena de texto en URLENCODE.
    ''' </summary>
    ''' <param name="text">Texto a codificar.</param>
    ''' <returns></returns>
    Public Shared Function UrlWebEncode(ByVal text As String) As String
        Dim PreTreatedText As String = Net.WebUtility.UrlEncode(text)
        Dim TreatedText As String = Regex.Replace(PreTreatedText, "%\w{2}", Function(x) x.Value.ToUpper)
        Return TreatedText
    End Function

    ''' <summary>
    ''' Decodifica una cadena de texto en URLENCODE.
    ''' </summary>
    ''' <param name="text">Texto a decodificar.</param>
    ''' <returns></returns>
    Public Shared Function UrlWebDecode(ByVal text As String) As String
        Return Net.WebUtility.UrlDecode(text)
    End Function

    ''' <summary>
    ''' Evalua una línea de texto (formato IRC según la RFC) y entrega el usuario que emitió el mensaje.
    ''' </summary>
    ''' <param name="response">Mensaje a evaluar.</param>
    ''' <returns></returns>
    Public Shared Function GetUserFromChatresponse(ByVal response As String) As String
        Return response.Split("!"c)(0).Replace(":", "")
    End Function

    ''' <summary>
    ''' Elimina todas las letras dejando únicamente números
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function RemoveAllAlphas(ByVal text As String) As String
        Return Regex.Replace(text, "[^0-9]", "")
    End Function
    ''' <summary>
    ''' Codifica texto para ser guardado en el LOG.
    ''' </summary>
    ''' <param name="text">Texto a codificar</param>
    ''' <returns></returns>
    Public Shared Function PsvSafeEncode(ByVal text As String) As String
        Return text.Replace("|"c, "%CHAR:U+007C%")
    End Function
    ''' <summary>
    ''' Decodifica texto guardado en el LOG.
    ''' </summary>
    ''' <param name="text">Texto a decodificar.</param>
    ''' <returns></returns>
    Public Shared Function PsvSafeDecode(ByVal text As String) As String
        Return text.Replace("%CHAR:U+007C%", "|")
    End Function

    ''' <summary>
    ''' Retorna una cadena de texto formateada de tal forma que muestra colores, para mas info ver https://github.com/myano/jenni/wiki/IRC-String-Formatting
    ''' </summary>
    ''' <param name="ForegroundColor">Color del texto</param>
    ''' <param name="BackgroundColor">Color del fondo, i se omite se usa el color por defecto del cliente irc</param>
    Public Shared Function ColoredText(ByVal text As String, ForegroundColor As Integer, Optional BackgroundColor As Integer = 99) As String
        Dim _foregroundColor As String = ForegroundColor.ToString("00")
        Dim _backgroundColor As String = BackgroundColor.ToString("00")
        If _backgroundColor = "99" Then
            Return Chr(3) & _foregroundColor & text & Chr(3) & Chr(15)
        Else
            Return Chr(3) & _foregroundColor & "," & _backgroundColor & text & Chr(3) & Chr(15)
        End If
    End Function



    Public Shared Function CountOccurrences(ByVal StToSerach As String, StToLookFor As String) As Integer
        Dim txtlen As Integer = StToSerach.Length
        Dim strlen As Integer = StToLookFor.Length
        Dim newstring As String = StToSerach.Replace(StToLookFor, String.Empty)
        Dim newtxtlen As Integer = newstring.Length
        Dim lenghtdiff As Integer = txtlen - newtxtlen
        Dim occurences As Integer = CInt(lenghtdiff / strlen)
        Return occurences
    End Function


    Public Shared Function SendMessagequeue(ByRef queuedat As Queue(Of Tuple(Of String, Boolean, String, IRC_Client, WikiBot.Bot))) As Boolean
        If queuedat.Count >= 1 Then
            SyncLock queuedat
                Try
                    Dim queuedmsg As Tuple(Of String, Boolean, String, IRC_Client, WikiBot.Bot) = queuedat.Dequeue()
                    Dim Commands As New IRCCommandResolver
                    Dim MsgResponse As IRCMessage = Commands.ResolveCommand(queuedmsg.Item1, queuedmsg.Item2, queuedmsg.Item3, queuedmsg.Item4, queuedmsg.Item5)
                    If Not MsgResponse Is Nothing Then
                        queuedmsg.Item4.Sendmessage(MsgResponse)
                    End If
                Catch ex As Exception
                    EventLogger.EX_Log(ex.Message, "SendMessagequeue", BotCodename)
                    Return False
                End Try
            End SyncLock
            Return True
        Else
            Return False
        End If
    End Function


    ''' <summary>
    ''' Retorna un array de string con todas las plantillas contenidas en un texto.
    ''' Pueden repetirse si hay plantillas que contienen otras en su interior.
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Public Shared Function GetTemplateTextArray(ByVal text As String) As List(Of String)
        Dim temptext As String = String.Empty
        Dim templist As New List(Of String)
        Dim CharArr As Char() = text.ToArray

        Dim OpenTemplateCount2 As Integer = 0
        Dim CloseTemplateCount2 As Integer = 0

        Dim Flag1 As Boolean = False
        Dim Flag2 As Boolean = False

        Dim beginindex As Integer = 0

        For i As Integer = 0 To CharArr.Length - 1

            If CharArr(i) = "{" Then
                If Flag1 Then
                    Flag1 = False
                    OpenTemplateCount2 += 1
                Else
                    Flag1 = True
                End If
            Else
                Flag1 = False
            End If

            If CharArr(i) = "}" Then
                If Flag2 Then
                    Flag2 = False
                    If CharArr.Count > i Then
                        If CharArr(i + 1) = "}" Then
                            Flag2 = True
                            CloseTemplateCount2 += -1
                        End If
                    End If
                    CloseTemplateCount2 += 1
                Else
                    Flag2 = True
                End If
            Else
                Flag2 = False
            End If

            If OpenTemplateCount2 > 0 Then
                If OpenTemplateCount2 = CloseTemplateCount2 Then
                    temptext = text.Substring(beginindex, (i - beginindex) + 1)
                    Dim BeginPos As Integer = temptext.IndexOf("{{")
                    Dim Textbefore As String = temptext.Substring(0, BeginPos)
                    Dim Lenght As Integer = temptext.Length - (Textbefore.Length)
                    Dim TemplateText As String = temptext.Substring(BeginPos, Lenght)
                    temptext = ""
                    beginindex = i + 1
                    OpenTemplateCount2 = 0
                    CloseTemplateCount2 = 0
                    templist.Add(TemplateText)
                End If
            End If
        Next
        Dim innertemplates As New List(Of String)
        For Each t As String In templist
            If t.Length >= 4 Then
                Dim innertext As String = t.Substring(2, t.Length - 4)
                innertemplates.AddRange(GetTemplateTextArray(innertext))
            End If
        Next
        templist.AddRange(innertemplates)
        Return templist
    End Function
#End Region


    Public Shared EventLogger As New LogEngine(Log_Filepath, User_Filepath, BotCodename)
    Public Shared BotSettings As New Settings(SettingsPath)

    ''' <summary>
    ''' Finaliza el programa correctamente.
    ''' </summary>
    Public Shared Sub ExitProgram()
        EventLogger.EndLog = True
        Environment.Exit(0)
    End Sub

    ''' <summary>
    ''' Retorna true si un numero es par.
    ''' </summary>
    ''' <param name="Number"></param>
    ''' <returns></returns>
    Public Shared Function IsODD(ByVal number As Integer) As Boolean
        Return (number Mod 2 = 0)
    End Function

    ''' <summary>
    ''' Cuenta cuantas veces se repite una cadena de texto dada dentro de otra cadena de texto.
    ''' </summary>
    ''' <param name="input">Cadena de texto donde se busca</param>
    ''' <param name="value">CAdena de texto a buscar</param>
    ''' <returns></returns>
    Public Shared Function CountString(ByVal input As String, ByVal value As String) As Integer
        Return Regex.Split(input, RegexParser(value)).Length - 1
    End Function

    ''' <summary>
    ''' Entrega un valor que simboliza el nivel de aparición de las palabras indicadas
    ''' </summary>
    ''' <param name="Phrase">Frase a evaluar</param>
    ''' <param name="words">Palabras a buscar</param>
    ''' <returns></returns>
    Public Shared Function LvlOfAppereance(ByVal phrase As String, words As String()) As Double
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
    Public Shared Function UnixToTime(ByVal strUnixTime As String) As Date
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
    Public Shared Function TimeStringToSeconds(ByVal time As String) As Integer
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
            EventLogger.Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "CommFuncs")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Establece un tiempo de espera (en segundos)
    ''' </summary>
    ''' <param name="seconds"></param>
    ''' <returns></returns>
    Public Shared Function WaitSeconds(ByVal seconds As Integer) As Boolean
        System.Threading.Thread.Sleep(seconds * 1000)
        Return True
    End Function

    ''' <summary>
    ''' Convierte una fecha a un numero entero que representa la hora en formato unix
    ''' </summary>
    ''' <param name="dteDate"></param>
    ''' <returns></returns>
    Public Shared Function TimeToUnix(ByVal dteDate As Date) As Integer
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
    Public Shared Function SplitStringArrayIntoChunks(strArray As String(), chunkSize As Integer) As List(Of List(Of String))
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
    Public Shared Function SplitIntegerArrayIntoChunks(intArray As Integer(), chunkSize As Integer) As List(Of List(Of Integer))
        Return intArray.
                Select(Function(x, i) New With {Key .Index = i, Key .Value = x}).
                GroupBy(Function(x) (x.Index \ chunkSize)).
                Select(Function(x) x.Select(Function(v) v.Value).ToList()).
                ToList()
    End Function


    ''' <summary>
    ''' Retorna una lista de plantillas si se le entrega como parámetro un array de tipo string con texto en formato válido de plantilla.
    ''' Si uno de los items del array no tiene formato válido, entregará una plantilla vacia en su lugar ("{{}}").
    ''' </summary>
    ''' <param name="templatearray"></param>
    ''' <returns></returns>
    Public Shared Function GetTemplates(ByVal templatearray As List(Of String)) As List(Of Template)
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
    ''' Retorna todas las plantillas que encuentre en una pagina, de no haber entregará una lista vacia.
    ''' </summary>
    ''' <param name="WikiPage"></param>
    ''' <returns></returns>
    Public Shared Function GetTemplates(ByVal wikiPage As Page) As List(Of Template)
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

    ''' <summary>
    ''' Retorna todas las plantillas que encuentre en un texto, de no haber entregará una lista vacia.
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function GetTemplates(ByVal text As String) As List(Of Template)
        If String.IsNullOrWhiteSpace(text) Then
            Return New List(Of Template)
        End If
        Dim TemplateList As New List(Of Template)
        Dim temps As List(Of String) = GetTemplateTextArray(text)

        For Each t As String In temps
            TemplateList.Add(New Template(t, False))
        Next
        Return TemplateList
    End Function

    Public Shared Function GetCurrentThreads() As Integer
        Try
            Return Process.GetCurrentProcess().Threads.Count
        Catch ex As Exception
            EventLogger.Debug_Log(ex.Message, "LOCAL")
            Return 0
        End Try

    End Function

    Public Shared Function PrivateMemory() As Long
        Try
            Return CLng(Process.GetCurrentProcess().PrivateMemorySize64 / 1024)
        Catch ex As Exception
            EventLogger.Debug_Log(ex.Message, "LOCAL")
            Return 0
        End Try

    End Function

    Public Shared Function UsedMemory() As Long
        Try
            Return CLng(Process.GetCurrentProcess().WorkingSet64 / 1024)
        Catch ex As Exception
            EventLogger.Debug_Log(ex.Message, "LOCAL")
            Return 0
        End Try

    End Function


    Public Shared Sub WriteLine(ByVal type As String, ByVal source As String, message As String)
        Dim msgstr As String = "[" & DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & "]" & " [" & source & " " & type & "] " & message
        Console.WriteLine(msgstr)
    End Sub

    Public Shared Function PressKeyTimeout() As Boolean
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

End Class

''' <summary>
''' Excepción que se produce cuando se alcanza un número máximo de reintentos.
''' </summary>
Public Class MaxRetriesExceededExeption : Inherits System.Exception
    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(ByVal message As String, ByVal innerException As System.Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.New(info, context)
    End Sub
End Class
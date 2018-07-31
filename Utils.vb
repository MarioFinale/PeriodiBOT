Option Strict On
Option Explicit On
Imports System.Runtime.Serialization
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot

NotInheritable Class Utils
#Region "Prevent init"
    Private Sub New()
    End Sub
#End Region

    Public Shared signpattern As String = "([0-9]{2}):([0-9]{2}) ([0-9]{2}|[0-9]) ([A-z]{3})([\.,])* [0-9]{4}( \([A-z]{3,4}\))*"

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
    ''' Obtiene el último hilo que coincida con el título entregado.
    ''' </summary>
    ''' <param name="threads"></param>
    ''' <returns></returns>
    Public Shared Function GetLastThreadByTitle(ByVal threads As String(), title As String) As String
        Dim matchingthread As String = String.Empty
        For Each t As String In threads
            If GetTitleFromThread(t) = title Then
                matchingthread = t
            End If
        Next
        Return matchingthread
    End Function

    ''' <summary>
    ''' Entrega el primer título del hilo en formato wikitexto que se le pase como parámetro. Si no tiene título entregará una cadena vacía.
    ''' </summary>
    ''' <param name="thread"></param>
    ''' <returns></returns>
    Public Shared Function GetTitleFromThread(ByVal thread As String) As String
        Dim TitlesList As New List(Of String)
        Dim temptext As String = thread
        Dim commentMatch As MatchCollection = Regex.Matches(temptext, "(<!--)[\s\S]*?(-->)")
        Dim CommentsList As New List(Of String)
        For i As Integer = 0 To commentMatch.Count - 1
            CommentsList.Add(commentMatch(i).Value)
            temptext = temptext.Replace(commentMatch(i).Value, ColoredText("PERIODIBOT::::COMMENTREPLACE::::" & i, 4))
        Next
        Dim mc As MatchCollection = Regex.Matches(temptext, "([\n\r]|^)((==(?!=)).+?(==(?!=)))")
        For Each m As Match In mc
            TitlesList.Add(m.Value)
        Next
        If TitlesList.Count > 0 Then
            Return TitlesList.First
        Else
            Return String.Empty
        End If
    End Function


    ''' <summary>
    ''' Entrega únicamente los títulos de los hilos en formato wikitexto que se les pase como parámetro. Si uno de los parámetros no tiene título entregará una cadena vacía en su lugar.
    ''' </summary>
    ''' <param name="threads"></param>
    ''' <returns></returns>
    Public Shared Function GetTitlesFromThreads(ByVal threads As String()) As String()
        Dim TitlesList As New List(Of String)
        For Each threadtext As String In threads
            TitlesList.Add(GetTitleFromThread(threadtext))
        Next
        Return TitlesList.ToArray
    End Function

    ''' <summary>
    ''' Entrega los elementos en el segundo array que no estén presentes en el primero
    ''' </summary>
    ''' <param name="Arr1">Array base</param>
    ''' <param name="arr2">Array a comparar</param>
    ''' <returns></returns>
    Public Shared Function GetSecondArrayAddedDiff(ByVal arr1 As String(), arr2 As String()) As String()
        Dim Difflist As New List(Of String)
        For i As Integer = 0 To arr2.Count - 1
            If Not arr1.Contains(arr2(i)) Then
                Difflist.Add(arr2(i))
            End If
        Next
        Return Difflist.ToArray
    End Function

    ''' <summary>
    ''' Entrega los titulos de los hilos en el segundo array que sean distintos al primero
    ''' </summary>
    ''' <param name="threadlist1">Array base</param>
    ''' <param name="threadlist2">Array a comparar</param>
    ''' <returns></returns>
    Public Shared Function GetChangedThreadsTitle(ByVal threadlist1 As String(), threadlist2 As String()) As String()
        Dim Difflist As New List(Of String)
        Dim thread1 As List(Of String) = threadlist1.ToList
        Dim thread2 As List(Of String) = threadlist2.ToList
        thread1.Sort()
        thread2.Sort()

        If thread1.Count = thread2.Count Then
            For i As Integer = 0 To thread1.Count - 1
                If Not thread1(i) = thread2(i) Then
                    Difflist.Add(GetTitleFromThread(thread1(i)))
                End If
            Next
        ElseIf thread2.Count > thread1.Count - 1 Then
            Difflist.AddRange(GetSecondArrayAddedDiff(GetTitlesFromThreads(thread1.ToArray), GetTitlesFromThreads(thread2.ToArray)))
        End If
        Return Difflist.ToArray
    End Function


    ''' <summary>
    ''' Entrega los titulos de los hilos en el segundo array que sean distintos al primero. Los array deben tener los mismos elementos o se retornara un array vacio.
    ''' </summary>
    ''' <param name="threadlist1">Array base</param>
    ''' <param name="threadlist2">Array a comparar</param>
    ''' <returns></returns>
    Public Shared Function GetChangedThreads(ByVal threadlist1 As String(), threadlist2 As String()) As String()
        Dim Difflist As New List(Of String)
        Dim thread1 As List(Of String) = threadlist1.ToList
        Dim thread2 As List(Of String) = threadlist2.ToList
        thread1.Sort()
        thread2.Sort()
        If thread1.Count = thread2.Count Then
            For i As Integer = 0 To thread1.Count - 1
                If Not thread1(i) = thread2(i) Then
                    Difflist.Add(thread2(i))
                End If
            Next
        End If
        Return Difflist.ToArray
    End Function

    ''' <summary>
    ''' Entrega el último hilo en los hilos entregados que coincida con el nombre
    ''' </summary>
    ''' <param name="threads"></param>
    ''' <param name="title"></param>
    ''' <returns></returns>
    Public Shared Function GetThreadByTitle(ByVal threads As String(), title As String) As String
        Dim thread As String = String.Empty
        For Each threadtext As String In threads
            Dim currentthreadtitle As String = GetTitleFromThread(threadtext)
            If title = currentthreadtitle Then
                thread = currentthreadtitle
            End If
        Next
        Return thread
    End Function


    ''' <summary>
    ''' Convierte un array de tipo string numérico a integer. Si uno de los elementos no es numérico retorna 0 en su lugar.
    ''' </summary>
    ''' <param name="arr">Array a evaluar</param>
    ''' <returns></returns>
    Public Shared Function StringArrayToInt(ByVal arr As String()) As Integer()
        Dim intlist As New List(Of Integer)
        For i As Integer = 0 To arr.Count - 1
            If IsNumeric(arr(i)) Then
                intlist.Add(Integer.Parse(arr(i)))
            Else
                intlist.Add(0)
            End If
        Next
        Return intlist.ToArray
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

    ''' <summary>
    ''' Cuenta las veces que se repite una cadena de texto en otra cadena de texto.
    ''' </summary>
    ''' <param name="StToSerach"></param>
    ''' <param name="StToLookFor"></param>
    ''' <returns></returns>
    Public Shared Function CountOccurrences(ByVal StToSerach As String, StToLookFor As String) As Integer
        Dim txtlen As Integer = StToSerach.Length
        Dim strlen As Integer = StToLookFor.Length
        Dim newstring As String = StToSerach.Replace(StToLookFor, String.Empty)
        Dim newtxtlen As Integer = newstring.Length
        Dim lenghtdiff As Integer = txtlen - newtxtlen
        Dim occurences As Integer = CInt(lenghtdiff / strlen)
        Return occurences
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


    ''' <summary>
    ''' Entrega como array de DateTime todas las fechas (Formato de firma Wikipedia) en el texto dado.
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function AllDateTimes(ByVal text As String) As DateTime()
        Dim Datelist As New List(Of DateTime)
        For Each m As Match In Regex.Matches(text, signpattern)
            Dim TheDate As DateTime = ESWikiDatetime(m.Value)
            Datelist.Add(TheDate)
            EventLogger.Log("AllDateTimes: Adding " & TheDate.ToString, "LOCAL")
        Next
        Return Datelist.ToArray
    End Function

    ''' <summary>
    ''' Evalua texto (wikicódigo) y regresa un array de string con cada uno de los hilos del mismo (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
    ''' </summary>
    ''' <param name="pagetext">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function GetPageThreads(ByVal pagetext As String) As String()
        Dim temptext As String = pagetext
        Dim commentMatch As MatchCollection = Regex.Matches(temptext, "(<!--)[\s\S]*?(-->)")
        Dim NowikiMatch As MatchCollection = Regex.Matches(temptext, "(<[nN]owiki>)([\s\S]+?)(<\/[Nn]owiki>)")
        Dim CodeMatch As MatchCollection = Regex.Matches(temptext, "(<[cC]ode>)([\s\S]+?)(<\/[Cc]ode>)")

        Dim CommentsList As New List(Of String)
        Dim NowikiList As New List(Of String)
        Dim CodeList As New List(Of String)

        For i As Integer = 0 To commentMatch.Count - 1
            CommentsList.Add(commentMatch(i).Value)
            temptext = temptext.Replace(commentMatch(i).Value, ColoredText("PERIODIBOT::::COMMENTREPLACE::::" & i, 4))
        Next
        For i As Integer = 0 To NowikiMatch.Count - 1
            NowikiList.Add(NowikiMatch(i).Value)
            temptext = temptext.Replace(NowikiMatch(i).Value, ColoredText("PERIODIBOT::::NOWIKIREPLACE::::" & i, 4))
        Next
        For i As Integer = 0 To CodeMatch.Count - 1
            CodeList.Add(CodeMatch(i).Value)
            temptext = temptext.Replace(CodeMatch(i).Value, ColoredText("PERIODIBOT::::CODEREPLACE::::" & i, 4))
        Next

        Dim mc As MatchCollection = Regex.Matches(temptext, "([\n\r]|^)((==(?!=)).+?(==(?!=)))(( +)*)([\n\r]|$)")

        Dim threadlist As New List(Of String)


        For i As Integer = 0 To mc.Count - 1

            Dim nextmatch As Integer = (i + 1)

            If Not nextmatch = mc.Count Then

                Dim threadtitle As String = mc(i).Value
                Dim nextthreadtitle As String = mc(nextmatch).Value
                Dim threadtext As String = String.Empty

                threadtext = TextInBetween(temptext, threadtitle, nextthreadtitle)(0)
                Dim Completethread As String = threadtitle & threadtext
                threadlist.Add(Completethread)
                temptext = ReplaceFirst(temptext, Completethread, "")

            Else
                Dim threadtitle As String = mc(i).Value

                Dim ThreadPos As Integer = temptext.IndexOf(threadtitle)
                Dim threadlenght As Integer = temptext.Length - temptext.Substring(0, ThreadPos).Length
                Dim threadtext As String = temptext.Substring(ThreadPos, threadlenght)
                threadlist.Add(threadtext)

            End If
        Next
        Dim EndThreadList As New List(Of String)
        For Each t As String In threadlist
            Dim nthreadtext As String = t
            For i As Integer = 0 To commentMatch.Count - 1
                Dim commenttext As String = ColoredText("PERIODIBOT::::COMMENTREPLACE::::" & i, 4)
                nthreadtext = nthreadtext.Replace(commenttext, CommentsList(i))
            Next
            For i As Integer = 0 To NowikiMatch.Count - 1
                Dim codetext As String = ColoredText("PERIODIBOT::::NOWIKIREPLACE::::" & i, 4)
                nthreadtext = nthreadtext.Replace(codetext, NowikiList(i))
            Next
            For i As Integer = 0 To CodeMatch.Count - 1
                Dim codetext As String = ColoredText("PERIODIBOT::::CODEREPLACE::::" & i, 4)
                nthreadtext = nthreadtext.Replace(codetext, CodeList(i))
            Next
            EndThreadList.Add(nthreadtext)
        Next

        Return EndThreadList.ToArray
    End Function
    ''' <summary>
    ''' Entrega como DateTime la última fecha (formato firma Wikipedia) en el último parrafo. Si no encuentra firma retorna 31/12/9999.
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Public Shared Function LastParagraphDateTime(ByVal text As String) As DateTime
        If String.IsNullOrEmpty(text) Then
            Throw New ArgumentException("Empty var", "text")
        End If
        text = text.Trim(CType(vbCrLf, Char())) & " "
        Dim lastparagraph As String = Regex.Match(text, ".+[\s\s]+(?===.+==|$)").Value
        Dim matchc As MatchCollection = Regex.Matches(lastparagraph, signpattern)

        If matchc.Count = 0 And Not (((lastparagraph(0) = ";"c) Or (lastparagraph(0) = ":"c) Or (lastparagraph(0) = "*"c) Or (lastparagraph(0) = "#"c))) Then
            Dim mlines As MatchCollection = Regex.Matches(text, ".+\n")
            For i As Integer = mlines.Count - 1 To 0 Step -1

                If i = (mlines.Count - 1) Then
                    If Not ((mlines(i).Value(0) = ";"c) Or (mlines(i).Value(0) = ":"c) Or (mlines(i).Value(0) = "*"c) Or (mlines(i).Value(0) = "#"c)) Then
                        If Regex.Match(mlines(i).Value, signpattern).Success Then
                            lastparagraph = mlines(i).Value
                            Exit For
                        End If
                    Else
                        Exit For
                    End If
                Else
                    If Not ((mlines(i).Value(0) = ";"c) Or (mlines(i).Value(0) = ":"c) Or (mlines(i).Value(0) = "*"c) Or (mlines(i).Value(0) = "#"c)) Then
                        If Regex.Match(mlines(i).Value, signpattern).Success Then
                            lastparagraph = mlines(i).Value
                            Exit For
                        End If
                    Else
                        Exit For
                    End If
                End If
            Next

        End If

        Dim TheDate As DateTime = ESWikiDatetime(lastparagraph)
        EventLogger.Debug_Log("LastParagraphDateTime: Returning " & TheDate.ToString, "LOCAL")
        Return TheDate
    End Function

    ''' <summary>
    ''' Entrega 
    ''' </summary>
    ''' <param name="text">Entrega la ultima fecha, que aparezca en un texto dado (si la fecha tiene formato de firma wikipedia).</param>
    ''' <returns></returns>
    Public Shared Function ESWikiDatetime(ByVal text As String) As DateTime
        Dim TheDate As DateTime = Nothing
        Dim matchc As MatchCollection = Regex.Matches(text, signpattern)

        If matchc.Count = 0 Then
            EventLogger.Debug_Log("No date match", "ESWikiDateTime")
            Return New Date(9999, 12, 31, 23, 59, 59)
        End If

        For Each m As Match In matchc
            Try
                Dim parsedtxt As String = m.Value.Replace(" "c, "/"c)
                parsedtxt = parsedtxt.Replace(":"c, "/"c)
                parsedtxt = parsedtxt.ToLower.Replace("ene", "01").Replace("feb", "02") _
            .Replace("mar", "03").Replace("abr", "04").Replace("may", "05") _
            .Replace("jun", "06").Replace("jul", "07").Replace("ago", "08") _
            .Replace("sep", "09").Replace("oct", "10").Replace("nov", "11") _
            .Replace("dic", "12")

                parsedtxt = Regex.Replace(parsedtxt, "([^0-9/])", "")
                Dim dates As New List(Of Integer)
                For Each s As String In parsedtxt.Split("/"c)
                    If Not String.IsNullOrWhiteSpace(s) Then
                        dates.Add(Integer.Parse(RemoveAllAlphas(s)))
                    End If
                Next

                Dim dat As New DateTime(dates(4), dates(3), dates(2), dates(0), dates(1), 0)
                TheDate = dat
                EventLogger.Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """" & " to """ & dat.ToShortDateString & """", "LOCAL")
            Catch ex As System.FormatException
                EventLogger.Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions")
            End Try

        Next
        Return TheDate

    End Function


    ''' <summary>
    ''' Entrega como DateTime la fecha más reciente en el texto dado (en formato de firma wikipedia).
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Public Shared Function MostRecentDate(ByVal text As String) As DateTime
        Dim dates As New List(Of DateTime)
        Dim matchc As MatchCollection = Regex.Matches(text, signpattern)

        If matchc.Count = 0 Then
            EventLogger.Debug_Log("No date match", "ESWikiDateTime")
            Return New DateTime(9999, 12, 31, 23, 59, 59)
        End If

        For Each m As Match In matchc
            Try
                Dim parsedtxt As String = m.Value.Replace(" "c, "/"c)
                parsedtxt = parsedtxt.Replace(":"c, "/"c)
                parsedtxt = parsedtxt.ToLower.Replace("ene", "01").Replace("feb", "02") _
            .Replace("mar", "03").Replace("abr", "04").Replace("may", "05") _
            .Replace("jun", "06").Replace("jul", "07").Replace("ago", "08") _
            .Replace("sep", "09").Replace("oct", "10").Replace("nov", "11") _
            .Replace("dic", "12")

                parsedtxt = Regex.Replace(parsedtxt, "([^0-9/])", "")
                Dim datesInt As New List(Of Integer)
                For Each s As String In parsedtxt.Split("/"c)
                    If Not String.IsNullOrWhiteSpace(s) Then
                        datesInt.Add(Integer.Parse(s))
                    End If
                Next
                Dim dat As New DateTime(datesInt(4), datesInt(3), datesInt(2), datesInt(0), datesInt(1), 0)
                dates.Add(dat)
                EventLogger.Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """" & " to """ & dat.ToShortDateString & """", "LOCAL")
            Catch ex As System.FormatException
                EventLogger.Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions")
            End Try

        Next
        dates.Sort()
        Return dates.Last

    End Function

    ''' <summary>
    ''' Entrega como DateTime la primera fecha que aparece en el hilo.
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Public Shared Function FirstDate(ByVal text As String) As DateTime
        Dim matchc As MatchCollection = Regex.Matches(text, signpattern)
        Dim tdat As New DateTime(9999, 12, 31, 23, 59, 59)
        If matchc.Count = 0 Then
            EventLogger.Debug_Log("No date match", "ESWikiDateTime")
            Return tdat
        End If

        For Each m As Match In matchc
            Try
                Dim parsedtxt As String = m.Value.Replace(" "c, "/"c)
                parsedtxt = parsedtxt.Replace(":"c, "/"c)
                parsedtxt = parsedtxt.ToLower.Replace("ene", "01").Replace("feb", "02") _
                .Replace("mar", "03").Replace("abr", "04").Replace("may", "05") _
                .Replace("jun", "06").Replace("jul", "07").Replace("ago", "08") _
                .Replace("sep", "09").Replace("oct", "10").Replace("nov", "11") _
                .Replace("dic", "12")

                parsedtxt = Regex.Replace(parsedtxt, "([^0-9/])", "")
                Dim datesInt As New List(Of Integer)
                For Each s As String In parsedtxt.Split("/"c)
                    If Not String.IsNullOrWhiteSpace(s) Then
                        datesInt.Add(Integer.Parse(s))
                    End If
                Next
                tdat = New DateTime(datesInt(4), datesInt(3), datesInt(2), datesInt(0), datesInt(1), 0)
                EventLogger.Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """" & " to """ & tdat.ToShortDateString & """", "LOCAL")
            Catch ex As System.FormatException
                EventLogger.Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions")
            Catch ex As Exception
                EventLogger.Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions")
            End Try

            Return tdat
        Next
        Return tdat
    End Function

    ''' <summary>
    ''' Evalua una wikipágina y regresa un array de string con cada uno de los hilos del esta (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
    ''' </summary>
    ''' <param name="tpage">Página a evaluar</param>
    ''' <returns></returns>
    Public Shared Function GetPageThreads(ByVal tpage As Page) As String()
        If tpage Is Nothing Then Return Nothing
        Return GetPageThreads(tpage.Text)
    End Function

    Public Shared Function GetSizeText(ByVal bytes As Integer) As String
        If bytes < 999 Then
            Return bytes.ToString & " Bytes"
        ElseIf bytes < 999999 Then
            Return System.Math.Round((bytes / 1000), 2).ToString & " KB"
        ElseIf bytes > 999999 Then
            Return System.Math.Round((bytes / 1000000), 2).ToString & " MB"
        End If
        Return " Wtf?"
    End Function

    Public Shared Function GetSpanishTimeString(ByVal tDate As Date) As String
        Return tDate.ToString("dd 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES"))
    End Function

End Class




''' <summary>
''' Excepción que se produce cuando se alcanza un número máximo de reintentos.
''' </summary>
Public Class MaxRetriesExceededExeption : Inherits System.Exception
    Implements ISerializable
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
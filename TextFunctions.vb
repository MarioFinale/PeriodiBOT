Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions

Module TextFunctions
    ''' <summary>
    ''' Elimina los tags html de una cadena de texto dada.
    ''' </summary>
    ''' <param name="html">Texto a evaluar.</param>
    ''' <returns></returns>
    Function StripTags(ByVal html As String) As String
        Return Regex.Replace(html, "<.*?>", String.Empty)
    End Function
    ''' <summary>
    ''' Elimina las líneas en blanco.
    ''' </summary>
    ''' <param name="Str">Texto a evaluar.</param>
    ''' <returns></returns>
    Function Removewhitelines(ByVal Str As String) As String
        Return Regex.Replace(Str, "^\s+$[\r\n]*", "", RegexOptions.Multiline)
    End Function
    ''' <summary>
    ''' Elimina los excesos de espacios (consecutivos) en una cadena de texto.
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Function RemoveExcessOfSpaces(ByVal text As String) As String
        Return Regex.Replace(text, "\s{2,}", " ")
    End Function
    ''' <summary>
    ''' Cuenta las veces que un carácter aparece en una cadena de texto dada.
    ''' </summary>
    ''' <param name="value">Texto a evaluar.</param>
    ''' <param name="ch">Carácter a buscar.</param>
    ''' <returns></returns>
    Public Function CountCharacter(ByVal value As String, ByVal ch As Char) As Integer
        Return value.Count(Function(c As Char) c = ch)
    End Function
    ''' <summary>
    ''' Regresa la misma cadena de texto, pero con la primera letra mayúscula.
    ''' </summary>
    ''' <param name="val">Texto a evaluar</param>
    ''' <returns></returns>
    Function UppercaseFirstCharacter(ByVal val As String) As String
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
    Function NormalizeText(ByVal text As String) As String
        Dim s As String = text.ToLower
        Return UppercaseFirstCharacter(s)
    End Function
    ''' <summary>
    ''' verifica si una cadena de texto es numérica.
    ''' </summary>
    ''' <param name="number">Texto a evaluar.</param>
    ''' <returns></returns>
    Function IsNumeric(ByVal number As String) As Boolean
        If Regex.IsMatch(number, "^[0-9 ]+$") Then
            Return True
        Else
            Return False
        End If
    End Function
    ''' <summary>
    ''' Realiza los escapes para usar una cadena de texto dentro de una expresión regular.
    ''' </summary>
    ''' <param name="s">Texto a evaluar.</param>
    ''' <returns></returns>
    Function RegexParser(ByVal s As String) As String
        s = s.Replace("\", "\\")
        Return s.Replace("[", "\[").Replace("/", "\/").Replace("^", "\^").Replace("$", "\$").Replace(".", "\.") _
            .Replace("|", "\|").Replace("?", "\?").Replace("*", "\*").Replace("+", "\+").Replace("(", "\(").Replace(")", "\)") _
            .Replace("{", "\{").Replace("}", "\}")
    End Function
    ''' <summary>
    ''' Entrega el título de la página en la pseudoplantilla de resúmenes de página.
    ''' </summary>
    ''' <param name="SourceString">Texto a evaluar.</param>
    ''' <returns></returns>
    Function GetTitlesOfTemplate(ByVal SourceString As String) As String()
        Dim mlist As New List(Of String)
        mlist = TextInBetween(SourceString, "|" & Environment.NewLine & "|", "=").ToList
        Return mlist.ToArray
    End Function
    ''' <summary>
    ''' Soluciona un error de la api en los resúmenes, donde cuertas plantillas los números los entrega repetidos con varios símbolos en medio.
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Function FixResumeNumericExp(ByVal text As String) As String
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
    Function GetNormalizedTitlesOfTemplate(ByVal SourceString As String) As String()
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
    Function TextInBetween(ByVal SourceString As String, string1 As String, string2 As String) As String()
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
    Function TextInBetweenInclusive(ByVal SourceString As String, string1 As String, string2 As String) As String()
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
    Function MatchQuotedIntegers(ByVal SourceString As String) As String()
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
    Function GetTitlesFromQueryText(ByVal sourcestring As String) As String()
        Return TextInBetween(sourcestring, ",""title"":""", """,")
    End Function
    ''' <summary>
    ''' Normaliza el texto ASCII con códigos unicodes escapados con el formato \\u(número)
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Function NormalizeUnicodetext(ByVal text As String) As String
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
    Function UrlWebEncode(ByVal text As String) As String
        Return Net.WebUtility.UrlEncode(text)
    End Function
    ''' <summary>
    ''' Decodifica una cadena de texto en URLENCODE.
    ''' </summary>
    ''' <param name="text">Texto a decodificar.</param>
    ''' <returns></returns>
    Function UrlWebDecode(ByVal text As String) As String
        Return Net.WebUtility.UrlDecode(text)
    End Function

    ''' <summary>
    ''' Evalua una línea de texto (formato IRC según la RFC) y entrega el usuario que emitió el mensaje.
    ''' </summary>
    ''' <param name="response">Mensaje a evaluar.</param>
    ''' <returns></returns>
    Function GetUserFromChatresponse(ByVal response As String) As String
        Return response.Split("!"c)(0).Replace(":", "")
    End Function
    ''' <summary>
    ''' Elimina todas las letras dejando únicamente números
    ''' </summary>
    ''' <param name="text">Texto a evaluar</param>
    ''' <returns></returns>
    Function RemoveAllAlphas(ByVal text As String) As String
        Return Regex.Replace(text, "[^0-9]", "")
    End Function
    ''' <summary>
    ''' Codifica texto para ser guardado en el LOG.
    ''' </summary>
    ''' <param name="text">Texto a codificar</param>
    ''' <returns></returns>
    Function PsvSafeEncode(ByVal text As String) As String
        Return text.Replace("|"c, "%CHAR:U+007C%")
    End Function
    ''' <summary>
    ''' Decodifica texto guardado en el LOG.
    ''' </summary>
    ''' <param name="text">Texto a decodificar.</param>
    ''' <returns></returns>
    Function PsvSafeDecode(ByVal text As String) As String
        Return text.Replace("%CHAR:U+007C%", "|")
    End Function

    ''' <summary>
    ''' Retorna una cadena de texto formateada de tal forma que muestra colores, para mas info ver https://github.com/myano/jenni/wiki/IRC-String-Formatting
    ''' </summary>
    ''' <param name="ForegroundColor">Color del texto</param>
    ''' <param name="BackgroundColor">Color del fondo, i se omite se usa el color por defecto del cliente irc</param>
    Function ColoredText(ByVal text As String, ForegroundColor As String, Optional BackgroundColor As String = "99") As String
        If BackgroundColor = "99" Then
            Return Chr(3) & ForegroundColor & text & Chr(3) & Chr(27)
        Else
            Return Chr(3) & ForegroundColor & "," & BackgroundColor & text & Chr(3) & Chr(27)
        End If
    End Function

    ''' <summary>
    ''' Retorna un array de string con todas las plantillas contenidas en un texto.
    ''' Pueden repetirse si hay plantillas que contienen otras en su interior.
    ''' </summary>
    ''' <param name="text"></param>
    ''' <returns></returns>
    Function GetTemplateTextArray(ByVal text As String) As List(Of String)

        Dim temptext As String = String.Empty
        Dim templist As New List(Of String)
        For i As Integer = 0 To text.Length - 1

            temptext = temptext & text(i)

            Dim OpenTemplate As MatchCollection = Regex.Matches(temptext, "{{")
            Dim CloseTemplate As MatchCollection = Regex.Matches(temptext, "}}")

            If OpenTemplate.Count > 0 Then

                If OpenTemplate.Count = CloseTemplate.Count Then

                    Dim BeginPos As Integer = OpenTemplate(0).Index
                    Dim Textbefore As String = temptext.Substring(0, BeginPos)
                    Dim Lenght As Integer = temptext.Length - (Textbefore.Length)
                    Dim TemplateText As String = temptext.Substring(BeginPos, Lenght)

                    temptext = ""
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






End Module

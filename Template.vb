Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions

Public Class Template


    Private _name As String
    Private _parameters As List(Of Tuple(Of String, String))
    Private _text As String
    ''' <summary>
    ''' Nombre de la plantilla (con el espacio de nombres).
    ''' </summary>
    ''' <returns></returns>
    Public Property Name As String
        Get
            Return _name
        End Get
        Set(value As String)
            _name = value
        End Set
    End Property

    ''' <summary>
    ''' Lista con pares de (Nombre de parámetro, Valor).
    ''' </summary>
    ''' <returns></returns>
    Public Property Parameters As List(Of Tuple(Of String, String))
        Get
            Return _parameters
        End Get
        Set(value As List(Of Tuple(Of String, String)))
            _parameters = value
        End Set
    End Property

    ''' <summary>
    ''' Texto de la plantilla.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Text As String
        Get
            Return _text
        End Get
    End Property

    ''' <summary>
    ''' Crea una nueva plantilla. Si es una nueva se considera el texto como el título, de lo contrario se considera como el contenido de la plantilla y se extrae de este los parámetros.
    ''' El texto de ser inválido, genera una plantilla vacía ("{{}}").
    ''' </summary>
    ''' <param name="Texto">Texto a evaluar.</param>
    ''' <param name="newtemplate">¿Es una plantilla nueva?</param>
    Sub New(ByVal Texto As String, ByVal newtemplate As Boolean)
        If newtemplate Then
            _name = Texto
            _text = MakeSimpleTemplateText(Texto)
            _parameters = New List(Of Tuple(Of String, String))
        Else
            GetTemplateOfText(Texto)
        End If
    End Sub
    ''' <summary>
    ''' Crea una nueva plantilla con los parámetros indicados.
    ''' </summary>
    ''' <param name="Templatename">Nombre de la plantilla.</param>
    ''' <param name="templateparams">Parámetros de la plantilla.</param>
    Sub New(ByVal Templatename As String, ByVal templateparams As List(Of Tuple(Of String, String)))
        _name = Templatename
        _parameters = templateparams
        _text = MakeTemplateText(Templatename, templateparams)
    End Sub
    ''' <summary>
    ''' Crea una nueva plantilla vacía ("{{}}")
    ''' </summary>
    Sub New()
        _name = String.Empty
        _text = String.Empty
        _parameters = New List(Of Tuple(Of String, String))
    End Sub

    ''' <summary>
    ''' Crea el texto de una plantilla simple, que solo contiene el nombre de la misma.
    ''' </summary>
    ''' <param name="tempname">Nombre de la plantilla</param>
    ''' <returns></returns>
    Private Function MakeSimpleTemplateText(ByVal tempname As String) As String
        Return "{{" & tempname & "}}"
    End Function

    ''' <summary>
    ''' Genera el texto de una plantilla a partir del nombre y parámetros indicados.
    ''' </summary>
    ''' <param name="tempname">Nombre de la plantilla.</param>
    ''' <param name="tempparams">Parámetros de la plantilla.</param>
    ''' <returns></returns>
    Private Function MakeTemplateText(ByVal tempname As String, ByVal tempparams As List(Of Tuple(Of String, String))) As String
        Dim templatetext As String = String.Empty
        Dim opening As String = "{{"
        Dim closing As String = "}}"

        Dim paramstext As New List(Of String)
        tempparams = tempparams.OrderBy(Function(X) X.Item1).ToList

        For Each parampair As Tuple(Of String, String) In tempparams
            If IsNumeric(parampair.Item1) Then
                paramstext.Add(parampair.Item2.Trim(CType(" ", Char())))
            Else
                paramstext.Add(parampair.Item1.Trim(CType(" ", Char())) & " = " & parampair.Item2.Trim(CType(" ", Char())))
            End If
        Next
        templatetext = opening & tempname

        For Each s As String In paramstext
            templatetext = templatetext & "|" & s
        Next
        templatetext = templatetext & closing
        Return templatetext

    End Function

    ''' <summary>
    ''' Inicializa la plantilla extrayendo los datos de un texto que debería ser en formato de plantilla.
    ''' </summary>
    ''' <param name="text"></param>
    Sub GetTemplateOfText(ByVal text As String)

        If Not text.Substring(0, 2) = "{{" Then
            Exit Sub
        End If
        If Not CountCharacter(text, CChar("{")) = CountCharacter(text, CChar("}")) Then
            Exit Sub
        End If

        Dim Ttext As String = text.Substring(2, text.Length - 4)
        Dim tempname As String = String.Empty

        For cha As Integer = 0 To Ttext.Count - 1
            If Not (Ttext(cha) = "}" Or Ttext(cha) = "{" Or Ttext(cha) = "|") Then
                tempname = tempname & Ttext(cha)
            Else
                Exit For
            End If
        Next

        _name = tempname.Trim(CType(" ", Char())).Trim(CType(Environment.NewLine, Char()))
        _text = text
        _parameters = New List(Of Tuple(Of String, String))

        Dim containstemplates As Boolean = True
        Dim newtext As String = _text
        Dim replacedtemplates As New List(Of String)
        Dim TemplateInnerText = newtext.Substring(2, newtext.Length - 4)

        Dim temparray As List(Of String) = GetTemplateTextArray(TemplateInnerText)

        For templ As Integer = 0 To temparray.Count - 1
            Dim tempreplace As String = ColoredText("PERIODIBOT:TEMPLATEREPLACE::::" & templ.ToString, "01")
            newtext = newtext.Replace(temparray(templ), tempreplace)
            replacedtemplates.Add(temparray(templ))
        Next


        Dim params As MatchCollection = Regex.Matches(newtext, "\|[^|]+")
        Dim NamedParams As New List(Of Tuple(Of String, String))
        Dim UnnamedParams As New List(Of String)
        Dim TotalParams As New List(Of Tuple(Of String, String))
        For Each m As Match In params
            Dim ntext As String = newtext.Substring(1, m.Value.Length - 1)
            Dim ParamNamematch As Match = Regex.Match(m.Value, "\|[^\|=]+=")

            If ParamNamematch.Success Then

                Dim ParamName As String = ParamNamematch.Value.Substring(1, ParamNamematch.Length - 2)
                Dim Paramvalue As String = m.Value.Replace(ParamNamematch.Value, "")
                NamedParams.Add(New Tuple(Of String, String)(ParamName, Paramvalue))

            Else
                Dim UnnamedParamValue As String = m.Value.Substring(1, m.Value.Length - 1)
                UnnamedParams.Add(UnnamedParamValue)

            End If

        Next

        For param As Integer = 0 To UnnamedParams.Count - 1
            NamedParams.Add(New Tuple(Of String, String)((param + 1).ToString, UnnamedParams(param)))
        Next

        For Each tup As Tuple(Of String, String) In NamedParams
            Dim ParamName As String = tup.Item1
            Dim ParamValue As String = tup.Item2

            For reptempindex As Integer = 0 To replacedtemplates.Count - 1
                Dim tempreplace As String = ColoredText("PERIODIBOT:TEMPLATEREPLACE::::" & reptempindex.ToString, "01")

                ParamName = ParamName.Replace(tempreplace, replacedtemplates(reptempindex))
                ParamValue = ParamValue.Replace(tempreplace, replacedtemplates(reptempindex))
            Next
            TotalParams.Add(New Tuple(Of String, String)(ParamName, ParamValue))


        Next
        _parameters.AddRange(TotalParams)

    End Sub





End Class

﻿Option Strict On
Option Explicit On
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.CommFunctions
Namespace WikiBot

    Public Class Template
        Private _name As String
        Public Parameters As List(Of Tuple(Of String, String))
        Private _text As String
        Private _newtemplate As Boolean = True
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
        ''' Texto de la plantilla.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Text As String
            Get
                If _newtemplate Then
                    _text = CreateTemplatetext(_name, Parameters, True)
                    Return _text
                Else
                    Return _text
                End If
            End Get
        End Property


        ''' <summary>
        ''' Crea una nueva plantilla. Si es una nueva se considera el texto como el título, de lo contrario se considera como el contenido de la plantilla y se extrae de este los parámetros.
        ''' El texto de ser inválido, genera una plantilla vacía ("{{}}").
        ''' </summary>
        ''' <param name="Text">Texto a evaluar.</param>
        ''' <param name="newTemplate">¿Es una plantilla nueva?</param>
        Sub New(ByVal text As String, ByVal newTemplate As Boolean)
            _newtemplate = newTemplate
            If newTemplate Then
                _name = text
                _text = MakeSimpleTemplateText(text)
                Parameters = New List(Of Tuple(Of String, String))
            Else
                GetTemplateOfText(text)
            End If
        End Sub
        ''' <summary>
        ''' Crea una nueva plantilla con los parámetros indicados.
        ''' </summary>
        ''' <param name="Templatename">Nombre de la plantilla.</param>
        ''' <param name="templateparams">Parámetros de la plantilla.</param>
        Sub New(ByVal templateName As String, ByVal templateParams As List(Of Tuple(Of String, String)))
            _name = templateName
            Parameters = templateParams
            _text = MakeTemplateText(templateName, templateParams)
        End Sub
        ''' <summary>
        ''' Crea una nueva plantilla vacía ("{{}}")
        ''' </summary>
        Sub New()
            _name = String.Empty
            _text = String.Empty
            Parameters = New List(Of Tuple(Of String, String))
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
        ''' Genera la plantilla a partir de los parámetros y el nombre de la misma.
        ''' </summary>
        ''' <param name="templatename"></param>
        ''' <param name="templateparams"></param>
        ''' <param name="newlines"></param>
        ''' <returns></returns>
        Private Function CreateTemplatetext(ByVal templatename As String, ByVal templateparams As List(Of Tuple(Of String, String)), ByVal newlines As Boolean) As String
            Dim linechar As String = ""
            If newlines Then linechar = Environment.NewLine
            Dim text As String = "{{" & templatename & linechar
            For Each t As Tuple(Of String, String) In templateparams
                text = text & "|" & t.Item1 & "=" & t.Item2 & linechar
            Next
            text = text & "}}"
            Return text
        End Function


        ''' <summary>
        ''' Genera el texto de la plantilla a partir del nombre y parámetros indicados.
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
        ''' <param name="templatetext"></param>
        Sub GetTemplateOfText(ByVal templatetext As String)
            If String.IsNullOrWhiteSpace(templatetext) Then
                Throw New ArgumentException("Empty parameter", "templatetext")
            End If
            'Verificar si se paso una plantilla
            If Not templatetext.Substring(0, 2) = "{{" Then
                Exit Sub
            End If
            If Not CountCharacter(templatetext, CChar("{")) = CountCharacter(templatetext, CChar("}")) Then
                Exit Sub
            End If
            If Not templatetext.Substring(templatetext.Length - 2, 2) = "}}" Then
                Exit Sub
            End If

            _text = templatetext
            Parameters = New List(Of Tuple(Of String, String))


            Dim NewText As String = _text
            Dim ReplacedTemplates As New List(Of String)
            Dim TemplateInnerText = NewText.Substring(2, NewText.Length - 4)

            'Reemplazar plantillas internas con texto para reconocer parametros de principal
            Dim temparray As List(Of String) = GetTemplateTextArray(TemplateInnerText)

            For templ As Integer = 0 To temparray.Count - 1
                Dim tempreplace As String = ColoredText("PERIODIBOT:TEMPLATEREPLACE::::" & templ.ToString, "01")
                NewText = NewText.Replace(temparray(templ), tempreplace)
                ReplacedTemplates.Add(temparray(templ))
            Next

            'Reemplazar enlaces dentro de la plantilla para reconocer parametros de principal
            Dim ReplacedLinks As New List(Of String)
            Dim LinkArray As New List(Of String)
            For Each m As Match In Regex.Matches(NewText, "((\[\[)([^\]]+)(\]\]))")
                LinkArray.Add(m.Value)
            Next

            For temp2 As Integer = 0 To LinkArray.Count - 1
                Dim LinkReplace As String = ColoredText("PERIODIBOT:LINKREPLACE::::" & temp2.ToString, "01")
                NewText = NewText.Replace(LinkArray(temp2), LinkReplace)
                ReplacedLinks.Add(LinkArray(temp2))
            Next

            'Obtener nombre de la plantilla
            Dim tempname As String = String.Empty
            Dim innertext As String = NewText.Substring(2, NewText.Length - 4)
            For cha As Integer = 0 To innertext.Count - 1
                If Not innertext(cha) = "|" Then
                    tempname = tempname & innertext(cha)
                Else
                    Exit For
                End If
            Next

            'Reemplazar plantillas internas en el titulo con texto para reconocer nombre de la principal
            For reptempindex As Integer = 0 To ReplacedTemplates.Count - 1
                Dim tempreplace As String = ColoredText("PERIODIBOT:TEMPLATEREPLACE::::" & reptempindex.ToString, "01")
                tempname = tempname.Replace(tempreplace, ReplacedTemplates(reptempindex)).Trim(CType(" ", Char())).Trim(CType(Environment.NewLine, Char()))
            Next
            _name = tempname

            'Obtener parametros de texto tratado y agregarlos a lista
            Dim params As MatchCollection = Regex.Matches(innertext, "\|[^|]+")
            Dim NamedParams As New List(Of Tuple(Of String, String))
            Dim UnnamedParams As New List(Of String)
            Dim TotalParams As New List(Of Tuple(Of String, String))

            For Each m As Match In params
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
            'Los parametros sin nombre son procesados y nombrados segun su posicion
            For param As Integer = 0 To UnnamedParams.Count - 1
                NamedParams.Add(New Tuple(Of String, String)((param + 1).ToString, UnnamedParams(param)))
            Next

            'Restaurar plantillas internas y enlaces en parametros, luego agregarlas a lista de parametros
            For Each tup As Tuple(Of String, String) In NamedParams
                Dim ParamName As String = tup.Item1
                Dim ParamValue As String = tup.Item2

                For reptempindex As Integer = 0 To ReplacedTemplates.Count - 1
                    Dim tempreplace As String = ColoredText("PERIODIBOT:TEMPLATEREPLACE::::" & reptempindex.ToString, "01")

                    ParamName = ParamName.Replace(tempreplace, ReplacedTemplates(reptempindex))
                    ParamValue = ParamValue.Replace(tempreplace, ReplacedTemplates(reptempindex))
                Next

                For RepLinkIndex As Integer = 0 To ReplacedLinks.Count - 1
                    Dim LinkReplace As String = ColoredText("PERIODIBOT:LINKREPLACE::::" & RepLinkIndex.ToString, "01")

                    ParamName = ParamName.Replace(LinkReplace, ReplacedLinks(RepLinkIndex))
                    ParamValue = ParamValue.Replace(LinkReplace, ReplacedLinks(RepLinkIndex))
                Next
                TotalParams.Add(New Tuple(Of String, String)(ParamName, ParamValue))

            Next
            'Agregar parametros locales a parametros de clase
            Parameters.AddRange(TotalParams)

        End Sub


        ''' <summary>
        ''' Elimina los parámetros en blanco de la plantilla.
        ''' </summary>
        ''' <returns></returns>
        Function OptimizeTemplate() As Boolean
            Dim newParameters As New List(Of Tuple(Of String, String))

            For Each parameter As Tuple(Of String, String) In Parameters
                If Not String.IsNullOrWhiteSpace(parameter.Item2.ToString) Then
                    newParameters.Add(parameter)
                End If
            Next
            Parameters = newParameters
            _text = MakeTemplateText(_name, newParameters)
            Return True
        End Function
    End Class
End Namespace
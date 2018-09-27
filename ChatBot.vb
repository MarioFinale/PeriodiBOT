Option Strict On
Option Explicit On

Namespace IRC
    Public Class ChatBot
        Private _botName As String

        Sub New(ByVal botName As String)
            _botName = botName
        End Sub


        Public Function CheckMessage(ByVal Source As String, ByVal User As String, ByVal text As String, ByVal WorkingBot As WikiBot.Bot) As ChatMessage
            Dim NormalizedIntroString As String = text.ToLower.Trim
            Dim IsQuestion As Boolean = False
            Dim Tvar As String = String.Empty
            If NormalizedIntroString.StartsWith("¿") Then
                NormalizedIntroString = NormalizedIntroString.Substring(1, NormalizedIntroString.Length - 2)
            End If
        End Function


        Function GetPhrase(ByVal StartStrings As String, ByVal tline As String) As String
            For Each svar As String In StartStrings
                If tline.ToLower.StartsWith(svar.ToLower) Then
                    Dim resline As String = tline.ToLower
                    If resline.Contains(Environment.NewLine) Or resline.Contains(vbCrLf) Then
                        resline = resline.Replace(vbCrLf, Environment.NewLine)
                        resline = resline.Split(CType(Environment.NewLine, Char()))(0)
                    End If
                    resline = resline.Replace(svar.ToLower, "").Replace("?"c, "").Trim
                    If Not String.IsNullOrWhiteSpace(resline) Then
                        Return resline
                    End If
                End If
            Next
            Return Nothing
        End Function



        Function GetPhrase(ByVal tline As String) As String

        End Function


        Function IsMentioned(ByVal tline As String) As Boolean
            If tline.Contains("*") Then Return False
            Dim divisor As String = _botName.ToLower
            Return tline.Contains(divisor)
        End Function




    End Class


End Namespace



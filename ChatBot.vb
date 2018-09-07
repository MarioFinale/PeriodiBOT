Option Strict On
Option Explicit On

Public Class ChatBot






    Public Function CheckMessage(ByVal Source As String, ByVal User As String, ByVal text As String, ByVal WorkingBot As WikiBot.Bot) As ChatMessage

        Dim NormalizedIntroString As String = text.ToLower.Trim
        Dim IsQuestion As Boolean = False
        If NormalizedIntroString.StartsWith("¿") Then
            NormalizedIntroString = NormalizedIntroString.Substring(1, NormalizedIntroString.Length - 2)
        End If
        Select Case True
            Case NormalizedIntroString = ("hola")
            Case NormalizedIntroString = ("ola")
            Case NormalizedIntroString = ("olas")
            Case NormalizedIntroString = ("holas")
            Case NormalizedIntroString = ("buena")
            Case NormalizedIntroString = ("buenas")
            Case NormalizedIntroString = ("que tal")
            Case NormalizedIntroString = ("hey")
            Case NormalizedIntroString = ("oigan")


            Case NormalizedIntroString.StartsWith("que es una ")
            Case NormalizedIntroString.StartsWith("que es la ")

            Case NormalizedIntroString.StartsWith("que es un ")

            Case NormalizedIntroString.StartsWith("que es ")

            Case NormalizedIntroString.StartsWith("que son ")

            Case NormalizedIntroString.StartsWith("cual son las ")

            Case NormalizedIntroString.StartsWith("cual son la ")
            Case NormalizedIntroString.StartsWith("cual es las ")
            Case NormalizedIntroString.StartsWith("cual es la ")
            Case NormalizedIntroString.StartsWith("cual es el ")
            Case NormalizedIntroString.StartsWith("cuales son las ")
            Case NormalizedIntroString.StartsWith("quien fue ")
            Case NormalizedIntroString.StartsWith("quien es ")
            Case NormalizedIntroString.StartsWith("quienes fueron los ")
            Case NormalizedIntroString.StartsWith("quienes fueron las ")



        End Select












    End Function







End Class




Public Class ChatMessage
    Property Source As String
    Property User As String
    Property Text As String
    Sub New(ByVal tSource As String, tUser As String, tText As String)
        Source = tSource
        User = tUser
        tText = tText
    End Sub
End Class
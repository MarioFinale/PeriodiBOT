﻿Option Strict On
Option Explicit On
Imports System.Globalization
Imports PeriodiBOT_IRC.CommFunctions

Namespace WikiBot

    Public Class WikiUser
        Private _bot As Bot

        Private _userName As String
        Private _userId As Integer
        Private _editCount As Integer
        Private _registration As Date
        Private _groups As New List(Of String)
        Private _gender As String

        Private _blocked As Boolean
        Private _blockID As Integer
        Private _blockedTimestamp As String
        Private _blockedBy As String
        Private _blockedbyId As Integer
        Private _blockReason As String
        Private _blockExpiry As String
        Private _lastEdit As Date

        Private _exists As Boolean

#Region "Properties"
        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public ReadOnly Property EditCount As Integer
            Get
                Return _editCount
            End Get
        End Property

        Public ReadOnly Property Registration As Date
            Get
                Return _registration
            End Get
        End Property

        Public ReadOnly Property Groups As List(Of String)
            Get
                Return _groups
            End Get
        End Property

        Public ReadOnly Property Blocked As Boolean
            Get
                Return _blocked
            End Get
        End Property

        Public ReadOnly Property BlockedBy As String
            Get
                Return _blockedBy
            End Get
        End Property

        Public ReadOnly Property BlockReason As String
            Get
                Return _blockReason
            End Get
        End Property

        Public ReadOnly Property BlockedTimestamp As String
            Get
                Return _blockedTimestamp
            End Get
        End Property

        Public ReadOnly Property BlockExpiry As String
            Get
                Return _blockExpiry
            End Get
        End Property

        Public ReadOnly Property BlockId As Integer
            Get
                Return _blockID
            End Get
        End Property

        Public ReadOnly Property Exists As Boolean
            Get
                Return _exists
            End Get
        End Property

        Public ReadOnly Property BlockedById As Integer
            Get
                Return _blockedbyId
            End Get
        End Property

        Public ReadOnly Property LastEdit As Date
            Get
                Return _lastEdit
            End Get
        End Property

        Public ReadOnly Property UserId As Integer
            Get
                Return _userId
            End Get
        End Property

        Public ReadOnly Property Gender As String
            Get
                Return _gender
            End Get
        End Property
#End Region


        Sub New(ByVal wikiBot As Bot, userName As String)
            _userName = userName
            _bot = wikiBot
            LoadInfo()
            _lastEdit = GetLastEditTimestampUser(_userName)
        End Sub

        Sub LoadInfo()
            Dim queryresponse As String = _bot.POSTQUERY("action=query&format=json&list=users&usprop=blockinfo|groups|editcount|registration|gender&ususers=" & _userName)
            Try
                _userName = NormalizeUnicodetext(TextInBetween(queryresponse, """name"":""", """")(0))

                If queryresponse.Contains("""missing"":""""") Then
                    _exists = False
                    Exit Sub
                Else
                    _exists = True
                End If

                _userId = Integer.Parse(TextInBetween(queryresponse, """userid"":", ",")(0))
                _editCount = Integer.Parse(TextInBetween(queryresponse, """editcount"":", ",")(0))
                Try
                    Dim registrationString As String = TextInBetween(queryresponse, """registration"":""", """")(0).Replace("-"c, "").Replace("T"c, "").Replace("Z"c, "").Replace(":"c, "")
                    _registration = Date.ParseExact(registrationString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture)
                Catch ex As IndexOutOfRangeException
                    'En caso de usuarios tan antiguos que la API no regresa la fecha de ingreso.
                    _registration = New Date(2004, 1, 1, 0, 0, 0)
                End Try

                _groups.AddRange(TextInBetween(queryresponse, """userid"":", ",")(0).Split(","c))
                _gender = TextInBetween(queryresponse, """gender"":""", """")(0)

                If queryresponse.Contains("blockid") Then
                    _blocked = True
                    _blockID = Integer.Parse(TextInBetween(queryresponse, """blockid"":", ",")(0))
                    _blockedTimestamp = TextInBetween(queryresponse, """blockedtimestamp"":""", """")(0)
                    _blockedBy = NormalizeUnicodetext(TextInBetween(queryresponse, """blockedby"":""", """")(0))
                    _blockedbyId = Integer.Parse(TextInBetween(queryresponse, """blockedbyid"":", ",")(0))
                    _blockReason = NormalizeUnicodetext(TextInBetween(queryresponse, """blockreason"":""", """")(0))
                    _blockExpiry = TextInBetween(queryresponse, """blockexpiry"":""", """")(0)
                End If

            Catch ex As IndexOutOfRangeException
                EventLogger.EX_Log("Wikiuser LoadInfo" & ex.Message, "LOCAL")

            Catch ex2 As Exception
                EventLogger.EX_Log("Wikiuser LoadInfo: " & ex2.Message, "LOCAL")
            End Try

        End Sub

        ''' <summary>
        ''' Entrega como DateTime la fecha de la última edición del usuario entregado como parámetro.
        ''' </summary>
        ''' <param name="user">Nombre exacto del usuario</param>
        ''' <returns></returns>
        Function GetLastEditTimestampUser(ByVal user As String) As DateTime
            user = UrlWebEncode(user)
            Dim qtest As String = _bot.POSTQUERY("action=query&list=usercontribs&uclimit=1&format=json&ucuser=" & user)

            If qtest.Contains("""usercontribs"":[]") Then
                Dim fec As DateTime = DateTime.ParseExact("1111-11-11|11:11:11", "yyyy-MM-dd|HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                Return fec
            Else
                Try
                    Dim timestring As String = TextInBetween(qtest, """timestamp"":""", """,")(0).Replace("T", "|").Replace("Z", String.Empty)
                    Dim fec As DateTime = DateTime.ParseExact(timestring, "yyyy-MM-dd|HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    Return fec
                Catch ex As IndexOutOfRangeException
                    Dim fec As DateTime = DateTime.ParseExact("1111-11-11|11:11:11", "yyyy-MM-dd|HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                    Return fec
                End Try

            End If

        End Function

    End Class

End Namespace
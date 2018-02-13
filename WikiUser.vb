Option Strict On
Option Explicit On
Imports System.Globalization

Namespace WikiBot


    Public Class WikiUser
        Private _bot As Bot

        Private _username As String
        Private _userid As Integer
        Private _editcount As Integer
        Private _registration As Date
        Private _groups As String()
        Private _gender As String

        Private _blocked As Boolean
        Private _blockid As Integer
        Private _blockedTimestamp As String
        Private _blockedby As String
        Private _blockedbyID As Integer
        Private _blockreason As String
        Private _blockexpiry As String
        Private _lastedit As Date

        Private _exists As Boolean

#Region "Properties"
        Public ReadOnly Property Username As String
            Get
                Return _username
            End Get
        End Property

        Public ReadOnly Property Editcount As Integer
            Get
                Return _editcount
            End Get
        End Property

        Public ReadOnly Property Registration As Date
            Get
                Return _registration
            End Get
        End Property

        Public ReadOnly Property Groups As String()
            Get
                Return _groups
            End Get
        End Property

        Public ReadOnly Property Blocked As Boolean
            Get
                Return _blocked
            End Get
        End Property

        Public ReadOnly Property Blockedby As String
            Get
                Return _blockedby
            End Get
        End Property

        Public ReadOnly Property Blockreason As String
            Get
                Return _blockreason
            End Get
        End Property

        Public ReadOnly Property BlockedTimestamp As String
            Get
                Return _blockedTimestamp
            End Get
        End Property

        Public ReadOnly Property Blockexpiry As String
            Get
                Return _blockexpiry
            End Get
        End Property

        Public ReadOnly Property Blockid As Integer
            Get
                Return _blockid
            End Get
        End Property

        Public ReadOnly Property Exists As Boolean
            Get
                Return _exists
            End Get
        End Property

        Public ReadOnly Property BlockedbyID As Integer
            Get
                Return _blockedbyID
            End Get
        End Property

        Public ReadOnly Property Lastedit As Date
            Get
                Return _lastedit
            End Get
        End Property
#End Region


        Sub New(ByVal WikiBot As Bot, username As String)
            _username = username
            _bot = WikiBot
            LoadInfo()
        End Sub

        Sub LoadInfo()
            Dim queryresponse As String = _bot.POSTQUERY("action=query&format=json&list=users&usprop=blockinfo|groups|editcount|registration|gender&ususers=" & _username)
            Try
                _username = TextInBetween(queryresponse, """name"":""", """")(0)

                If queryresponse.Contains("""missing"":""""") Then
                    _exists = False
                    Exit Sub
                End If

                _userid = Integer.Parse(TextInBetween(queryresponse, """userid"":", ",")(0))
                _editcount = Integer.Parse(TextInBetween(queryresponse, """editcount"":", ",")(0))
                Dim registrationString As String = TextInBetween(queryresponse, """registration"":""", """")(0).Replace("-"c, "").Replace("T"c, "").Replace("Z"c, "").Replace(":"c, "")
                _registration = Date.ParseExact(registrationString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture)
                _groups = TextInBetween(queryresponse, """userid"":", ",")(0).Split(","c)
                _gender = TextInBetween(queryresponse, """gender"":""", """")(0)

                If queryresponse.Contains("blockid") Then
                    _blocked = True
                    _blockid = Integer.Parse(TextInBetween(queryresponse, """blockid"":", ",")(0))
                    _blockedTimestamp = TextInBetween(queryresponse, """blockedtimestamp"":""", """")(0)
                    _blockedby = TextInBetween(queryresponse, """blockedby"":""", """")(0)
                    _blockedbyID = Integer.Parse(TextInBetween(queryresponse, """blockedbyid"":", ",")(0))
                    _blockreason = TextInBetween(queryresponse, """blockreason"":""", """")(0)
                    _blockexpiry = TextInBetween(queryresponse, """blockexpiry"":""", """")(0)
                End If

            Catch ex As Exception
                Log("Wikiuser LoadInfo EX: " & ex.Message, "LOCAL", BOTName)
            End Try

        End Sub

        ''' <summary>
        ''' Entrega como DateTime la fecha de la última edición del usuario entregado como parámetro.
        ''' </summary>
        ''' <param name="user">Nombre exacto del usuario</param>
        ''' <returns></returns>
        Function GetLastEditTimestampUser(ByVal user As String) As DateTime
            user = UrlWebEncode(user)
            Dim qtest As String = _bot.POSTQUERY("?action=query&list=usercontribs&uclimit=1&format=json&ucuser=" & user)

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
﻿Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.CommFunctions
Imports PeriodiBOT_IRC.IRC

Namespace WikiBot
    Public Class Bot

#Region "Properties"
        Private _botPassword As String
        Private _botUserName As String
        Private _apiUrl As String
        Private _wikiUrl As String

        Private Api As APIHandler
        Private _localName As String
        Private _userName As String

        Private _ircNickName As String
        Private _ircChannel As String
        Private _ircPassword As String
        Private _ircUrl As String
        Public ReadOnly Property Bot As Boolean
            Get
                Dim postdata As String = "action=query&assert=bot&format=json"
                Dim postresponse As String = POSTQUERY(postdata)
                If postresponse.Contains("assertbotfailed") Then
                    Return False
                Else
                    Return True
                End If
            End Get
        End Property

        Public ReadOnly Property LoggedIn As Boolean
            Get
                Dim postdata As String = "action=query&assert=user&format=json"
                Dim postresponse As String = POSTQUERY(postdata)
                If postresponse.Contains("assertuserfailed") Then
                    Return False
                Else
                    Return True
                End If
            End Get
        End Property

        Public ReadOnly Property ApiUrl As String
            Get
                Return _apiUrl
            End Get
        End Property

        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public ReadOnly Property LocalName As String
            Get
                Return _localName
            End Get
        End Property

        Public ReadOnly Property WikiUrl As String
            Get
                Return _wikiUrl
            End Get
        End Property

        Public ReadOnly Property IrcUrl As String
            Get
                Return _ircUrl
            End Get
        End Property

        Public ReadOnly Property IrcPassword As String
            Get
                Return _ircPassword
            End Get
        End Property

        Public ReadOnly Property IrcChannel As String
            Get
                Return _ircChannel
            End Get
        End Property

        Public ReadOnly Property IrcNickName As String
            Get
                Return _ircNickName
            End Get
        End Property

#End Region

#Region "Init"
        Sub New(ByVal configPath As ConfigFile)
            LoadConfig(configPath)
            Api = New APIHandler(_botUserName, _botPassword, _apiUrl)
            _userName = Api.UserName
        End Sub

        Sub Relogin()
            Api = New APIHandler(_botUserName, _botPassword, _apiUrl)
        End Sub
        ''' <summary>
        ''' Inicializa las configuraciones genereales del programa desde el archivo de configuración.
        ''' Si no existe el archivo, solicita datos al usuario y lo genera.
        ''' </summary>
        ''' <returns></returns>
        Function LoadConfig(ByVal path As ConfigFile) As Boolean
            If path Is Nothing Then Throw New ArgumentNullException(System.Reflection.MethodBase.GetCurrentMethod().Name)
            Dim MainBotName As String = String.Empty
            Dim WPSite As String = String.Empty
            Dim WPAPI As String = String.Empty
            Dim WPBotUserName As String = String.Empty
            Dim WPBotPassword As String = String.Empty
            Dim IRCBotNickName As String = String.Empty
            Dim IRCBotPassword As String = String.Empty
            Dim MainIRCNetwork As String = String.Empty
            Dim MainIRCChannel As String = String.Empty
            Dim ConfigOK As Boolean = False
            Console.WriteLine("==================== PeriodiBOT " & Version & " ====================")
            EventLogger.Debug_Log("PeriodiBOT " & Version, "LOCAL", "Undefined")

            If System.IO.File.Exists(path.GetPath) Then
                EventLogger.Log("Loading config", "LOCAL", "Undefined")
                Dim Configstr As String = System.IO.File.ReadAllText(path.GetPath)
                Try
                    MainBotName = TextInBetween(Configstr, "BOTName=""", """")(0)
                    WPBotUserName = TextInBetween(Configstr, "WPUserName=""", """")(0)
                    WPSite = TextInBetween(Configstr, "PageURL=""", """")(0)
                    WPBotPassword = TextInBetween(Configstr, "WPBotPassword=""", """")(0)
                    WPAPI = TextInBetween(Configstr, "ApiURL=""", """")(0)
                    MainIRCNetwork = TextInBetween(Configstr, "IRCNetwork=""", """")(0)
                    IRCBotNickName = TextInBetween(Configstr, "IRCBotNickName=""", """")(0)
                    IRCBotPassword = TextInBetween(Configstr, "IRCBotPassword=""", """")(0)
                    MainIRCChannel = TextInBetween(Configstr, "IRCChannel=""", """")(0)
                    ConfigOK = True
                Catch ex As IndexOutOfRangeException
                    EventLogger.Log("Malformed config", "LOCAL", "Undefined")
                End Try
            Else
                EventLogger.Log("No config file", "LOCAL", "Undefined")
                Try
                    System.IO.File.Create(path.ToString).Close()
                Catch ex As System.IO.IOException
                    EventLogger.Log("Error creating new config file", "LOCAL", "Undefined")
                End Try

            End If

            If Not ConfigOK Then
                Console.Clear()
                Console.WriteLine("No config file, please fill the data or exit the program and create a mew config file.")
                Console.WriteLine("Bot Name: ")
                MainBotName = Console.ReadLine
                Console.WriteLine("Wikipedia Bot Username: ")
                WPBotUserName = Console.ReadLine
                Console.WriteLine("Wikipedia bot password: ")
                WPBotPassword = Console.ReadLine
                Console.WriteLine("Wikipedia main URL: ")
                WPSite = Console.ReadLine
                Console.WriteLine("Wikipedia API URL: ")
                WPAPI = Console.ReadLine
                Console.WriteLine("IRC Network: ")
                MainIRCNetwork = Console.ReadLine
                Console.WriteLine("IRC NickName: ")
                IRCBotNickName = Console.ReadLine
                Console.WriteLine("IRC nickserv/server password: ")
                IRCBotPassword = Console.ReadLine
                Console.WriteLine("IRC main Channel: ")
                MainIRCChannel = Console.ReadLine

                Dim configstr As String = String.Format("======================CONFIG======================
BOTName=""{0}""
WPUserName=""{1}""
WPBotPassword=""{2}""
PageURL=""{3}""
ApiURL=""{4}""
IRCNetwork=""{5}""
IRCBotNickName=""{6}""
IRCBotPassword=""{7}""
IRCChannel=""{8}""", MainBotName, WPBotUserName, WPBotPassword, WPSite, WPAPI, MainIRCNetwork, IRCBotNickName, IRCBotPassword, MainIRCChannel)

                Try
                    System.IO.File.WriteAllText(path.GetPath, configstr)
                Catch ex As System.IO.IOException
                    EventLogger.Log("Error saving config file", "LOCAL", "Undefined")
                End Try

            End If

            _localName = MainBotName
            _botUserName = WPBotUserName
            _botPassword = WPBotPassword
            _apiUrl = WPAPI
            _wikiUrl = WPSite

            _ircUrl = MainIRCNetwork
            _ircChannel = MainIRCChannel
            _ircNickName = IRCBotNickName
            _ircPassword = IRCBotPassword
            Return True
        End Function

#End Region

#Region "ApiFunctions"
        Function POSTQUERY(ByVal postdata As String) As String
            Return Api.POSTQUERY(postdata)
        End Function

        Function GETQUERY(ByVal getdata As String) As String
            Return Api.GETQUERY(getdata)
        End Function

        Function [GET](ByVal url As String) As String
            Return Api.GET(url)
        End Function
#End Region

#Region "BotFunctions"
        Function GetSpamListregexes(ByVal spamlistPage As Page) As String()
            If spamlistPage Is Nothing Then Throw New ArgumentNullException(System.Reflection.MethodBase.GetCurrentMethod().Name)
            Dim Lines As String() = GetLines(spamlistPage.Text, True) 'Extraer las líneas del texto de la página
            Dim Regexes As New List(Of String) 'Declarar lista con líneas con expresiones regulares

            For Each l As String In Lines 'Por cada línea...
                Dim tempText As String = l
                If l.Contains("#"c) Then 'Si contiene un comentario
                    tempText = tempText.Split("#"c)(0) 'Obtener el texto antes del comentario
                End If
                tempText.Trim() 'Eliminar los espacios en blanco
                If Not String.IsNullOrWhiteSpace(tempText) Then 'Verificar que no esté vacio
                    Regexes.Add(tempText) 'Añadir a la lista
                End If
            Next
            Return Regexes.ToArray
        End Function


        ''' <summary>
        ''' Retorna una pagina aleatoria
        ''' </summary>
        Function GetRandomPage() As String()
            EventLogger.Log("GetRandomPage: Starting query of random page.", "LOCAL")
            Try
                Dim QueryText As String = String.Empty
                QueryText = GETQUERY("action=query&format=json&list=random&rnnamespace=0&rnlimit=10")

                Dim plist As New List(Of String)

                For Each s As String In TextInBetween(QueryText, """title"":""", """}")
                    EventLogger.Log("GetRandomPage: Found """ & s & """.", "LOCAL")
                    plist.Add(NormalizeUnicodetext(s))
                Next
                EventLogger.Log("GetRandomPage: Ended.", "LOCAL")
                Return plist.ToArray
            Catch ex As Exception
                EventLogger.Debug_Log("GetRandomPage: ex message: " & ex.Message, "LOCAL")
                Return {""}
            End Try
        End Function

        ''' <summary>
        ''' Retorna el ultimo REVID (como integer) de las paginas indicadas como SortedList (con el formato {Pagename,Revid}), las paginas deben ser distintas. 
        ''' En caso de no existir la pagina, retorna -1 como REVID.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de paginas unicos.</param>
        ''' <remarks></remarks>
        Function GetLastRevIds(ByVal pageNames As String()) As SortedList(Of String, Integer)
            EventLogger.Debug_Log("GetLastRevIDs: Get Wikipedia last RevisionID of """ & pageNames.Count.ToString & """ pages.", "LOCAL")
            Dim PageNamesList As List(Of String) = pageNames.ToList
            PageNamesList.Sort()
            Dim PageList As List(Of List(Of String)) = SplitStringArrayIntoChunks(PageNamesList.ToArray, 50)
            Dim PagenameAndLastId As New SortedList(Of String, Integer)
            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty
                For Each s As String In ListInList
                    s = UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))

                Dim QueryResponse As String = GETQUERY(("action=query&prop=revisions&format=json&titles=" & Qstring))
                Dim ResponseArray As String() = TextInBetweenInclusive(QueryResponse, ",""title"":", "}]")

                For Each s As String In ResponseArray

                    Dim pagetitle As String = TextInBetween(s, ",""title"":""", """,""")(0)

                    If s.Contains(",""missing"":") Then
                        If Not PagenameAndLastId.ContainsKey(pagetitle) Then
                            PagenameAndLastId.Add(UrlWebDecode(NormalizeUnicodetext(pagetitle)).Replace(" ", "_"), -1)
                        End If
                    Else
                        If Not PagenameAndLastId.ContainsKey(pagetitle) Then
                            Dim TemplateTitle As String = String.Empty
                            Dim Revid As String = TextInBetween(s, pagetitle & """,""revisions"":[{""revid"":", ",""parentid"":")(0)
                            Dim Revid_ToInt As Integer = CType(Revid, Integer)

                            Dim modlist As New List(Of String)

                            For Each tx As String In PageNamesList.ToArray
                                Dim tmp As String = tx.ToLower.Replace("_", " ")
                                modlist.Add(tmp)
                            Next

                            Dim normtext As String = NormalizeUnicodetext(pagetitle)
                            normtext = normtext.ToLower.Replace("_", " ")
                            Dim ItemIndex As Integer = modlist.IndexOf(normtext)
                            TemplateTitle = PageNamesList(ItemIndex)
                            PagenameAndLastId.Add(TemplateTitle, Revid_ToInt)

                        End If
                    End If
                Next
            Next
            EventLogger.Debug_Log("GetLastRevIDs: Done """ & PagenameAndLastId.Count.ToString & """ pages returned.", "LOCAL")
            Return PagenameAndLastId
        End Function

        ''' <summary>
        ''' Entrega el título de la primera página que coincida remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, entrega una cadena de texto vacía.
        ''' </summary>
        ''' <param name="Text">Título aproximado o similar al de una página</param>
        ''' <returns></returns>
        Function TitleFirstGuess(text As String) As String
            Dim titles As String() = GetTitlesFromQueryText(GETQUERY("action=query&format=json&list=search&utf8=1&srsearch=" & text))
            If titles.Count >= 1 Then
                Return titles(0)
            Else
                Return String.Empty
            End If
        End Function

        ''' <summary>
        ''' Retorna el valor ORES (en %) de los EDIT ID (eswiki) indicados como SortedList (con el formato {ID,Score}), los EDIT ID deben ser distintos. 
        ''' En caso de no existir el EDIT ID, retorna 0.
        ''' </summary>
        ''' <param name="revids">Array con EDIT ID's unicos.</param>
        ''' <remarks>Los EDIT ID deben ser distintos</remarks>
        Function GetORESScores(ByVal revids As Integer()) As SortedList(Of Integer, Double())
            Dim Revlist As List(Of List(Of Integer)) = SplitIntegerArrayIntoChunks(revids, 50)
            Dim EditAndScoreList As New SortedList(Of Integer, Double())
            For Each ListOfList As List(Of Integer) In Revlist

                Dim Qstring As String = String.Empty
                For Each n As Integer In ListOfList
                    Qstring = Qstring & n.ToString & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))
                Try
                    Dim s As String = Api.GET(("https://ores.wikimedia.org/v3/scores/eswiki/?models=damaging|goodfaith&format=json&revids=" & UrlWebEncode(Qstring)))

                    For Each m As Match In Regex.Matches(s, "({|, )(""[0-9]+"":).+?(}}}})")

                        Dim EditID_str As String = Regex.Match(m.Value, """[0-9]+""").Value
                        EditID_str = EditID_str.Trim(CType("""", Char()))
                        EditID_str = RemoveAllAlphas(EditID_str)
                        Dim EditID As Integer = Integer.Parse(EditID_str)

                        If m.Value.Contains("error") Then

                            EventLogger.Debug_Log("GetORESScore: Server error in query of ORES score from revid " & EditID_str & " (invalid diff?)", "LOCAL")
                            EditAndScoreList.Add(EditID, {0, 0})
                        Else
                            Try
                                Dim DMGScore_str As String = TextInBetween(m.Value, """true"": ", "}")(0).Replace(".", DecimalSeparator)
                                Dim GoodFaithScore_str As String = TextInBetween(m.Value, """true"": ", "}")(1).Replace(".", DecimalSeparator)


                                EventLogger.Debug_Log("GetORESScore: Query of ORES score from revid done, Strings: GF: " & GoodFaithScore_str & " DMG:" & DMGScore_str, "LOCAL")

                                Dim DMGScore As Double = Double.Parse(DMGScore_str) * 100
                                Dim GoodFaithScore As Double = Double.Parse(GoodFaithScore_str) * 100

                                EventLogger.Debug_Log("GetORESScore: Query of ORES score from revid done, Double: GF: " & GoodFaithScore.ToString & " DMG:" & DMGScore.ToString, "LOCAL")

                                EditAndScoreList.Add(EditID, {DMGScore, GoodFaithScore})
                            Catch ex As IndexOutOfRangeException
                                EventLogger.Debug_Log("GetORESScore: IndexOutOfRange EX in ORES score from revid " & EditID_str & " EX: " & ex.Message, "LOCAL")
                                EditAndScoreList.Add(EditID, {0, 0})
                            Catch ex2 As Exception
                                EventLogger.Debug_Log("GetORESScore: EX in ORES score from revid " & EditID_str & " EX: " & ex2.Message, "LOCAL")
                                EditAndScoreList.Add(EditID, {0, 0})
                            End Try
                        End If

                    Next
                Catch ex As Exception
                    EventLogger.Debug_Log("GetORESScore: EX obttaining ORES scores EX: " & ex.Message, "LOCAL")
                End Try
            Next

            Return EditAndScoreList

        End Function



        ''' <summary>
        ''' Retorna las imagenes de preview de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Nombre de imagen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir la imagen, retorna string.empty.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de página unicos.</param>
        Function GetImagesExtract(ByVal pageNames As String()) As SortedList(Of String, String)
            Dim PageNamesList As List(Of String) = pageNames.ToList
            PageNamesList.Sort()

            Dim PageList As List(Of List(Of String)) = SplitStringArrayIntoChunks(PageNamesList.ToArray, 20)
            Dim PagenameAndImage As New SortedList(Of String, String)

            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty

                For Each s As String In ListInList
                    s = UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))

                Dim QueryResponse As String = GETQUERY("action=query&formatversion=2&prop=pageimages&format=json&titles=" & Qstring)
                Dim ResponseArray As New List(Of String)

                For Each m As Match In Regex.Matches(QueryResponse, "({).+?(})(,|])(?={|})")
                    ResponseArray.Add(m.Value)
                Next

                For Each s As String In ResponseArray.ToArray

                    Dim pagetitle As String = TextInBetween(s, ",""title"":""", """")(0)
                    Dim PageImage As String = String.Empty
                    If Not s.Contains(",""missing"":") Then

                        If Not PagenameAndImage.ContainsKey(pagetitle) Then
                            Dim PageKey As String = String.Empty
                            Dim modlist As New List(Of String)
                            For Each tx As String In PageNamesList.ToArray
                                modlist.Add(tx.ToLower.Replace("_", " "))
                            Next
                            Dim normtext As String = NormalizeUnicodetext(pagetitle)
                            normtext = normtext.ToLower.Replace("_", " ")

                            Dim ItemIndex As Integer = modlist.IndexOf(normtext)
                            PageKey = PageNamesList(ItemIndex)

                            If s.Contains("pageimage") Then
                                PageImage = TextInBetweenInclusive(s, """title"":""" & pagetitle & """", """}")(0)
                                PageImage = TextInBetween(PageImage, """pageimage"":""", """}")(0)
                            Else
                                PageImage = String.Empty
                            End If
                            PagenameAndImage.Add(PageKey, PageImage)

                        End If
                    End If
                Next
            Next
            Return PagenameAndImage
        End Function

        ''' <summary>
        ''' Retorna la entradilla de la página indicada de entrada como string con el límite indicado. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la página.</param>
        ''' <param name="CharLimit">Cantidad máxima de carácteres.</param>
        Overloads Function GetPageExtract(ByVal pageName As String, charLimit As Integer) As String
            Return GetPagesExtract({pageName}, charLimit).Values(0)
        End Function

        ''' <summary>
        ''' Retorna la entradilla de la página indicada de entrada como string con el límite indicado. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="pageName">Nombre exacto de la página.</param>
        Overloads Function GetPageExtract(ByVal pageName As String) As String
            Return GetPagesExtract({pageName}, 660).Values(0)
        End Function

        ''' <summary>
        ''' Retorna en nombre del archivo de imagen de la página indicada de entrada como string. 
        ''' En caso de no existir el la página o la imagen, no lo retorna.
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la página.</param>
        Function GetImageExtract(ByVal pageName As String) As String
            Return GetImagesExtract({pageName}).Values(0)
        End Function

        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de página unicos.</param>
        ''' <remarks></remarks>
        Overloads Function GetPagesExtract(ByVal pageNames As String()) As SortedList(Of String, String)
            Return BOTGetPagesExtract(pageNames, 660, False)
        End Function

        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de página unicos.</param>
        ''' <param name="characterLimit">Límite de carácteres en el resumen.</param>
        ''' <remarks></remarks>
        Overloads Function GetPagesExtract(ByVal pageNames As String(), ByVal characterLimit As Integer) As SortedList(Of String, String)
            Return BOTGetPagesExtract(pageNames, characterLimit, False)
        End Function

        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de página unicos.</param>
        ''' <param name="characterLimit">Límite de carácteres en el resumen.</param>
        ''' <remarks></remarks>
        Overloads Function GetPagesExtract(ByVal pageNames As String(), ByVal characterLimit As Integer, ByVal wiki As Boolean) As SortedList(Of String, String)
            Return BOTGetPagesExtract(pageNames, characterLimit, wiki)
        End Function


        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de página unicos.</param>
        ''' <remarks></remarks>
        Private Function BOTGetPagesExtract(ByVal pageNames As String(), charLimit As Integer, wiki As Boolean) As SortedList(Of String, String)
            EventLogger.Log("Get Wikipedia page extracts on chunks", "LOCAL")
            Dim PageNamesList As List(Of String) = pageNames.ToList
            PageNamesList.Sort()

            Dim PageList As List(Of List(Of String)) = SplitStringArrayIntoChunks(PageNamesList.ToArray, 20)
            Dim PagenameAndResume As New SortedList(Of String, String)

            Try
                For Each ListInList As List(Of String) In PageList
                    Dim Qstring As String = String.Empty

                    For Each s As String In ListInList
                        s = UrlWebEncode(s)
                        Qstring = Qstring & s & "|"
                    Next
                    Qstring = Qstring.Trim(CType("|", Char))
                    Dim QueryResponse As String = GETQUERY("format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" & Qstring)
                    Dim ResponseArray As String() = TextInBetweenInclusive(QueryResponse, ",""title"":", """}")
                    For Each s As String In ResponseArray
                        Dim pagetitle As String = TextInBetween(s, ",""title"":""", """,""")(0)

                        If Not s.Contains(",""missing"":") Then

                            If Not PagenameAndResume.ContainsKey(pagetitle) Then
                                Dim TreatedExtract As String = TextInBetween(s, pagetitle & """,""extract"":""", """}")(0)

                                Dim PageKey As String = String.Empty
                                Dim modlist As New List(Of String)
                                For Each tx As String In PageNamesList.ToArray
                                    modlist.Add(tx.ToLower.Replace("_", " "))
                                Next
                                Dim normtext As String = NormalizeUnicodetext(pagetitle)
                                normtext = normtext.ToLower.Replace("_", " ")

                                Dim ItemIndex As Integer = modlist.IndexOf(normtext)
                                PageKey = PageNamesList(ItemIndex)
                                TreatedExtract = NormalizeUnicodetext(TreatedExtract)
                                TreatedExtract = TreatedExtract.Replace("​", String.Empty) 'This isn't a empty string, it contains the invisible character u+200
                                TreatedExtract = TreatedExtract.Replace("\n", Environment.NewLine)
                                TreatedExtract = TreatedExtract.Replace("\""", """")
                                TreatedExtract = Regex.Replace(TreatedExtract, "\{\\\\.*\}", " ")
                                TreatedExtract = Regex.Replace(TreatedExtract, "\[[0-9]+\]", " ")
                                TreatedExtract = Regex.Replace(TreatedExtract, "\[nota\ [0-9]+\]", " ")
                                TreatedExtract = RemoveExcessOfSpaces(TreatedExtract)
                                TreatedExtract = FixResumeNumericExp(TreatedExtract)
                                If TreatedExtract.Contains(""",""missing"":""""}}}}") Then
                                    TreatedExtract = Nothing
                                End If
                                If TreatedExtract.Length > charLimit Then
                                    TreatedExtract = TreatedExtract.Substring(0, charLimit + 1)
                                    For a As Integer = -charLimit To 0
                                        If (TreatedExtract.Chars(0 - a) = ".") Or (TreatedExtract.Chars(0 - a) = ";") Then

                                            If TreatedExtract.Contains("(") Then
                                                If Not CountCharacter(TreatedExtract, CType("(", Char)) = CountCharacter(TreatedExtract, CType(")", Char)) Then
                                                    Continue For
                                                End If
                                            End If
                                            If TreatedExtract.Contains("<") Then
                                                If Not CountCharacter(TreatedExtract, CType("<", Char)) = CountCharacter(TreatedExtract, CType(">", Char)) Then
                                                    Continue For
                                                End If
                                            End If
                                            If TreatedExtract.Contains("«") Then
                                                If Not CountCharacter(TreatedExtract, CType("«", Char)) = CountCharacter(TreatedExtract, CType("»", Char)) Then
                                                    Continue For
                                                End If
                                            End If
                                            If TreatedExtract.Contains("{") Then
                                                If Not CountCharacter(TreatedExtract, CType("{", Char)) = CountCharacter(TreatedExtract, CType("}", Char)) Then
                                                    Continue For
                                                End If
                                            End If

                                            'Verifica que no este cortando un numero
                                            If TreatedExtract.Length - 1 >= (0 - a + 1) Then
                                                If Regex.Match(TreatedExtract.Chars(0 - a + 1), "[0-9]+").Success Then
                                                    Continue For
                                                Else
                                                    Exit For
                                                End If
                                            End If
                                            'Verifica que no este cortando un n/f
                                            If ((TreatedExtract.Chars(0 - a - 2) & TreatedExtract.Chars(0 - a - 1)).ToString.ToLower = "(n") Or
                                            ((TreatedExtract.Chars(0 - a - 2) & TreatedExtract.Chars(0 - a - 1)).ToString.ToLower = "(f") Then
                                                Continue For
                                            Else
                                                Exit For
                                            End If

                                        End If
                                        TreatedExtract = TreatedExtract.Substring(0, 1 - a)
                                    Next
                                    If Regex.Match(TreatedExtract, "{\\.+}").Success Then
                                        For Each m As Match In Regex.Matches(TreatedExtract, "{\\.+}")
                                            TreatedExtract = TreatedExtract.Replace(m.Value, "")
                                        Next
                                        TreatedExtract = RemoveExcessOfSpaces(TreatedExtract)
                                    End If
                                End If

                                'Si el título de la página está en el resumen, coloca en negritas la primera ocurrencia
                                If wiki Then
                                    Dim regx As New Regex(Regex.Escape(pagetitle), RegexOptions.IgnoreCase)
                                    TreatedExtract = regx.Replace(TreatedExtract, "'''" & pagetitle & "'''", 1)
                                End If

                                PagenameAndResume.Add(PageKey, TreatedExtract)
                            End If
                        End If

                    Next
                Next
            Catch ex As Exception
                EventLogger.Debug_Log("BOTGetPagesExtract EX: " & ex.Message, "LOCAL")
            End Try
            Return PagenameAndResume
        End Function

        ''' <summary>
        ''' Entrega el título de la primera página en el espacio de nombre usuario que coincida remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, entrega una cadena de texto vacía.
        ''' </summary>
        ''' <param name="text">Título relativo a buscar</param>
        ''' <returns></returns>
        Function UserFirstGuess(text As String) As String
            Try
                Return GetTitlesFromQueryText(GETQUERY("action=query&format=json&list=search&utf8=1&srnamespace=2&srsearch=" & text))(0)
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function

        ''' <summary>
        ''' Busca un texto exacto en una página y lo reemplaza.
        ''' </summary>
        ''' <param name="Requestedpage">Página a realizar el cambio</param>
        ''' <param name="requestedtext">Texto a reeplazar</param>
        ''' <param name="newtext">Texto que reemplaza</param>
        ''' <param name="reason">Motivo del reemplazo</param>
        ''' <returns></returns>
        Function Replacetext(ByVal requestedpage As Page, requestedtext As String, newtext As String, reason As String) As Boolean
            If requestedpage Is Nothing Then
                Return False
            End If
            Dim PageText As String = requestedpage.Text
            If PageText.Contains(requestedtext) Then
                PageText = PageText.Replace(requestedtext, newtext)
            End If
            requestedpage.CheckAndSave(PageText, String.Format("(Bot): Reemplazando '{0}' por '{1}' {2}.", requestedtext, newtext, reason))
            Return True

        End Function

        ''' <summary>
        ''' Evalua texto (wikicódigo) y regresa un array de string con cada uno de los hilos del mismo (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
        ''' </summary>
        ''' <param name="pagetext">Texto a evaluar</param>
        ''' <returns></returns>
        Function GetSubThreads(ByVal pagetext As String) As String()
            Dim temptext As String = pagetext

            Dim commentMatch As MatchCollection = Regex.Matches(temptext, "(<!--)[\s\S]*?(-->)")

            Dim CommentsList As New List(Of String)
            For i As Integer = 0 To commentMatch.Count - 1
                CommentsList.Add(commentMatch(i).Value)
                temptext = temptext.Replace(commentMatch(i).Value, ColoredText("PERIODIBOT::::COMMENTREPLACE::::" & i, 4))
            Next

            Dim mc As MatchCollection = Regex.Matches(temptext, "([\n\r]|^)((===(?!=)).+?(===(?!=)))")

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
                EndThreadList.Add(nthreadtext)
            Next

            Return EndThreadList.ToArray
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y realiza un archivado siguiendo una lógica similar a la de Grillitus.
        ''' </summary>
        ''' <param name="pageToArchive">Página a archivar</param>
        ''' <returns></returns>
        Function Archive(ByVal pageToArchive As Page) As Boolean
            Dim ArchiveFcn As New GrillitusArchive(Me)
            Return ArchiveFcn.Archive(pageToArchive)
        End Function

        ''' <summary>
        ''' Retorna un array de tipo string con todas las páginas donde el nombre de la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="pageName">Nombre exacto de la pagina.</param>
        Function GetallInclusions(ByVal pageName As String) As String()
            Dim newlist As New List(Of String)
            Dim s As String = String.Empty
            s = POSTQUERY("action=query&list=embeddedin&eilimit=500&format=json&eititle=" & pageName)
            Dim pages As String() = TextInBetween(s, """title"":""", """}")
            For Each _pag As String In pages
                newlist.Add(NormalizeUnicodetext(_pag))
            Next
            Return newlist.ToArray
        End Function

        ''' <summary>
        ''' Obtiene las diferencias de la última edición de la página.
        ''' </summary>
        ''' <param name="thepage">Página a revisar.</param>
        ''' <returns></returns>
        Function GetLastDiff(ByVal thepage As Page) As List(Of Tuple(Of String, String))
            If thepage Is Nothing Then Throw New ArgumentNullException(System.Reflection.MethodBase.GetCurrentMethod().Name)
            If Not thepage.Exists Then
                Return Nothing
            End If
            Dim toid As Integer = thepage.CurrentRevId
            Dim fromid As Integer = thepage.ParentRevID
            If fromid = -1 Then
                Return Nothing
            End If
            Return GetDiff(fromid, toid)
        End Function

        ''' <summary>
        ''' Obtiene las diferencias entre dos rev id.
        ''' </summary>
        ''' <param name="fromid">Id base en la comparación.</param>
        ''' <param name="toid">Id a compara.r</param>
        ''' <returns></returns>
        Function GetDiff(ByVal fromid As Integer, ByVal toid As Integer) As List(Of Tuple(Of String, String))
            Dim Changedlist As New List(Of Tuple(Of String, String))
            Dim page1 As Page = Getpage(fromid)
            Dim page2 As Page = Getpage(toid)
            If Not (page1.Exists And page2.Exists) Then
                Return Changedlist
            End If
            Dim querydata As String = "format=json&action=compare&fromrev=" & fromid.ToString & "&torev=" & toid.ToString
            Dim querytext As String = POSTQUERY(querydata)
            Dim difftext As String = String.Empty
            Try
                difftext = NormalizeUnicodetext(TextInBetween(querytext, ",""*"":""", "\n""}}")(0))
            Catch ex As Exception
                Return Changedlist
            End Try
            Dim Rows As String() = TextInBetween(difftext, "<tr>", "</tr>")
            Dim Diffs As New List(Of Tuple(Of String, String))
            For Each row As String In Rows
                Dim matches As MatchCollection = Regex.Matches(row, "<td class=""diff-(addedline|deletedline|context)"">[\S\s]*?<\/td>")
                If matches.Count >= 1 Then
                    Dim cells As New List(Of String)
                    For Each cell As Match In matches
                        Dim TreatedString As String = Regex.Replace(cell.Value, "<td class=""diff-(addedline|deletedline|context)"">", "")
                        TreatedString = Regex.Replace(TreatedString, "(<del class=""diffchange diffchange-inline"">|<div>|</div>|<\/td>|<\/del>|<ins class=""diffchange diffchange-inline"">|<\/ins>)", "")
                        cells.Add(TreatedString)
                    Next
                    If cells.Count = 1 Then
                        Diffs.Add(New Tuple(Of String, String)(String.Empty, cells(0)))
                    ElseIf cells.Count >= 2 Then
                        If Not (cells(0) = cells(1)) Then
                            Diffs.Add(New Tuple(Of String, String)(cells(0), cells(1)))
                        End If
                        Continue For
                    End If
                End If
            Next
            Return Diffs
        End Function

        ''' <summary>
        ''' Retorna un array de tipo string con todas las páginas donde la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="tpage">Página que se llama.</param>
        Function GetallInclusions(ByVal tpage As Page) As String()
            If tpage Is Nothing Then Throw New ArgumentNullException(System.Reflection.MethodBase.GetCurrentMethod().Name)
            Return GetallInclusions(tpage.Title)
        End Function

        ''' <summary>
        ''' Retorna un array con todas las páginas donde la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="pageName">Nombre exacto de la pagina.</param>
        Function GetallInclusionsPages(ByVal pageName As String) As Page()
            Dim pages As String() = GetallInclusions(pageName)
            Dim pagelist As New List(Of Page)
            For Each p As String In pages
                pagelist.Add(Getpage(p))
            Next
            Return pagelist.ToArray
        End Function

        ''' <summary>
        ''' Retorna un array con todas las páginas donde la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="tpage">Página que se llama.</param>
        Function GetallInclusionsPages(ByVal tpage As Page) As Page()
            If tpage Is Nothing Then Throw New ArgumentNullException(System.Reflection.MethodBase.GetCurrentMethod().Name)
            Return GetallInclusionsPages(tpage.Title)
        End Function

        ''' <summary>
        ''' Retorna un elemento Page coincidente al nombre entregado como parámetro.
        ''' </summary>
        ''' <param name="pageName">Nombre exacto de la página</param>
        Function Getpage(ByVal pageName As String) As Page
            Return New Page(pageName, Me)
        End Function

        ''' <summary>
        ''' Retorna un elemento Page coincidente al nombre entregado como parámetro.
        ''' </summary>
        ''' <param name="revId">ID de la revisión.</param>
        Function Getpage(ByVal revId As Integer) As Page
            Return New Page(revId, Me)
        End Function

        ''' <summary>
        ''' Verifica si un usuario programado no ha editado en el tiempo especificado.
        ''' </summary>
        ''' <returns></returns>
        Function CheckUsers() As IRCMessage()
            EventLogger.Log("CheckUsers: Checking users", "LOCAL")
            Dim Messages As New List(Of IRCMessage)
            Try
                For Each UserdataLine As String() In EventLogger.LogUserData
                    Dim username As String = UserdataLine(1)
                    Dim OP As String = UserdataLine(0)
                    Dim UserDate As String = UserdataLine(2)
                    Dim User As New WikiUser(Me, username)
                    Dim LastEdit As DateTime = User.LastEdit
                    If Not User.Exists Then
                        EventLogger.Log("CheckUsers: The user " & username & " has not edited on this wiki", "IRC")
                        Continue For
                    End If

                    Dim actualtime As DateTime = DateTime.UtcNow

                    Dim LastEditUnix As Integer = CInt(TimeToUnix(LastEdit))
                    Dim ActualTimeUnix As Integer = CInt(TimeToUnix(actualtime))

                    Dim Timediff As Integer = ActualTimeUnix - LastEditUnix
                    If Not OS.ToLower.Contains("unix") Then 'En sistemas windows hay una hora de desfase
                        Timediff = Timediff - 3600
                    End If

                    Dim TriggerTimeDiff As Long = TimeStringToSeconds(UserDate)

                    Dim TimediffToHours As Integer = CInt(Math.Truncate(Timediff / 3600))
                    Dim TimediffToMinutes As Integer = CInt(Math.Truncate(Timediff / 60))
                    Dim TimediffToDays As Integer = CInt(Math.Truncate(Timediff / 86400))
                    Dim responsestring As String = String.Empty

                    If Timediff > TriggerTimeDiff Then

                        If TimediffToMinutes <= 1 Then
                            responsestring = String.Format("¡{0} editó recién!", User.UserName)
                        Else
                            If TimediffToMinutes < 60 Then
                                responsestring = String.Format("La última edición de {0} fue hace {1} minutos", User.UserName, TimediffToMinutes)
                            Else
                                If TimediffToMinutes < 120 Then
                                    responsestring = String.Format("La última edición de {0} fue hace más de {1} hora", User.UserName, TimediffToHours)
                                Else
                                    If TimediffToMinutes < 1440 Then
                                        responsestring = String.Format("La última edición de {0} fue hace más de {1} horas", User.UserName, TimediffToHours)
                                    Else
                                        If TimediffToMinutes < 2880 Then
                                            responsestring = String.Format("La última edición de {0} fue hace {1} día", User.UserName, TimediffToDays)
                                        Else
                                            responsestring = String.Format("La última edición de {0} fue hace más de {1} días", User.UserName, TimediffToDays)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                        responsestring = responsestring & ". El proximo aviso será en 5 minutos."

                        Messages.Add(New IRCMessage(OP, responsestring))
                    End If
                Next
            Catch ex As System.ObjectDisposedException
                EventLogger.Debug_Log("CheckUsers EX: " & ex.Message, "IRC")
            End Try
            Return Messages.ToArray

        End Function

        ''' <summary>
        ''' Verifica si el usuario que se le pase cumple con los requisitos para verificar su actividad
        ''' </summary>
        ''' <param name="user">Usuario de Wiki</param>
        ''' <returns></returns>
        Private Function ValidUser(ByVal user As WikiUser) As Boolean
            EventLogger.Debug_Log("ValidUser: Check user", "LOCAL")
            'Verificar si el usuario existe
            If Not user.Exists Then
                EventLogger.Log("ValidUser: User " & user.UserName & " doesn't exist", "LOCAL")
                Return False
            End If

            'Verificar si el usuario está bloqueado.
            If user.Blocked Then
                EventLogger.Log("ValidUser: User " & user.UserName & " is blocked", "LOCAL")
                Return False
            End If

            'Verificar si el usuario editó hace al menos 4 días.
            If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
                EventLogger.Log("ValidUser: User " & user.UserName & " is inactive", "LOCAL")
                Return False
            End If
            Return True
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
        ''' </summary>
        ''' <returns></returns>
        Function ArchiveAllInclusions(ByVal irc As Boolean) As Boolean
            Dim Archive As New GrillitusArchive(Me)
            Return Archive.ArchiveAllInclusions(irc)
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de actualizacion de temas y actualiza el cafe temático.
        ''' </summary>
        ''' <returns></returns>
        Function UpdateTopics() As Boolean
            Dim topicw As New AddTopic(Me)
            Return topicw.UpdateTopics()
        End Function

        ''' <summary>
        ''' Revisa todas las páginas que llamen a la página indicada y las retorna como sortedlist.
        ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
        ''' el último usuario que la editó y el segundo el título real de la página.
        ''' </summary>
        Function GetAllRequestedpages() As SortedList(Of String, String())
            Dim _bot As Bot = Me
            Dim plist As New SortedList(Of String, String())
            For Each s As String In _bot.GetallInclusions(ResumePageName)
                Dim Pag As Page = _bot.Getpage(s)
                Dim pagetext As String = Pag.Text
                For Each s2 As String In TextInBetween(pagetext, "{{" & ResumePageName & "|", "}}")
                    If Not plist.Keys.Contains(s2) Then
                        plist.Add(s2, {Pag.Lastuser, Pag.Title})
                    End If
                Next
            Next
            Return plist
        End Function

        ''' <summary>
        ''' Compara las páginas que llaman a la plantilla y retorna retorna un sortedlist.
        ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
        ''' el último usuario que la editó y el segundo el título real de la página.
        ''' Solo contiene las páginas que no existen en la plantilla.
        ''' </summary>
        Function GetResumeRequests() As SortedList(Of String, String())
            Dim slist As SortedList(Of String, String()) = GetAllRequestedpages()
            Dim Reqlist As New SortedList(Of String, String())
            Dim ResumePage As Page = Getpage(ResumePageName)
            Dim rtext As String = ResumePage.Text

            For Each pair As KeyValuePair(Of String, String()) In slist
                Try
                    If Not rtext.Contains("|" & pair.Key & "=") Then
                        Dim pag As Page = Getpage(pair.Key)
                        If pag.Exists Then
                            Reqlist.Add(pair.Key, pair.Value)
                        End If
                    End If
                Catch ex As Exception
                End Try
            Next
            Return Reqlist

        End Function

        ''' <summary>
        ''' Actualiza los resúmenes de página basado en varios parámetros,
        ''' por defecto estos son de un máximo de 660 carácteres.
        ''' </summary>
        ''' <returns></returns>
        Public Function UpdatePageExtracts() As Boolean
            Return UpdatePageExtracts(False)
        End Function

        ''' <summary>
        ''' Actualiza los resúmenes de página basado en varios parámetros,
        ''' por defecto estos son de un máximo de 660 carácteres.
        ''' </summary>
        ''' <param name="IRC">Si se establece este valor envía un comando en IRC avisando de la actualización</param>
        ''' <returns></returns>
        Public Function UpdatePageExtracts(ByVal irc As Boolean) As Boolean
            If irc Then
                BotIRC.Sendmessage(ColoredText("Actualizando extractos.", 4))
            End If

            EventLogger.Log("UpdatePageExtracts: Beginning update of page extracts", "LOCAL")
            EventLogger.Debug_Log("UpdatePageExtracts: Declaring Variables", "LOCAL")
            Dim NewResumes As New SortedList(Of String, String)
            Dim OldResumes As New SortedList(Of String, String)
            Dim FinalList As New List(Of String)


            EventLogger.Debug_Log("UpdatePageExtracts: Loading resume page", "LOCAL")
            Dim ResumePage As Page = Getpage(ResumePageName)

            Dim ResumePageText As String = ResumePage.Text
            EventLogger.Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL")


            Dim NewResumePageText As String = "{{#switch:{{{1}}}" & Environment.NewLine
            EventLogger.Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL")

            Dim Safepages As Integer = 0
            Dim NotSafepages As Integer = 0
            Dim NewPages As Integer = 0
            Dim NotSafePagesAdded As Integer = 0

            EventLogger.Debug_Log("UpdatePageExtracts: Parsing resume template", "LOCAL")
            Dim templatelist As List(Of String) = GetTemplateTextArray(ResumePageText)
            Dim ResumeTemplate As New Template(templatelist(0), False)
            EventLogger.Debug_Log("UpdatePageExtracts: Adding Old resumes to list", "LOCAL")
            Dim PageNames As New List(Of String)

            For Each PageResume As Tuple(Of String, String) In ResumeTemplate.Parameters
                PageNames.Add(PageResume.Item1)
                OldResumes.Add(PageResume.Item1, "|" & PageResume.Item1 & "=" & PageResume.Item2)
            Next

            For Each p As KeyValuePair(Of String, String()) In GetResumeRequests()
                PageNames.Add(p.Key)
                NewPages += 1
            Next

            PageNames.Sort()

            EventLogger.Debug_Log("UpdatePageExtracts: Get last revision ID", "LOCAL")
            Dim IDLIST As SortedList(Of String, Integer) = GetLastRevIds(PageNames.ToArray)

            EventLogger.Debug_Log("UpdatePageExtracts: Adding New resumes to list", "LOCAL")
            '============================================================================================
            ' Adding New resumes to list
            Dim Page_Resume_pair As SortedList(Of String, String) = GetPagesExtract(PageNames.ToArray, 660, True)
            Dim Page_Image_pair As SortedList(Of String, String) = GetImagesExtract(PageNames.ToArray)

            For Each Page As String In Page_Resume_pair.Keys

                If Not Page_Image_pair.Item(Page) = String.Empty Then
                    'If the page contais a image
                    NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                           & "[[File:" & Page_Image_pair(Page) & "|thumb|x120px]]" & Environment.NewLine _
                           & Page_Resume_pair.Item(Page) & Environment.NewLine _
                           & ":'''[[" & Page & "|Leer más...]]'''" & Environment.NewLine)
                Else
                    'If the page doesn't contain a image
                    NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                          & Page_Resume_pair.Item(Page) & Environment.NewLine _
                          & ":'''[[" & Page & "|Leer más...]]'''" & Environment.NewLine)
                End If
            Next

            '===========================================================================================

            EventLogger.Debug_Log("UpdatePageExtracts: getting ORES of IDS", "LOCAL")
            Dim EditScoreList As SortedList(Of Integer, Double()) = GetORESScores(IDLIST.Values.ToArray)

            '==========================================================================================
            'Choose between a old resume and a new resume depending if new resume is safe to use
            EventLogger.Debug_Log("UpdatePageExtracts: Recreating text", "LOCAL")
            For Each s As String In PageNames.ToArray
                Try
                    If (EditScoreList(IDLIST(s))(0) < 20) And (CountCharacter(NewResumes(s), CType("[", Char)) = CountCharacter(NewResumes(s), CType("]", Char))) Then
                        'Safe edit
                        FinalList.Add(NewResumes(s))
                        Safepages += 1
                    Else
                        'Isn't a safe edit
                        Try
                            FinalList.Add(OldResumes(s))
                            NotSafepages += 1
                        Catch ex As KeyNotFoundException
                            FinalList.Add(NewResumes(s))
                            NotSafePagesAdded += 1
                        End Try
                    End If
                Catch ex As KeyNotFoundException
                    'If the resume doesn't exist, will try to use the old resume text
                    FinalList.Add(OldResumes(s))
                    NotSafepages += 1
                End Try
            Next
            '==========================================================================================

            EventLogger.Debug_Log("UpdatePageExtracts: Concatenating text", "LOCAL")
            NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "}}" & Environment.NewLine & "<noinclude>{{documentación}}</noinclude>"
            EventLogger.Debug_Log("UpdatePageExtracts: Done, trying to save", "LOCAL")

            Try
                If NotSafepages = 0 Then
                    If NewPages = 0 Then
                        ResumePage.Save(NewResumePageText, "(Bot) : Actualizando " & Safepages.ToString & " resúmenes.", False)
                    Else
                        ResumePage.Save(NewResumePageText, "(Bot) : Actualizando " & Safepages.ToString & " resúmenes. Se han añadido " & NewPages.ToString & " resúmenes nuevos", False)
                    End If
                Else

                    Dim NumbText As String = " Resumen inseguro fue omitido. "
                    If NotSafepages > 1 Then
                        NumbText = " Resúmenes inseguros fueron omitidos. "
                    End If

                    If NewPages = 0 Then
                        ResumePage.Save(NewResumePageText,
                                                    "(Bot): Actualizando " & Safepages.ToString & " resúmenes, " _
                                                    & NotSafepages.ToString & NumbText, False)
                    Else
                        ResumePage.Save(NewResumePageText,
                                                    "(Bot): Actualizando " & Safepages.ToString & " resúmenes," _
                                                    & NotSafepages.ToString & NumbText & "Se han añadido " & NewPages.ToString & " resúmenes nuevos.", False)
                    End If

                End If

                EventLogger.Log("UpdatePageExtracts: Update of page extracts completed successfully", "LOCAL")
                If irc Then
                    BotIRC.Sendmessage(ColoredText("¡Extractos actualizados!", 4))
                End If

                Return True

            Catch ex As Exception
                EventLogger.Log("UpdatePageExtracts: Error updating page extracts", "LOCAL")
                EventLogger.Debug_Log(ex.Message, "LOCAL")
                BotIRC.Sendmessage(ColoredText("Error al actualizar los extractos, ver LOG.", 4))
                Return False
            End Try

        End Function

        Function CheckInformalMediation() As Boolean
            Dim newThreads As Boolean = False
            Dim membPage As Page = Getpage(InformalMediationMembers)
            Dim MedPage As Page = Getpage(InfMedPage)
            Dim subthreads As String() = GetSubThreads(membPage.Text)
            Dim uTempList As List(Of Template) = GetTemplates(subthreads(0))
            Dim userList As New List(Of String)
            For Each temp As Template In uTempList
                If temp.Name = "u" Then
                    userList.Add(temp.Parameters(0).Item2)
                End If
            Next

            Dim currentThreads As Integer = GetPageThreads(MedPage).Count

            If BotSettings.Contains("InformalMediationLastThreadCount") Then
                If BotSettings.Get("InformalMediationLastThreadCount").GetType Is GetType(Integer) Then
                    Dim lastthreadcount As Integer = Integer.Parse(BotSettings.Get("InformalMediationLastThreadCount").ToString)
                    If currentThreads > lastthreadcount Then
                        BotSettings.Set("InformalMediationLastThreadCount", currentThreads)
                        newThreads = True
                    Else
                        BotSettings.Set("InformalMediationLastThreadCount", currentThreads) 'Si disminuye la cantidad de hilos entonces lo guarda
                    End If
                End If
            Else
                BotSettings.NewVal("InformalMediationLastThreadCount", currentThreads)
            End If

            If newThreads Then
                For Each u As String In userList
                    Dim user As New WikiUser(Me, u)
                    If user.Exists Then
                        Dim userTalkPage As Page = user.TalkPage
                        userTalkPage.AddSection("Atención en [[Wikipedia:Mediación informal/Solicitudes|Mediación informal]]", InfMedMessage, "Bot: Aviso automático de nueva solicitud.", False)
                    End If
                Next
            End If
            Return True
        End Function


        Function GetLastUnsignedSection(ByVal tpage As Page) As Tuple(Of String, String, Date)
            If tpage Is Nothing Then Throw New ArgumentNullException(System.Reflection.MethodBase.GetCurrentMethod().Name)
            Dim oldPage As Page = Getpage(tpage.ParentRevID)
            Dim currentPage As Page = tpage

            Dim oldPageThreads As String() = oldPage.Threads
            Dim currentPageThreads As String() = currentPage.Threads

            Dim LastEdit As Date = currentPage.LastEdit
            Dim LastUser As String = currentPage.Lastuser

            Dim oldThreadTitles As String() = GetTitlesFromThreads(oldPageThreads)
            Dim currentThreadTitles As String() = GetTitlesFromThreads(currentPageThreads)

            Dim threaddiffs As String() = GetSecondArrayAddedDiff(oldThreadTitles, currentThreadTitles)

            If threaddiffs.Count > 0 Then
                Return New Tuple(Of String, String, Date)(threaddiffs.Last, LastUser, LastEdit)
            End If
            Return Nothing
        End Function

        Function AddMissingSignature(ByVal tpage As Page) As Boolean
            Dim UnsignedSectionInfo As Tuple(Of String, String, Date) = GetLastUnsignedSection(tpage)
            If UnsignedSectionInfo Is Nothing Then Return False
            Dim pagetext As String = tpage.Text
            Dim LastNewThread As String = GetLastThreadByTitle(tpage.Threads, UnsignedSectionInfo.Item1)
            Dim lastsingnature As Date = FirstDate(LastNewThread)
            If Not lastsingnature = New DateTime(9999, 12, 31, 23, 59, 59) Then
                Return False
            End If
            Dim UnsignedThread As String = LastNewThread
            pagetext = pagetext.Replace(UnsignedThread, UnsignedThread & " {{sust:No firmado|" & UnsignedSectionInfo.Item2 & "|" & UnsignedSectionInfo.Item3.ToString("HH:mm d MMM yyyy") & " (UTC)}}")
            If tpage.Save(pagetext, "Bot: Completando sección sin firmar.", True, True) = EditResults.Edit_successful Then
                Return True
            Else
                Return False
            End If
        End Function

#End Region

#Region "Subs"
        Sub CheckUsersActivity(ByVal templatePage As Page, ByVal pageToSave As Page)
            If pageToSave Is Nothing Then Exit Sub

            Dim ActiveUsers As New Dictionary(Of String, WikiUser)
            Dim InactiveUsers As New Dictionary(Of String, WikiUser)
            For Each p As Page In GetallInclusionsPages(templatePage)

                If (p.PageNamespace = 3) Or (p.PageNamespace = 2) Then
                    Dim Username As String = p.Title.Split(":"c)(1)
                    'si es una subpágina
                    If Username.Contains("/") Then
                        Username = Username.Split("/"c)(0)
                    End If
                    'Cargar usuario
                    Dim User As New WikiUser(Me, Username)
                    'Validar usuario
                    If Not ValidUser(User) Then
                        EventLogger.Log("Archive: The user" & User.UserName & " doesn't meet the requirements.", "LOCAL")
                        Continue For
                    End If

                    If Date.Now.Subtract(User.LastEdit) < New TimeSpan(0, 30, 0) Then
                        If Not ActiveUsers.Keys.Contains(User.UserName) Then
                            ActiveUsers.Add(User.UserName, User)
                        End If
                    Else
                        If Not InactiveUsers.Keys.Contains(User.UserName) Then
                            InactiveUsers.Add(User.UserName, User)
                        End If
                    End If
                End If
            Next

            Dim t As New Template
            t.Name = "#switch:{{{1|}}}"
            t.Parameters.Add(New Tuple(Of String, String)("", "'''Error''': No se ha indicado usuario."))
            t.Parameters.Add(New Tuple(Of String, String)("#default", "[[Archivo:WX circle red.png|10px|link=]]&nbsp;<span style=""color:red;"">'''Desconectado'''</span>"))

            For Each u As WikiUser In ActiveUsers.Values
                Dim gendertext As String = "Conectado"
                If u.Gender = "female" Then
                    gendertext = "Conectada"
                End If
                t.Parameters.Add(New Tuple(Of String, String)(u.UserName, "[[Archivo:WX circle green.png|10px|link=]]&nbsp;<span style=""color:green;"">'''" & gendertext & "'''</span>"))
            Next

            For Each u As WikiUser In InactiveUsers.Values
                Dim gendertext As String = "Desconectado"
                If u.Gender = "female" Then
                    gendertext = "Desconectada"
                End If
                Dim lastedit As Date = u.LastEdit
                t.Parameters.Add(New Tuple(Of String, String)(u.UserName, "[[Archivo:WX circle red.png|10px|link=|Última edición el " & Integer.Parse(lastedit.ToString("dd")).ToString & lastedit.ToString(" 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES")) & "]]&nbsp;<span style=""color:red;"">'''" & gendertext & "'''</span>"))
            Next

            Dim templatetext As String = "{{Noart|1=<div style=""position:absolute; z-index:100; right:10px; top:5px;"" class=""metadata"">" & Environment.NewLine & t.Text
            templatetext = templatetext & Environment.NewLine & "</div>}}" & Environment.NewLine & "<noinclude>" & "{{documentación}}" & "</noinclude>"
            pageToSave.Save(templatetext, "Bot: Actualizando lista.")

        End Sub

#End Region

    End Class


End Namespace
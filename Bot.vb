Option Strict On
Option Explicit On
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.My.Resources

Namespace WikiBot
    Public Class Bot

#Region "Properties"
        Private _botPassword As String
        Private _botUserName As String
        Private _apiUri As Uri
        Private _wikiUri As Uri

        Private Api As ApiHandler
        Private _localName As String
        Private _userName As String

        Private _ircNickName As String
        Private _ircChannels As String()
        Private _ircPassword As String
        Private _ircUrl As String
        Public ReadOnly Property Bot As Boolean
            Get
                Dim postdata As String = SStrings.AssertBotData
                Dim postresponse As String = POSTQUERY(postdata)
                If postresponse.Contains(SStrings.AssertBotFailed) Then
                    Return False
                Else
                    Return True
                End If
            End Get
        End Property

        Public ReadOnly Property LoggedIn As Boolean
            Get
                Dim postdata As String = SStrings.AssertUserData
                Dim postresponse As String = POSTQUERY(postdata)
                If postresponse.Contains(SStrings.AssertUserFailed) Then
                    Return False
                Else
                    Return True
                End If
            End Get
        End Property

        Public ReadOnly Property ApiUri As Uri
            Get
                Return _apiUri
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

        Public ReadOnly Property WikiUri As Uri
            Get
                Return _wikiUri
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

        Public ReadOnly Property IrcChannels As String()
            Get
                Return _ircChannels
            End Get
        End Property

        Public ReadOnly Property IrcNickName As String
            Get
                Return _ircNickName
            End Get
        End Property

        Public Property AutoArchiveTemplatePageName As String
        Public Property AutoSignatureTemplatePageName As String
        Public Property AutoArchiveProgrammedArchivePageName As String
        Public Property AutoArchiveDoNotArchivePageName As String
        Public Property ArchiveBoxTemplate As String
        Public Property ArchiveMessageTemplate As String
#End Region

#Region "Init"
        Sub New(ByVal configPath As ConfigFile)
            Dim valid As Boolean = LoadConfig(configPath)
            Do Until valid
                valid = LoadConfig(configPath)
            Loop
            Api = New ApiHandler(_botUserName, _botPassword, _apiUri)
            _userName = Api.UserName
        End Sub

        Sub Relogin()
            Api = New ApiHandler(_botUserName, _botPassword, _apiUri)
        End Sub
        ''' <summary>
        ''' Inicializa las configuraciones genereales del programa desde el archivo de configuración.
        ''' Si no existe el archivo, solicita datos al usuario y lo genera.
        ''' </summary>
        ''' <returns></returns>
        Function LoadConfig(ByVal path As ConfigFile) As Boolean
            If path Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
            Dim MainBotName As String = String.Empty
            Dim WPSite As String = String.Empty
            Dim WPAPI As String = String.Empty
            Dim WPBotUserName As String = String.Empty
            Dim WPBotPassword As String = String.Empty
            Dim IRCBotNickName As String = String.Empty
            Dim IRCBotPassword As String = String.Empty
            Dim MainIRCNetwork As String = String.Empty
            Dim IRCChannels As String() = {String.Empty}
            Dim _autoArchiveTemplatePagename As String = String.Empty
            Dim _autoSignatureTemplatePageName As String = String.Empty
            Dim _autoArchiveProgrammedArchivePageName As String = String.Empty
            Dim _autoArchiveDoNotArchivePageName As String = String.Empty
            Dim _archiveBoxTemplate As String = String.Empty
            Dim _doNotArchiveTemplate As String = String.Empty
            Dim _archiveMessageTemplate As String = String.Empty
            Dim ConfigOK As Boolean = False
            Console.WriteLine(String.Format(Messages.GreetingMsg, Version))
            Utils.EventLogger.Debug_Log(Messages.BotEngine & Version, Reflection.MethodBase.GetCurrentMethod().Name)
            If System.IO.File.Exists(path.GetPath) Then
                Utils.EventLogger.Log(Messages.LoadingConfig, Reflection.MethodBase.GetCurrentMethod().Name)
                Dim Configstr As String = System.IO.File.ReadAllText(path.GetPath)
                Try
                    MainBotName = Utils.TextInBetween(Configstr, "BOTName=""", """")(0)
                    WPBotUserName = Utils.TextInBetween(Configstr, "WPUserName=""", """")(0)
                    WPSite = Utils.TextInBetween(Configstr, "PageURL=""", """")(0)
                    WPBotPassword = Utils.TextInBetween(Configstr, "WPBotPassword=""", """")(0)
                    WPAPI = Utils.TextInBetween(Configstr, "ApiURL=""", """")(0)
                    MainIRCNetwork = Utils.TextInBetween(Configstr, "IRCNetwork=""", """")(0)
                    IRCBotNickName = Utils.TextInBetween(Configstr, "IRCBotNickName=""", """")(0)
                    IRCBotPassword = Utils.TextInBetween(Configstr, "IRCBotPassword=""", """")(0)
                    IRCChannels = Utils.TextInBetween(Configstr, "IRCChannel=""", """")(0).Split("|"c)
                    _autoArchiveTemplatePagename = Utils.TextInBetween(Configstr, "AutoArchiveTemplatePagename=""", """")(0)
                    _autoSignatureTemplatePageName = Utils.TextInBetween(Configstr, "AutoSignatureTemplatePageName=""", """")(0)
                    _autoArchiveProgrammedArchivePageName = Utils.TextInBetween(Configstr, "AutoArchiveProgrammedArchivePageName=""", """")(0)
                    _autoArchiveDoNotArchivePageName = Utils.TextInBetween(Configstr, "AutoArchiveDoNotArchivePageName=""", """")(0)
                    _archiveBoxTemplate = Utils.TextInBetween(Configstr, "ArchiveBoxTemplate=""", """")(0)
                    _archiveMessageTemplate = Utils.TextInBetween(Configstr, "ArchiveMessageTemplate=""", """")(0)
                    ConfigOK = True
                Catch ex As IndexOutOfRangeException
                    Utils.EventLogger.Log(Messages.ConfigError, Reflection.MethodBase.GetCurrentMethod().Name)
                End Try
            Else
                Utils.EventLogger.Log(Messages.NoConfigFile, Reflection.MethodBase.GetCurrentMethod().Name)
                Try
                    System.IO.File.Create(path.ToString).Close()
                Catch ex As System.IO.IOException
                    Utils.EventLogger.Log(Messages.NewConfigFileError, Reflection.MethodBase.GetCurrentMethod().Name)
                End Try
            End If

            If Not ConfigOK Then
                Console.Clear()
                Console.WriteLine(Messages.NewConfigMessage)
                Console.WriteLine(Messages.NewBotName)
                MainBotName = Console.ReadLine
                Console.WriteLine(Messages.NewUserName)
                WPBotUserName = Console.ReadLine
                Console.WriteLine(Messages.NewBotPassword)
                WPBotPassword = Console.ReadLine
                Console.WriteLine(Messages.NewWikiMainUrl)
                WPSite = Console.ReadLine
                Console.WriteLine(Messages.NewWikiMainApiUrl)
                WPAPI = Console.ReadLine
                Console.WriteLine(Messages.NewIrcNetworkAdress)
                MainIRCNetwork = Console.ReadLine
                Console.WriteLine(Messages.NewIrcNetworkNickName)
                IRCBotNickName = Console.ReadLine
                Console.WriteLine(Messages.NewIrcNetworkPass)
                IRCBotPassword = Console.ReadLine
                Console.WriteLine(Messages.NewIrcNetworkChannels)
                IRCChannels = {Console.ReadLine}
                Console.WriteLine(Messages.NewAutoArchiveTemplatePagename)
                _AutoArchiveTemplatePagename = Console.ReadLine
                Console.WriteLine(Messages.NewAutoSignatureTemplatePageName)
                _AutoSignatureTemplatePageName = Console.ReadLine
                Console.WriteLine(Messages.NewAutoArchiveProgrammedArchivePageName)
                _AutoArchiveProgrammedArchivePageName = Console.ReadLine
                Console.WriteLine(Messages.NewAutoArchiveDoNotArchivePageName)
                _AutoArchiveDoNotArchivePageName = Console.ReadLine
                Console.WriteLine(Messages.NewArchiveBoxTemplate)
                _archiveBoxTemplate = Console.ReadLine
                Console.WriteLine(Messages.NewArchiveMessageTemplate)
                _archiveMessageTemplate = Console.ReadLine

                Dim configstr As String = String.Format(SStrings.ConfigTemplate, MainBotName, WPBotUserName, WPBotPassword,
                                                        WPSite, WPAPI, MainIRCNetwork, IRCBotNickName, IRCBotPassword, String.Join("|"c, IRCChannels),
                                                        _autoArchiveTemplatePagename, _autoSignatureTemplatePageName, _autoArchiveProgrammedArchivePageName,
                                                        _autoArchiveDoNotArchivePageName, _archiveBoxTemplate, _archiveMessageTemplate)
                Try
                    System.IO.File.WriteAllText(path.GetPath, configstr)
                Catch ex As System.IO.IOException
                    Utils.EventLogger.Log(Messages.SaveConfigError, Reflection.MethodBase.GetCurrentMethod().Name)
                End Try
            End If

            AutoArchiveTemplatePageName = _autoArchiveTemplatePagename
            AutoSignatureTemplatePageName = _autoSignatureTemplatePageName
            AutoArchiveProgrammedArchivePageName = _autoArchiveProgrammedArchivePageName
            AutoArchiveDoNotArchivePageName = _autoArchiveDoNotArchivePageName
            ArchiveBoxTemplate = _archiveBoxTemplate
            ArchiveMessageTemplate = _archiveMessageTemplate

            _localName = MainBotName
            _botUserName = WPBotUserName
            _botPassword = WPBotPassword
            Try
                _apiUri = New Uri(WPAPI)
                _wikiUri = New Uri(WPSite)
            Catch ex As ArgumentException
                Utils.EventLogger.Log(Messages.InvalidUrl, Reflection.MethodBase.GetCurrentMethod().Name)
                System.IO.File.Delete(path.GetPath)
                Utils.WaitSeconds(5)
                Return False
            Catch ex2 As UriFormatException
                Utils.EventLogger.Log(Messages.InvalidUrl, Reflection.MethodBase.GetCurrentMethod().Name)
                System.IO.File.Delete(path.GetPath)
                Utils.PressKeyTimeout(5)
                Return False
            End Try
            _ircUrl = MainIRCNetwork
            _ircChannels = IRCChannels
            _ircNickName = IRCBotNickName
            _ircPassword = IRCBotPassword
            Return True
        End Function

#End Region

#Region "ApiFunctions"
        Function POSTQUERY(ByVal postdata As String) As String
            Return Api.Postquery(postdata)
        End Function

        Function GETQUERY(ByVal getdata As String) As String
            Return Api.Getquery(getdata)
        End Function

        Function [GET](ByVal turi As Uri) As String
            Return Api.GET(turi)
        End Function
#End Region

#Region "BotFunctions"
        Public Function GetSpamListregexes(ByVal spamlistPage As Page) As String()
            If spamlistPage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
            Dim Lines As String() = Utils.GetLines(spamlistPage.Content, True) 'Extraer las líneas del texto de la página
            Dim Regexes As New List(Of String) 'Declarar lista con líneas con expresiones regulares

            For Each l As String In Lines 'Por cada línea...
                Dim tempText As String = l
                If l.Contains("#"c) Then 'Si contiene un comentario
                    tempText = tempText.Split("#"c)(0) 'Obtener el texto antes del comentario
                End If
                tempText = tempText.Trim() 'Eliminar los espacios en blanco
                If Not String.IsNullOrWhiteSpace(tempText) Then 'Verificar que no esté vacio
                    Regexes.Add(tempText) 'Añadir a la lista
                End If
            Next
            Return Regexes.ToArray
        End Function

        ''' <summary>
        ''' Retorna el ultimo REVID (como integer) de las paginas indicadas como SortedList (con el formato {Pagename,Revid}), las paginas deben ser distintas. 
        ''' En caso de no existir la pagina, retorna -1 como REVID.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de paginas unicos.</param>
        ''' <remarks></remarks>
        Function GetLastRevIds(ByVal pageNames As String()) As SortedList(Of String, Integer)
            Utils.EventLogger.Debug_Log(String.Format(Messages.GetLastrevIDs, pageNames.Count), Reflection.MethodBase.GetCurrentMethod().Name)
            Dim PageNamesList As List(Of String) = pageNames.ToList
            PageNamesList.Sort()
            Dim PageList As List(Of List(Of String)) = Utils.SplitStringArrayIntoChunks(PageNamesList.ToArray, 50)
            Dim PagenameAndLastId As New SortedList(Of String, Integer)
            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty
                For Each s As String In ListInList
                    s = Utils.UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))

                Dim QueryResponse As String = GETQUERY((SStrings.GetLastRevIds & Qstring))
                Dim ResponseArray As String() = Utils.TextInBetweenInclusive(QueryResponse, ",""title"":", "}]")

                For Each s As String In ResponseArray

                    Dim pagetitle As String = Utils.TextInBetween(s, ",""title"":""", """,""")(0)

                    If s.Contains(",""missing"":") Then
                        If Not PagenameAndLastId.ContainsKey(pagetitle) Then
                            PagenameAndLastId.Add(Utils.UrlWebDecode(Utils.NormalizeUnicodetext(pagetitle)).Replace(" ", "_"), -1)
                        End If
                    Else
                        If Not PagenameAndLastId.ContainsKey(pagetitle) Then
                            Dim TemplateTitle As String = String.Empty
                            Dim Revid As String = Utils.TextInBetween(s, pagetitle & """,""revisions"":[{""revid"":", ",""parentid"":")(0)
                            Dim Revid_ToInt As Integer = CType(Revid, Integer)

                            Dim modlist As New List(Of String)

                            For Each tx As String In PageNamesList.ToArray
                                Dim tmp As String = tx.ToLower.Replace("_", " ")
                                modlist.Add(tmp)
                            Next

                            Dim normtext As String = Utils.NormalizeUnicodetext(pagetitle)
                            normtext = normtext.ToLower.Replace("_", " ")
                            Dim ItemIndex As Integer = modlist.IndexOf(normtext)
                            TemplateTitle = PageNamesList(ItemIndex)
                            PagenameAndLastId.Add(TemplateTitle, Revid_ToInt)

                        End If
                    End If
                Next
            Next
            Utils.EventLogger.Debug_Log(String.Format(Messages.DoneXPagesReturned, PagenameAndLastId.Count), Reflection.MethodBase.GetCurrentMethod().Name)
            Return PagenameAndLastId
        End Function

        ''' <summary>
        ''' Entrega los títulos de las páginas que coincidan remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, entrega un un string() vacio.
        ''' </summary>
        ''' <param name="PageName">Título aproximado o similar al de una página</param>
        ''' <returns></returns>
        Function SearchForPages(pageName As String) As String()
            Return Utils.GetTitlesFromQueryText(GETQUERY(SStrings.Search & pageName))
        End Function

        ''' <summary>
        ''' Entrega el título de la primera página que coincida remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, entrega una cadena de texto vacía.
        ''' </summary>
        ''' <param name="PageName">Título aproximado o similar al de una página</param>
        ''' <returns></returns>
        Function SearchForPage(pageName As String) As String
            Dim titles As String() = SearchForPages(pageName)
            If titles.Count >= 1 Then
                Return titles(0)
            Else
                Return String.Empty
            End If
        End Function

        ''' <summary>
        ''' Entrega la primera página que coincida remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, no retorna nada.
        ''' </summary>
        ''' <param name="PageName"></param>
        ''' <returns></returns>
        Function GetSearchedPage(pageName As String) As Page
            Dim titles As String() = SearchForPages(pageName)
            If titles.Count >= 1 Then
                Return Getpage(titles(0))
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Retorna el valor ORES (en %) de los EDIT ID (eswiki) indicados como SortedList (con el formato {ID,Score}), los EDIT ID deben ser distintos. 
        ''' En caso de no existir el EDIT ID, retorna 0.
        ''' </summary>
        ''' <param name="revids">Array con EDIT ID's unicos.</param>
        ''' <remarks>Los EDIT ID deben ser distintos</remarks>
        Function GetORESScores(ByVal revids As Integer()) As SortedList(Of Integer, Double())
            Dim Revlist As List(Of List(Of Integer)) = Utils.SplitIntegerArrayIntoChunks(revids, 50)
            Dim EditAndScoreList As New SortedList(Of Integer, Double())
            For Each ListOfList As List(Of Integer) In Revlist

                Dim Qstring As String = String.Empty
                For Each n As Integer In ListOfList
                    Qstring = Qstring & n.ToString & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))
                Dim apiuri As Uri = New Uri(SStrings.OresScoresApiQueryUrl & Utils.UrlWebEncode(Qstring))
                Dim s As String = Api.GET(apiuri)

                For Each m As Match In Regex.Matches(s, "({|, )(""[0-9]+"":).+?(}}}})")
                    Dim EditID_str As String = Regex.Match(m.Value, """[0-9]+""").Value
                    EditID_str = EditID_str.Trim(CType("""", Char()))
                    EditID_str = Utils.RemoveAllAlphas(EditID_str)
                    Dim EditID As Integer = Integer.Parse(EditID_str)

                    If m.Value.Contains("error") Then

                        Utils.EventLogger.Debug_Log(String.Format(Messages.OresQueryError, EditID_str), Reflection.MethodBase.GetCurrentMethod().Name)
                        EditAndScoreList.Add(EditID, {0, 0})
                    Else
                        Try
                            Dim DMGScore_str As String = Utils.TextInBetween(m.Value, """true"": ", "}")(0).Replace(".", DecimalSeparator)
                            Dim GoodFaithScore_str As String = Utils.TextInBetween(m.Value, """true"": ", "}")(1).Replace(".", DecimalSeparator)
                            Dim DMGScore As Double = Double.Parse(DMGScore_str) * 100
                            Dim GoodFaithScore As Double = Double.Parse(GoodFaithScore_str) * 100
                            Utils.EventLogger.Debug_Log(String.Format(Messages.OresQueryResult, EditID_str, GoodFaithScore.ToString, DMGScore.ToString), Reflection.MethodBase.GetCurrentMethod().Name)
                            EditAndScoreList.Add(EditID, {DMGScore, GoodFaithScore})
                        Catch ex As IndexOutOfRangeException
                            Utils.EventLogger.Debug_Log(String.Format(Messages.OresQueryEx, EditID_str, ex.Message), Reflection.MethodBase.GetCurrentMethod().Name)
                            EditAndScoreList.Add(EditID, {0, 0})
                        End Try
                    End If
                Next
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

            Dim PageList As List(Of List(Of String)) = Utils.SplitStringArrayIntoChunks(PageNamesList.ToArray, 20)
            Dim PagenameAndImage As New SortedList(Of String, String)

            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty

                For Each s As String In ListInList
                    s = Utils.UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))

                Dim QueryResponse As String = GETQUERY(SStrings.GetPagesImage & Qstring)
                Dim ResponseArray As New List(Of String)

                For Each m As Match In Regex.Matches(QueryResponse, "({).+?(})(,|])(?={|})")
                    ResponseArray.Add(m.Value)
                Next

                For Each s As String In ResponseArray.ToArray

                    Dim pagetitle As String = Utils.TextInBetween(s, ",""title"":""", """")(0)
                    Dim PageImage As String = String.Empty
                    If Not s.Contains(",""missing"":") Then

                        If Not PagenameAndImage.ContainsKey(pagetitle) Then
                            Dim PageKey As String = String.Empty
                            Dim modlist As New List(Of String)
                            For Each tx As String In PageNamesList.ToArray
                                modlist.Add(tx.ToLower.Replace("_", " "))
                            Next
                            Dim normtext As String = Utils.NormalizeUnicodetext(pagetitle)
                            normtext = normtext.ToLower.Replace("_", " ")

                            Dim ItemIndex As Integer = modlist.IndexOf(normtext)
                            PageKey = PageNamesList(ItemIndex)

                            If s.Contains("pageimage") Then
                                PageImage = Utils.TextInBetweenInclusive(s, """title"":""" & pagetitle & """", """}")(0)
                                PageImage = Utils.TextInBetween(PageImage, """pageimage"":""", """}")(0)
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
        ''' Corta de la mejor forma que pueda un extracto para que esté debajo del límite de caracteres especificado.
        ''' </summary>
        ''' <param name="safetext"></param>
        ''' <param name="charlimit"></param>
        ''' <returns></returns>
        Function SafeTrimExtract(ByVal safetext As String, ByVal charlimit As Integer) As String
            Dim TrimmedText As String = safetext
            For a As Integer = charlimit To 0 Step -1
                If (TrimmedText.Chars(a) = ".") Or (TrimmedText.Chars(a) = ";") Then

                    If TrimmedText.Contains("(") Then
                        If Not Utils.CountCharacter(TrimmedText, CType("(", Char)) = Utils.CountCharacter(TrimmedText, CType(")", Char)) Then
                            Continue For
                        End If
                    End If
                    If TrimmedText.Contains("<") Then
                        If Not Utils.CountCharacter(TrimmedText, CType("<", Char)) = Utils.CountCharacter(TrimmedText, CType(">", Char)) Then
                            Continue For
                        End If
                    End If
                    If TrimmedText.Contains("«") Then
                        If Not Utils.CountCharacter(TrimmedText, CType("«", Char)) = Utils.CountCharacter(TrimmedText, CType("»", Char)) Then
                            Continue For
                        End If
                    End If
                    If TrimmedText.Contains("{") Then
                        If Not Utils.CountCharacter(TrimmedText, CType("{", Char)) = Utils.CountCharacter(TrimmedText, CType("}", Char)) Then
                            Continue For
                        End If
                    End If

                    'Verifica que no este cortando un numero
                    If TrimmedText.Length - 1 >= (a + 1) Then
                        If Regex.Match(TrimmedText.Chars(a + 1), "[0-9]+").Success Then
                            Continue For
                        Else
                            Exit For
                        End If
                    End If
                    'Verifica que no este cortando un n/f
                    If ((TrimmedText.Chars(a - 2) & TrimmedText.Chars(a - 1)).ToString.ToLower = "(n") Or
                    ((TrimmedText.Chars(a - 2) & TrimmedText.Chars(a - 1)).ToString.ToLower = "(f") Then
                        Continue For
                    Else
                        Exit For
                    End If

                End If
                TrimmedText = TrimmedText.Substring(0, a)
            Next
            If Regex.Match(TrimmedText, "{\\.+}").Success Then
                For Each m As Match In Regex.Matches(TrimmedText, "{\\.+}")
                    TrimmedText = TrimmedText.Replace(m.Value, "")
                Next
                TrimmedText = Utils.RemoveExcessOfSpaces(TrimmedText)
            End If
            Return TrimmedText
        End Function

        Function GetExtractsFromApiResponse(ByVal queryresponse As String, ByVal charLimit As Integer, ByVal wiki As Boolean) As HashSet(Of WikiExtract)
            Dim ExtractsList As New HashSet(Of WikiExtract)
            Dim ResponseArray As String() = Utils.TextInBetweenInclusive(queryresponse, ",""title"":", """}")
            For Each s As String In ResponseArray
                If Not s.Contains(",""missing"":") Then
                    Dim pagetitle As String = Utils.TextInBetween(s, ",""title"":""", """,""")(0).Replace("_"c, " ")
                    Dim TreatedExtract As String = Utils.TextInBetween(s, pagetitle & """,""extract"":""", """}")(0)
                    TreatedExtract = Utils.NormalizeUnicodetext(TreatedExtract)
                    TreatedExtract = TreatedExtract.Replace("\n", Environment.NewLine)
                    TreatedExtract = TreatedExtract.Replace("\""", """")
                    TreatedExtract = Regex.Replace(TreatedExtract, "\{\\\\.*\}", " ")
                    TreatedExtract = Regex.Replace(TreatedExtract, "\[[0-9]+\]", " ")
                    TreatedExtract = Regex.Replace(TreatedExtract, "\[nota\ [0-9]+\]", " ")
                    TreatedExtract = Utils.RemoveExcessOfSpaces(TreatedExtract)
                    TreatedExtract = Utils.FixResumeNumericExp(TreatedExtract)
                    If TreatedExtract.Contains(""",""missing"":""""}}}}") Then
                        TreatedExtract = Nothing
                    End If
                    If TreatedExtract.Length > charLimit Then
                        TreatedExtract = SafeTrimExtract(TreatedExtract.Substring(0, charLimit + 1), charLimit)
                    End If
                    'Si el título de la página está en el resumen, coloca en negritas la primera ocurrencia
                    If wiki Then
                        Dim regx As New Regex(Regex.Escape(pagetitle), RegexOptions.IgnoreCase)
                        TreatedExtract = regx.Replace(TreatedExtract, "'''" & pagetitle & "'''", 1)
                    End If
                    Dim Extract As New WikiExtract With {
                        .ExtractContent = TreatedExtract,
                        .PageName = Utils.NormalizeUnicodetext(pagetitle)}
                    ExtractsList.Add(Extract)
                End If
            Next
            Return ExtractsList
        End Function


        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="pageNames">Array con nombres de página unicos.</param>
        ''' <remarks></remarks>
        Private Function BOTGetPagesExtract(ByVal pageNames As String(), charLimit As Integer, wiki As Boolean) As SortedList(Of String, String)
            Utils.EventLogger.Log(String.Format(Messages.GetPagesExtract, pageNames.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name)
            If pageNames Is Nothing Then Return Nothing
            Dim PageNamesList As List(Of String) = pageNames.ToList
            PageNamesList.Sort()
            Dim PageList As List(Of List(Of String)) = Utils.SplitStringArrayIntoChunks(PageNamesList.ToArray, 20)
            Dim PagenameAndResume As New SortedList(Of String, String)

            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty
                For Each s As String In ListInList
                    s = Utils.UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))
                Dim QueryResponse As String = GETQUERY(SStrings.GetPagesExtract & Qstring)
                Dim ExtractsList As HashSet(Of WikiExtract) = GetExtractsFromApiResponse(QueryResponse, charLimit, wiki)

                Dim NormalizedNames As New List(Of String)
                For Each pageName As String In PageNamesList.ToArray
                    NormalizedNames.Add(pageName.ToLower.Replace("_", " "))
                Next

                For Each Extract As WikiExtract In ExtractsList
                    Dim OriginalNameIndex As Integer = NormalizedNames.IndexOf(Extract.PageName.ToLower)
                    Dim OriginalName As String = PageNamesList(OriginalNameIndex)
                    PagenameAndResume.Add(OriginalName, Extract.ExtractContent)
                Next
            Next

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
            Dim titles As String() = Utils.GetTitlesFromQueryText(GETQUERY(SStrings.SearchForUser & text))
            If titles.Count >= 1 Then
                Return titles(0)
            Else
                Return String.Empty
            End If
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
            Dim PageText As String = requestedpage.Content
            If PageText.Contains(requestedtext) Then
                PageText = PageText.Replace(requestedtext, newtext)
            End If
            requestedpage.CheckAndSave(PageText, String.Format(Messages.TextReplaced, requestedtext, newtext, reason))
            Return True

        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y realiza un archivado siguiendo una lógica similar a la de Grillitus.
        ''' </summary>
        ''' <param name="pageToArchive">Página a archivar</param>
        ''' <returns></returns>
        Function Archive(ByVal pageToArchive As Page) As Boolean
            Dim ArchiveFcn As New OldGrillitusTasks(Me)
            Return ArchiveFcn.AutoArchive(pageToArchive)
        End Function

        ''' <summary>
        ''' Retorna un array de tipo string con todas las páginas donde el nombre de la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="pageName">Nombre exacto de la pagina.</param>
        Function GetallInclusions(ByVal pageName As String) As String()
            Dim newlist As New List(Of String)
            Dim s As String = String.Empty
            s = POSTQUERY(SStrings.GetPageInclusions & pageName)
            Dim pages As String() = Utils.TextInBetween(s, """title"":""", """}")
            For Each _pag As String In pages
                newlist.Add(Utils.NormalizeUnicodetext(_pag))
            Next
            Return newlist.ToArray
        End Function

        ''' <summary>
        ''' Obtiene las diferencias de la última edición de la página.
        ''' </summary>
        ''' <param name="thepage">Página a revisar.</param>
        ''' <returns></returns>
        Function GetLastDiff(ByVal thepage As Page) As WikiDiff
            If thepage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
            If Not thepage.Exists Then
                Return Nothing
            End If
            Dim toid As Integer = thepage.CurrentRevId
            Dim fromid As Integer = thepage.ParentRevId
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
        Function GetDiff(ByVal fromid As Integer, ByVal toid As Integer) As WikiDiff

            Dim Changedlist As New List(Of Tuple(Of String, String))
            Dim page1 As Page = Getpage(fromid)
            Dim page2 As Page = Getpage(toid)
            If Not (page1.Exists And page2.Exists) Then
                Return New WikiDiff(fromid, toid, Changedlist)
            End If
            Dim querydata As String = String.Format(SStrings.GetDiffQuery, fromid.ToString, toid.ToString)
            Dim querytext As String = POSTQUERY(querydata)
            Dim difftext As String = String.Empty
            Try
                difftext = Utils.NormalizeUnicodetext(Utils.TextInBetween(querytext, ",""*"":""", "\n""}}")(0))
            Catch ex As IndexOutOfRangeException
                Return New WikiDiff(fromid, toid, Changedlist)
            End Try
            Dim Rows As String() = Utils.TextInBetween(difftext, "<tr>", "</tr>")
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
            Return New WikiDiff(fromid, toid, Diffs)
        End Function

        ''' <summary>
        ''' Retorna un array de tipo string con todas las páginas donde la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="tpage">Página que se llama.</param>
        Function GetallInclusions(ByVal tpage As Page) As String()
            If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
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
            If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
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
            Utils.EventLogger.Log(Messages.CheckingUsers, Reflection.MethodBase.GetCurrentMethod().Name)
            Dim MessagesList As New List(Of IRCMessage)
            Try
                For Each UserdataLine As String() In Utils.EventLogger.LogUserData
                    Dim username As String = UserdataLine(1)
                    Dim OP As String = UserdataLine(0)
                    Dim UserDate As String = UserdataLine(2)
                    Dim User As New WikiUser(Me, username)
                    Dim LastEdit As DateTime = User.LastEdit
                    If Not User.Exists Then
                        Utils.EventLogger.Log(String.Format(Messages.UserNoEdits, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name, SStrings.IrcSource)
                        Continue For
                    End If

                    Dim actualtime As DateTime = DateTime.UtcNow

                    Dim LastEditUnix As Integer = CInt(Utils.TimeToUnix(LastEdit))
                    Dim ActualTimeUnix As Integer = CInt(Utils.TimeToUnix(actualtime))

                    Dim Timediff As Integer = ActualTimeUnix - LastEditUnix
                    If Not OS.ToLower.Contains("unix") Then 'En sistemas windows hay una hora de desfase
                        Timediff = Timediff - 3600
                    End If

                    Dim TriggerTimeDiff As Long = Utils.TimeStringToSeconds(UserDate)

                    Dim TimediffToHours As Integer = CInt(Math.Truncate(Timediff / 3600))
                    Dim TimediffToMinutes As Integer = CInt(Math.Truncate(Timediff / 60))
                    Dim TimediffToDays As Integer = CInt(Math.Truncate(Timediff / 86400))
                    Dim responsestring As String = String.Empty

                    If Timediff > TriggerTimeDiff Then

                        If TimediffToMinutes <= 1 Then
                            responsestring = String.Format(Messages.UserJustEdited, User.UserName)
                        Else
                            If TimediffToMinutes < 60 Then
                                responsestring = String.Format(Messages.UserEditedMinutes, User.UserName, TimediffToMinutes)
                            Else
                                If TimediffToMinutes < 120 Then
                                    responsestring = String.Format(Messages.UserEditedHour, User.UserName, TimediffToHours)
                                Else
                                    If TimediffToMinutes < 1440 Then
                                        responsestring = String.Format(Messages.UserEditedHours, User.UserName, TimediffToHours)
                                    Else
                                        If TimediffToMinutes < 2880 Then
                                            responsestring = String.Format(Messages.UserEditedDay, User.UserName, TimediffToDays)
                                        Else
                                            responsestring = String.Format(Messages.UserEditedDays, User.UserName, TimediffToDays)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                        responsestring = responsestring & Messages.NextNotif

                        MessagesList.Add(New IRCMessage(OP, responsestring))
                    End If
                Next
            Catch ex As System.ObjectDisposedException
                Utils.EventLogger.Debug_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name, SStrings.IrcSource)
            End Try
            Return MessagesList.ToArray

        End Function

        ''' <summary>
        ''' Verifica si el usuario que se le pase cumple con los requisitos para verificar su actividad
        ''' </summary>
        ''' <param name="user">Usuario de Wiki</param>
        ''' <returns></returns>
        Private Function ValidUser(ByVal user As WikiUser) As Boolean
            Utils.EventLogger.Debug_Log(String.Format(Messages.CheckingUser, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
            'Verificar si el usuario existe
            If Not user.Exists Then
                Utils.EventLogger.Log(String.Format(Messages.UserInexistent, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End If

            'Verificar si el usuario está bloqueado.
            If user.Blocked Then
                Utils.EventLogger.Log(String.Format(Messages.UserBlocked, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End If

            'Verificar si el usuario editó hace al menos 4 días.
            If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
                Utils.EventLogger.Log(String.Format(Messages.UserInactive, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End If
            Return True
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
        ''' </summary>
        ''' <returns></returns>
        Function ArchiveAllInclusions() As Boolean
            Dim Archive As New OldGrillitusTasks(Me)
            Return Archive.ArchiveAllInclusions()
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
        ''' </summary>
        ''' <returns></returns>
        Function SignAllInclusions() As Boolean
            Dim signtask As New OldGrillitusTasks(Me)
            Return signtask.SignAllInclusions()
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de actualizacion de temas y actualiza el cafe temático.
        ''' </summary>
        ''' <returns></returns>
        Function UpdateTopics() As Boolean
            Dim topicw As New WikiTopicList(Me)
            Return topicw.UpdateTopics()
        End Function

        ''' <summary>
        ''' Revisa todas las páginas que llamen a la página indicada y las retorna como sortedlist.
        ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
        ''' el último usuario que la editó y el segundo el título real de la página.
        ''' </summary>
        Function GetAllRequestedpages(pageName As String) As SortedList(Of String, String())
            Dim _bot As Bot = Me
            Dim plist As New SortedList(Of String, String())
            For Each s As String In _bot.GetallInclusions(pageName)
                Dim Pag As Page = _bot.Getpage(s)
                Dim pagetext As String = Pag.Content
                For Each s2 As String In Utils.TextInBetween(pagetext, "{{" & pageName & "|", "}}")
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
        Function GetResumeRequests(ByVal pageName As String) As SortedList(Of String, String())
            Dim slist As SortedList(Of String, String()) = GetAllRequestedpages(pageName)
            Dim Reqlist As New SortedList(Of String, String())
            Dim ResumePage As Page = Getpage(ResumePageName)
            Dim rtext As String = ResumePage.Content

            For Each pair As KeyValuePair(Of String, String()) In slist
                Try
                    If Not rtext.Contains("|" & pair.Key & "=") Then
                        Dim pag As Page = Getpage(pair.Key)
                        If pag.Exists Then
                            Reqlist.Add(pair.Key, pair.Value)
                        End If
                    End If
                Catch ex As IndexOutOfRangeException
                End Try
            Next
            Return Reqlist

        End Function

        ''' <summary>
        ''' Actualiza los resúmenes de página basado en varios parámetros,
        ''' por defecto estos son de un máximo de 660 carácteres.
        ''' </summary>
        ''' <returns></returns>
        Public Function UpdatePageExtracts(ByVal pageName As String) As Boolean
            Utils.EventLogger.Log(String.Format(Messages.GetPageExtract, pageName), Reflection.MethodBase.GetCurrentMethod().Name)
            Dim NewResumes As New SortedList(Of String, String)
            Dim OldResumes As New SortedList(Of String, String)
            Dim FinalList As New List(Of String)

            Dim ResumePage As Page = Getpage(pageName)
            Dim ResumePageText As String = ResumePage.Content
            Dim NewResumePageText As String = "{{#switch:{{{1}}}" & Environment.NewLine

            Dim Safepages As Integer = 0
            Dim NotSafepages As Integer = 0
            Dim NewPages As Integer = 0
            Dim NotSafePagesAdded As Integer = 0

            Dim templatelist As List(Of String) = Template.GetTemplateTextArray(ResumePageText)
            Dim ResumeTemplate As New Template(templatelist(0), False)
            Utils.EventLogger.Debug_Log(String.Format(Messages.LoadingOldExtracts, ResumeTemplate.Parameters.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name)
            Dim PageNames As New List(Of String)

            For Each PageResume As Tuple(Of String, String) In ResumeTemplate.Parameters
                PageNames.Add(PageResume.Item1)
                OldResumes.Add(PageResume.Item1, "|" & PageResume.Item1 & "=" & PageResume.Item2)
            Next

            For Each p As KeyValuePair(Of String, String()) In GetResumeRequests(pageName)
                PageNames.Add(p.Key)
                NewPages += 1
            Next
            PageNames.Sort()
            Dim IDLIST As SortedList(Of String, Integer) = GetLastRevIds(PageNames.ToArray)

            Utils.EventLogger.Debug_Log(String.Format(Messages.LoadingNewExtracts, PageNames.Count.ToString), Reflection.MethodBase.GetCurrentMethod().Name)
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

            Dim EditScoreList As SortedList(Of Integer, Double()) = GetORESScores(IDLIST.Values.ToArray)

            '==========================================================================================
            'Choose between a old resume and a new resume depending if new resume is safe to use
            Utils.EventLogger.Debug_Log(Messages.RecreatingText, Reflection.MethodBase.GetCurrentMethod().Name)
            For Each s As String In PageNames.ToArray
                Try
                    If (EditScoreList(IDLIST(s))(0) < 20) And (Utils.CountCharacter(NewResumes(s), CType("[", Char)) = Utils.CountCharacter(NewResumes(s), CType("]", Char))) Then
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
            NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "}}" & Environment.NewLine & "<noinclude>{{documentación}}</noinclude>"
            Utils.EventLogger.Debug_Log(String.Format(Messages.TryingToSave, ResumePage.Title), Reflection.MethodBase.GetCurrentMethod().Name)

            Try
                Dim EditSummary As String = String.Format(Messages.UpdatedExtracts, Safepages.ToString)

                If NewPages > 0 Then
                    Dim NewPageText As String = String.Format(Messages.AddedExtract, NewPages.ToString)
                    If NewPages > 1 Then
                        NewPageText = String.Format(Messages.AddedExtracts, NewPages.ToString)
                    End If
                    EditSummary = EditSummary & NewPageText
                End If

                If NotSafepages > 0 Then
                    Dim NumbText As String = String.Format(Messages.OmittedExtract, NotSafepages.ToString)
                    If NotSafepages > 1 Then
                        NumbText = String.Format(Messages.OmittedExtracts, NotSafepages.ToString)
                    End If
                    NumbText = String.Format(NumbText, NotSafepages)
                    EditSummary = EditSummary & NumbText
                End If
                Dim Result As EditResults = ResumePage.Save(NewResumePageText, EditSummary, True, True)

                If Result = EditResults.Edit_successful Then
                    Utils.EventLogger.Log(Messages.SuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name)
                    Return True
                Else
                    Utils.EventLogger.Log(Messages.UnsuccessfulOperation & " (" & [Enum].GetName(GetType(EditResults), Result) & ").", Reflection.MethodBase.GetCurrentMethod().Name)
                    Return False
                End If
            Catch ex As IndexOutOfRangeException
                Utils.EventLogger.Log(Messages.UnsuccessfulOperation, Reflection.MethodBase.GetCurrentMethod().Name)
                Utils.EventLogger.Debug_Log(ex.Message, Reflection.MethodBase.GetCurrentMethod().Name)
                Return False
            End Try

        End Function

        Function CheckInformalMediation() As Boolean
            Dim newThreads As Boolean = False
            Dim membPage As Page = Getpage(InformalMediationMembers)
            Dim MedPage As Page = Getpage(InfMedPage)
            Dim subthreads As String() = Utils.GetPageSubThreads(membPage.Content)
            Dim uTempList As List(Of Template) = Template.GetTemplates(subthreads(0))
            Dim userList As New List(Of String)
            For Each temp As Template In uTempList
                If temp.Name = "u" Then
                    userList.Add(temp.Parameters(0).Item2)
                End If
            Next

            Dim currentThreads As Integer = Utils.GetPageThreads(MedPage).Count

            If Utils.BotSettings.Contains(SStrings.InfMedSettingsName) Then
                If Utils.BotSettings.Get(SStrings.InfMedSettingsName).GetType Is GetType(Integer) Then
                    Dim lastthreadcount As Integer = Integer.Parse(Utils.BotSettings.Get(SStrings.InfMedSettingsName).ToString)
                    If currentThreads > lastthreadcount Then
                        Utils.BotSettings.Set(SStrings.InfMedSettingsName, currentThreads)
                        newThreads = True
                    Else
                        Utils.BotSettings.Set(SStrings.InfMedSettingsName, currentThreads) 'Si disminuye la cantidad de hilos entonces lo guarda
                    End If
                End If
            Else
                Utils.BotSettings.NewVal(SStrings.InfMedSettingsName, currentThreads)
            End If

            If newThreads Then
                For Each u As String In userList
                    Dim user As New WikiUser(Me, u)
                    If user.Exists Then
                        Dim userTalkPage As Page = user.TalkPage
                        userTalkPage.AddSection(SStrings.InfMedTitle, SStrings.InfMedMsg, SStrings.InfMedSumm, False)
                    End If
                Next
            End If
            Return True
        End Function

        Function GetLastUnsignedSection(ByVal tpage As Page, newthreads As Boolean) As Tuple(Of String, String, Date)
            If tpage Is Nothing Then Throw New ArgumentNullException(Reflection.MethodBase.GetCurrentMethod().Name)
            Dim oldPage As Page = Getpage(tpage.ParentRevId)
            Dim currentPage As Page = tpage

            Dim oldPageThreads As String() = oldPage.Threads
            Dim currentPageThreads As String() = currentPage.Threads

            Dim LastEdit As Date = currentPage.LastEdit
            Dim LastUser As String = currentPage.Lastuser
            Dim editedthreads As String()

            If newthreads Then
                editedthreads = Utils.GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
            Else
                If oldPageThreads.Count = currentPageThreads.Count Then
                    editedthreads = Utils.GetChangedThreads(oldPageThreads, currentPageThreads)
                ElseIf oldPageThreads.Count < currentPageThreads.Count Then
                    editedthreads = Utils.GetSecondArrayAddedDiff(oldPageThreads, currentPageThreads)
                Else
                    editedthreads = {}
                End If
            End If

            If editedthreads.Count > 0 Then
                Dim lasteditedthread As String = editedthreads.Last
                Dim lastsign As Date = Utils.LastParagraphDateTime(lasteditedthread)
                If lastsign = New DateTime(9999, 12, 31, 23, 59, 59) Then
                    Return New Tuple(Of String, String, Date)(lasteditedthread, LastUser, LastEdit)
                End If
            End If
            Return Nothing
        End Function

        Function AddMissingSignature(ByVal tpage As Page, newthreads As Boolean, minor As Boolean) As Boolean
            If tpage.Lastuser = _userName Then Return False 'No completar firma en páginas en las que haya editado
            Dim LastUser As WikiUser = New WikiUser(Me, tpage.Lastuser)
            If LastUser.IsBot Then Return False
            Dim UnsignedSectionInfo As Tuple(Of String, String, Date) = GetLastUnsignedSection(tpage, newthreads)
            If UnsignedSectionInfo Is Nothing Then Return False
            Dim pagetext As String = tpage.Content
            Dim UnsignedThread As String = UnsignedSectionInfo.Item1
            Dim Username As String = UnsignedSectionInfo.Item2
            Dim UnsignedDate As Date = UnsignedSectionInfo.Item3
            Dim dstring As String = Utils.GetSpanishTimeString(UnsignedDate)
            pagetext = pagetext.Replace(UnsignedThread, UnsignedThread & " {{sust:No firmado|" & Username & "|" & dstring & "}}")
            If tpage.Save(pagetext, String.Format(Messages.UnsignedSumm, Username), minor, True) = EditResults.Edit_successful Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Retorna la cantidad máxima que permita la api de páginas que comienze con el texto.
        ''' </summary>
        ''' <param name="pagePrefix"></param>
        ''' <returns></returns>
        Function PrefixSearch(ByVal pagePrefix As String) As String()
            Dim QueryString As String = SStrings.PrefixSearchQuery & Utils.UrlWebEncode(pagePrefix)
            Dim QueryResult As String = POSTQUERY(QueryString)
            Dim Pages As String() = Utils.TextInBetween(QueryResult, """title"":""", """,""")
            Dim DecodedPages As New List(Of String)
            For Each p As String In Pages
                DecodedPages.Add(Utils.NormalizeUnicodetext(p))
            Next
            Return DecodedPages.ToArray
        End Function


        Function BiggestThreadsEver() As Boolean
            Dim topicw As New WikiTopicList(Me)
            Return topicw.BiggestThreadsEver()
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
                        Utils.EventLogger.Debug_Log(String.Format(Messages.InvalidUser, User.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
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
            pageToSave.Save(templatetext, "Bot: Actualizando lista.", True, True, False)

        End Sub


        Sub MessageDelivery(ByVal userList As String(), messageTitle As String, messageContent As String, editSummary As String)
            For Each u As String In userList
                Dim user As New WikiUser(Me, u)
                If Not user.Exists Then
                    Utils.EventLogger.Log(String.Format(Messages.UserInexistent, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
                    Continue For
                End If
                If user.Blocked Then
                    Utils.EventLogger.Log(String.Format(Messages.UserBlocked, user.UserName), Reflection.MethodBase.GetCurrentMethod().Name)
                    Continue For
                End If
                Dim userTalkPage As Page = user.TalkPage
                userTalkPage.AddSection(messageTitle, messageContent, editSummary, False)
            Next
        End Sub

#End Region

    End Class
End Namespace
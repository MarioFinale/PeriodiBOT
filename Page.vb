Option Strict On
Option Explicit On
Imports System.Net
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.CommFunctions

Namespace WikiBot
    Public Class Page
        Private _text As String
        Private _title As String
        Private _lastuser As String
        Private _username As String
        Private _ID As Integer
        Private _siteurl As String
        Private _currentRevID As Integer
        Private _ORESScores As Double()
        Private _timestamp As String
        Private _sections As String()
        Private _categories As String()
        Private _pageViews As Integer
        Private _size As Integer
        Private _Namespace As Integer
        Private _extract As String
        Private _thumbnail As String
        Private _rootPage As String
        Private _bot As Bot

#Region "Properties"
        ''' <summary>
        ''' Entrega el puntaje ORES {reverted,goodfaith} de la página.
        ''' </summary>
        ''' <returns></returns>
        Public Function ORESScores() As Double()
            Return _ORESScores
        End Function
        ''' <summary>
        ''' Entrega el wikitexto de la página.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Text As String
            Get
                Return _text
            End Get
        End Property
        ''' <summary>
        ''' Entrega el revid actual de la página.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property CurrentRevId As Integer
            Get
                Return _currentRevID
            End Get
        End Property
        ''' <summary>
        ''' entrega el último usuario en editar la página.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Lastuser As String
            Get
                Return _lastuser
            End Get
        End Property
        ''' <summary>
        ''' Entrega el título de la página (con el namespace).
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Title As String
            Get
                Return _title
            End Get
        End Property
        ''' <summary>
        ''' Entrega la marca de tiempo de la edición actual de la página.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Timestamp As String
            Get
                Return _timestamp
            End Get
        End Property
        ''' <summary>
        ''' Entrega las secciones de la página
        ''' </summary>
        ''' <returns></returns>
        Function Sections() As String()

            Return _sections

        End Function
        ''' <summary>
        ''' Entrega las primeras 10 categorías de la página.
        ''' </summary>
        ''' <returns></returns>
        Public Function Categories() As String()
            Return _categories
        End Function
        ''' <summary>
        ''' Entrega el promedio de visitas diarias de la página en los últimos 2 meses.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property PageViews As Integer
            Get
                Return _pageViews
            End Get
        End Property
        ''' <summary>
        ''' Tamaño de la página en bytes.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Size As Integer
            Get
                Return _size
            End Get
        End Property

        ''' <summary>
        ''' Número del espacio de nombres al cual pertenece la página.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property PageNamespace As Integer
            Get
                Return _Namespace
            End Get
        End Property

        ''' <summary>
        ''' Extracto de la intro de la pagina (segun wikipedia, largo completo).
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Extract As String
            Get
                Return _extract
            End Get
        End Property

        ''' <summary>
        ''' Imagen de miniatura de la pagina.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Thumbnail As String
            Get
                Return _thumbnail
            End Get
        End Property

        Public ReadOnly Property RootPage As String
            Get
                Return _rootPage
            End Get
        End Property

        ''' <summary>
        ''' ¿La página existe?
        ''' </summary>
        ''' <returns></returns>
        Public Function Exists() As Boolean
            If _ID = -1 Then
                Return False
            Else
                Return True
            End If
        End Function
#End Region
        ''' <summary>
        ''' Inicializa una nueva página, por lo general no se llama de forma directa. Se puede obtener una página creandola con Bot.Getpage.
        ''' </summary>
        ''' <param name="PageTitle">Título exacto de la página</param>
        ''' <param name="wbot">Bot logueado a la wiki</param>
        Public Sub New(ByVal pageTitle As String, ByRef wbot As Bot)
            _bot = wbot
            EventLogger.Log("Loading page " & pageTitle, "LOCAL", _bot.UserName)
            _username = _bot.UserName
            Loadpage(pageTitle, _bot.WikiUrl)
        End Sub
        ''' <summary>
        ''' Inicializa de nuevo la página (al crear una página esta ya está inicializada).
        ''' </summary>
        Public Sub Load()
            EventLogger.Log("Loading page " & _title, "LOCAL", _username)
            Loadpage(_title, _siteurl)
        End Sub

        ''' <summary>
        ''' Inicializa la página, esta función no se llama de forma directa
        ''' </summary>
        ''' <param name="PageTitle">Título exacto de la página</param>
        ''' <param name="site">Sitio de la página</param>
        ''' <returns></returns>
        Private Function Loadpage(ByVal PageTitle As String, ByVal site As String) As Boolean
            EventLogger.Log("Loading page " & PageTitle, "LOCAL", _username)
            If String.IsNullOrEmpty(PageTitle) Or String.IsNullOrEmpty(site) Then
                Throw New ArgumentNullException("Empty parameter", "PageTitle")
            End If
            _siteurl = site
            PageInfoData(PageTitle)
            _sections = GetPageThreads(_text)
            _ORESScores = GetORESScores(_currentRevID)
            _pageViews = GetPageViewsAvg(_title)
            EventLogger.Log("Page " & PageTitle & " loaded", "LOCAL", _username)
            Return True
        End Function

        ''' <summary>
        ''' Retorna el valor ORES (en %) de un EDIT ID (eswiki) indicados como porcentaje en double. 
        ''' En caso de no existir el EDIT ID, retorna 0.
        ''' </summary>
        ''' <param name="revid">EDIT ID de la edicion a revisar</param>
        ''' <remarks>Los EDIT ID deben ser distintos</remarks>
        Private Function GetORESScores(ByVal revid As Integer) As Double()
            EventLogger.Debug_log("GetORESScore: Query of ORES score from revid " & revid.ToString, "LOCAL", _username)
            Try
                Dim s As String = _bot.GET("https://ores.wikimedia.org/v3/scores/eswiki/?models=damaging|goodfaith&format=json&revids=" & revid)

                Dim DMGScore_str As String = TextInBetween(TextInBetweenInclusive(s, "{""damaging"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)
                Dim GoodFaithScore_str As String = TextInBetween(TextInBetweenInclusive(s, """goodfaith"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)
                EventLogger.Debug_log("GetORESScore: Query of ORES score from revid done, Strings: GF: " & GoodFaithScore_str & " DMG:" & DMGScore_str, "LOCAL", _username)

                Dim DMGScore As Double = Math.Round((Double.Parse(DMGScore_str) * 100), 2)
                Dim GoodFaithScore As Double = Math.Round((Double.Parse(GoodFaithScore_str) * 100), 2)
                EventLogger.Debug_log("GetORESScore: Query of ORES score from revid done, Double: GF: " & GoodFaithScore.ToString & " DMG:" & DMGScore.ToString, "LOCAL", _username)

                Return {DMGScore, GoodFaithScore}
            Catch ex As IndexOutOfRangeException
                EventLogger.Debug_log("GetORESScore: Query of ORES score from revid " & revid.ToString & " failed, returning Nothing", "LOCAL", _username)
                Return Nothing
            Catch ex2 As Exception
                EventLogger.Debug_log("GetORESScore: Query of ORES score from revid " & revid.ToString & " failed: " & ex2.Message, "LOCAL", _username)
                Return Nothing
            End Try
        End Function


        ''' <summary>
        ''' Guarda la página en la wiki. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="EditSummary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <param name="Spamreplace">¿Reemplazar los link marcados como spam?</param>
        ''' <returns></returns>
        Overloads Function Save(ByVal text As String, ByVal EditSummary As String, ByVal IsMinor As Boolean, ByVal IsBot As Boolean, ByVal SpamReplace As Boolean) As String
            Return SavePage(text, EditSummary, IsMinor, IsBot, SpamReplace, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="EditSummary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Private Function SavePage(ByVal text As String, ByVal EditSummary As String, ByVal IsMinor As Boolean, ByVal IsBot As Boolean, ByVal Spamreplace As Boolean, ByRef RetryCount As Integer) As String
            If String.IsNullOrEmpty(text) Or String.IsNullOrWhiteSpace(text) Then
                Throw New ArgumentNullException("Empty parameter", "Text")
            End If
            Dim ntimestamp As String = GetLastTimeStamp(_title)

            If Not ntimestamp = _timestamp Then
                EventLogger.Log("Edit conflict on " & _title, "BOT", _username)
                Return "Edit conflict"
            End If
            Dim minorstr As String = String.Empty

            If IsMinor Then
                minorstr = "&minor="
            Else
                minorstr = "&notminor="
            End If

            Dim botstr As String = String.Empty
            If IsBot Then
                botstr = "&bot="
            End If

            Dim postdata As String = "format=json&action=edit&title=" & _title & botstr & minorstr & "&summary=" & UrlWebEncode(EditSummary) & "&text=" & UrlWebEncode(text) & "&token=" & UrlWebEncode(GetEditToken())
            Dim postresult As String = String.Empty

            Try
                postresult = _bot.POSTQUERY(postdata)
                System.Threading.Thread.Sleep(1000) 'Some time to the server to process the data
                Load() 'Update page data
            Catch ex As Exception
                EventLogger.Log("POST error on " & _title, "BOT", _username)
            End Try

            If String.IsNullOrWhiteSpace(postresult) Then
                Return "POST error"
            End If

            If postresult.Contains("""result"":""Success""") Then
                EventLogger.Log("Edit on " & _title & " successful!", "LOCAL", _username)
                Return "Edit successful!"
            End If

            If postresult.ToLower.Contains("abusefilter") Then
                EventLogger.Log("AbuseFilter Triggered! on " & _title, "LOCAL", _username)
                EventLogger.Debug_Log("ABUSEFILTER: " & postresult, "LOCAL", _username)
                Return "AbuseFilter"
            End If

            If postresult.ToLower.Contains("spamblacklist") Then
                EventLogger.Log("AbuseFilter Triggered! on " & _title, "LOCAL", _username)
                EventLogger.Debug_Log("ABUSEFILTER: " & postresult, "LOCAL", _username)
                If Spamreplace Then
                    Dim spamlinkRegex As String = TextInBetween(postresult, """spamblacklist"":""", """")(0)
                    Dim newtext As String = Regex.Replace(text, SpamListParser(spamlinkRegex), Function(x) "<nowiki>" & x.Value & "</nowiki>") 'Reeplazar links con el Nowiki
                    If Not RetryCount > MaxRetry Then
                        Return SavePage(newtext, EditSummary, IsMinor, IsBot, True, RetryCount + 1)
                    Else
                        EventLogger.Log("Max retry count saving " & _title, "LOCAL", _username)
                        Return "Max retry count"
                    End If
                Else
                    Return "SpamBlacklist"
                End If
            End If

            'Unexpected result, retry
            If Not RetryCount > MaxRetry Then
                'Refresh credentials, retry
                _bot.Relogin()
                Return SavePage(text, EditSummary, IsMinor, IsBot, True, RetryCount + 1)
            Else
                EventLogger.Log("Max retry count saving " & _title, "LOCAL", _username)
                EventLogger.Debug_Log("Unexpected result: " & postresult, "SavePage", _username)
                Return "Max retry count"
            End If

            Return "Unexpected result"
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Comprueba si la página tiene la plantilla {{nobots}}. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Overloads Function CheckAndSave(ByVal text As String, ByVal summary As String, ByVal isMinor As Boolean, ByVal isBOT As Boolean, ByVal SpamReplace As Boolean) As String
            If Not BotCanEdit(_text, _username) Then
                EventLogger.Log("Bots can't edit " & _title & "!", "BOT", _username)
                Return "No Bots"
            End If
            Return SavePage(text, summary, isMinor, isBOT, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Comprueba si la página tiene la plantilla {{nobots}}. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Overloads Function CheckAndSave(ByVal text As String, ByVal summary As String, ByVal isMinor As Boolean, ByVal isBOT As Boolean) As String
            If Not BotCanEdit(_text, _username) Then
                EventLogger.Log("Bots can't edit " & _title & "!", "BOT", _username)
                Return "No Bots"
            End If
            Return SavePage(text, summary, isMinor, isBOT, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Comprueba si la página tiene la plantilla {{nobots}}. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Overloads Function CheckAndSave(ByVal text As String, ByVal summary As String, ByVal isMinor As Boolean) As String
            If Not BotCanEdit(_text, _username) Then
                EventLogger.Log("Bots can't edit " & _title & "!", "BOT", _username)
                Return "No Bots"
            End If
            Return SavePage(text, summary, isMinor, False, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Comprueba si la página tiene la plantilla {{nobots}}. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <returns></returns>
        Overloads Function CheckAndSave(ByVal text As String, ByVal summary As String) As String
            If Not BotCanEdit(_text, _username) Then
                EventLogger.Log("Bots can't edit " & _title & "!", "BOT", _username)
                Return "No Bots"
            End If
            Return SavePage(text, summary, False, False, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Comprueba si la página tiene la plantilla {{nobots}}. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <returns></returns>
        Overloads Function CheckAndSave(ByVal text As String) As String
            If Not BotCanEdit(_text, _username) Then
                EventLogger.Log("Bots can't edit " & _title & "!", "BOT", _username)
                Return "No Bots"
            End If
            Return SavePage(text, "Bot edit", False, False, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Overloads Function Save(ByVal text As String, ByVal summary As String, ByVal isMinor As Boolean, ByVal isBOT As Boolean) As String
            Return SavePage(text, summary, isMinor, isBOT, False, 0)
        End Function


        ''' <summary>
        ''' Guarda la página en la wiki. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Overloads Function Save(ByVal text As String, ByVal summary As String, ByVal isMinor As Boolean) As String
            Return SavePage(text, summary, isMinor, False, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <param name="Summary">Resumen de la edición</param>
        ''' <returns></returns>
        Overloads Function Save(ByVal text As String, ByVal summary As String) As String
            Return SavePage(text, summary, False, False, False, 0)
        End Function

        ''' <summary>
        ''' Guarda la página en la wiki. Si la página no existe, la crea.
        ''' </summary>
        ''' <param name="text">Texto (wikicódigo) de la página</param>
        ''' <returns></returns>
        Overloads Function Save(ByVal text As String) As String
            Return SavePage(text, "Bot edit", False, False, False, 0)
        End Function

        ''' <summary>
        ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
        ''' </summary>
        ''' <param name="SectionTitle">Título de la sección nueva</param>
        ''' <param name="text">Texto de la sección</param>
        ''' <param name="EditSummary">Resumen de edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Private Function AddSectionPage(ByVal sectionTitle As String, ByVal text As String, ByVal editSummary As String, ByVal isMinor As Boolean) As String
            Dim additionalParameters As String = String.Empty
            If String.IsNullOrEmpty(text) Or String.IsNullOrWhiteSpace(text) Or String.IsNullOrWhiteSpace(sectionTitle) Then
                Throw New ArgumentNullException("Text")
            End If

            If Not GetLastTimeStamp(_title) = _timestamp Then
                EventLogger.Log("Edit conflict", "LOCAL", _username)
                Return "Edit conflict"
            End If

            If Not BotCanEdit(_text, _username) Then
                EventLogger.Log("Bots can't edit this page!", "LOCAL", _username)
                Return "No Bots"
            End If

            If isMinor Then
                additionalParameters = additionalParameters & "&minor=true"
            End If

            Dim postdata As String = "format=json&action=edit&title=" & _title & additionalParameters & "&summary=" & UrlWebEncode(editSummary) & "&section=new" _
            & "&sectiontitle=" & sectionTitle & "&text=" & UrlWebEncode(text) & "&token=" & UrlWebEncode(GetEditToken())

            Dim postresult As String = _bot.POSTQUERY(postdata)
            System.Threading.Thread.Sleep(1000) 'Some time to the server to process the data
            Load() 'Update page data

            If postresult.Contains("""result"":""Success""") Then
                EventLogger.Log("Edit successful!", "LOCAL", _username)
                Return "Edit successful!"
            End If

            If postresult.Contains("abusefilter") Then
                EventLogger.Log("AbuseFilter Triggered!", "LOCAL", _username)
                Return "AbuseFilter Triggered"
            End If

            Return "True"
        End Function
        ''' <summary>
        ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
        ''' </summary>
        ''' <param name="SectionTitle">Título de la sección nueva</param>
        ''' <param name="text">Texto de la sección</param>
        ''' <param name="EditSummary">Resumen de edición</param>
        ''' <param name="IsMinor">¿Marcar como menor?</param>
        ''' <returns></returns>
        Overloads Function AddSection(ByVal sectionTitle As String, ByVal text As String, ByVal editSummary As String, ByVal isMinor As Boolean) As String
            Return AddSectionPage(sectionTitle, text, editSummary, isMinor)
        End Function
        ''' <summary>
        ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
        ''' </summary>
        ''' <param name="SectionTitle">Título de la sección nueva</param>
        ''' <param name="text">Texto de la sección</param>
        ''' <param name="EditSummary">Resumen de edición</param>
        ''' <returns></returns>
        Overloads Function AddSection(ByVal sectionTitle As String, ByVal text As String, ByVal editSummary As String) As String
            Return AddSectionPage(sectionTitle, text, editSummary, False)
        End Function
        ''' <summary>
        ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
        ''' </summary>
        ''' <param name="SectionTitle">Título de la sección nueva</param>
        ''' <param name="text">Texto de la sección</param>
        ''' <returns></returns>
        Overloads Function AddSection(ByVal sectionTitle As String, ByVal text As String) As String
            Return AddSectionPage(sectionTitle, text, "Bot edit", False)
        End Function

        ''' <summary>
        ''' Función que verifica si la página puede ser editada por bots (se llama desde Save() y es obligatoria)
        ''' </summary>
        ''' <param name="text">Texto de la página</param>
        ''' <param name="user">Usuario que edita</param>
        ''' <returns></returns>
        Public Function BotCanEdit(ByVal text As String, ByVal user As String) As Boolean
            If String.IsNullOrWhiteSpace(text) Then
                'Página nueva, por lo tanto pueden editarla bots
                Return True
            End If
            If String.IsNullOrWhiteSpace(user) Then
                Throw New ArgumentException("Parameter empty", "user")
            End If
            user = user.Normalize
            Return Not Regex.IsMatch(text, "\{\{(nobots|bots\|(allow=none|deny=(?!none).*(" & user & "|all)|optout=all))\}\}", RegexOptions.IgnoreCase)
        End Function

        ''' <summary>
        ''' Obtiene un Token de edición desde la API de MediaWiki
        ''' </summary>
        ''' <returns></returns>
        Private Function GetEditToken() As String
            Dim querytext As String = "format=json&action=query&meta=tokens"
            Dim queryresult As String = _bot.POSTQUERY(querytext)
            Dim token As String = TextInBetween(queryresult, """csrftoken"":""", """}}")(0).Replace("\\", "\")
            Return token
        End Function

        ''' <summary>
        ''' Hace una solicitud a la API respecto a una página y retorna un array con valores sobre ésta.
        ''' {Título de la página, ID de la página, Ultimo usuario que la editó,Fecha de última edición,Wikitexto de la página,tamaño de la página (en bytes)}
        ''' </summary>
        ''' <param name="Pagename">Título exacto de la página</param>
        Private Sub PageInfoData(ByVal pageName As String)

            Dim querystring As String = "format=json&maxlag=5&action=query&prop=revisions" & UrlWebEncode("|") & "pageimages" & UrlWebEncode("|") & "categories" & UrlWebEncode("|") & "extracts" & "&rvprop=user" &
            UrlWebEncode("|") & "timestamp" & UrlWebEncode("|") & "size" & UrlWebEncode("|") & "content" & UrlWebEncode("|") & "ids" & "&exlimit=1&explaintext&exintro&titles=" & UrlWebEncode(pageName)
            'Fix temporal, un BUG en la api de Mediawiki provoca que los extractos en solicitudes POST sean distintos a los de GET
            Dim QueryText As String = _bot.GETQUERY(querystring)

            Dim PageID As String = "-1"
            Dim PRevID As String = "-1"
            Dim User As String = ""
            Dim PTitle As String = NormalizeUnicodetext(TextInBetween(QueryText, """title"":""", """,")(0))
            Dim Timestamp As String = ""
            Dim Wikitext As String = ""
            Dim Size As String = "0"
            Dim WNamespace As String = TextInBetween(QueryText, """ns"":", ",")(0)
            Dim PCategories As New List(Of String)
            Dim PageImage As String = ""
            Dim PExtract As String = ""
            Dim Rootp As String = ""
            Try
                PageID = TextInBetween(QueryText, "{""pageid"":", ",""ns")(0)
                User = NormalizeUnicodetext(TextInBetween(QueryText, """user"":""", """,")(0))
                Timestamp = TextInBetween(QueryText, """timestamp"":""", """,")(0)
                Wikitext = NormalizeUnicodetext(TextInBetween(QueryText, """wikitext"",""*"":""", """}]")(0))
                Size = NormalizeUnicodetext(TextInBetween(QueryText, ",""size"":", ",""")(0))
                PRevID = TextInBetween(QueryText, """revid"":", ",""")(0)
                PExtract = NormalizeUnicodetext(TextInBetween(QueryText, """extract"":""", """}")(0))
            Catch ex As IndexOutOfRangeException
                EventLogger.Log("Warning: The page '" & pageName & "' doesn't exist yet!", "LOCAL", _username)
            End Try

            If TextInBetween(QueryText, """pageimage"":""", """").Count >= 1 Then
                PageImage = TextInBetween(QueryText, """pageimage"":""", """")(0)
            Else
                EventLogger.Debug_log("The page '" & pageName & "' doesn't have any thumbnail", "LOCAL", _username)
            End If

            For Each m As Match In Regex.Matches(QueryText, "title"":""[Cc][a][t][\S\s]+?(?=""})")
                PCategories.Add(NormalizeUnicodetext(m.Value.Replace("title"":""", "")))
            Next

            If Regex.Match(PTitle, "\/.+").Success Then
                Rootp = PTitle.Split("/"c)(0)
            Else
                Rootp = PTitle
            End If

            _rootPage = Rootp
            _title = PTitle
            _ID = Integer.Parse(PageID)
            _lastuser = User
            _timestamp = Timestamp
            _text = Wikitext
            _size = Integer.Parse(Size)
            _Namespace = Integer.Parse(WNamespace)
            _categories = PCategories.ToArray
            _currentRevID = Integer.Parse(PRevID)
            _extract = PExtract
            _thumbnail = PageImage

        End Sub

        ''' <summary>
        ''' Entrega la ultima marca de tiempo de la pagina.
        ''' </summary>
        ''' <param name="pagename">Nombre exacto de la pagina.</param>
        ''' <returns></returns>
        Function GetLastTimeStamp(ByVal pageName As String) As String
            Dim querystring As String = "format=json&maxlag=5&action=query&prop=revisions&rvprop=timestamp&titles=" & pageName
            Dim QueryText As String = _bot.POSTQUERY(querystring)
            Try
                Return TextInBetween(QueryText, """timestamp"":""", """")(0)
            Catch ex As IndexOutOfRangeException
                Return ""
            End Try

        End Function


        ''' <summary>
        ''' Evalua texto (wikicódigo) y regresa un array de string con cada uno de los hilos o secciones del mismo (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
        ''' </summary>
        ''' <param name="pagetext">Texto a evaluar</param>
        ''' <returns></returns>
        Function GetPageThreads(ByVal pageText As String) As String()
            Dim threads As New List(Of String)
            Dim newline As String = Environment.NewLine
            For Each m As Match In Regex.Matches(pageText, "(" & newline & "==(?!=))[\s\S]+?(?=" & newline & "==(?!=)|$)")
                threads.Add(m.Value)
            Next
            Return threads.ToArray
        End Function


        ''' <summary>
        ''' Retorna el promedio de los últimos dos meses de la página entregada (solo para páginas wikimedia)
        ''' El proyecto se deduce extrayendo el texto entre "https://" y ".org"
        ''' </summary>
        ''' <param name="Page">Nombre exacto de la página a evaluar</param>
        ''' <returns></returns>
        Private Function GetPageViewsAvg(ByVal page As String) As Integer
            Try
                Dim Project As String = TextInBetween(_siteurl, "https://", ".org")(0)
                Dim currentDate As DateTime = DateTime.Now
                Dim Month As Integer = currentDate.Month - 1
                Dim CurrentMonth As Integer = currentDate.Month
                Dim Year As Integer = currentDate.Year
                Dim Currentyear As Integer = currentDate.Year
                Dim FirstDay As String = "01"
                Dim LastDay As Integer = System.DateTime.DaysInMonth(Year, CurrentMonth)
                Dim Views As New List(Of Integer)
                Dim ViewAverage As Integer
                Dim totalviews As Integer

                If Month = 0 Then
                    Month = 12
                    Year = Year - 1
                End If

                Dim Url As String = String.Format("https://wikimedia.org/api/rest_v1/metrics/pageviews/per-article/{0}/all-access/all-agents/{1}/daily/{2}{4}{6}00/{3}{5}{7}00",
                              Project, page, Year, Currentyear, Month.ToString("00"), CurrentMonth.ToString("00"), FirstDay, LastDay)
                Dim response As String = _bot.GET(Url)

                For Each view As String In TextInBetween(response, """views"":", "}")
                    Views.Add(Integer.Parse(view))
                Next
                For Each i As Integer In Views
                    totalviews = totalviews + i
                Next
                ViewAverage = CInt((totalviews / (Views.Count - 1)))

                Return ViewAverage
            Catch ex As Exception
                Return 0
            End Try

        End Function
    End Class

End Namespace
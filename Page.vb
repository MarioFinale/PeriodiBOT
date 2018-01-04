Imports System.Net
Imports System.Text.RegularExpressions

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
    Private _cookies As CookieContainer

    ''' <summary>
    ''' Entrega el puntaje ORES {reverted,goodfaith} de la página.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ORESScores As Double()
        Get
            Return _ORESScores
        End Get
    End Property
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
    Public ReadOnly Property CurrentRevID As Integer
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
    ''' Entrega el timestamp de la edición actual de la página
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
    Public ReadOnly Property Sections As String()
        Get
            Return _sections
        End Get
    End Property
    ''' <summary>
    ''' Entrega las primeras 10 categorías de la página.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Categories As String()
        Get
            Return _categories
        End Get
    End Property
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
    Public Property PageNamespace As Integer
        Get
            Return _Namespace
        End Get
        Set(value As Integer)
            _Namespace = value
        End Set
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

    ''' <summary>
    ''' Inicializa una nueva página, por lo general no se llama de forma directa. Se puede obtener una página creandola con Bot.Getpage.
    ''' </summary>
    ''' <param name="PageTitle">Título exacto de la página</param>
    ''' <param name="site">Sitio de la página</param>
    ''' <param name="Cookies">Cookiecontainer con los permisos de usuario</param>
    ''' <param name="username">Nombre de usuario que realiza las ediciones</param>
    Public Sub New(ByVal PageTitle As String, ByVal site As String, ByRef Cookies As CookieContainer, ByVal username As String)
        Log("Loading page " & PageTitle, "", BOTName)
        _username = username
        Loadpage(PageTitle, site, Cookies)
    End Sub
    ''' <summary>
    ''' Inicializa de nuevo la página (al crear una página esta ya está inicializada).
    ''' </summary>
    Public Sub Load()
        Log("Loading page " & _title, "", BOTName)
        Loadpage(_title, _siteurl, _cookies)
    End Sub

    ''' <summary>
    ''' Inicializa la página, esta función no se llama de forma directa
    ''' </summary>
    ''' <param name="PageTitle">Título exacto de la página</param>
    ''' <param name="site">Sitio de la página</param>
    ''' <param name="Cookies">CookieContainer con loging del usuario</param>
    ''' <returns></returns>
    Private Function Loadpage(ByVal PageTitle As String, ByVal site As String, ByRef Cookies As CookieContainer) As Boolean
        Log("Obtaining server data of " & PageTitle, "", BOTName)
        If String.IsNullOrEmpty(PageTitle) Or String.IsNullOrEmpty(site) Then
            Throw New ArgumentNullException
        End If
        If Cookies Is Nothing Then
            Cookies = New CookieContainer
        End If
        _siteurl = site
        _cookies = Cookies
        Dim PageData As String() = PageInfoData(PageTitle)
        _title = PageData(0)
        _ID = Integer.Parse(PageData(1))
        _lastuser = PageData(2)
        _timestamp = PageData(3)
        _text = PageData(4)
        _size = Integer.Parse(PageData(5))
        _Namespace = Integer.Parse(PageData(6))
        _sections = GetPageThreads(_text)
        _categories = GetCategories(_title)

        _currentRevID = GetLastRevID(_title)
        _ORESScores = GetORESScores(_currentRevID)
        _pageViews = GetPageViewsAvg(_title)
        Log("Page " & PageTitle, " loaded", BOTName)
        Return True
    End Function

    ''' <summary>
    ''' Retorna el ultimo REVID (como integer) de la pagina indicada como integer. 
    ''' En caso de no existir la pagina, retorna -1 como REVID.
    ''' </summary>
    ''' <param name="Page_Name">Nombre exacto de la pagina.</param> 
    Private Function GetLastRevID(ByVal Page_Name As String) As Integer
        Log("GetLastRevID: Starting Wikipedia last RevisionID of page """ & Page_Name & """.", "LOCAL", BOTName)
        Page_Name = UrlWebEncode(Page_Name)
        Try
            Dim QueryText As String = String.Empty
            Log("GetLastRevID: Query of last RevisionID of page """ & Page_Name & """.", "LOCAL", BOTName)
            QueryText = Gethtmlsource((_siteurl & "?action=query&prop=revisions&format=json&titles=" & Page_Name), False, _cookies)

            Dim ID As Integer = CType(TextInBetween(QueryText, """revid"":", ",""")(0), Integer)
            Debug_Log("GetLastRevID: Query of last RevisionID of page """ & Page_Name & " successful, result: " & ID.ToString, "LOCAL", BOTName)
            Return ID
        Catch ex As Exception
            Debug_Log("GetLastRevID: Query of last RevisionID from page """ & Page_Name & " failed, returning Nothing", "LOCAL", BOTName)
            Debug_Log("GetLastRevID: ex message: " & ex.Message, "LOCAL", BOTName)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Retorna el valor ORES (en %) de un EDIT ID (eswiki) indicados como porcentaje en double. 
    ''' En caso de no existir el EDIT ID, retorna 0.
    ''' </summary>
    ''' <param name="revid">EDIT ID de la edicion a revisar</param>
    ''' <remarks>Los EDIT ID deben ser distintos</remarks>
    Private Function GetORESScores(ByVal revid As Integer) As Double()
        Debug_Log("GetORESScore: Query of ORES score from revid " & revid.ToString, "LOCAL", BOTName)
        Try
            Dim s As String = Gethtmlsource(("https://ores.wikimedia.org/v3/scores/eswiki/?models=damaging|goodfaith&format=json&revids=" & revid), False, _cookies)

            Dim DMGScore_str As String = TextInBetween(TextInBetweenInclusive(s, "{""damaging"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)
            Dim GoodFaithScore_str As String = TextInBetween(TextInBetweenInclusive(s, """goodfaith"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)
            Debug_Log("GetORESScore: Query of ORES score from revid done, Strings: GF: " & GoodFaithScore_str & " DMG:" & DMGScore_str, "LOCAL", BOTName)

            Dim DMGScore As Double = Math.Round((Double.Parse(DMGScore_str) * 100), 2)
            Dim GoodFaithScore As Double = Math.Round((Double.Parse(GoodFaithScore_str) * 100), 2)
            Debug_Log("GetORESScore: Query of ORES score from revid done, Double: GF: " & GoodFaithScore.ToString & " DMG:" & DMGScore.ToString, "LOCAL", BOTName)

            Return {DMGScore, GoodFaithScore}
        Catch ex As IndexOutOfRangeException
            Debug_Log("GetORESScore: Query of ORES score from revid " & revid.ToString & " failed, returning Nothing", "LOCAL", BOTName)
            Return Nothing
        Catch ex2 As Exception
            Debug_Log("GetORESScore: Query of ORES score from revid " & revid.ToString & " failed: " & ex2.Message, "LOCAL", BOTName)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Guarda la página en la wiki. Si la página no existe, la crea.
    ''' </summary>
    ''' <param name="text">Texto (wikicódigo) de la página</param>
    ''' <param name="EditSummary">Resumen de la edición</param>
    ''' <param name="IsMinor">¿Marcar como menor?</param>
    ''' <returns></returns>
    Private Function SavePage(ByVal text As String, ByVal EditSummary As String, ByVal IsMinor As Boolean, ByVal IsBot As Boolean) As String
        If String.IsNullOrEmpty(text) Or String.IsNullOrWhiteSpace(text) Then
            Throw New ArgumentNullException
        End If

        If Not PageInfoData(_title)(3) = _timestamp Then
            Console.WriteLine("Edit conflict")
            Return "Edit conflict"
        End If

        If Not BotCanEdit(_text, _username) Then
            Console.WriteLine("Bots can't edit this page!")
            Return "No Bots"
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
        Dim postresult As String = PostDataAndGetResult(_siteurl, postdata, True, _cookies)
        System.Threading.Thread.Sleep(1000) 'Some time to the server to process the data
        Load() 'Update page data

        If postresult.Contains("""result"":""Success""") Then
            Console.WriteLine("Edit successful!")
            Return "Edit successful!"
        End If

        If postresult.Contains("abusefilter") Then
            Console.WriteLine("AbuseFilter Triggered!")
            Debug_Log("ABUSEFILTER: " & postresult, "BOT", BOTName)
            Return "AbuseFilter Triggered"
        End If

        Return "True"
    End Function

    ''' <summary>
    ''' Guarda la página en la wiki. Si la página no existe, la crea.
    ''' </summary>
    ''' <param name="text">Texto (wikicódigo) de la página</param>
    ''' <param name="Summary">Resumen de la edición</param>
    ''' <param name="IsMinor">¿Marcar como menor?</param>
    ''' <returns></returns>
    Overloads Function Save(ByVal text As String, ByVal Summary As String, ByVal IsMinor As Boolean, ByVal IsBOT As Boolean) As String
        Return SavePage(text, Summary, IsMinor, IsBOT)
    End Function

    ''' <summary>
    ''' Guarda la página en la wiki. Si la página no existe, la crea.
    ''' </summary>
    ''' <param name="text">Texto (wikicódigo) de la página</param>
    ''' <param name="Summary">Resumen de la edición</param>
    ''' <param name="IsMinor">¿Marcar como menor?</param>
    ''' <returns></returns>
    Overloads Function Save(ByVal text As String, ByVal Summary As String, ByVal IsMinor As Boolean) As String
        Return SavePage(text, Summary, IsMinor, False)
    End Function

    ''' <summary>
    ''' Guarda la página en la wiki. Si la página no existe, la crea.
    ''' </summary>
    ''' <param name="text">Texto (wikicódigo) de la página</param>
    ''' <param name="Summary">Resumen de la edición</param>
    ''' <returns></returns>
    Overloads Function Save(ByVal Text As String, ByVal Summary As String) As String
        Return SavePage(Text, Summary, False, False)
    End Function

    ''' <summary>
    ''' Guarda la página en la wiki. Si la página no existe, la crea.
    ''' </summary>
    ''' <param name="text">Texto (wikicódigo) de la página</param>
    ''' <returns></returns>
    Overloads Function Save(ByVal Text As String) As String
        Return SavePage(Text, "Bot edit", False, False)
    End Function

    ''' <summary>
    ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
    ''' </summary>
    ''' <param name="SectionTitle">Título de la sección nueva</param>
    ''' <param name="text">Texto de la sección</param>
    ''' <param name="EditSummary">Resumen de edición</param>
    ''' <param name="IsMinor">¿Marcar como menor?</param>
    ''' <returns></returns>
    Private Function AddSectionPage(ByVal SectionTitle As String, ByVal text As String, ByVal EditSummary As String, ByVal IsMinor As Boolean) As String
        If String.IsNullOrEmpty(text) Or String.IsNullOrWhiteSpace(text) Or String.IsNullOrWhiteSpace(SectionTitle) Then
            Throw New ArgumentNullException
        End If

        If Not PageInfoData(_title)(3) = _timestamp Then
            Console.WriteLine("Edit conflict")
            Return "Edit conflict"
        End If

        If Not BotCanEdit(_text, _username) Then
            Console.WriteLine("Bots can't edit this page!")
            Return "No Bots"
        End If

        Dim postdata As String = "format=json&action=edit&title=" & _title & "&summary=" & UrlWebEncode(EditSummary) & "&section=new" _
            & "&sectiontitle=" & SectionTitle & "&text=" & UrlWebEncode(text) & "&token=" & UrlWebEncode(GetEditToken())

        Dim postresult As String = PostDataAndGetResult(_siteurl, postdata, True, _cookies)
        System.Threading.Thread.Sleep(1000) 'Some time to the server to process the data
        Load() 'Update page data

        If postresult.Contains("""result"":""Success""") Then
            Console.WriteLine("Edit successful!")
            Return "Edit successful!"
        End If

        If postresult.Contains("abusefilter") Then
            Console.WriteLine("AbuseFilter Triggered!")
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
    Overloads Function AddSection(ByVal SectionTitle As String, ByVal text As String, ByVal EditSummary As String, ByVal IsMinor As Boolean) As String
        Return AddSectionPage(SectionTitle, text, EditSummary, IsMinor)
    End Function
    ''' <summary>
    ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
    ''' </summary>
    ''' <param name="SectionTitle">Título de la sección nueva</param>
    ''' <param name="text">Texto de la sección</param>
    ''' <param name="EditSummary">Resumen de edición</param>
    ''' <returns></returns>
    Overloads Function AddSection(ByVal SectionTitle As String, ByVal text As String, ByVal EditSummary As String) As String
        Return AddSectionPage(SectionTitle, text, EditSummary, False)
    End Function
    ''' <summary>
    ''' Añade una sección nueva a una página dada. Útil en casos como messagedelivery.
    ''' </summary>
    ''' <param name="SectionTitle">Título de la sección nueva</param>
    ''' <param name="text">Texto de la sección</param>
    ''' <returns></returns>
    Overloads Function AddSection(ByVal SectionTitle As String, ByVal text As String) As String
        Return AddSectionPage(SectionTitle, text, "Bot edit", False)
    End Function

    ''' <summary>
    ''' Función que verifica si la página puede ser editada por bots (se llama desde Save() y es obligatoria)
    ''' </summary>
    ''' <param name="text">Texto de la página</param>
    ''' <param name="user">Usuario que edita</param>
    ''' <returns></returns>
    Public Function BotCanEdit(ByVal text As String, ByVal user As String) As Boolean
        user = user.Normalize
        Return Not Regex.IsMatch(text, "\{\{(nobots|bots\|(allow=none|deny=(?!none).*(" & user & "|all)|optout=all))\}\}", RegexOptions.IgnoreCase)
    End Function

    ''' <summary>
    ''' Obtiene un Token de edición desde la API de MediaWiki
    ''' </summary>
    ''' <returns></returns>
    Private Function GetEditToken() As String

        Dim querytext As String = "format=json&action=query&meta=tokens"
        Dim queryresult As String = PostDataAndGetResult(_siteurl, querytext, True, _cookies)
        Dim token As String = TextInBetween(queryresult, """csrftoken"":""", """}}")(0).Replace("\\", "\")
        Return token
    End Function

    ''' <summary>
    ''' HAce una solicitud a la API respecto a una página y retorna un array con valores sobre ésta.
    ''' {Título de la página, ID de la página, Ultimo usuario que la editó,Fecha de última edición,Wikitexto de la página,tamaño de la página (en bytes)}
    ''' </summary>
    ''' <param name="Pagename">Título exacto de la página</param>
    ''' <returns></returns>
    Private Function PageInfoData(ByVal Pagename As String) As String()

        Dim querystring As String = "format=json&maxlag=5&action=query&prop=revisions&rvprop=user" & UrlWebEncode("|") & "timestamp" & UrlWebEncode("|") & "size" & UrlWebEncode("|") & "content" & "&titles=" & UrlWebEncode(Pagename)
        Dim QueryText As String = PostDataAndGetResult(_siteurl, querystring, False, _cookies)

        Dim PageID As String = "-1"
        Dim User As String = ""
        Dim PTitle As String = NormalizeUnicodetext(TextInBetween(QueryText, """title"":""", """,")(0))
        Dim Timestamp As String = ""
        Dim Wikitext As String = ""
        Dim Size As String = "0"
        Dim WNamespace As String = TextInBetween(QueryText, """ns"":", ",")(0)

        Try
            PageID = TextInBetween(QueryText, "{""pageid"":", ",""ns")(0)
            User = TextInBetween(QueryText, "{""user"":""", """,")(0)
            Timestamp = TextInBetween(QueryText, """timestamp"":""", """,")(0)
            Wikitext = NormalizeUnicodetext(TextInBetween(QueryText, """wikitext"",""*"":""", """}]}}}}")(0))
            Size = NormalizeUnicodetext(TextInBetween(QueryText, ",""size"":", ",""")(0))
        Catch ex As IndexOutOfRangeException
            Console.WriteLine("Warning: The page '" & Pagename & "' doesn't exist yet!")
        End Try

        Return {PTitle, PageID, User, Timestamp, Wikitext, Size, WNamespace}


    End Function

    ''' <summary>
    ''' Evalua texto (wikicódigo) y regresa un array de string con cada uno de los hilos o secciones del mismo (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
    ''' </summary>
    ''' <param name="pagetext">Texto a evaluar</param>
    ''' <returns></returns>
    Function GetPageThreads(ByVal pagetext As String) As String()
        Dim threads As New List(Of String)
        Dim newline As String = Environment.NewLine
        For Each m As Match In Regex.Matches(pagetext, "(" & newline & "==(?!=))[\s\S]+?(?=" & newline & "==(?!=)|$)")
            threads.Add(m.Value)
        Next
        Return threads.ToArray
    End Function

    ''' <summary>
    ''' Entrega las primeras 10 categorías de la página
    ''' </summary>
    ''' <param name="Page">Título exacto de la página</param>
    ''' <returns></returns>
    Private Function GetCategories(ByVal Page As String) As String()
        Try
            Dim querystring As String = "format=json&action=query&prop=categories&titles=" & UrlWebEncode(Page)
            Dim QueryText As String = PostDataAndGetResult(_siteurl, querystring, False, _cookies)
            Dim Cats As New List(Of String)
            For Each m As Match In Regex.Matches(QueryText, "title"":""[Cc][a][t][\S\s]+?(?=""})")
                Cats.Add(NormalizeUnicodetext(m.Value.Replace("title"":""", "")))
            Next
            Return Cats.ToArray
        Catch ex As IndexOutOfRangeException
            Return {"No cats"}
        Catch exex As Exception
            Throw New Exception("Unknown error")
        End Try

    End Function

    ''' <summary>
    ''' Retorna el promedio de los últimos dos meses de la página entregada (solo para páginas wikimedia)
    ''' El proyecto se deduce extrayendo el texto entre "https://" y ".org"
    ''' </summary>
    ''' <param name="Page">Nombre exacto de la página a evaluar</param>
    ''' <returns></returns>
    Private Function GetPageViewsAvg(ByVal Page As String) As Integer
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
                              Project, Page, Year, Currentyear, Month, CurrentMonth, FirstDay, LastDay)
            Dim response As String = GetDataAndResult(Url, False)

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



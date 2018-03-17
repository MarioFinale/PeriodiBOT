Option Strict On
Option Explicit On
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Namespace WikiBot
    Public Class Bot

        Private BotCookies As CookieContainer
        Private _userName As String = String.Empty
        Private _siteurl As String = String.Empty
        Private Api As ApiSession
        Private signpattern As String = "([0-9]{2}):([0-9]{2}) ([0-9]{2}|[0-9]) ([Z-z]{3}) [0-9]{4}( \([A-z]{3,4}\))*"
#Region "Properties"
        Public ReadOnly Property Bot As Boolean
            Get
                Return IsBot()
            End Get
        End Property

        Public ReadOnly Property Siteurl As String
            Get
                Return _siteurl
            End Get
        End Property

        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property
#End Region

        Sub New(ByVal botUserName As String, botPassword As String, pageURL As String)
            Api = New ApiSession(botUserName, botPassword, pageURL)
            _userName = Api.UserName
            _siteurl = Api.Siteurl
        End Sub

        Function POSTQUERY(ByVal postdata As String) As String
            Return Api.POSTQUERY(postdata)
        End Function

        Function GETQUERY(ByVal getdata As String) As String
            Return Api.GETQUERY(getdata)
        End Function

        Function [GET](ByVal url As String) As String
            Return Api.GET(url)
        End Function

        Function IsBot() As Boolean
            Dim postdata As String = "action=query&assert=bot&format=json"
            Dim postresponse As String = POSTQUERY(postdata)
            If postresponse.Contains("assertbotfailed") Then
                Return False
            Else
                Return True
            End If
        End Function

        Function IsLoggedIn() As Boolean
            Dim postdata As String = "action=query&assert=user&format=json"
            Dim postresponse As String = POSTQUERY(postdata)
            If postresponse.Contains("assertuserfailed") Then
                Return False
            Else
                Return True
            End If
        End Function

        Function GetSpamListregexes(ByVal spamlistPage As Page) As String()
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
            Log("GetRandomPage: Starting query of random page.", "LOCAL", BOTName)
            Try
                Dim QueryText As String = String.Empty
                QueryText = GETQUERY("action=query&format=json&list=random&rnnamespace=0&rnlimit=10")

                Dim plist As New List(Of String)

                For Each s As String In TextInBetween(QueryText, """title"":""", """}")
                    Log("GetRandomPage: Found """ & s & """.", "LOCAL", BOTName)
                    plist.Add(NormalizeUnicodetext(s))
                Next
                Log("GetRandomPage: Ended.", "LOCAL", BOTName)
                Return plist.ToArray
            Catch ex As Exception
                Debug_Log("GetRandomPage: ex message: " & ex.Message, "LOCAL", BOTName)
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
            Debug_Log("GetLastRevIDs: Get Wikipedia last RevisionID of """ & pageNames.Count.ToString & """ pages.", "LOCAL", BOTName)
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
            Debug_Log("GetLastRevIDs: Done """ & PagenameAndLastId.Count.ToString & """ pages returned.", "LOCAL", BOTName)
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

                            Debug_Log("GetORESScore: Server error in query of ORES score from revid " & EditID_str & " (invalid diff?)", "LOCAL", BOTName)
                            EditAndScoreList.Add(EditID, {0, 0})
                        Else
                            Try
                                Dim DMGScore_str As String = TextInBetween(TextInBetweenInclusive(s, "{""damaging"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)
                                Dim GoodFaithScore_str As String = TextInBetween(TextInBetweenInclusive(s, """goodfaith"": {""score"":", "}}}")(0), """true"": ", "}}}")(0).Replace(".", DecimalSeparator)

                                Debug_Log("GetORESScore: Query of ORES score from revid done, Strings: GF: " & GoodFaithScore_str & " DMG:" & DMGScore_str, "LOCAL", BOTName)

                                Dim DMGScore As Double = Double.Parse(DMGScore_str) * 100
                                Dim GoodFaithScore As Double = Double.Parse(GoodFaithScore_str) * 100

                                Debug_Log("GetORESScore: Query of ORES score from revid done, Double: GF: " & GoodFaithScore.ToString & " DMG:" & DMGScore.ToString, "LOCAL", BOTName)

                                EditAndScoreList.Add(EditID, {DMGScore, GoodFaithScore})
                            Catch ex As IndexOutOfRangeException
                                Debug_Log("GetORESScore: IndexOutOfRange EX in ORES score from revid " & EditID_str & " EX: " & ex.Message, "LOCAL", BOTName)
                                EditAndScoreList.Add(EditID, {0, 0})
                            Catch ex2 As Exception
                                Debug_Log("GetORESScore: EX in ORES score from revid " & EditID_str & " EX: " & ex2.Message, "LOCAL", BOTName)
                                EditAndScoreList.Add(EditID, {0, 0})
                            End Try
                        End If

                    Next
                Catch ex As Exception
                    Debug_Log("GetORESScore: EX obttaining ORES scores EX: " & ex.Message, "LOCAL", BOTName)
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
            Log("Get Wikipedia page extracts on chunks", "LOCAL", BOTName)
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
                Debug_Log("BOTGetPagesExtract EX: " & ex.Message, "LOCAL", BOTName)
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
        ''' Elimina una referencia que contenga una cadena exacta.
        ''' (No usar a menos de que se esté absolutamente seguro de lo que se hace).
        ''' </summary>
        ''' <param name="RequestedPage">Página a revisar</param>
        ''' <param name="RequestedRef">Texto que determina que referencia se elimina</param>
        ''' <returns></returns>
        Function RemoveRef(ByVal requestedPage As Page, requestedRef As String) As Boolean
            If String.IsNullOrWhiteSpace(requestedRef) Then
                Return False
            End If
            If requestedPage Is Nothing Then
                Return False
            End If
            Dim pageregex As String = String.Empty
            Dim PageText As String = requestedPage.Text
            For Each c As Char In requestedRef
                pageregex = pageregex & "[" & c.ToString.ToUpper & c.ToString.ToLower & "]"
            Next

            If Not (requestedPage.Title.ToLower.Contains("usuario:") Or requestedPage.Title.ToLower.Contains("wikipedia:") Or
            requestedPage.Title.ToLower.Contains("usuaria:") Or requestedPage.Title.ToLower.Contains("especial:") Or
             requestedPage.Title.ToLower.Contains("wikiproyecto:") Or requestedPage.Title.ToLower.Contains("discusión:") Or
              requestedPage.Title.ToLower.Contains("discusion:")) Then


                For Each m As Match In Regex.Matches(PageText, "(<[REFref]+>)([^<]+?)" & pageregex & ".+?(<[/REFref]+>)")
                    PageText = PageText.Replace(m.Value, "")
                Next

                Try
                    requestedPage.CheckAndSave(PageText, "(Bot): Removiendo referencias que contengan '" & requestedRef & "' según solicitud", False, True)
                    Return True
                Catch ex As Exception
                    Return False
                End Try

            Else
                Return False
            End If

        End Function

        ''' <summary>
        ''' Evalua texto (wikicódigo) y regresa un array de string con cada uno de los hilos del mismo (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
        ''' </summary>
        ''' <param name="pagetext">Texto a evaluar</param>
        ''' <returns></returns>
        Function GetPageThreads(ByVal pagetext As String) As String()
            Dim newline As String = Environment.NewLine
            Dim temptext As String = pagetext

            Dim commentMatch As MatchCollection = Regex.Matches(temptext, "(<!--)[\s\S]*?(-->)")

            Dim CommentsList As New List(Of String)
            For i As Integer = 0 To commentMatch.Count - 1
                CommentsList.Add(commentMatch(i).Value)
                temptext = temptext.Replace(commentMatch(i).Value, ColoredText("PERIODIBOT::::COMMENTREPLACE::::" & i, "04"))
            Next

            Dim mc As MatchCollection = Regex.Matches(temptext, "[\n\r]((==(?!=)).+?(==(?!=)))")

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
                    Dim commenttext As String = ColoredText("PERIODIBOT::::COMMENTREPLACE::::" & i, "04")
                    nthreadtext = nthreadtext.Replace(commenttext, CommentsList(i))
                Next
                EndThreadList.Add(nthreadtext)
            Next

            Return EndThreadList.ToArray
        End Function
        ''' <summary>
        ''' Entrega como DateTime la última fecha (formato firma Wikipedia) en el último parrafo. Si no encuentra firma retorna 31/12/9999.
        ''' </summary>
        ''' <param name="text">Texto a evaluar</param>
        ''' <returns></returns>
        Function LastParagraphDateTime(ByVal text As String) As DateTime
            If String.IsNullOrEmpty(text) Then
                Throw New ArgumentException("Empty var", "text")
            End If
            text = text.Trim(CType(vbCrLf, Char())) & " "
            Dim lastparagraph As String = Regex.Match(text, ".+[\s\s]+(?===.+==|$)").Value
            Dim matchc As MatchCollection = Regex.Matches(lastparagraph, signpattern)

            If matchc.Count = 0 And Not (((lastparagraph(0) = ";"c) Or (lastparagraph(0) = ":"c) Or (lastparagraph(0) = "*"c) Or (lastparagraph(0) = "#"c))) Then
                Dim mlines As MatchCollection = Regex.Matches(text, ".+\n")
                For i As Integer = mlines.Count - 1 To 0 Step -1

                    If i = (mlines.Count - 1) Then
                        If Not ((mlines(i).Value(0) = ";"c) Or (mlines(i).Value(0) = ":"c) Or (mlines(i).Value(0) = "*"c) Or (mlines(i).Value(0) = "#"c)) Then
                            If Regex.Match(mlines(i).Value, signpattern).Success Then
                                lastparagraph = mlines(i).Value
                                Exit For
                            End If
                        Else
                            Exit For
                        End If
                    Else
                        If Not ((mlines(i).Value(0) = ";"c) Or (mlines(i).Value(0) = ":"c) Or (mlines(i).Value(0) = "*"c) Or (mlines(i).Value(0) = "#"c)) Then
                            If Regex.Match(mlines(i).Value, signpattern).Success Then
                                lastparagraph = mlines(i).Value
                                Exit For
                            End If
                        Else
                            Exit For
                        End If
                    End If
                Next

            End If

            Dim TheDate As DateTime = ESWikiDatetime(lastparagraph)
            Debug_Log("LastParagraphDateTime: Returning " & TheDate.ToString, "LOCAL", BOTName)
            Return TheDate
        End Function

        ''' <summary>
        ''' Entrega 
        ''' </summary>
        ''' <param name="text">Entrega la ultima fecha, que aparezca en un texto dado (si la fecha tiene formato de firma wikipedia).</param>
        ''' <returns></returns>
        Function ESWikiDatetime(ByVal text As String) As DateTime
            Dim TheDate As DateTime = Nothing
            Dim matchc As MatchCollection = Regex.Matches(text, signpattern)

            If matchc.Count = 0 Then
                EX_Log("No date match", "ESWikiDateTime", BOTName)
                Return DateTime.Parse("23:59 31/12/9999")
            End If

            For Each m As Match In matchc
                Try
                    Dim parsedtxt As String = m.Value.Replace(" "c, "/"c)
                    parsedtxt = parsedtxt.Replace(":"c, "/"c)
                    parsedtxt = parsedtxt.ToLower.Replace("ene", "01").Replace("feb", "02") _
                .Replace("mar", "03").Replace("abr", "04").Replace("may", "05") _
                .Replace("jun", "06").Replace("jul", "07").Replace("ago", "08") _
                .Replace("sep", "09").Replace("oct", "10").Replace("nov", "11") _
                .Replace("dic", "12")

                    parsedtxt = Regex.Replace(parsedtxt, "([^0-9/])", "")
                    Dim dates As New List(Of Integer)
                    For Each s As String In parsedtxt.Split("/"c)
                        If Not String.IsNullOrWhiteSpace(s) Then
                            dates.Add(Integer.Parse(s))
                        End If
                    Next

                    Dim dat As New DateTime(dates(4), dates(3), dates(2), dates(0), dates(1), 0)
                    TheDate = dat
                    Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """" & " to """ & dat.ToShortDateString & """", "LOCAL", BOTName)
                Catch ex As System.FormatException
                    Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions", BOTName)
                End Try

            Next
            Return TheDate

        End Function


        ''' <summary>
        ''' Entrega como array de DateTime todas las fechas (Formato de firma Wikipedia) en el texto dado.
        ''' </summary>
        ''' <param name="text">Texto a evaluar</param>
        ''' <returns></returns>
        Function AllDateTimes(ByVal text As String) As DateTime()
            Dim Datelist As New List(Of DateTime)
            For Each m As Match In Regex.Matches(text, signpattern)
                Dim TheDate As DateTime = ESWikiDatetime(m.Value)
                Datelist.Add(TheDate)
                Log("AllDateTimes: Adding " & TheDate.ToString, "LOCAL", BOTName)
            Next
            Return Datelist.ToArray
        End Function

        ''' <summary>
        ''' Entrega como DateTime la fecha más reciente en el texto dado (en formato de firma wikipedia).
        ''' </summary>
        ''' <param name="text"></param>
        ''' <returns></returns>
        Function MostRecentDate(ByVal text As String) As DateTime
            Dim dates As New List(Of DateTime)
            Dim matchc As MatchCollection = Regex.Matches(text, signpattern)

            If matchc.Count = 0 Then
                EX_Log("No date match", "ESWikiDateTime", BOTName)
                Return DateTime.Parse("23:59 31/12/9999")
            End If

            For Each m As Match In matchc
                Try
                    Dim parsedtxt As String = m.Value.Replace(" "c, "/"c)
                    parsedtxt = parsedtxt.Replace(":"c, "/"c)
                    parsedtxt = parsedtxt.ToLower.Replace("ene", "01").Replace("feb", "02") _
                .Replace("mar", "03").Replace("abr", "04").Replace("may", "05") _
                .Replace("jun", "06").Replace("jul", "07").Replace("ago", "08") _
                .Replace("sep", "09").Replace("oct", "10").Replace("nov", "11") _
                .Replace("dic", "12")

                    parsedtxt = Regex.Replace(parsedtxt, "([^0-9/])", "")
                    Dim datesInt As New List(Of Integer)
                    For Each s As String In parsedtxt.Split("/"c)
                        If Not String.IsNullOrWhiteSpace(s) Then
                            datesInt.Add(Integer.Parse(s))
                        End If
                    Next
                    Dim dat As New DateTime(datesInt(4), datesInt(3), datesInt(2), datesInt(0), datesInt(1), 0)
                    dates.Add(dat)
                    Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """" & " to """ & dat.ToShortDateString & """", "LOCAL", BOTName)
                Catch ex As System.FormatException
                    Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions", BOTName)
                End Try

            Next
            dates.Sort()
            Return dates.Last

        End Function


        ''' <summary>
        ''' Entrega como DateTime la fecha más reciente en el texto dado (en formato de firma wikipedia).
        ''' </summary>
        ''' <param name="text"></param>
        ''' <returns></returns>
        Function FirstDate(ByVal text As String) As DateTime
            Dim dates As New List(Of DateTime)
            Dim matchc As MatchCollection = Regex.Matches(text, signpattern)

            If matchc.Count = 0 Then
                EX_Log("No date match", "ESWikiDateTime", BOTName)
                Return New DateTime(9999, 12, 31, 23, 59, 59)
            End If

            For Each m As Match In matchc
                Try
                    Dim parsedtxt As String = m.Value.Replace(" "c, "/"c)
                    parsedtxt = parsedtxt.Replace(":"c, "/"c)
                    parsedtxt = parsedtxt.ToLower.Replace("ene", "01").Replace("feb", "02") _
                .Replace("mar", "03").Replace("abr", "04").Replace("may", "05") _
                .Replace("jun", "06").Replace("jul", "07").Replace("ago", "08") _
                .Replace("sep", "09").Replace("oct", "10").Replace("nov", "11") _
                .Replace("dic", "12")

                    parsedtxt = Regex.Replace(parsedtxt, "([^0-9/])", "")
                    Dim datesInt As New List(Of Integer)
                    For Each s As String In parsedtxt.Split("/"c)
                        If Not String.IsNullOrWhiteSpace(s) Then
                            datesInt.Add(Integer.Parse(s))
                        End If
                    Next
                    Dim dat As New DateTime(datesInt(4), datesInt(3), datesInt(2), datesInt(0), datesInt(1), 0)
                    dates.Add(dat)
                    Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """" & " to """ & dat.ToShortDateString & """", "LOCAL", BOTName)
                Catch ex As System.FormatException
                    Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions", BOTName)
                End Try

            Next
            dates.Sort()
            Return dates.First

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
        ''' Retorna un array de tipo string con todas las páginas donde la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="ThePage">Página que se llama.</param>
        Function GetallInclusions(ByVal ThePage As Page) As String()
            Return GetallInclusions(ThePage.Title)
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
        ''' <param name="ThePage">Página que se llama.</param>
        Function GetallInclusionsPages(ByVal ThePage As Page) As Page()
            Return GetallInclusionsPages(ThePage.Title)
        End Function

        ''' <summary>
        ''' Retorna un elemento Page coincidente al nombre entregado como parámetro.
        ''' </summary>
        ''' <param name="pageName">Nombre exacto de la página</param>
        Function Getpage(ByVal pageName As String) As Page
            Return New Page(pageName, _siteurl, Me, _userName)
        End Function


    End Class








    Class ApiSession

        Private ApiCookies As CookieContainer
        Private _userName As String = String.Empty

        Private _botusername As String = String.Empty
        Private _botpass As String = String.Empty
        Private _siteurl As String = String.Empty
        Private UserAgent As String = BOTName & " (http://es.wikipedia.org/wiki/Usuario_discusión:MarioFinale)"

#Region "Properties"
        Public ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public ReadOnly Property Siteurl As String
            Get
                Return _siteurl
            End Get
        End Property
#End Region

        ''' <summary>
        ''' Inicializa una nueva instancia del BOT.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="PageURL">Nombre exacto de la página</param>
        Sub New(ByVal botUserName As String, botPassword As String, pageURL As String)
            If String.IsNullOrWhiteSpace(botUserName) Then
                Throw New ArgumentException("No username")
            End If
            If String.IsNullOrWhiteSpace(botPassword) Then
                Throw New ArgumentException("No BotPassword")
            End If
            If String.IsNullOrWhiteSpace(pageURL) Then
                Throw New ArgumentException("No PageURL")
            End If
            _botusername = botUserName
            _botpass = botPassword
            _siteurl = pageURL
            ApiCookies = New CookieContainer
            LogOn()
            _userName = botUserName.Split("@"c)(0).Trim()
        End Sub

        ''' <summary>
        ''' Obtiene un Token y cookies de ingreso, establece las cookies de la clase y retorna el token como string.
        ''' </summary>
        Private Function GetWikiToken() As String
            Log("Obtaining token...", "LOCAL", BOTName)
            Dim postdata As String = "action=query&meta=tokens&type=login&format=json"
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, ApiCookies)
            Dim token As String = TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Log("Token obtained!", "LOCAL", BOTName)
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Public Function LogOn() As String
            Log("Signing in...", "LOCAL", BOTName)
            Dim token As String = String.Empty
            Dim url As String = _siteurl
            Dim postdata As String = String.Empty
            Dim postresponse As String = String.Empty
            Dim lresult As String = String.Empty
            Dim exitloop As Boolean = False

            Do Until exitloop
                Try
                    token = GetWikiToken()
                    postdata = "action=login&format=json&lgname=" & _botusername & "&lgpassword=" & _botpass & "&lgdomain=" & "&lgtoken=" & UrlWebEncode(token)
                    postresponse = PostDataAndGetResult(url, postdata, True, ApiCookies)
                    lresult = TextInBetween(postresponse, "{""result"":""", """,")(0)
                    Log("Login result: " & lresult, "LOCAL", BOTName)
                    Dim lUserID As String = TextInBetween(postresponse, """lguserid"":", ",")(0)
                    Log("UserID: " & lUserID, "LOCAL", BOTName)
                    Dim lUsername As String = TextInBetween(postresponse, """lgusername"":""", """}")(0)
                    Log("Username: " & lUsername, "LOCAL", BOTName)
                    Return lresult
                Catch ex As IndexOutOfRangeException
                    Log("Logon error", "LOCAL", BOTName)
                    If lresult.ToLower = "failed" Then
                        Dim reason As String = TextInBetween(postresponse, """reason"":""", """")(0)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.WriteLine("Login Failed")
                        Console.WriteLine("Reason: " & reason)
                        Console.WriteLine(Environment.NewLine & Environment.NewLine)
                        Console.Write("Press any key to exit...")
                        Console.ReadKey()
                        ExitProgram()
                        Return lresult
                    End If
                    Return lresult
                Catch ex2 As System.Net.WebException
                    Log("Network error", "LOCAL", BOTName)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Dim reason As String = ex2.Message
                    Console.WriteLine("Login Failed (Network error)")
                    Console.WriteLine(reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                Catch ex3 As Exception
                    Log("Logon error", "LOCAL", BOTName)
                    Debug_Log("Logon error: " & ex3.Message, "LOCAL", BOTName)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Dim reason As String = ex3.Message
                    Console.WriteLine("Login Failed")
                    Console.WriteLine("Reason: " & reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                End Try
                exitloop = PressKeyTimeout()
            Loop
            ExitProgram()
            Return lresult
        End Function

        Public Function POSTQUERY(ByVal postdata As String) As String
            Dim postresponse As String = PostDataAndGetResult(_siteurl, postdata, True, ApiCookies)
            Return postresponse
        End Function

        Public Function GETQUERY(ByVal getdata As String) As String
            Dim getresponse As String = GetDataAndResult(_siteurl & "?" & getdata, True, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal urlstring As String) As String
            If String.IsNullOrWhiteSpace(urlstring) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(urlstring, True, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function [GET](ByVal url As Uri) As String
            If String.IsNullOrWhiteSpace(url.ToString) Then
                Return String.Empty
            End If
            Dim getresponse As String = GetDataAndResult(url.ToString, True, ApiCookies)
            Return getresponse
        End Function

        Public Overloads Function POST(ByVal urlstring As String, ByVal postdata As String) As String
            If String.IsNullOrWhiteSpace(urlstring) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postdata) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(urlstring, postdata, True, ApiCookies)
            Return postresponse
        End Function

        Public Overloads Function POST(ByVal url As Uri, ByVal postdata As String) As String
            If String.IsNullOrWhiteSpace(url.ToString) Then
                Return String.Empty
            End If
            If String.IsNullOrWhiteSpace(postdata) Then
                Return String.Empty
            End If
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, ApiCookies)
            Return postresponse
        End Function

        ''' <summary>
        ''' Limpia todas las cookies, retorna "true" si finaliza correctamente.
        ''' </summary>
        Public Function CleanCookies() As Boolean
            Try
                ApiCookies = New CookieContainer
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
        Public Function GetDataAndResult(ByVal pageURL As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String
            If Cookies Is Nothing Then
                Cookies = New CookieContainer
            End If

            Dim tempcookies As CookieContainer = Cookies

            Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageURL), HttpWebRequest)
            postreq.Method = "GET"
            postreq.KeepAlive = True
            postreq.CookieContainer = tempcookies
            postreq.UserAgent = UserAgent
            postreq.ContentType = "application/x-www-form-urlencoded"
            Dim postresponse As HttpWebResponse
            postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
            tempcookies.Add(postresponse.Cookies)
            If getCookies Then
                Cookies = tempcookies
            End If
            Dim postreqreader As New StreamReader(postresponse.GetResponseStream())
            Return postreqreader.ReadToEnd

        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURI">URI absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
        Public Function PostDataAndGetResult(pageURI As Uri, postData As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String
            Return PostDataAndGetResult(pageURI.ToString, postData, getCookies, Cookies)
        End Function

        ''' <summary>Realiza una solicitud de tipo POST a un recurso web y retorna el texto.</summary>
        ''' <param name="pageURL">URL absoluta del recurso web.</param>
        ''' <param name="postData">Cadena de texto que se envia en el POST.</param>
        ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
        Public Function PostDataAndGetResult(pageURL As String, postData As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String

            If String.IsNullOrEmpty(pageURL) Then
                Throw New ArgumentNullException("pageURL", "No URL specified.")
            End If

            If Cookies Is Nothing Then
                Cookies = New CookieContainer
            End If

            Dim tempcookies As CookieContainer = Cookies

            Dim encoding As New UTF8Encoding
            Dim byteData As Byte() = encoding.GetBytes(postData)
            Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageURL), HttpWebRequest)

            postreq.Method = "POST"
            postreq.KeepAlive = True
            postreq.CookieContainer = tempcookies
            postreq.UserAgent = UserAgent
            postreq.ContentType = "application/x-www-form-urlencoded"
            postreq.ContentLength = byteData.Length

            Dim postreqstream As Stream = postreq.GetRequestStream()
            postreqstream.Write(byteData, 0, byteData.Length)
            postreqstream.Close()

            Dim postresponse As HttpWebResponse
            postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
            tempcookies.Add(postresponse.Cookies)
            If getCookies Then
                Cookies = tempcookies
            End If
            Dim postreqreader As New StreamReader(postresponse.GetResponseStream())
            Return postreqreader.ReadToEnd


        End Function



    End Class






End Namespace
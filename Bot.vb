Option Strict On
Imports System.Net
Imports System.Text.RegularExpressions

Namespace WikiBot
    Public Class Bot

        Private BotCookies As CookieContainer
        Private Username As String = String.Empty
        Private _botusername As String = String.Empty
        Private _botpass As String = String.Empty
        Private _siteurl As String = String.Empty
        Private ResumePageName As String = "Usuario:PeriodiBOT/Resumen página"

        ''' <summary>
        ''' Inicializa una nueva instancia del BOT.
        ''' </summary>
        ''' <param name="BotUsername">Nombre de usuario del bot</param>
        ''' <param name="BotPassword">Contraseña del bot (solo botpassword), más información ver https://www.mediawiki.org/wiki/Manual:Bot_passwords </param>
        ''' <param name="PageURL">Nombre exacto de la página</param>
        Sub New(ByVal BotUsername As String, BotPassword As String, PageURL As String)
            _botusername = BotUsername
            _botpass = BotPassword
            _siteurl = PageURL
            BotCookies = New CookieContainer
            Console.WriteLine(WikiLogOn())
            Username = BotUsername.Split("@"c)(0).Trim()
        End Sub
        ''' <summary>
        ''' Limpia todas las cookies, retorna "true" si finaliza correctamente.
        ''' </summary>
        Function CleanCookies() As Boolean
            Try
                BotCookies = New CookieContainer
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Retorna un elemento Page coincidente al nombre entregado como parámetro.
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la página</param>
        Function Getpage(ByVal PageName As String) As Page
            Return New Page(PageName, _siteurl, BotCookies, Username)
        End Function

        ''' <summary>
        ''' Obtiene un Token y cookies de ingreso, establece las cookies de la clase y retorna el token como string.
        ''' </summary>
        ''' <param name="SiteUrl">Url de la wiki.</param>
        Private Function GetWikiToken(ByVal SiteUrl As String) As String
            Dim url As String = SiteUrl
            Dim postdata As String = "action=query&meta=tokens&type=login&&format=json"
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, BotCookies)
            Dim token As String = TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Function WikiLogOn() As String
            Dim token As String = GetWikiToken(_siteurl)
            Console.WriteLine(token)
            Dim url As String = _siteurl
            Dim postdata As String = "action=login&format=json&lgname=" & _botusername & "&lgpassword=" & _botpass & "&lgdomain=" & "&lgtoken=" & UrlWebEncode(token)
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, BotCookies)
            Return postresponse
        End Function

        ''' <summary>
        ''' Retorna las imagenes de preview de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Nombre de imagen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir la imagen, retorna string.empty.
        ''' </summary>
        ''' <param name="PageNames">Array con nombres de página unicos.</param>
        Function GetImagesExtract(ByVal PageNames As String()) As SortedList(Of String, String)
            Dim PageNamesList As List(Of String) = PageNames.ToList
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

                Dim QueryResponse As String = Gethtmlsource(_siteurl & "?action=query&formatversion=2&prop=pageimages&format=json&titles=" & Qstring)
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
        Overloads Function GetPageExtract(ByVal PageName As String, CharLimit As Integer) As String
            Return GetPagesExtract({PageName}, CharLimit).Values(0)
        End Function

        ''' <summary>
        ''' Retorna la entradilla de la página indicada de entrada como string con el límite indicado. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="Page_name">Nombre exacto de la página.</param>
        Overloads Function GetPageExtract(ByVal Page_name As String) As String
            Return GetPagesExtract({Page_name}, 660).Values(0)
        End Function

        ''' <summary>
        ''' Retorna en nombre del archivo de imagen de la página indicada de entrada como string. 
        ''' En caso de no existir el la página o la imagen, no lo retorna.
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la página.</param>
        Function GetImageExtract(ByVal PageName As String) As String
            Return GetImagesExtract({PageName}).Values(0)
        End Function

        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="Page_names">Array con nombres de página unicos.</param>
        ''' <remarks></remarks>
        Overloads Function GetPagesExtract(ByVal page_names As String()) As SortedList(Of String, String)
            Return BOTGetPagesExtract(page_names, 660)
        End Function

        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="Page_names">Array con nombres de página unicos.</param>
        ''' <param name="CharacterLimit">Límite de carácteres en el resumen.</param>
        ''' <remarks></remarks>
        Overloads Function GetPagesExtract(ByVal page_names As String(), ByVal CharacterLimit As Integer) As SortedList(Of String, String)
            Return BOTGetPagesExtract(page_names, CharacterLimit)
        End Function

        ''' <summary>
        ''' Retorna los resúmenes de las páginas indicadas en el array de entrada como SortedList (con el formato {Página,Resumen}), los nombres de página deben ser distintos. 
        ''' En caso de no existir el la página o el resumen, no lo retorna.
        ''' </summary>
        ''' <param name="Page_names">Array con nombres de página unicos.</param>
        ''' <remarks></remarks>
        Private Function BOTGetPagesExtract(ByVal Page_names As String(), CharLimit As Integer) As SortedList(Of String, String)
            Log("Starting Wikipedia page extracts of chunks", "LOCAL", BOTName)
            Dim PageNamesList As List(Of String) = Page_names.ToList
            PageNamesList.Sort()

            Dim PageList As List(Of List(Of String)) = SplitStringArrayIntoChunks(PageNamesList.ToArray, 20)
            Dim PagenameAndResume As New SortedList(Of String, String)

            For Each ListInList As List(Of String) In PageList
                Dim Qstring As String = String.Empty

                For Each s As String In ListInList
                    s = UrlWebEncode(s)
                    Qstring = Qstring & s & "|"
                Next
                Qstring = Qstring.Trim(CType("|", Char))
                Dim QueryResponse As String = GetDataAndResult(_siteurl & "?format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" & Qstring, False, BotCookies)
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
                            If TreatedExtract.Length > CharLimit Then
                                TreatedExtract = TreatedExtract.Substring(0, CharLimit + 1)
                                For a As Integer = -CharLimit To 0
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

                                        If Regex.Match(TreatedExtract.Chars(0 - a + 1), "[0-9]+").Success Or
                                            (TreatedExtract.Chars(0 - a - 2) & TreatedExtract.Chars(0 - a - 1)) = "(n" Or
                                            (TreatedExtract.Chars(0 - a - 2) & TreatedExtract.Chars(0 - a - 1)) = "(f" Then
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

                            PagenameAndResume.Add(PageKey, TreatedExtract)
                        End If
                    End If

                Next
            Next

            Return PagenameAndResume

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
                    Dim s As String = Gethtmlsource(("https://ores.wikimedia.org/v3/scores/eswiki/?models=damaging|goodfaith&format=json&revids=" & UrlWebEncode(Qstring)), False, BotCookies)

                    For Each m As Match In Regex.Matches(s, "({|, )(""[0-9]+"":).+?(}}}})")

                        Dim EditID_str As String = Regex.Match(m.Value, """[0-9]+""").Value
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
        ''' Retorna el ultimo REVID (como integer) de las paginas indicadas como SortedList (con el formato {Pagename,Revid}), las paginas deben ser distintas. 
        ''' En caso de no existir la pagina, retorna -1 como REVID.
        ''' </summary>
        ''' <param name="PageNames">Array con nombres de paginas unicos.</param>
        ''' <remarks></remarks>
        Function GetLastRevIds(ByVal PageNames As String()) As SortedList(Of String, Integer)
            Dim PageNamesList As List(Of String) = PageNames.ToList
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

                Dim QueryResponse As String = Gethtmlsource((_siteurl & "?action=query&prop=revisions&format=json&titles=" & Qstring), False, BotCookies)
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
            Return PagenameAndLastId
        End Function




        ''' <summary>
        ''' Retorna el ultimo REVID (como integer) de la pagina indicada como integer. 
        ''' En caso de no existir la pagina, retorna -1 como REVID.
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la pagina.</param>
        ''' <remarks></remarks>
        Function GetLastRevID(ByVal PageName As String) As Integer
            Log("GetLastRevID: Starting Wikipedia last RevisionID of page """ & PageName & """.", "LOCAL", BOTName)
            PageName = UrlWebEncode(PageName)
            Try
                Dim QueryText As String = String.Empty
                Log("GetLastRevID: Query of last RevisionID of page """ & PageName & """.", "LOCAL", BOTName)
                QueryText = Gethtmlsource((_siteurl & "?action=query&prop=revisions&format=json&titles=" & PageName), False, BotCookies)

                Dim ID As Integer = Integer.Parse(TextInBetween(QueryText, """revid"":", ",""")(0))
                Debug_Log("GetLastRevID: Query of last RevisionID of page """ & PageName & " successful, result: " & ID.ToString, "LOCAL", BOTName)
                Return ID
            Catch ex As Exception
                Debug_Log("GetLastRevID: Query of last RevisionID from page """ & PageName & " failed, returning Nothing", "LOCAL", BOTName)
                Debug_Log("GetLastRevID: ex message: " & ex.Message, "LOCAL", BOTName)
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Retorna una pagina aleatoria
        ''' </summary>
        Function GetRandomPage() As String()
            Log("GetRandomPage: Starting query of random page.", "LOCAL", BOTName)
            Try
                Dim QueryText As String = String.Empty
                QueryText = Gethtmlsource((_siteurl & "?action=query&format=json&list=random&rnnamespace=0&rnlimit=10"), False, BotCookies)

                Dim plist As New List(Of String)

                For Each s As String In TextInBetween(QueryText, """title"":""", """}")
                    Log("GetRandomPage: Found """ & s & """.", "LOCAL", BOTName)
                    plist.Add(NormalizeUnicodetext(s))
                Next
                Log("GetRandomPage: Ended.", "LOCAL", BOTName)
                Return plist.ToArray
            Catch ex As Exception
                Debug_Log("GetRandomPage: ex message: " & ex.Message, "LOCAL", BOTName)
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Retorna un array de tipo string con todas las páginas donde la página indicada es llamada (no confundir con "lo que enlaza aquí").
        ''' </summary>
        ''' <param name="PageName">Nombre exacto de la pagina.</param>
        Function GetallInclusions(ByVal PageName As String) As String()
            Dim newlist As New List(Of String)
            Dim s As String = String.Empty
            s = Gethtmlsource((_siteurl & "?action=query&list=embeddedin&eilimit=500&format=json&eititle=" & PageName), False, BotCookies)


            Dim pages As String() = TextInBetween(s, """title"":""", """}")

            For Each _pag As String In pages
                newlist.Add(NormalizeUnicodetext(_pag))
            Next

            Return newlist.ToArray
        End Function

        ''' <summary>
        ''' Revisa todas las páginas que llamen a la página indicada y las retorna como sortedlist.
        ''' La Key es el nombre de la página en la plantilla y el valor asociado es un array donde el primer elemento es
        ''' el último usuario que la editó y el segundo el título real de la página.
        ''' </summary>
        Function GetAllRequestedpages() As SortedList(Of String, String())
            Dim plist As New SortedList(Of String, String())
            For Each s As String In GetallInclusions(ResumePageName)
                Dim Pag As Page = Getpage(s)
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
        ''' Compara las páginas que llaman a la plantilla y retorna retorna un sortedlist
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
        Overloads Function UpdatePageExtracts() As Boolean
            Return BotUpdatePageExtracts(False)
        End Function
        ''' <summary>
        ''' Actualiza los resúmenes de página basado en varios parámetros,
        ''' por defecto estos son de un máximo de 660 carácteres.
        ''' </summary>
        ''' <param name="IRC">Si se establece este valor envía un comando en IRC avisando de la actualización</param>
        ''' <returns></returns>
        Overloads Function UpdatePageExtracts(ByVal irc As Boolean) As Boolean
            Return BotUpdatePageExtracts(irc)
        End Function
        ''' <summary>
        ''' Actualiza los resúmenes de página basado en varios parámetros,
        ''' por defecto estos son de un máximo de 660 carácteres.
        ''' </summary>
        ''' <param name="IRC">Si se establece este valor envía un comando en IRC avisando de la actualización</param>
        ''' <returns></returns>
        Private Function BotUpdatePageExtracts(ByVal irc As Boolean) As Boolean
            If irc Then
                BotIRC.Sendmessage(ColoredText("Actualizando extractos...", "04"))
            End If

            Log("UpdatePageExtracts: Beginning update of page extracts", "LOCAL", BOTName)
            Debug_Log("UpdatePageExtracts: Declaring Variables", "LOCAL", BOTName)
            Dim NewResumes As New SortedList(Of String, String)
            Dim OldResumes As New SortedList(Of String, String)
            Dim FinalList As New List(Of String)


            Debug_Log("UpdatePageExtracts: Loading resume page", "LOCAL", BOTName)
            Dim ResumePage As Page = Getpage(ResumePageName)

            Dim ResumePageText As String = ResumePage.Text
            Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL", BOTName)


            Dim NewResumePageText As String = "{{#switch:{{{1}}}|" & Environment.NewLine
            Debug_Log("UpdatePageExtracts: Resume page loaded", "LOCAL", BOTName)

            Dim Extracttext As String = String.Empty
            Dim ExtractImage As String = String.Empty
            Dim Safepages As Integer = 0
            Dim NotSafepages As Integer = 0
            Dim NewPages As Integer = 0
            Dim NotSafePagesAdded As Integer = 0
            Debug_Log("UpdatePageExtracts: Match list to ListOf", "LOCAL", BOTName)
            Dim p As New List(Of String)

            p.AddRange(GetTitlesOfTemplate(ResumePageText))

            For Each item As KeyValuePair(Of String, String()) In GetResumeRequests()
                If Not p.Contains(item.Key) Then
                    p.Add(item.Key)
                    NewPages += 1
                End If
            Next

            Debug_Log("UpdatePageExtracts: Sort of ListOf", "LOCAL", BOTName)
            p.Sort()
            Debug_Log("UpdatePageExtracts: Creating new ResumePageText", "LOCAL", BOTName)


            Debug_Log("UpdatePageExtracts: Adding IDS to IDLIST", "LOCAL", BOTName)
            Dim IDLIST As SortedList(Of String, Integer) = GetLastRevIds(p.ToArray)

            Debug_Log("UpdatePageExtracts: Adding Old resumes to list", "LOCAL", BOTName)
            For Each s As String In p.ToArray
                ' Adding Old resumes to list
                Try
                    Dim cont As String = TextInBetween(ResumePageText, "|" & s & "=", "Leer más...]]'''|")(0)
                    OldResumes.Add(s, ("|" & s & "=" & cont & "Leer más...]]'''|" & Environment.NewLine))
                Catch ex As IndexOutOfRangeException
                    Debug_Log("UpdatePageExtracts: No old resume of " & s, "LOCAL", BOTName)
                End Try

            Next

            Debug_Log("UpdatePageExtracts: Adding New resumes to list", "LOCAL", BOTName)

            '============================================================================================
            ' Adding New resumes to list
            Dim Page_Resume_pair As SortedList(Of String, String) = GetPagesExtract(p.ToArray)
            Dim Page_Image_pair As SortedList(Of String, String) = GetImagesExtract(p.ToArray)

            For Each Page As String In Page_Resume_pair.Keys

                If Not Page_Image_pair.Item(Page) = String.Empty Then
                    'If the page contais a image
                    NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                                   & "[[File:" & Page_Image_pair(Page) & "|thumb|x120px]]" & Environment.NewLine _
                                   & Page_Resume_pair.Item(Page) & Environment.NewLine _
                                   & ":'''[[" & Page & "|Leer más...]]'''|" & Environment.NewLine)
                Else
                    'If the page doesn't contain a image
                    NewResumes.Add(Page, "|" & Page & "=" & Environment.NewLine _
                                  & Page_Resume_pair.Item(Page) & Environment.NewLine _
                                  & ":'''[[" & Page & "|Leer más...]]'''|" & Environment.NewLine)
                End If
            Next

            '===========================================================================================

            Debug_Log("UpdatePageExtracts: getting ORES of IDS", "LOCAL", BOTName)
            Dim EditScoreList As SortedList(Of Integer, Double()) = GetORESScores(IDLIST.Values.ToArray)

            '==========================================================================================
            'Choose between a old resume and a new resume depending if new resume is safe to use
            Debug_Log("UpdatePageExtracts: Recreating text", "LOCAL", BOTName)
            For Each s As String In p.ToArray
                Try
                    If (EditScoreList(IDLIST(s))(0) > 22) And
              (CountCharacter(NewResumes(s), CType("[", Char)) =
              CountCharacter(NewResumes(s), CType("]", Char))) Then
                        'Is a safe edit
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

            Debug_Log("UpdatePageExtracts: Concatenating recreated text", "LOCAL", BOTName)
            NewResumePageText = NewResumePageText & String.Join(String.Empty, FinalList) & "<!-- MARK -->" & Environment.NewLine & "|}}"

            Debug_Log("UpdatePageExtracts: Done, trying to save", "LOCAL", BOTName)

            Try
                If NotSafepages = 0 Then
                    If NewPages = 0 Then
                        ResumePage.Save(NewResumePageText, "(Bot) : Actualizando " & Safepages.ToString & " resúmenes.", False)
                    Else
                        ResumePage.Save(NewResumePageText, "(Bot) : Actualizando " & Safepages.ToString & " resúmenes. Se han añadido " & NewPages.ToString & " resúmenes nuevos", False)
                    End If

                Else
                    If NewPages = 0 Then
                        ResumePage.Save(NewResumePageText,
                                                            "(Bot): Actualizando " & Safepages.ToString & " resúmenes, fueron omitidos " _
                                                            & NotSafepages.ToString & " resumenes posiblemente inseguros.", False)
                    Else
                        ResumePage.Save(NewResumePageText,
                                                            "(Bot): Actualizando " & Safepages.ToString & " resúmenes, fueron omitidos " _
                                                            & NotSafepages.ToString & " resumenes posiblemente inseguros. Se han añadido " & NewPages.ToString & " resúmenes nuevos.", False)
                    End If

                End If

                Log("UpdatePageExtracts: Update of page extracts completed successfully", "LOCAL", BOTName)
                If irc Then
                    BotIRC.Sendmessage(ColoredText("Extractos actualizados!", "04"))
                End If

                Return True

            Catch ex As Exception
                Log("UpdatePageExtracts: Error updating page extracts", "LOCAL", BOTName)
                Debug_Log(ex.Message, "LOCAL", BOTName)
                CleanCookies()
                WikiLogOn()
                If ex.Message.ToLower.Contains("token") Then
                    Debug_Log("UpdatePageExtracts: Token exception", "LOCAL", BOTName)
                End If
                BotIRC.Sendmessage(ColoredText("Error al actualizar los extractos, ver LOG.", "04"))
                Return False
            End Try

        End Function

        ''' <summary>
        ''' Entrega el título de la primera página que coincida remotamente con el texto entregado como parámetro.
        ''' Usa las mismas sugerencias del cuadro de búsqueda de Wikipedia, pero por medio de la API.
        ''' Si no hay coincidencia, entrega una cadena de texto vacía.
        ''' </summary>
        ''' <param name="Text">Título aproximado o similar al de una página</param>
        ''' <returns></returns>
        Function TitleFirstGuess(Text As String) As String
            Try
                Return GetTitlesFromQueryText(Gethtmlsource((_siteurl & "?action=query&format=json&list=search&utf8=1&srsearch=" & Text), False, BotCookies))(0)
            Catch ex As Exception
                Return String.Empty
            End Try
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
                Return GetTitlesFromQueryText(Gethtmlsource((_siteurl & "?action=query&format=json&list=search&utf8=1&srnamespace=2&srsearch=" & text), False, BotCookies))(0)
            Catch ex As Exception
                Return String.Empty
            End Try
        End Function

        ''' <summary>
        ''' Entrega como DateTime la fecha de la última edición del usuario entregado como parámetro.
        ''' </summary>
        ''' <param name="user">Nombre exacto del usuario</param>
        ''' <returns></returns>
        Function GetLastEditTimestampUser(ByVal user As String) As DateTime
            user = UrlWebEncode(user)
            Dim qtest As String = Gethtmlsource((_siteurl & "?action=query&list=usercontribs&uclimit=1&format=json&ucuser=" & user), False, BotCookies)

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
        ''' <summary>
        ''' Busca un texto exacto en una página y lo reemplaza.
        ''' </summary>
        ''' <param name="Requestedpage">Página a realizar el cambio</param>
        ''' <param name="requestedtext">Texto a reeplazar</param>
        ''' <param name="newtext">Texto que reemplaza</param>
        ''' <param name="reason">Motivo del reemplazo</param>
        ''' <returns></returns>
        Function Replacetext(ByVal Requestedpage As Page, requestedtext As String, newtext As String, reason As String) As Boolean

            Dim PageText As String = Requestedpage.Text
            If PageText.Contains(requestedtext) Then

                PageText = PageText.Replace(requestedtext, newtext)

            End If
            Requestedpage.Save(PageText, String.Format("(Bot): Reemplazando '{0}' por '{1}' {2}.", requestedtext, newtext, reason))
            Return True

        End Function

        ''' <summary>
        ''' Elimina una referencia que contenga una cadena exacta.
        ''' (No usar a menos de que se esté absolutamente seguro de lo que se hace).
        ''' </summary>
        ''' <param name="RequestedPage">Página a revisar</param>
        ''' <param name="RequestedRef">Texto que determina que referencia se elimina</param>
        ''' <returns></returns>
        Function RemoveRef(ByVal RequestedPage As Page, RequestedRef As String) As Boolean
            Dim pageregex As String = String.Empty
            Dim PageText As String = RequestedPage.Text
            For Each c As Char In RequestedRef
                pageregex = pageregex & "[" & c.ToString.ToUpper & c.ToString.ToLower & "]"
            Next

            If Not (RequestedPage.Title.ToLower.Contains("usuario:") Or RequestedPage.Title.ToLower.Contains("wikipedia:") Or
                RequestedPage.Title.ToLower.Contains("usuaria:") Or RequestedPage.Title.ToLower.Contains("especial:") Or
                 RequestedPage.Title.ToLower.Contains("wikiproyecto:") Or RequestedPage.Title.ToLower.Contains("discusión:") Or
                  RequestedPage.Title.ToLower.Contains("discusion:")) Then


                For Each m As Match In Regex.Matches(PageText, "(<[REFref]+>)([^<]+?)" & pageregex & ".+?(<[/REFref]+>)")
                    PageText = PageText.Replace(m.Value, "")
                Next

                Try
                    RequestedPage.Save(PageText, "(Bot): Removiendo referencias que contengan '" & RequestedRef & "' según solicitud", False)
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
            Dim threads As New List(Of String)
            For Each m As Match In Regex.Matches(pagetext, "(==.+==)[\s\S]+?(?===.+==|$)")
                threads.Add(m.Value)
            Next
            Return threads.ToArray
        End Function

        ''' <summary>
        ''' Entrega como DateTime la última fecha (formato firma Wikipedia) en el último parrafo. Si no encuentra firma retorna 31/12/9999.
        ''' </summary>
        ''' <param name="text">Texto a evaluar</param>
        ''' <returns></returns>
        Function LastParagraphDateTime(ByVal text As String) As DateTime
            Dim Datelist As New List(Of DateTime)
            Dim lastparagraph As String = Regex.Match(text, ".+[\s\s]+(?===.+==|$)").Value
            Dim TheDate As DateTime = EsWikiDatetime(lastparagraph)
            Log("LastParagraphDateTime: Returning " & TheDate.ToString, "LOCAL", BOTName)
            Return TheDate
        End Function

        ''' <summary>
        ''' Entrega como array de DateTime todas las fechas (Formato de firma Wikipedia) en el texto dado.
        ''' </summary>
        ''' <param name="text">Texto a evaluar</param>
        ''' <returns></returns>
        Function AllDateTimes(ByVal text As String) As DateTime()
            Dim Datelist As New List(Of DateTime)
            For Each m As Match In Regex.Matches(text, "([0-9]{2}):([0-9]{2}) ([0-9]{2}|[0-9]) ([Z-z]{3}) [0-9]{4} \(UTC\)")
                Dim TheDate As DateTime = EsWikiDatetime(m.Value)
                Datelist.Add(TheDate)
                Log("AllDateTimes: Adding " & TheDate.ToString, "LOCAL", BOTName)
            Next
            Return Datelist.ToArray
        End Function

        ''' <summary>
        ''' Entrega como DateTime la fecha más reciente en el texto dado (en formato de firma wikipedia).
        ''' </summary>
        ''' <param name="comment"></param>
        ''' <returns></returns>
        Function MostRecentDate(ByVal comment As String) As DateTime

            Dim dattimelist As New List(Of DateTime)
            Debug_Log("Begin GetLastDateTime", "LOCAL", BOTName)

            For Each m As Match In Regex.Matches(comment, "([0-9]{2}):([0-9]{2}) ([0-9]{2}|[0-9]) ([Z-z]{3}) [0-9]{4} \(UTC\)")
                Debug_Log("GetLastDateTime match: """ & m.Value & """", "LOCAL", BOTName)
                Try
                    Dim parsedtxt As String = m.Value.ToLower.Replace(" ene ", "/01/").Replace(" feb ", "/02/") _
                    .Replace(" mar ", "/03/").Replace(" abr ", "/04/").Replace(" may ", "/05/") _
                    .Replace(" jun ", "/06/").Replace(" jul ", "/07/").Replace(" ago ", "/08/") _
                    .Replace(" sep ", "/09/").Replace(" oct ", "/10/").Replace(" nov ", "/11/") _
                    .Replace(" dic ", "/12/").Replace(vbLf, String.Empty).Replace(" (utc)", String.Empty)

                    Debug_Log("GetLastDateTime: Try parse", "LOCAL", BOTName)
                    dattimelist.Add(DateTime.Parse(parsedtxt))
                    Debug_Log("GetLastDateTime parse string: """ & parsedtxt & """", "LOCAL", BOTName)
                Catch ex As System.FormatException
                    Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "TextFunctions", BOTName)
                End Try
            Next
            If Not dattimelist.Count = 0 Then
                Log("GetMostRecentDateTime: returning """ & dattimelist.Last.ToLongDateString & """", "LOCAL", BOTName)
                Return dattimelist.Last
            Else
                Debug_Log("GetMostRecentDateTime: Returning nothing ", "LOCAL", BOTName)
                Return Nothing
            End If

        End Function

        ''' <summary>
        ''' Busca en el texto una plantilla de archivado usada por grillitus.
        ''' De encontrar la plantilla entrega un array de tipo string con: {Destino del archivado, Días a mantener, Avisar archivado, Estrategia de archivado, mantener caja}.
        ''' Los parámetros que estén vacíos en la plantilla se entregan vacíos también.
        ''' De no encontrar los parámetros regresa un array con todos los parámetros vacíos.
        ''' </summary>
        ''' <param name="Pagetext">Texto a evaluar</param>
        ''' <returns></returns>
        Function GetGrillitusTemplateData(PageText As String) As String()
            Dim template As String = Regex.Match(PageText, "{{Usuario:Grillitus\/Archivar[\s\S]+?}}").Value

            Dim Destiny As String = Regex.Match(template, "(\|Destino)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
            Destiny = Regex.Replace(Destiny, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char()))

            Dim Days As String = Regex.Match(template, "(\|Días a mantener)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
            Days = Regex.Replace(Days, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char()))

            Dim Notice As String = Regex.Match(template, "(\|Avisar al archivar)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
            Notice = Regex.Replace(Notice, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char()))

            Dim Estrategy As String = Regex.Match(template, "(\|Estrategia)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
            Estrategy = Regex.Replace(Estrategy, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char()))

            Dim Box As String = Regex.Match(template, "(\|MantenerCajaDeArchivos)=[^}|]+(?=\||})", RegexOptions.IgnoreCase).Value
            Box = Regex.Replace(Box, "\|[^=]+=", "", RegexOptions.IgnoreCase).Trim(CType(Environment.NewLine, Char()))

            Return {Destiny, Days, Notice, Estrategy, Box}

        End Function

        ''' <summary>
        ''' Actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
        ''' </summary>
        ''' <returns></returns>
        Function ArchiveAllInclusions(ByVal IRC As Boolean) As Boolean
            If IRC Then
                BotIRC.Sendmessage(ColoredText("Archivando todas las discusiones...", "04"))
            End If
            Dim includedpages As String() = GetallInclusions("Usuario:Grillitus/Archivar")
            For Each pa As String In includedpages
                Log("ArchiveAllInclusions: Page " & pa, "LOCAL", BOTName)
                Dim _Page As Page = Getpage(pa)
                If _Page.Exists Then
                    GrillitusArchive(_Page)
                End If
            Next
            If IRC Then
                BotIRC.Sendmessage(ColoredText("Archivado completo...", "04"))
            End If
            Return True
        End Function


        ''' <summary>
        ''' Realiza un archivado siguiendo la lógica de Grillitus.
        ''' </summary>
        ''' <param name="PageToArchive">Página a archivar</param>
        ''' <returns></returns>
        Function GrillitusArchive(ByVal PageToArchive As Page) As Boolean
            Log("GrillitusArchive: Page " & PageToArchive.Title, "LOCAL", BOTName)
            Dim IndexPage As Page = Getpage(PageToArchive.Title & "/Archivo-00-índice")
            Dim IndexpageText As String = IndexPage.Text
            Dim PageTitle As String = PageToArchive.Title
            Dim pagetext As String = PageToArchive.Text
            Dim Newpagetext As String = pagetext
            Dim ArchivePageText As String = String.Empty
            Dim threads As String() = GetPageThreads(pagetext)
            Dim GrillitusCfg As String() = GetGrillitusTemplateData(pagetext)
            Dim Notify As Boolean = False
            Dim Strategy As String = String.Empty
            Dim UseBox As Boolean = False
            Dim ArchivePageTitle As String = GrillitusCfg(0)
            Dim MaxDays As Integer = 0
            Dim ArchivedThreads As Integer = 0
            Dim quarter As Integer = CInt((DateTime.Now.Month - 1) / 3 + 1)
            Dim Currentyear As String = DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)
            Dim CurrentMonth As String = DateTime.Now.ToString("MM", System.Globalization.CultureInfo.InvariantCulture)
            Dim CurrentMonthStr As String = DateTime.Now.ToString("MMMM", New Globalization.CultureInfo("es-ES"))
            Dim CurrentDay As String = DateTime.Now.ToString("dd", System.Globalization.CultureInfo.InvariantCulture)

            If String.IsNullOrEmpty(GrillitusCfg(0)) Then
                Return False
            End If
            If String.IsNullOrEmpty(GrillitusCfg(1)) Then
                Return False
            Else
                MaxDays = Integer.Parse(GrillitusCfg(1))
            End If
            If String.IsNullOrEmpty(GrillitusCfg(2)) Then
                Notify = False
            Else
                If GrillitusCfg(2).ToLower.Contains("si") Then
                    Notify = True
                Else
                    Notify = False
                End If
            End If

            Notify = Not (Notify)

            If String.IsNullOrEmpty(GrillitusCfg(3)) Then
                Strategy = "FirmaEnÚltimoPárrafo"
            Else
                If GrillitusCfg(3) = "FirmaEnÚltimoPárrafo" Then
                    Strategy = "FirmaEnÚltimoPárrafo"
                ElseIf GrillitusCfg(3) = "FirmaMásRecienteEnLaSección" Then
                    Strategy = "FirmaMásRecienteEnLaSección"
                Else
                    Strategy = "FirmaEnÚltimoPárrafo"
                End If
            End If

            If String.IsNullOrEmpty(GrillitusCfg(4)) Then
                UseBox = False
            Else
                If GrillitusCfg(4).ToLower.Contains("si") Or GrillitusCfg(4).ToLower.Contains("sí") Then
                    UseBox = True
                Else
                    UseBox = False
                End If
            End If


            If Not ArchivePageTitle.Contains("SEM") Then
                ArchivePageTitle = ArchivePageTitle.Replace("AAAA", Currentyear)
                ArchivePageTitle = ArchivePageTitle.Replace("MM", CurrentMonth)
                ArchivePageTitle = ArchivePageTitle.Replace("DD", CurrentDay)
            Else

                ArchivePageTitle = ArchivePageTitle.Replace("AAAA", CurrentDay)
                ArchivePageTitle = ArchivePageTitle.Replace("SEM", quarter.ToString)
            End If

            Dim LimitDate As DateTime = DateTime.Now.AddDays(-MaxDays)

            For Each t As String In threads
                Try


                    If Strategy = "FirmaMásRecienteEnLaSección" Then
                        Dim threaddate As DateTime = MostRecentDate(t)
                        If Not t.Contains("{{Usuario:Grillitus/No archivar}}") Then

                            If t.Contains("{{Usuario:Grillitus/Archivo programado") Then
                                Dim fechastr As String = TextInBetween(t, "{{Usuario:Grillitus/Archivo programado|fecha=", "}}")(0)
                                Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'mm'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)
                                If DateTime.Now >= fecha Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    ArchivePageText = ArchivePageText & t
                                    ArchivedThreads += 1
                                End If
                            Else
                                If threaddate < LimitDate Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    ArchivePageText = ArchivePageText & t
                                    ArchivedThreads += 1
                                End If
                            End If


                        End If


                    ElseIf Strategy = "FirmaEnÚltimoPárrafo" Then
                        Dim threaddate As DateTime = LastParagraphDateTime(t)
                        If Not t.Contains("{{Usuario:Grillitus/No archivar}}") Then

                            If t.Contains("{{Usuario:Grillitus/Archivo programado") Then
                                Dim fechastr As String = TextInBetween(t, "{{Usuario:Grillitus/Archivo programado|fecha=", "}}")(0)
                                fechastr = " " & fechastr & " "
                                fechastr = fechastr.Replace(" 1-", "01-").Replace(" 2-", "02-").Replace(" 3-", "03-").Replace(" 4-", "04-") _
                                    .Replace(" 5-", "05-").Replace(" 6-", "06-").Replace(" 7-", "07-").Replace(" 8-", "08-").Replace(" 9-", "09-") _
                                    .Replace("-1-", "-01-").Replace("-2-", "-02-").Replace("-3-", "-03-").Replace("-4-", "-04-").Replace("-5-", "-05-") _
                                    .Replace("-6-", "-06-").Replace("-7-", "-07-").Replace("-8-", "-08-").Replace("-9-", "-09-").Trim()

                                Dim fecha As DateTime = DateTime.ParseExact(fechastr, "dd'-'MM'-'yyyy", System.Globalization.CultureInfo.InvariantCulture)
                                If DateTime.Now >= fecha Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    ArchivePageText = ArchivePageText & t
                                    ArchivedThreads += 1
                                End If
                            Else
                                If threaddate < LimitDate Then
                                    Newpagetext = Newpagetext.Replace(t, "")
                                    ArchivePageText = ArchivePageText & t
                                    ArchivedThreads += 1
                                End If
                            End If


                        End If
                    End If
                Catch ex As Exception
                    Log("GrillitusArchive: Error in one thread on " & PageToArchive.Title, "LOCAL", BOTName)
                End Try
            Next

            If ArchivedThreads > 0 Then



                If UseBox Then

                    If IndexPage.Exists Then

                        Dim ArchiveBoxMatch As Match = Regex.Match(IndexpageText, "{{caja archivos\|[\s\S]+?}}")
                        Dim Newbox As String = String.Empty
                        If ArchiveBoxMatch.Success Then

                            Newbox = ArchiveBoxMatch.Value.Replace("{{caja archivos|", "").Replace("}}", "")

                            If Not Newbox.ToLower.Contains(ArchivePageTitle) Then
                                Dim newlink As String = String.Empty

                                If GrillitusCfg(0).Contains("SEM") Then
                                    newlink = "[[" & ArchivePageTitle & "|Archivo " & quarter.ToString & "]]"
                                Else

                                    If GrillitusCfg(0).Contains("DD") Then
                                        newlink = "[[" & ArchivePageTitle & "|Archivo del " & CurrentDay & "/" & CurrentMonth & "/" & Currentyear & "]]"
                                    ElseIf GrillitusCfg(0).Contains("MM") Then
                                        newlink = "[[" & ArchivePageTitle & "|Archivo de " & CurrentMonthStr & " de " & Currentyear & "]]"
                                    ElseIf GrillitusCfg(0).Contains("AAAA") Then
                                        newlink = "[[" & ArchivePageTitle & "|Archivo del " & Currentyear & "]]"
                                    Else

                                    End If

                                End If

                                If Not Newbox.Contains(newlink) Then
                                    Newbox = Newbox & "<center>" & newlink & "</center>" & "<br />" & Environment.NewLine
                                    Newbox = "{{caja archivos|" & Newbox & "}}"
                                    IndexpageText = IndexpageText.Replace(ArchiveBoxMatch.Value, Newbox)
                                End If

                            End If

                        Else

                            Dim newlink As String = String.Empty
                            If GrillitusCfg(0).Contains("SEM") Then
                                newlink = "<center>[[" & ArchivePageTitle & "|Archivo " & quarter.ToString & "]]"

                            Else
                                If GrillitusCfg(0).Contains("DD") Then
                                    newlink = "<center>[[" & ArchivePageTitle & "|Archivo del " & CurrentDay & "/" & CurrentMonth & "/" & Currentyear & "]]"
                                ElseIf GrillitusCfg(0).Contains("MM") Then
                                    newlink = "<center>[[" & ArchivePageTitle & "|Archivo de " & CurrentMonthStr & " de " & Currentyear & "]]"
                                ElseIf GrillitusCfg(0).Contains("AAAA") Then
                                    newlink = "<center>[[" & ArchivePageTitle & "|Archivo del " & Currentyear & "]]"
                                Else

                                End If

                            End If
                            IndexpageText = "{{caja archivos|" & Environment.NewLine & newlink & "<br />" & Environment.NewLine & "}}"
                        End If


                    Else


                        If GrillitusCfg(0).Contains("SEM") Then
                            IndexpageText = "[[" & ArchivePageTitle & "|Archivo " & quarter.ToString & "]]<br>"
                        Else
                            If GrillitusCfg(0).Contains("DD") Then
                                IndexpageText = "[[" & ArchivePageTitle & "|Archivo del " & CurrentDay & "/" & CurrentMonth & "/" & Currentyear & "]]"
                            ElseIf GrillitusCfg(0).Contains("MM") Then
                                IndexpageText = "[[" & ArchivePageTitle & "|Archivo de " & CurrentMonthStr & " de " & Currentyear & "]]"
                            ElseIf GrillitusCfg(0).Contains("AAAA") Then
                                IndexpageText = "[[" & ArchivePageTitle & "|Archivo del " & Currentyear & "]]"
                            Else
                                Return False
                            End If

                        End If
                        IndexpageText = "<center>" & IndexpageText & "</center>" & "<br />"
                        IndexpageText = "{{caja archivos|" & Environment.NewLine & IndexpageText & Environment.NewLine & "}}"


                    End If


                    If Not String.IsNullOrEmpty(ArchivePageText) Then
                        If Not Newpagetext.Contains("{{" & IndexPage.Title & "}}") Then
                            Dim grillitustemplate As String = Regex.Match(pagetext, "{{Usuario:Grillitus\/Archivar[\s\S]+?}}").Value
                            Newpagetext = Newpagetext.Replace(grillitustemplate, grillitustemplate & Environment.NewLine & "{{" & IndexPage.Title & "}}" & Environment.NewLine)
                        End If

                        If Not ArchivePageText.Contains("{{" & IndexPage.Title & "}}") Then
                            ArchivePageText = "{{" & IndexPage.Title & "}}" & Environment.NewLine & ArchivePageText
                        End If

                    End If


                End If



            End If

            If Not String.IsNullOrEmpty(ArchivePageText) Then
                Dim NewPage As Page = Getpage(ArchivePageTitle)
                Dim Summary As String = String.Empty

                If ArchivedThreads > 1 Then
                    Summary = String.Format("Archivando {0} hilos con más de {1} días de antiguedad en [[{2}]].", ArchivedThreads, MaxDays.ToString, ArchivePageTitle)
                Else
                    Summary = String.Format("Archivando {0} hilo con más de {1} días de antiguedad en [[{2}]].", ArchivedThreads, MaxDays.ToString, ArchivePageTitle)
                End If

                Dim ArchiveSummary As String = String.Empty
                If ArchivedThreads > 1 Then
                    ArchiveSummary = String.Format("Archivando {0} hilos con más de {1} días de antiguedad desde [[{2}]].", ArchivedThreads, MaxDays.ToString, PageTitle)
                Else
                    ArchiveSummary = String.Format("Archivando {0} hilo con más de {1} días de antiguedad desde [[{2}]].", ArchivedThreads, MaxDays.ToString, PageTitle)
                End If
                IndexPage.Save(IndexpageText, "Actualizando caja de archivos", True)
                NewPage.Save(NewPage.Text & Environment.NewLine & ArchivePageText, ArchiveSummary, Notify)
                PageToArchive.Save(Newpagetext, Summary, Notify)

            Else
                Log("GrillitusArchive: Nothing to archive ", "LOCAL", BOTName)
            End If


            Log("GrillitusArchive: " & PageToArchive.Title & " done.", "LOCAL", BOTName)
            Return True
        End Function


    End Class

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
            _username = username
            Loadpage(PageTitle, site, Cookies)
        End Sub
        ''' <summary>
        ''' Inicializa de nuevo la página (al crear una página esta ya está inicializada).
        ''' </summary>
        Public Sub Load()
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
            _sections = GetPageThreads(_text)
            _categories = GetCategories(_title)

            _currentRevID = GetLastRevID(_title)
            _ORESScores = GetORESScores(_currentRevID)
            _pageViews = GetPageViewsAvg(_title)
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
                minorstr = "&minor"
            Else
                minorstr = "&notminor"
            End If

            Dim botstr As String = String.Empty
            If IsBot Then
                botstr = "&bot"
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
            Try
                Dim querystring As String = "format=json&maxlag=5&action=query&prop=revisions&rvprop=user" & UrlWebEncode("|") & "timestamp" & UrlWebEncode("|") & "size" & UrlWebEncode("|") & "content" & "&titles=" & UrlWebEncode(Pagename)
                Dim QueryText As String = PostDataAndGetResult(_siteurl, querystring, False, _cookies)
                Dim PageID As String = TextInBetween(QueryText, "{""pageid"":", ",""ns")(0)
                Dim User As String = TextInBetween(QueryText, "{""user"":""", """,")(0)
                Dim PTitle As String = NormalizeUnicodetext(TextInBetween(QueryText, """title"":""", """,""revisions""")(0))
                Dim Timestamp As String = TextInBetween(QueryText, """timestamp"":""", """,")(0)
                Dim Wikitext As String = NormalizeUnicodetext(TextInBetween(QueryText, """wikitext"",""*"":""", """}]}}}}")(0))
                Dim Size As String = NormalizeUnicodetext(TextInBetween(QueryText, ",""size"":", ",""")(0))
                Return {PTitle, PageID, User, Timestamp, Wikitext, Size}
            Catch ex As IndexOutOfRangeException
                Console.WriteLine("Warning: The page '" & Pagename & "' doesn't exist yet!")
                Return {Pagename, "-1", "", "", "", "0"}
            Catch exex As Exception
                Throw New Exception("Unknown error")
            End Try

        End Function

        ''' <summary>
        ''' Evalua texto (wikicódigo) y regresa un array de string con cada uno de los hilos o secciones del mismo (los que comienzan con == ejemplo == y terminan en otro comienzo o el final de la página).
        ''' </summary>
        ''' <param name="pagetext">Texto a evaluar</param>
        ''' <returns></returns>
        Function GetPageThreads(ByVal pagetext As String) As String()
            Dim threads As New List(Of String)
            For Each m As Match In Regex.Matches(pagetext, "(==.+==)[\s\S]+?(?===.+==|$)")
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







End Namespace
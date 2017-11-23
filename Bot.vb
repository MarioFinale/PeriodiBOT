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
            WikiLogOn()
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
            Console.WriteLine("Obtaining token...")
            Dim url As String = SiteUrl
            Dim postdata As String = "action=query&meta=tokens&type=login&&format=json"
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, BotCookies)
            Dim token As String = TextInBetween(postresponse, """logintoken"":""", """}}}")(0).Replace("\\", "\")
            Console.WriteLine("Token obtained!")
            Return token
        End Function

        ''' <summary>
        ''' Luego de obtener un Token y cookies de ingreso, envía estos al servidor para loguear y guarda las cookies de sesión.
        ''' </summary>
        Function WikiLogOn() As String
            Console.WriteLine("Logging in...")
            Dim token As String = GetWikiToken(_siteurl)
            Dim url As String = _siteurl
            Dim postdata As String = "action=login&format=json&lgname=" & _botusername & "&lgpassword=" & _botpass & "&lgdomain=" & "&lgtoken=" & UrlWebEncode(token)
            Dim postresponse As String = PostDataAndGetResult(url, postdata, True, BotCookies)
            Dim lresult As String = String.Empty
            Try
                lresult = TextInBetween(postresponse, "{""result"":""", """,")(0)
                Console.WriteLine("Login result: " & lresult)
                Dim lUserID As String = TextInBetween(postresponse, """lguserid"":", ",")(0)
                Console.WriteLine("UserID: " & lUserID)
                Dim lUsername As String = TextInBetween(postresponse, """lgusername"":""", """}")(0)
                Console.WriteLine("Username: " & lUsername)
                Return lresult
            Catch ex As IndexOutOfRangeException
                If lresult.ToLower = "failed" Then
                    Dim reason As String = TextInBetween(postresponse, """reason"":""", """")(0)
                    Console.WriteLine("Reason: " & reason)
                    Console.WriteLine(Environment.NewLine & Environment.NewLine)
                    Console.WriteLine("Press any key to exit...")
                    Console.ReadLine()
                    ExitProgram()
                End If
                Return lresult
            End Try
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
            Dim newline As String = Environment.NewLine
            For Each m As Match In Regex.Matches(pagetext, "(" & newline & "==(?!=))[\s\S]+?(?=" & newline & "==(?!=)|$)")
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
            text = text.Trim(CType(vbCrLf, Char())) & " "
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
                Return New Date(9999, 12, 31)

            End If

        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y realiza un archivado siguiendo la lógica de Grillitus.
        ''' </summary>
        ''' <param name="PageToArchive">Página a archivar</param>
        ''' <returns></returns>
        Function GrillitusArchive(ByVal PageToArchive As Page) As Boolean
            Dim Archive As New GrillitusArchive(Me)
            Return Archive.GrillitusArchive(PageToArchive)
        End Function

        Function Archive(ByVal PageToArchive As Page) As Boolean
            Dim ArchiveFcn As New GrillitusArchive(Me)
            Return ArchiveFcn.Archive(PageToArchive)
        End Function

        ''' <summary>
        ''' Crea una nueva instancia de la clase de archivado y actualiza todas las paginas que incluyan la pseudoplantilla de archivado de grillitus.
        ''' </summary>
        ''' <returns></returns>
        Function ArchiveAllInclusions(ByVal IRC As Boolean) As Boolean
            Dim Archive As New GrillitusArchive(Me)
            Return Archive.ArchiveAllInclusions(IRC)
        End Function

    End Class






End Namespace
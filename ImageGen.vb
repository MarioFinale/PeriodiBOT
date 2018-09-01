Option Strict On
Imports System.Drawing
Imports System.Net
Imports System.Text.RegularExpressions
Imports PeriodiBOT_IRC.IRC

Class ImageGen

    Property Bot As WikiBot.Bot


    Private Header As String = Exepath & "Res" & DirSeparator & "header.hres"
    Private Bottom As String = Exepath & "Res" & DirSeparator & "bottom.hres"
    Private Hfolder As String = Exepath & "hfiles" & DirSeparator

    Sub New(ByRef workingbot As WikiBot.Bot)
        Bot = workingbot
    End Sub


    Function Allefe() As Boolean
        Dim results As New List(Of Boolean)
        For i As Integer = 0 To 6
            Utils.BotSettings.NewVal("efecheck", False.ToString)
            Utils.BotSettings.NewVal("efe", "")
            Dim tdate As Date = Date.UtcNow.AddDays(i)
            Utils.EventLogger.Log("Generar efemérides " & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00"), "GenEfemerides")
            If Not Checked(tdate) Then
                Utils.BotSettings.Set("efecheck", False.ToString)
                Utils.EventLogger.Log("Efemérides no revisadas", "GenEfemerides")
                results.Add(False)
                Continue For
            End If
            results.Add(GenEfemerides(tdate))
            Utils.BotSettings.Set("efecheck", True.ToString)
        Next

        Dim tresult As Boolean = True
        For Each b As Boolean In results
            tresult = (tresult And b)
        Next
        Return tresult
    End Function


    Function CheckEfe() As Boolean
        Dim tdate As Date = Date.Now.AddDays(-1)
        Dim yfile1 As String = Exepath & "hfiles" & DirSeparator & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00") & ".htm"
        Dim yfile2 As String = Exepath & "hfiles" & DirSeparator & tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00") & ".mp4"
        If IO.File.Exists(yfile1) Then
            IO.File.Delete(yfile1)
        End If
        If IO.File.Exists(yfile2) Then
            IO.File.Delete(yfile2)
        End If
        Return Allefe()
    End Function

    Function Createhfiles(ByVal tdate As Date) As Boolean
        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim Fecha As String = tdate.ToString("d 'de' MMMM", New Globalization.CultureInfo("es-ES"))

        If Not IO.File.Exists(Header) Then
            IO.File.Create(Header).Close()
        End If
        If Not IO.File.Exists(Bottom) Then
            IO.File.Create(Bottom).Close()
        End If
        Dim htext As String = IO.File.ReadAllText(Header)
        Dim btext As String = IO.File.ReadAllText(Bottom)

        Dim efeinfopath As String = Hfolder & tdatestring & ".htm"
        Dim efeinfotext As String = htext & "Efemérides del " & Fecha & " en Wikipedia, la enciclopedia libre."
        efeinfotext = efeinfotext & Environment.NewLine & Environment.NewLine & "Enlaces:"
        Dim efes As EfeInfo = GetEfeInfo(tdate)
        For Each ef As Efe In efes.EfeDetails
            efeinfotext = efeinfotext & Environment.NewLine & "• "
            If ef.Type = Efetype.Nacimiento Then
                efeinfotext = efeinfotext & "Nacimiento de "
            End If
            If ef.Type = Efetype.Defunción Then
                efeinfotext = efeinfotext & "Muerte de "
            End If
            efeinfotext = efeinfotext & ef.Page & ": "
            efeinfotext = efeinfotext & "http://es.wikipedia.org/wiki/" & Utils.UrlWebEncode(ef.Page.Replace(" "c, "_"c))
        Next
        efeinfotext = efeinfotext & btext
        IO.File.WriteAllText(efeinfopath, efeinfotext)
        Return True
    End Function


    Function GenEfemerides(ByVal tdate As Date) As Boolean
        Dim Generated As Boolean = True
        Createhfiles(tdate)
        If Not CheckResources() Then
            Utils.EventLogger.Log("Faltan recursos en /Res", "GenEfemerides")
            Return False
        End If

        Utils.EventLogger.Log("Generando imágenes para las efemérides", "GenEfemerides")
        Dim Tpath As String = Exepath & "Images" & DirSeparator
        Dim imagename As String = "efe"
        Dim current As Integer = Createintro(imagename, Tpath, tdate)
        current = CallImages(current, imagename, Tpath, tdate)
        current = Blackout(current, imagename, Tpath, tdate)
        Utils.EventLogger.Log(current.ToString & " Imágenes generadas.", "GenEfemerides")

        If Not EncodeVideo(Tpath, tdate) Then
            Utils.EventLogger.EX_Log("No se ha generado video", "GenEfemerides")
            Generated = False
        End If

        Utils.EventLogger.Log("Limpiando imágenes temporales", "GenEfemerides")
        For Each f As String In IO.Directory.GetFiles(Tpath)
            Try
                IO.File.Delete(f)
            Catch ex As Exception
                Utils.EventLogger.EX_Log("Error al eliminar el archivo """ & f & """", "GenEfemerides")
            End Try
        Next
        Utils.EventLogger.Log("Proceso completo", "GenEfemerides")
        Utils.BotSettings.Set("efe", tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00"))
        Return Generated
    End Function


    Function EncodeVideo(ByVal tpath As String, tdate As Date) As Boolean
        Utils.EventLogger.Log("Generando video", "EncodeVideo")
        Dim tdatestring As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Try
            If OS.ToLower.Contains("windows") Then
                Utils.EventLogger.Log("Plataforma: Windows", "EncodeVideo")
                Utils.EventLogger.Log("Llamando a ffmpeg", "EncodeVideo")
                Using exec As New Process
                    exec.StartInfo.FileName = "ffmpeg"
                    exec.StartInfo.UseShellExecute = True
                    exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "efe%04d.jpg""" & " -vcodec libx264 -preset slower -crf 19 """ & Hfolder & tdatestring & ".mp4"""
                    exec.Start()
                    exec.WaitForExit()
                End Using
            Else
                'Assume linux
                Utils.EventLogger.Log("Plataforma: Linux", "EncodeVideo")
                Utils.EventLogger.Log("Llamando a avconv", "EncodeVideo")
                Using exec As New Process
                    exec.StartInfo.FileName = "avconv"
                    exec.StartInfo.UseShellExecute = True
                    exec.StartInfo.Arguments = "-y -r 29 -i """ & tpath & "efe%04d.jpg""" & " -vcodec libx264 -preset slower -crf 19 """ & Hfolder & tdatestring & ".mp4"""
                    exec.Start()
                    exec.WaitForExit()
                End Using
            End If
        Catch ex As Exception
            Utils.EventLogger.EX_Log("EX Encoding: " & ex.Message, "EncodeVideo")
            Return False
        End Try
        Return True
    End Function

    Function CheckResources() As Boolean
        Dim bmargin As String = Exepath & "Res" & DirSeparator & "bmargin.png"
        Dim Efetxt As String = Exepath & "Res" & DirSeparator & "efetxt.png"
        Dim tbg As String = Exepath & "Res" & DirSeparator & "tbg.png"
        Dim wlogo As String = Exepath & "Res" & DirSeparator & "wlogo.png"
        If Not IO.File.Exists(bmargin) Then Return False
        If Not IO.File.Exists(Efetxt) Then Return False
        If Not IO.File.Exists(tbg) Then Return False
        If Not IO.File.Exists(wlogo) Then Return False
        Return True
    End Function


    Function Createintro(ByVal imagename As String, path As String, tdate As Date) As Integer
        Dim current As Integer = 0
        Using Efeimg As Image = Image.FromFile(Exepath & "Res" & DirSeparator & "efetxt.png")
            Dim wikiimg As Image = Image.FromFile(Exepath & "Res" & DirSeparator & "wlogo.png")
            wikiimg = New Bitmap(wikiimg, New Size(CInt(wikiimg.Width / 3), CInt(wikiimg.Height / 3)))
            Using Bgimg As Image = New Bitmap(700, 720)
                Using tdrawing As Graphics = Graphics.FromImage(Bgimg)
                    tdrawing.Clear(Color.White)
                    tdrawing.Save()
                    Dim Fecha As String = tdate.ToString("dd 'de' MMMM", New Globalization.CultureInfo("es-ES"))
                    Using fechaimg As Image = DrawText(Fecha, New Font(FontFamily.GenericSansSerif, 35.0!, FontStyle.Regular), Color.Black, Color.White, True)
                        current = DragRightToLeft(Bgimg, Efeimg, New Point(0, 0), 0.2F, imagename, path, 0)
                        Dim lastimg As Image = Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
                        current = DragRightToLeft(lastimg, fechaimg, New Point(lastimg.Width - fechaimg.Width, 0), 0.08F, imagename, path, current)
                        lastimg = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
                        Using timage As Image = PasteImage(lastimg, wikiimg, New Point(CInt((lastimg.Width - wikiimg.Width) / 2), 150))
                            current = PasteFadeIn(lastimg, timage, New Point(0, 0), imagename, path, current)
                            lastimg = Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
                            current = Repeatimage(path, imagename, current, lastimg, 60)
                        End Using
                        lastimg.Dispose()
                    End Using
                End Using
            End Using
            wikiimg.Dispose()
        End Using
        Return current
    End Function

    Function Blackout(ByVal current As Integer, imagename As String, path As String, tDate As Date) As Integer
        Dim lastimg As Drawing.Image = Drawing.Image.FromFile(path & imagename & current.ToString("0000") & ".jpg")
        Dim outimg As Image = New Bitmap(700, 720)
        Using timg As Image = New Bitmap(700, 720)
            Using gr As Graphics = Graphics.FromImage(timg)
                gr.Clear(Color.Black)
                gr.Save()
                outimg = CType(timg.Clone, Image)
            End Using
        End Using
        current = PasteFadeIn(lastimg, outimg, New Point(0, 0), imagename, path, current)
        outimg.Dispose()
        Return current
    End Function

    Function GetEfeInfo(ByVal tdate As Date) As EfeInfo
        Dim efelist As New List(Of Tuple(Of String, String()))
        Dim efetxt As String() = GetEfetxt(tdate)
        Dim Varlist As New Dictionary(Of String, List(Of Tuple(Of String, String)))

        If efetxt.Count > 0 Then
            For Each s As String In efetxt
                If Not String.IsNullOrWhiteSpace(s) Then
                    Dim var As String = s.Split("="c)(0).Trim
                    Dim vValue As String = s.Split("="c)(1).Trim
                    Dim vProperty As String = String.Empty
                    If var.Contains("."c) Then
                        vProperty = var.Split("."c)(1).Trim
                        var = var.Split("."c)(0).Trim
                    End If
                    If Not Varlist.Keys.Contains(var) Then
                        Varlist.Add(var, New List(Of Tuple(Of String, String)))
                    End If
                    If Not String.IsNullOrWhiteSpace(vProperty) Then
                        Varlist(var).Add(New Tuple(Of String, String)(vProperty, vValue))
                    Else
                        Varlist(var).Add(New Tuple(Of String, String)("", vValue))
                    End If
                End If
            Next
        End If

        Dim Efinfo As New EfeInfo
        For Each k As String In Varlist.Keys
            If k = "revisadas" Then
                Dim varrev As String = Varlist("revisadas")(0).Item2
                If varrev = "sí" Then
                    Efinfo.Revised = True
                Else
                    Efinfo.Revised = False
                End If
            End If
            If k = "página para imagen" Then
                Dim varval As String = Varlist("página para imagen")(0).Item2
                Efinfo.ImagePage = varval
            End If
            If k = "wikitexto" Then
                Dim varval As String = Varlist("wikitexto")(0).Item2
                Efinfo.Wikitext = varval
            End If
            If k.Contains("-"c) Then
                Dim tef As New Efe
                For Each t As Tuple(Of String, String) In Varlist(k)
                    Dim varname As String = t.Item1
                    Dim varval As String = t.Item2
                    If varname = "año" Then
                        tef.Year = Integer.Parse(varval)
                    End If
                    If varname = "descripción" Then
                        tef.Description = varval
                    End If
                    If varname = "imagen" Then
                        tef.Image = varval
                    End If
                    If varname = "página" Then
                        tef.Page = varval
                    End If
                    If varname = "tamaño" Then
                        tef.TextSize = Double.Parse(varval.Replace(","c, DecimalSeparator))
                    End If
                    If varname = "tipo" Then
                        Select Case varval
                            Case "F"
                                tef.Type = Efetype.Defunción
                            Case "A"
                                tef.Type = Efetype.Acontecimiento
                            Case "N"
                                tef.Type = Efetype.Nacimiento
                        End Select
                    End If
                Next
                Efinfo.EfeDetails.Add(tef)
            End If
        Next
        Return Efinfo
    End Function

    Function GetEfetxt(ByVal tdate As Date) As String()
        Dim fechastr As String = tdate.Year.ToString & tdate.Month.ToString("00") & tdate.Day.ToString("00")
        Dim efetxt As String = "https://tools.wmflabs.org/jembot/ef/pub/" & fechastr & "/" & fechastr & ".txt"
        Dim txt As String = String.Empty
        Try
            txt = Bot.GET(efetxt)
        Catch ex As Exception
        End Try
        If String.IsNullOrWhiteSpace(txt) Then Return {""}
        Dim txtlist As List(Of String) = txt.Split(CType(vbLf, Char())).ToList
        For i As Integer = 0 To txtlist.Count - 1
            txtlist(i) = txtlist(i).Replace("█", Environment.NewLine)
        Next
        Return txtlist.ToArray
    End Function

    Function Checked(tdate As Date) As Boolean
        Dim ttext As String() = GetEfetxt(tdate)
        If ttext.Count > 31 Then
            Return GetEfetxt(tdate)(31).Split("="c)(1).ToLower.Trim = "sí"
        Else
            Return False
        End If
        Return False
    End Function

    Function CallImages(ByVal current As Integer, imagename As String, path As String, tDate As Date) As Integer
        Dim efes As EfeInfo = GetEfeInfo(tDate)
        Dim c As Integer = 0
        For Each ef As Efe In efes.EfeDetails
            Dim año As Integer = ef.Year
            Dim Description As String = ef.Description
            Dim Commonsimg As String = "File:" & ef.Image
            Dim commonsimgdata As Tuple(Of Image, String()) = GetCommonsFile(Commonsimg)
            Dim licence As String = commonsimgdata.Item2(0)
            Dim licenceurl As String = commonsimgdata.Item2(1)
            Dim author As String = commonsimgdata.Item2(2)
            Using cimage As Image = commonsimgdata.Item1
                current = Createimages(path, imagename, current, ef.Image, cimage, licence, licenceurl, author, año, Description, ef.TextSize)
            End Using
            c += 6
        Next
        Return current
    End Function

    Function Createimages(ByVal Path As String, imagename As String, current As Integer, efimgname As String, efimg As Image, licencename As String, licenceurl As String, artist As String, year As Integer, description As String, textsize As Double) As Integer
        If licencename.ToLower = "public domain" Then licencename = "En dominio público"
        If Not String.IsNullOrWhiteSpace(licenceurl) Then licencename = licencename & " (" & licenceurl & ")"
        Dim CommonsName As String = "Imagen en Wikimedia Commons:  " & Utils.NormalizeUnicodetext(efimgname)
        Dim detailstext As String = CommonsName & Environment.NewLine & "Autor: " & artist & Environment.NewLine & "Licencia: " & licencename
        Dim yeardiff As Integer = Date.Now.Year - year
        Dim syeardiff As String = "Hace " & yeardiff.ToString & " años"
        Dim hratio As Double = efimg.Width / efimg.Height

        efimg = New Bitmap(efimg, New Size(700, CInt(720 / hratio)))
        If efimg.Height < 720 Then
            Using timg As Image = New Bitmap(700, 720)
                Using gr As Graphics = Graphics.FromImage(timg)
                    gr.Clear(Color.White)
                    gr.Save()
                    efimg = PasteImage(timg, efimg, New Point(0, CInt((720 - efimg.Height) / 2)))
                End Using
            End Using
        End If

        Dim lastimg As Drawing.Image = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
        lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
        current = PasteFadeIn(lastimg, efimg, New Point(0, 0), imagename, Path, current)
        lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")

        Using yrimg As Image = DrawText2(year.ToString, New Font(FontFamily.GenericSansSerif, 70.0!, FontStyle.Regular), Color.Black, Color.White, True)

            Using añoimg As Drawing.Image = PasteImage(lastimg, yrimg, New Point(0, 0))
                current = PasteFadeIn(lastimg, añoimg, New Point(0, 0), imagename, Path, current)
                lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
            End Using

            Using dimg As Image = DrawText2(syeardiff, New Font(FontFamily.GenericSansSerif, 30.0!, FontStyle.Regular), Color.Black, Color.White, True)
                Using Diffimg As Image = PasteImage(lastimg, dimg, New Point(0, 90))
                    current = PasteFadeIn(lastimg, Diffimg, New Point(0, 0), imagename, Path, current)
                End Using
                Using bgt As Image = Image.FromFile(Exepath & "Res" & DirSeparator & "tbg.png")
                    lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
                    Dim defbgt As Image = PasteImage(bgt, yrimg, New Point(0, 0))
                    defbgt = PasteImage(defbgt, dimg, New Point(0, 90))
                    current = PasteFadeIn(lastimg, defbgt, New Point(0, 0), imagename, Path, current)
                    defbgt.Dispose()
                    lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
                End Using
            End Using
        End Using

        Using descimg As Drawing.Image = DrawText(description, New Font(FontFamily.GenericSansSerif, Convert.ToSingle(3.0! * textsize), FontStyle.Regular), Color.White, Color.White, True)
            Using timage As Image = PasteImage(lastimg, descimg, New Point(CInt((lastimg.Width - descimg.Width) / 2), 350))
                current = PasteFadeIn(lastimg, timage, New Point(0, 0), imagename, Path, current)
                lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
            End Using
        End Using

        Using bmargin As Image = Image.FromFile(Exepath & "Res" & DirSeparator & "bmargin.png")
            Dim bimg As Image = PasteImage(lastimg, bmargin, New Point(0, 642))
            current = PasteFadeIn(lastimg, bimg, New Point(0, 0), imagename, Path, current)
            lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
        End Using

        Using detailsimg As Image = DrawText(detailstext, New Font(FontFamily.GenericMonospace, 10.0!, FontStyle.Regular), Color.LightGray, Color.White, False)
            current = DragRightToLeft(lastimg, detailsimg, New Point(0, 650), 0.6F, imagename, Path, current)
            lastimg = Drawing.Image.FromFile(Path & imagename & current.ToString("0000") & ".jpg")
            current = Repeatimage(Path, imagename, current, lastimg, 120)
        End Using

        Using transimg As Image = New Bitmap(700, 720)
            Using g As Graphics = Graphics.FromImage(transimg)
                g.Clear(Color.White)
                g.Save()
            End Using
            current = PasteFadeIn(lastimg, transimg, New Point(0, 0), imagename, Path, current)
            lastimg.Dispose()
        End Using

        Return current
    End Function

    Public Function Repeatimage(ByVal Path As String, imagename As String, current As Integer, efimg As Image, repetitions As Integer) As Integer
        For i = 0 To repetitions
            current += 1
            Using timg As Image = CType(efimg.Clone, Image)
                timg.Save(Path & imagename & current.ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
            End Using
        Next
        Return current
    End Function

    Public Function DragRightToLeft(ByVal Bgimg As Drawing.Image, ByVal fimg As Drawing.Image, imgpos As Point, ByVal speed As Double, Imagename As String, Imagepath As String, Startingindex As Integer) As Integer
        Dim last As Integer = Startingindex
        Dim imglist As New List(Of Drawing.Image)
        Using bgim As Drawing.Image = CType(Bgimg.Clone, Image)
            Using fim As Drawing.Image = CType(fimg.Clone, Image)
                Dim twidth As Integer = bgim.Width
                For i As Integer = twidth To imgpos.X Step -1
                    Using tfimg As Drawing.Image = CType(fim.Clone, Image)
                        Using tBgimg As Drawing.Image = CType(bgim.Clone, Image)
                            Dim tpos As Integer = CInt(twidth * (Math.E ^ (-(twidth - i) * speed)))
                            If (tpos < (imgpos.X + 5)) Then
                                Using nimg As Drawing.Image = PasteImage(tBgimg, tfimg, New Point(imgpos.X, imgpos.Y))
                                    imglist.Add(CType(nimg.Clone, Image))
                                End Using
                                Exit For
                            End If
                            Using nimg As Drawing.Image = PasteImage(tBgimg, tfimg, New Point(tpos, imgpos.Y))
                                imglist.Add(CType(nimg.Clone, Image))
                            End Using
                        End Using
                    End Using
                Next
                For i As Integer = 0 To imglist.Count - 1
                    Using timg As Drawing.Image = CType(imglist(i).Clone, Image)
                        timg.Save(Imagepath.ToString & DirSeparator & Imagename & (last + i + 1).ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                    End Using
                Next
            End Using
        End Using
        Dim imgcount As Integer = imglist.Count
        imglist = Nothing
        last = last + imgcount
        Return last
    End Function

    Public Function PasteImage(ByVal bgimage As Drawing.Image, ByVal frontimage As Drawing.Image, ByVal startpos As Point) As Drawing.Image
        Dim newimg As Bitmap = CType(bgimage.Clone, Bitmap)
        Using fimg As Bitmap = CType(frontimage.Clone, Bitmap)
            Using g As Graphics = Graphics.FromImage(newimg)
                g.DrawImage(fimg, New Point(startpos.X, startpos.Y))
                g.Save()
            End Using
        End Using
        Dim timg As Drawing.Image = newimg
        Return timg
    End Function

    Public Function PasteFadeIn(ByVal Bgimg As Drawing.Image, ByVal fimg As Drawing.Image, imgpos As Point, Imagename As String, Imagepath As String, Startingindex As Integer) As Integer
        Dim Counter As Integer = Startingindex
        Using nimg As Image = PasteImage(CType(Bgimg.Clone, Image), CType(fimg.Clone, Image), imgpos)
            Counter = Fadein(Imagepath, Imagename, Startingindex, CType(Bgimg.Clone, Image), CType(nimg.Clone, Image))
        End Using
        Return Counter
    End Function

    Function CutImage(ByVal timg As Image, pos As Point, size As Point) As Image
        Dim newimg As New Bitmap(size.X, size.Y)
        Using fimg As Bitmap = CType(timg.Clone, Bitmap)
            Dim xpos As Integer = 0
            Dim ypos As Integer = 0
            For y As Integer = pos.Y To size.Y - 1 + pos.Y
                For x As Integer = pos.X To size.X - 1 + pos.X
                    Dim tcolor As Color = fimg.GetPixel(x, y)
                    newimg.SetPixel(xpos, ypos, tcolor)
                    xpos += 1
                Next
                xpos = 0
                ypos += 1
            Next
        End Using
        Return newimg
    End Function

    ''' <summary>
    ''' La imagen de fondo debe tener el mismo tamaño que la de frente
    ''' </summary>
    ''' <param name="Bgimg"></param>
    ''' <param name="image"></param>
    ''' <returns></returns>
    Public Function Fadein(ByVal Path As String, imagename As String, current As Integer, ByVal Bgimg As Image, ByVal image As Drawing.Image) As Integer
        Using orig As Bitmap = CType(image.Clone, Bitmap)
            Using bg As Bitmap = CType(Bgimg.Clone, Bitmap)
                For b As Integer = 0 To 30
                    Using graphic As Graphics = Graphics.FromImage(bg)
                        Dim tmatrix As Single()() = {
                        New Single() {1, 0, 0, 0, 0},
                        New Single() {0, 1, 0, 0, 0},
                        New Single() {0, 0, 1, 0, 0},
                        New Single() {0, 0, 0, Convert.ToSingle((1 / 30) * b), 0},
                        New Single() {0, 0, 0, 0, 1}
                        }
                        Dim cmatrix As Imaging.ColorMatrix = New Imaging.ColorMatrix(tmatrix)
                        Dim imageatt As New Imaging.ImageAttributes()
                        imageatt.SetColorMatrix(cmatrix, Imaging.ColorMatrixFlag.Default, Imaging.ColorAdjustType.Bitmap)

                        Dim ignorecallback As New Graphics.DrawImageAbort(AddressOf Ignore)
                        Dim tpoint As New Point(0, 0)
                        Dim trectangle As New Rectangle(0, 0, bg.Width, bg.Height)
                        graphic.DrawImage(orig, trectangle, 0F, 0F, bg.Width, bg.Height, GraphicsUnit.Pixel, imageatt)
                        graphic.Save()
                    End Using
                    bg.Save(Path & imagename & (current + 1).ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                    current = current + 1
                Next
                orig.Save(Path & imagename & (current + 1).ToString("0000") & ".jpg", Imaging.ImageFormat.Jpeg)
                current = current + 1
            End Using
        End Using
        Return current
    End Function

    Function Ignore(ByVal callbackdata As IntPtr) As Boolean
        Return False
    End Function

    Public Function DrawText2(ByVal text As String, ByVal font As Font, ByVal textcolor As Color, ByVal backcolor As Color, ByVal center As Boolean) As Drawing.Image
        text = text.Replace("'''", "") 'Por ahora ignoremos las negritas
        Dim Lines As String() = Utils.GetLines(text)
        Dim images As New List(Of Drawing.Image)
        For Each line As String In Lines
            images.Add(DrawSpecialText(line, font, textcolor, backcolor))
        Next

        Dim timg As Drawing.Image = New Bitmap(1, 1)
        Dim totalwidth As Integer = 0
        Dim totalheight As Integer = 0
        For Each limage As Drawing.Image In images
            If limage.Width > totalwidth Then
                totalwidth = limage.Width
            End If
            totalheight = totalheight + limage.Height
        Next
        timg = New Bitmap(totalwidth, totalheight)
        Dim lastheight As Integer = 0
        For Each limage As Drawing.Image In images
            If center Then
                timg = PasteImage(timg, limage, New Point(CInt((totalwidth - limage.Width) / 2), lastheight))
                lastheight = lastheight + limage.Height
            Else
                timg = PasteImage(timg, limage, New Point(0, lastheight))
                lastheight = lastheight + limage.Height
            End If
        Next
        Return timg
    End Function


    Public Function DrawSpecialText(ByVal text As String, ByVal font As Font, ByVal textColor As Color, ByVal backColor As Color) As Image
        text = text.Replace("'''", "")
        text = text.Replace(Environment.NewLine, "").Replace(vbLf, "").Replace(vbCr, "").Replace(vbCrLf, "")
        Dim img As Image = New Bitmap(1, 1)
        Dim drawing As Graphics = Graphics.FromImage(img)
        If String.IsNullOrEmpty(text) Then Return img

        Dim boldfont As New Font(font.FontFamily, font.Size, FontStyle.Regular)
        Dim textSize As SizeF = drawing.MeasureString(text, boldfont)

        img = New Bitmap(CInt(Math.Ceiling(textSize.Width) * 1.2F), CInt((Math.Ceiling(textSize.Height)) * 1.1F))
        drawing = Graphics.FromImage(img)
        drawing.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
        drawing.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Using outlinePath As New Drawing2D.GraphicsPath
            outlinePath.AddString(text, boldfont.FontFamily, FontStyle.Regular, boldfont.SizeInPoints, New Point(0, 0), StringFormat.GenericTypographic)
            drawing.FillPath(Brushes.LightGray, outlinePath)
            drawing.DrawPath(New Pen(textColor), outlinePath)
        End Using

        drawing.Save()
        drawing.Dispose()
        Return img
    End Function

    Public Function DrawText(ByVal text As String, ByVal font As Font, ByVal textColor As Color, ByVal backColor As Color, ByVal center As Boolean) As Drawing.Image
        Dim Lines As String() = Utils.GetLines(text)
        Dim images As New List(Of Drawing.Image)

        For Each line As String In Lines
            images.Add(DrawLine(line, font, textColor, backColor))
        Next

        Dim timg As Drawing.Image = New Bitmap(1, 1)
        Dim totalwidth As Integer = 0
        Dim totalheight As Integer = 0

        For Each limage As Drawing.Image In images
            If limage.Width > totalwidth Then
                totalwidth = limage.Width
            End If
            totalheight = totalheight + limage.Height
        Next
        timg = New Bitmap(totalwidth, totalheight)

        Dim lastheight As Integer = 0
        For Each limage As Image In images
            If center Then
                If limage.Width < timg.Width Then
                    timg = PasteImage(timg, limage, New Point(CInt((timg.Width - limage.Width) / 2), lastheight))
                Else
                    timg = PasteImage(timg, limage, New Point(0, lastheight))
                End If
                lastheight = lastheight + limage.Height
            Else
                timg = PasteImage(timg, limage, New Point(0, lastheight))
                lastheight = lastheight + limage.Height
            End If
        Next
        Return timg
    End Function

    Public Function DrawLine(ByVal text As String, ByVal font As Font, ByVal textColor As Color, ByVal backColor As Color) As Drawing.Image
        Dim img As Drawing.Image = New Bitmap(1, 1)
        text = text.Replace("'''", "") 'Ignoremos negritas
        text = text.Replace(Environment.NewLine, "").Replace(vbLf, "").Replace(vbCr, "").Replace(vbCrLf, "") 'No saltos de linea
        Dim drawing As Graphics = Graphics.FromImage(img)
        If String.IsNullOrEmpty(text) Then Return img

        Dim sf As New StringFormat With {
            .Alignment = StringAlignment.Near,
            .FormatFlags = StringFormatFlags.NoClip,
            .Trimming = StringTrimming.None,
            .HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Hide
        }

        Dim boldfont As New Font(font.FontFamily, font.Size, FontStyle.Bold)
        Dim textSize As SizeF = drawing.MeasureString(text, font)
        Dim boldSize As SizeF = drawing.MeasureString(text, boldfont)

        img.Dispose()
        drawing.Dispose()

        img = New Bitmap(CType(textSize.Width, Integer), CType(textSize.Height, Integer))
        drawing = Graphics.FromImage(img)
        'drawing.Clear(backColor)

        Dim textBrush As Brush = New SolidBrush(textColor)
        Dim textBrush2 As Brush = New SolidBrush(Color.White)
        Dim wordSize As SizeF = drawing.MeasureString(text, font)
        Dim tstring As String = text.Trim

        drawing.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        drawing.DrawString(tstring, font, textBrush, 0, 0, sf)
        drawing.Save()
        textBrush.Dispose()
        drawing.Dispose()
        Return img
    End Function

    Function GetCommonsFile(ByVal CommonsFilename As String) As Tuple(Of Image, String())
        Dim responsestring As String = Utils.NormalizeUnicodetext(Bot.GETQUERY("action=query&format=json&titles=" & Utils.UrlWebEncode(CommonsFilename) & "&prop=imageinfo&iiprop=extmetadata|url&iiurlwidth=500"))
        Dim thumburlmatches As String() = Utils.TextInBetween(responsestring, """thumburl"":""", """,")
        Dim licencematches As String() = Utils.TextInBetween(responsestring, """LicenseShortName"":{""value"":""", """,")
        Dim licenceurlmatches As String() = Utils.TextInBetween(responsestring, """LicenseUrl"":{""value"":""", """,")
        Dim authormatches As String() = Utils.TextInBetween(responsestring, """Artist"":{""value"":""", """,")
        Dim matchstring As String = "<[\S\s]+?>"
        Dim matchstring2 As String = "\([\S\s]+?\)"

        Dim licence As String = String.Empty
        Dim licenceurl As String = String.Empty
        Dim author As String = String.Empty

        If licencematches.Count > 0 Then
            licence = Regex.Replace(licencematches(0), matchstring, "")
        End If
        If licenceurlmatches.Count > 0 Then
            licenceurl = Regex.Replace(licenceurlmatches(0), matchstring, "")
        End If
        If authormatches.Count > 0 Then
            author = Regex.Replace(authormatches(0), matchstring, "")
            author = Regex.Replace(author, matchstring2, "").Trim
            Dim authors As String() = author.Split(CType(Environment.NewLine, Char()))
            If authors.Count > 1 Then
                For i As Integer = 0 To authors.Count - 1
                    If Not String.IsNullOrWhiteSpace(authors(i)) Then
                        author = authors(i)
                    End If
                Next
            End If
            If author.Contains(":") Then
                author = author.Split(":"c)(1).Trim
            End If
        End If
        Dim img As Image = New Bitmap(1, 1)
        If thumburlmatches.Count > 0 Then
            img = PicFromUrl(thumburlmatches(0))
        End If
        If String.IsNullOrWhiteSpace(author) Then
            author = "Anónimo"
        End If
        Return New Tuple(Of Image, String())(img, {licence, licenceurl, author})
    End Function

    Function PicFromUrl(ByVal url As String) As Image
        Dim img As Image = New Bitmap(1, 1)
        Try
            Dim request = WebRequest.Create(url)
            Using response = request.GetResponse()
                Using stream = response.GetResponseStream()
                    img = CType(Image.FromStream(stream).Clone, Image)
                End Using
            End Using
        Catch ex As Exception
        End Try
        'img = Transparent2Color(img, Color.White)
        Return img
    End Function

    Private Function Transparent2Color(ByVal bmp1 As Image, ByVal target As Color) As Bitmap
        Dim bmp2 As Bitmap = New Bitmap(bmp1.Width, bmp1.Height)
        Dim rect As Rectangle = New Rectangle(Point.Empty, bmp1.Size)
        Using G As Graphics = Graphics.FromImage(bmp2)
            G.Clear(target)
            G.DrawImageUnscaledAndClipped(bmp1, rect)
        End Using
        Return bmp2
    End Function

    Private Function TransparentAndImage(bmp1 As Image, ByVal bmp2 As Image, location As Point) As Bitmap
        Dim rect As Rectangle = New Rectangle(location, bmp1.Size)
        Using G As Graphics = Graphics.FromImage(bmp2)
            G.DrawImageUnscaledAndClipped(bmp1, rect)
        End Using
        Return CType(bmp2, Bitmap)
    End Function
End Class

Class EfeInfo
    Property EfeDetails As New List(Of Efe)
    Property Revised As Boolean = False
    Property ImagePage As String
    Property Wikitext As String
End Class

Class Efe
    Property Page As String
    Property Description As String
    Property Year As Integer
    Property Image As String
    Property TextSize As Double
    Property Type As Efetype
End Class

Public Enum Efetype
    Nacimiento
    Defunción
    Acontecimiento
End Enum
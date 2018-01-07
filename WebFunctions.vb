Option Strict On
Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Module WebFunctions
    Private UserAgent As String = BOTName & " (http://es.wikipedia.org/wiki/Usuario_discusión:MarioFinale)"
    Private CRUserAgent As String = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36 OPR/48.0.2685.52"

    ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
    ''' <param name="Sourceurl">URL absoluta del recurso web.</param>
    ''' <param name="getCookies">(Opcional) establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
    Function Gethtmlsource(ByVal Sourceurl As String, Optional getcookies As Boolean = False, Optional ByRef Cookies As CookieContainer = Nothing) As String
        Return GetDataAndResult(Sourceurl, getcookies)
    End Function

    ''' <summary>Realiza una solicitud de tipo GET a un recurso web y retorna el texto.</summary>
    ''' <param name="pageURL">URL absoluta del recurso web.</param>
    ''' <param name="getCookies">Si es "true" establece los cookies en la variable local "cookies" (como cookiecontainer) </param>
    Public Function GetDataAndResult2(ByVal pageURL As String, getCookies As Boolean, Optional ByRef Cookies As CookieContainer = Nothing) As String
        If Cookies Is Nothing Then
            Cookies = New CookieContainer
        End If

        Dim tempcookies As CookieContainer = Cookies

        Dim postreq As HttpWebRequest = DirectCast(HttpWebRequest.Create(pageURL), HttpWebRequest)
        postreq.Method = "GET"
        postreq.KeepAlive = True
        postreq.CookieContainer = tempcookies
        postreq.UserAgent = CRUserAgent
        postreq.ContentType = "application/x-www-form-urlencoded"
        Dim postreqreader As StreamReader
        Dim postresponse As HttpWebResponse
        Dim ok As Boolean = False
        Do Until ok
            Try
                postresponse = DirectCast(postreq.GetResponse, HttpWebResponse)
                tempcookies.Add(postresponse.Cookies)
                postreqreader = New StreamReader(postresponse.GetResponseStream())
                Return postreqreader.ReadToEnd
                ok = True
            Catch ex As System.Net.WebException
                System.Threading.Thread.Sleep(1000)
            End Try
        Loop

        If getCookies Then
            Cookies = tempcookies
        End If

        Return ""


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


End Module

Option Strict On
Option Explicit On

Class LogEngine

#Region "Private vars"
    Private _userData As New List(Of String())
    Private LogQueue As New Queue(Of String())
    Private _endLog As Boolean = False
    Private _logPath As String
    Private _userPath As String
    Private _logging As Boolean
    Private _defaultUser As String
    Private _Debug As Boolean
    Private _maxLogLenght As Integer = 5000
#End Region

#Region "Properties"
    Public _logData As New List(Of String())
    ''' <summary>
    ''' Retorna una lista con todos los eventos en el LOG hasta el momento que se solicita.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Logdata() As List(Of String())
        Get
            Return _logData
        End Get
    End Property
    ''' <summary>
    ''' Retorna una lista con los usuarios que tienen programados avisos de inactividad.
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property LogUserData As List(Of String())
        Get
            Return _userData
        End Get
    End Property
    ''' <summary>
    ''' Si se establece como true, finaliza toda actividad de log y los eventos siguientes no serán guardados en el archivo de LOG (Pero sí en memoria). Es recomendable usar el método .Dispose en su lugar.
    ''' </summary>
    ''' <returns></returns>
    Public Property EndLog As Boolean
        Get
            Return _endLog
        End Get
        Set(value As Boolean)
            _endLog = value
        End Set
    End Property

    Public Property Debug As Boolean
        Get
            Return _Debug
        End Get
        Set(value As Boolean)
            _Debug = value
        End Set
    End Property

#End Region

#Region "Create and Dispose"
    ''' <summary>
    ''' Crea una nueva instancia del motor de registros locales.
    ''' </summary>
    ''' <param name="LogPath">Archivo con ruta donde se guardará el archivo de LOG.</param>
    ''' <param name="UserPath">Archivo con ruta donde se guardará el archivo de usuarios.</param>
    Sub New(ByVal LogPath As String, ByVal UserPath As String, ByVal DefaultUser As String)
        _Debug = False
        _logPath = LogPath
        _userPath = UserPath
        _defaultUser = DefaultUser
        Task.Run(Sub()
                     Do Until _endLog
                         SaveLogWorker()
                         _logging = True
                         System.Threading.Thread.Sleep(1000)
                     Loop
                     _logging = False
                 End Sub)
        LoadUsers()
    End Sub
    ''' <summary>
    ''' Cierra los eventos de log correctamente.
    ''' </summary>
    Public Sub EndLogging()
        EndLog = True
        Do Until _logging = False
            System.Threading.Thread.Sleep(100)
        Loop
    End Sub

#End Region

#Region "Log Functions"
    ''' <summary>
    ''' Registra un evento normal.
    ''' </summary>
    ''' <param name="text">Texto del evento</param>
    ''' <param name="source">origen del evento</param>
    ''' <returns></returns>
    Function Log(ByVal text As String, source As String) As Boolean
        Return Log(text, source, BotCodename)
    End Function

    ''' <summary>
    ''' Registra un evento de tipo debug.
    ''' </summary>
    ''' <param name="text">Texto del evento</param>
    ''' <param name="source">origen del evento</param>
    ''' <returns></returns>
    Function Debug_Log(ByVal text As String, source As String) As Boolean
        Return Debug_Log(text, source, BotCodename)
    End Function

    ''' <summary>
    ''' Registra una excepción.
    ''' </summary>
    ''' <param name="text">Texto del evento</param>
    ''' <param name="source">origen del evento</param>
    ''' <returns></returns>
    Function EX_Log(ByVal text As String, source As String) As Boolean
        Return EX_Log(text, source, BotCodename)
    End Function

    ''' <summary>
    ''' Inicia otro thread para guardar un evento de log
    ''' </summary>
    ''' <param name="text">Texto a registrar</param>
    ''' <param name="source">Fuente del evento</param>
    ''' <param name="user">Usuario origen del evento</param>
    ''' <returns></returns>
    Public Function Log(ByVal text As String, ByVal source As String, ByVal user As String) As Boolean
        AddEvent(text, source, user, "LOG")
        Utils.WriteLine("LOG", source, user & ": " & text)
        Return True
    End Function

    ''' <summary>
    ''' Inicia otro thread para guardar un evento de log (debug)
    ''' </summary>
    ''' <param name="text">Texto a registrar</param>
    ''' <param name="source">Fuente del evento</param>
    ''' <param name="user">Usuario origen del evento</param>
    ''' <returns></returns>
    Public Function Debug_Log(ByVal text As String, ByVal source As String, ByVal user As String) As Boolean
        If _Debug Then
            AddEvent(text, source, user, "DEBUG")
        End If
        Return True
    End Function

    ''' <summary>
    ''' Inicia otro thread para guardar un evento de log
    ''' </summary>
    ''' <param name="text">Texto a registrar</param>
    ''' <param name="source">Fuente del evento</param>
    ''' <param name="user">Usuario origen del evento</param>
    ''' <returns></returns>
    Public Function EX_Log(ByVal text As String, ByVal source As String, ByVal user As String) As Boolean
        AddEvent(text, source, user, "EX")
        Utils.WriteLine("EX", source, user & ": " & text)
        Return True
    End Function

    ''' <summary>
    ''' Guarda todos los usuarios y operadores en memoria al archivo.
    ''' </summary>
    ''' <returns></returns>
    Function SaveUsersToFile() As Boolean
        Dim StringToFile As New List(Of String)

        For Each Line As String() In _userData
            Dim Linetxt As String = String.Empty
            For Each item As String In Line
                Linetxt = Linetxt & Utils.PsvSafeEncode(item) & "|"
            Next
            Linetxt = Linetxt.Trim(CType("|", Char))
            StringToFile.Add(Linetxt)
        Next

        Try
            System.IO.File.WriteAllLines(_userPath, StringToFile.ToArray)
            LoadUsers()
            Return True
        Catch ex As Exception
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _defaultUser)
            'Do something, idk...\
            LoadUsers()
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Añade un nuevo usuario a la lista de aviso de inactividad de usuario
    ''' </summary>
    ''' <param name="UserAndTime">Array con {usuario a avisar, tiempo en formato d.hh:mm, operador} </param>
    ''' <returns></returns>
    Function SetUserTime(ByVal UserAndTime As String()) As Boolean
        Try
            Dim RequestedUser As String = UserAndTime(1)
            Dim UserTime As String = UserAndTime(2)
            Dim OP As String = UserAndTime(0)
            Dim UserList As New List(Of Integer)
            For Each line As String() In _userData
                If line(0) = RequestedUser Then
                    UserList.Add(_userData.IndexOf(line))
                End If
            Next
            Dim IsInList As Boolean = False
            Dim IsInListIndex As Integer = -1
            If UserList.Count >= 1 Then
                For Each i As Integer In UserList
                    If _userData(i)(1) = OP Then
                        IsInList = True
                        IsInListIndex = i
                    End If
                Next
            Else
            End If
            If IsInList Then
                _userData(IsInListIndex) = {OP, RequestedUser, UserTime}
            Else
                _userData.Add({OP, RequestedUser, UserTime})
            End If
            SaveUsersToFile()
            Return True
        Catch ex As Exception
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _defaultUser)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Carga los usuarios desde el archivo de usuarios y los guarda en la variable local.
    ''' </summary>
    ''' <returns></returns>
    Function LoadUsers() As Boolean
        Try
            _userData = GetUsersFromFile()
            Return True
        Catch ex As Exception
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _defaultUser)
            Return False
        End Try
    End Function
#End Region

#Region "Data Functions"
    ''' <summary>
    ''' Guarda los datos en el archivo de log, es llamado por otros threads.
    ''' </summary>
    Private Sub SaveLogWorker()
        SaveData(_logPath, LogQueue)
        SyncLock (_logData)
            If _logData.Count > 100 Then
                _logData.RemoveRange(0, 99)
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Guarda los datos desde un queue a un archivo de log.
    ''' </summary>
    ''' <param name="filepath"></param>
    ''' <param name="_queue"></param>
    ''' <returns></returns>
    Private Function SaveData(ByVal filepath As String, ByRef _queue As Queue(Of String())) As Boolean
        Try
            Do Until _queue.Count = 0
                AppendLinesToText(filepath, SafeDequeue(_queue))
                System.Threading.Thread.Sleep(10)
            Loop

            Dim totallines As String() = IO.File.ReadAllLines(filepath)
            If totallines.Count > _maxLogLenght Then
                IO.File.WriteAllLines(filepath, Utils.SplitStringArrayIntoChunks(totallines, _maxLogLenght).Last.ToArray)
            End If
            Return True
        Catch ex As Exception
            EX_Log(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, _defaultUser)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Añade un evento al queue
    ''' </summary>
    ''' <param name="text">Texto a registrar</param>
    ''' <param name="Source">Fuente del evento</param>
    ''' <param name="User">Usuario origen del evento</param>
    ''' <param name="Type">Tipo de evento</param>
    ''' <returns></returns>
    Private Function AddEvent(ByVal text As String, Source As String, User As String, Type As String) As Boolean
        Dim CurrDate As String = Date.Now().ToString("dd/MM/yyyy HH:mm:ss")
        SafeEnqueue(LogQueue, {CurrDate, text, Source, User, Type})
        Return True
    End Function

    ''' <summary>
    ''' Entrega el último registro de eventos.
    ''' </summary>
    ''' <param name="source">Fuente desde donde se solicita el último evento.</param>
    ''' <param name="user">Usuario que lo solicita.</param>
    ''' <returns></returns>
    Public Function Lastlog(ByRef source As String, user As String) As String()
        Dim logresponse As String() = Logdata.Last
        Log("Request of lastlog", source, user)
        Return logresponse
    End Function

    ''' <summary>
    ''' Regresa una lista de string() con todas las lineas en un archivo de texto.
    ''' </summary>
    ''' <param name="filename">Nombre y ruta del archivo</param>
    ''' <returns></returns>
    Private Function LoadLinesFromFile(ByRef filename As String) As List(Of String())
        Dim ItemList As New List(Of String())
        If Not System.IO.File.Exists(filename) Then
            System.IO.File.Create(filename).Close()
        End If
        For Each line As String In System.IO.File.ReadAllLines(filename)
            Dim items As String() = line.Split(CType("|", Char))
            ItemList.Add(items)
        Next
        Return ItemList
    End Function

    ''' <summary>
    ''' Añade línteas a un archivo de texto
    ''' </summary>
    ''' <param name="FilePath">Ruta y nombre del archivo</param>
    ''' <param name="Lines">Líneas a añadir</param>
    ''' <returns></returns>
    Private Function AppendLinesToText(ByVal FilePath As String, Lines As String()) As Boolean
        Try
            If Not System.IO.File.Exists(FilePath) Then
                System.IO.File.Create(FilePath).Close()
            End If
            Using Writer As New System.IO.StreamWriter(FilePath, True)
                Dim LineStr As String = String.Empty
                For Each item As String In Lines
                    LineStr = LineStr & item & "|"
                Next
                LineStr = LineStr.Trim(CType("|", Char))
                Writer.WriteLine(LineStr)
            End Using
            Return True
        Catch ex As Exception
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _defaultUser)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Obtiene los usuarios desde el el archivo y los regresa como string()
    ''' </summary>
    ''' <returns></returns>
    Private Function GetUsersFromFile() As List(Of String())
        Dim UserList As New List(Of String())
        If Not System.IO.File.Exists(User_Filepath) Then
            System.IO.File.Create(User_Filepath).Close()
            Return UserList
        Else
            For Each line As String In System.IO.File.ReadAllLines(User_Filepath)
                Dim Encodedline As New List(Of String)
                For Each s As String In line.Split(CType("|", Char))
                    Encodedline.Add(Utils.PsvSafeDecode(s))
                Next
                UserList.Add(Encodedline.ToArray)
            Next
            Return UserList
        End If
    End Function

    ''' <summary>
    ''' Añade un item al queue de forma segura para ser llamado desde múltiples threads.
    ''' </summary>
    ''' <param name="_QueueToEnqueue">Queue a modificar</param>
    ''' <param name="str">Cadea de texto a añadir</param>
    Private Sub SafeEnqueue(ByVal _QueueToEnqueue As Queue(Of String()), ByVal str As String())
        SyncLock (_logData)
            _logData.Add(str)
        End SyncLock
        SyncLock (_QueueToEnqueue)
            _QueueToEnqueue.Enqueue(str)
        End SyncLock
    End Sub

    ''' <summary>
    ''' Saca un ítem de un queue de forma segura para ser llamado desde múltiples threads.
    ''' </summary>
    ''' <param name="QueueToDequeue"></param>
    ''' <returns></returns>
    Private Shared Function SafeDequeue(ByVal QueueToDequeue As Queue(Of String())) As String()
        SyncLock (QueueToDequeue)
            Return QueueToDequeue.Dequeue()
        End SyncLock
    End Function
#End Region

End Class

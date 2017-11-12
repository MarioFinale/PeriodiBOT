Option Strict On
Imports Microsoft.VisualBasic
Imports System.IO
Imports System.Data
Imports System.Data.SqlClient

Class LogCore
    Public _logData As New List(Of String())
    Private _userData As New List(Of String())
    Private LogQueue As New Queue(Of String())
    Private _endLog As Boolean = False
    Private _logPath As String
    Private _userPath As String
    Private _logging As Boolean

    Public ReadOnly Property Logdata() As List(Of String())
        Get
            Return _logData
        End Get
    End Property

    Public ReadOnly Property LogUserData As List(Of String())
        Get
            Return _userData
        End Get
    End Property

    Public Property EndLog As Boolean
        Get
            Return _endLog
        End Get
        Set(value As Boolean)
            _endLog = value
        End Set
    End Property

    Public Sub New(ByVal LogPath As String, ByVal UserPath As String)

        _logPath = LogPath
        _userPath = UserPath
        Task.Run(Sub()
                     Do Until _endLog
                         SaveLogWorker()
                         _logging = True
                         System.Threading.Thread.Sleep(10000)
                     Loop
                     _logging = False
                 End Sub)
        LoadUsers()
    End Sub

    Public Sub Dispose()
        EndLog = True
        Do Until _logging = False
            System.Threading.Thread.Sleep(100)
        Loop
    End Sub

    Sub SaveLogWorker()
        SaveData(_LogPath, LogQueue)
    End Sub

    Private Function SaveData(ByVal filepath As String, ByRef _queue As Queue(Of String())) As Boolean
        Try
            Do Until _queue.Count = 0
                AppendLinesToText(filepath, SafeDequeue)
                System.Threading.Thread.Sleep(100)
            Loop
            Return True
        Catch ex As Exception
            Debug_log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", BOTName)
            Return False
        End Try
    End Function

    Public Function Log(ByVal text As String, ByVal source As String, ByVal user As String) As Boolean

        Task.Run(Sub()
                     AddEvent(text, source, user, "LOG")
                 End Sub)
        Console.WriteLine("Log|" & source & "|" & user & "|" & text)

        Return True
    End Function

    Public Function Debug_log(ByVal text As String, ByVal source As String, ByVal user As String) As Boolean
        Task.Run(Sub()
                     AddEvent(text, source, user, "DEBUG")
                 End Sub)
        Return True
    End Function

    Private Function AddEvent(ByVal text As String, Source As String, User As String, Type As String) As Boolean
        Dim CurrDate As String = Date.Now().ToString("dd/MM/yyyy HH:mm:ss")

        SafeEnqueue({CurrDate, text, Source, User, Type})
        Return True
    End Function

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
            Debug_log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", BOTName)
            Return False
        End Try

    End Function

    Public Function Lastlog(ByRef source As String, user As String) As String()
        Dim logresponse As String() = LogData.Last
        Log("Request of lastlog", source, user)
        Return logresponse
    End Function

    Function SaveUsersToFile() As Boolean
        Dim StringToFile As New List(Of String)

        For Each Line As String() In _userData
            Dim Linetxt As String = String.Empty
            For Each item As String In Line
                Linetxt = Linetxt & PsvSafeEncode(item) & "|"
            Next
            Linetxt = Linetxt.Trim(CType("|", Char))
            StringToFile.Add(Linetxt)
        Next

        Try
            System.IO.File.WriteAllLines(_userPath, StringToFile.ToArray)
            LoadUsers()
            Return True
        Catch ex As Exception
            Debug_log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", BOTName)
            'Do something, idk...\
            LoadUsers()
            Return False
        End Try
    End Function

    Public Function SetUserTime(ByVal UserAndTime As String()) As Boolean
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
            Userdata = _userData
            Return True
        Catch ex As Exception
            Debug_log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", BOTName)
            Return False
        End Try

    End Function

    Public Function LoadUsers() As Boolean
        Try
            _userData = GetUsersFromFile()
            UserData = _userData
            Return True
        Catch ex As Exception
            Debug_log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", BOTName)
            Return False
        End Try

    End Function

    Private Function GetUsersFromFile() As List(Of String())
        Dim UserList As New List(Of String())

        If Not System.IO.File.Exists(User_Filepath) Then
            System.IO.File.Create(User_Filepath).Close()
            Return UserList
        Else
            For Each line As String In System.IO.File.ReadAllLines(User_Filepath)
                Dim Encodedline As New List(Of String)
                For Each s As String In line.Split(CType("|", Char))
                    Encodedline.Add(PsvSafeDecode(s))
                Next
                UserList.Add(Encodedline.ToArray)
            Next
            Return UserList
        End If
    End Function



    Sub SafeEnqueue(ByVal str As String())
        SyncLock (_logData)
            Logdata.Add(str)
        End SyncLock
        SyncLock (LogQueue)
            LogQueue.Enqueue(str)
        End SyncLock
    End Sub

    Function SafeDequeue() As String()
        SyncLock (LogQueue)
            Return LogQueue.Dequeue()
        End SyncLock
    End Function

End Class


Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.WikiBot
Imports PeriodiBOT_IRC.CommFunctions

Namespace IRC
    Class IRCCommandResolver
        Private LastMessage As IRCMessage
        Private _IrcNickName As String
        Private Client As IRC_Client
        Private _bot As Bot
        Private CommandPrefixes As String() = {"%", "pb%", "pepino%"}


        Public Function ResolveCommand(ByVal imputline As String, ByRef HasExited As Boolean, ByVal BOTIRCNickName As String, IRCCLient As IRC_Client, WorkerBot As Bot) As IRCMessage
            Client = IRCCLient
            _bot = WorkerBot
            _IrcNickName = BOTIRCNickName

            Try
                If Not imputline = Nothing Then
                    Dim CommandResponse As New IRCMessage(_IrcNickName, "")
                    Dim sCommandParts As String() = imputline.Split(CType(" ", Char()))
                    Dim Prefix As String = sCommandParts(0)

                    If sCommandParts.Length >= 4 Then
                        Dim Command As String = sCommandParts(1)
                        If Command = "NOTICE" Then
                            Return Nothing
                        End If
                        Dim Source As String = sCommandParts(2)
                        Dim param As String = GetParamString(imputline)
                        Dim Realname As String = GetUserFromChatresponse(Prefix)

                        If Source.ToLower = _IrcNickName.ToLower Then
                            Source = Realname
                        End If
                        Dim sCommandText As String = String.Empty
                        For i As Integer = 3 To sCommandParts.Length - 1
                            sCommandText = sCommandText & " " & sCommandParts(i)
                        Next

                        Select Case Command
                            Case "PRIVMSG"
                                If Source = Realname Then
                                    WriteLine("MSG", "IRC", Source & "" & sCommandText)
                                Else
                                    WriteLine("MSG", "IRC", Source & " " & Realname & "" & sCommandText)
                                End If
                            Case "QUIT"
                                Dim quser As String = Prefix.Split("!"c)(0).Remove(0, 1)
                                WriteLine("MSG", "IRC", "USER " & quser & " QUIT: " & imputline.Split(":"c)(2))
                            Case Else
                                WriteLine("INFO", "IRC", Command & "" & sCommandText)
                        End Select

                        Dim Params As String() = GetParams(param)
                        Dim MainParam As String = Params(0).ToLower
                        Dim ParamsString As String = String.Empty
                        Dim Username As String = String.Empty
                        Dim Totalparam As String = String.Empty
                        If Not MainParam = String.Empty Then
                            ParamsString = param.Replace(Params(0), String.Empty).Trim(CType(" ", Char()))
                            Dim usrarr As String() = param.Split(CType(" ", Char()))
                            For i As Integer = 0 To usrarr.Count - 1
                                If i = 0 Then
                                Else
                                    Username = Username & " " & usrarr(i)
                                End If
                            Next
                            Username = Username.Trim(CType(" ", Char()))
                            Totalparam = Username
                        End If

                        Dim Ultima As String() = {"última", "ultima", "ult", "last"}
                        Dim Usuario As String() = {"usuario", "usuarios", "users", "usr", "usrs"}
                        Dim Programar As String() = {"programar", "programa", "prog", "progr", "prg", "avisa"}
                        Dim Quitar As String() = {"quitar", "quita", "saca", "sacar"}
                        Dim Ordenes As String() = {"ord", "ordenes", "órdenes"}
                        Dim Info As String() = {"??"}
                        Dim Ayuda As String() = {"?", "h", "help", "ayuda"}
                        Dim Resumen As String() = {"resumen", "res", "entrada", "entradilla"}
                        Dim InfoPagina As String() = {"info", "pag", "pageinfo", "infopagina"}
                        Dim ArchivaTodo As String() = {"archiveall"}
                        Dim Entra As String() = {"join"}
                        Dim Sal As String() = {"leave", "part"}
                        Dim Apagar As String() = {"q", "quit"}
                        Dim AgregarOP As String() = {"op"}
                        Dim QuitarOP As String() = {"deop"}
                        Dim ActualizarExtractos As String() = {"updateextracts", "update", "upex", "updex", "updext"}
                        Dim Archivar As String() = {"archive"}
                        Dim Divide0 As String() = {"div0"}
                        Dim Debug As String() = {"debug", "dbg"}
                        Dim CMPrefixess As String() = {"prefix", "prefixes"}
                        Dim Tasks As String() = {"taskl", "tasks"}
                        Dim TaskInf As String() = {"taskinf", "task"}
                        Dim GetLocalTime As String() = {"time", "localtime"}
                        Dim TPause As String() = {"pause", "tpause", "taskpause", "pausetask", "pausa", "pausar"}
                        Dim SetFlood As String() = {"setflood", "sflood"}
                        Dim GetFlood As String() = {"getflood", "gflood"}

                        If Not BeginsWithPrefix(CommandPrefixes, MainParam) Then Return Nothing
                        MainParam = RemovePrefixes(CommandPrefixes, MainParam)
                        If Params.Count >= 1 Then

                            Select Case True

                                Case Ultima.Contains(MainParam)
                                    Dim paramarr As String() = param.Split(CType(" ", Char))
                                    If paramarr.Count >= 2 Then
                                        CommandResponse = LastEdit(Source, Realname, Username)
                                    Else
                                        CommandResponse = CommandInfo(Source, MainParam, Realname)
                                    End If

                                Case Usuario.Contains(MainParam)
                                    CommandResponse = GetProgrammedUsers(Source, Realname)

                                Case Programar.Contains(MainParam)
                                    Dim paramarr As String() = param.Split(CType(" ", Char))
                                    If paramarr.Count >= 2 Then
                                        CommandResponse = ProgramNewUser(Source, Realname, paramarr(1).Trim(CType(" ", Char())))
                                    Else
                                        CommandResponse = CommandInfo(Source, MainParam, Realname)
                                    End If

                                Case Quitar.Contains(MainParam)
                                    CommandResponse = RemoveUser(Source, Realname, ParamsString)

                                Case Ordenes.Contains(MainParam)
                                    CommandResponse = Orders(Source, Realname)

                                Case Info.Contains(MainParam)
                                    CommandResponse = About(imputline, Source, Realname)

                                Case Ayuda.Contains(MainParam)
                                    Dim paramarr As String() = param.Split(CType(" ", Char))
                                    If paramarr.Count >= 2 Then
                                        CommandResponse = CommandInfo(Source, paramarr(1).Trim(CType(" ", Char())), Realname)
                                    Else
                                        CommandResponse = CommandInfo(Source, MainParam, Realname)
                                    End If

                                Case Resumen.Contains(MainParam)
                                    CommandResponse = GetResume(Source, Totalparam, Realname)

                                Case InfoPagina.Contains(MainParam)
                                    CommandResponse = PageInfo(Source, Totalparam, Realname)

                                Case ArchivaTodo.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        Dim paramarr As String() = param.Split(CType(" ", Char))
                                        If paramarr.Count >= 2 Then
                                            CommandResponse = CommandInfo(Source, MainParam, Realname)
                                        Else
                                            Dim arfunc As New Func(Of Boolean)(Function() ArchiveAllInclusions(True))
                                            NewThread("Archivado a solicitud", Realname, arfunc, 1, False)
                                        End If
                                    End If

                                Case Entra.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        Return JoinRoom(Source, Totalparam, Realname)
                                    End If

                                Case Sal.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        Return LeaveRoom(Source, Totalparam, Realname)
                                    End If

                                Case Apagar.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        Return Quit(Source, Realname, HasExited)
                                    End If

                                Case AgregarOP.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = SetOp(imputline, Source, Realname)
                                    End If

                                Case QuitarOP.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = DeOp(imputline, Source, Realname)
                                    End If

                                Case ActualizarExtractos.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        Dim updex As New Func(Of Boolean)(Function() UpdatePageExtracts(True))
                                        NewThread("Actualizar extractos a solicitud", Realname, updex, 1, False)
                                    End If

                                Case Archivar.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        Dim paramarr As String() = param.Split(CType(" ", Char))
                                        If paramarr.Count >= 2 Then
                                            CommandResponse = ArchivePage(Source, Totalparam, Realname, Client)
                                        Else
                                            CommandResponse = CommandInfo(Source, MainParam, Realname)
                                        End If
                                    End If

                                Case Divide0.Contains(MainParam)
                                    Return Div0(Source, Realname)

                                Case Debug.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = SetDebug(Source, Realname)
                                    End If

                                Case CMPrefixess.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = GetPrefixes(Source, CommandPrefixes)
                                    End If

                                Case Tasks.Contains(MainParam)
                                    CommandResponse = GetTasks(Source, Realname)

                                Case TaskInf.Contains(MainParam)
                                    CommandResponse = TaskInfo(Source, Totalparam, Realname)

                                Case GetLocalTime.Contains(MainParam)
                                    CommandResponse = GetTime(Source, Realname)

                                Case TPause.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = PauseTask(Source, Totalparam, Realname)
                                    End If
                                Case SetFlood.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = SetFloodDelay(Source, Totalparam, Realname)
                                    End If
                                Case GetFlood.Contains(MainParam)
                                    If Client.IsOp(imputline, Source, Realname) Then
                                        CommandResponse = GetFloodDelay(Source, Realname)
                                    End If

                                Case Else
                                    If param.ToLower.Contains(_IrcNickName.ToLower) And Not param.ToLower.Contains("*") And Not imputline.Contains(".freenode.net ") Then
                                        CommandResponse = Commands(Source, Realname)
                                    End If
                            End Select
                        End If
                    End If
                    If LastMessage Is Nothing Then
                        Return CommandResponse
                    Else
                        If Not LastMessage.Text.ToArray Is CommandResponse.Text.ToArray Then
                            LastMessage = CommandResponse
                            Return CommandResponse
                        Else
                            Return Nothing
                        End If

                    End If
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                EventLogger.EX_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _IrcNickName)
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Verifica si un comando comienza por uno de los prefijos pasados como parámetro
        ''' </summary>
        ''' <param name="Prefixes">Prefijos.</param>
        ''' <param name="Commandline">Línea a analizar.</param>
        ''' <returns></returns>
        Private Function BeginsWithPrefix(ByVal Prefixes As String(), ByVal Commandline As String) As Boolean
            For Each prefix As String In Prefixes
                If Commandline.ToLower.StartsWith(prefix) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Function RemovePrefixes(ByVal Prefixes As String(), ByVal Commandline As String) As String
            Dim line As String = Commandline
            For Each prefix As String In Prefixes
                If line.StartsWith(prefix) Then
                    line = ReplaceFirst(Commandline, prefix, "")
                    If Not line = Commandline Then
                        Exit For
                    End If
                End If
            Next
            Return line
        End Function


    End Class

End Namespace
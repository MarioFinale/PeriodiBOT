Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.WikiBot

Namespace IRC
    Class IRCCommandResolver
        Private Client As IRC_Client
        Private _bot As Bot
        Private Clist As List(Of IRCCommand)
        Public CommandPrefixes As String() = {"%", "pb%", "pepino%"}

        Sub New()
            Clist = CommandList()
        End Sub

        Function ResolveCommand(ByVal imputline As String, ByVal BOTIRCNickName As String, IRCCLient As IRC_Client, WorkerBot As Bot) As IRCMessage
            Client = IRCCLient
            _bot = WorkerBot
            Dim arg As New IRCCommandParams(imputline, Client, _bot)
            If Not BeginsWithPrefix(CommandPrefixes, arg.MessageLine) Then Return Nothing
            Dim requestedCommand As String = RemovePrefix(arg.CommandName)
            For Each Command As IRCCommand In Clist
                If Command.Aliases.Contains(requestedCommand) Then
                    Utils.EventLogger.Log("Command """ & requestedCommand & """ issued, parameter/s: """ & arg.CParam & """", arg.Source, arg.Realname)
                    Return Command.ComFunc(arg)
                End If
            Next
            If arg.MessageLine.ToLower.Contains(BOTIRCNickName) Then
                Return Greetings(arg)
            End If
            Return Nothing
        End Function

        Private Function CommandList() As List(Of IRCCommand)
            Dim Commands As New IRCCommands

            Dim Ultima As String() = {"última", "ultima", "ult", "last", "getlastedit"}
            Dim Usuario As String() = {"usuarios", "users", "usr", "usrs", "getprogusers"}
            Dim Programar As String() = {"programar", "programa", "prog", "progr", "prg", "avisa", "setprogusers"}
            Dim Quitar As String() = {"quitar", "quita", "saca", "sacar", "removeproguser"}
            Dim Ordenes As String() = {"ord", "ords", "orders", "commands", "avcomms", "ordenes", "órdenes"}
            Dim Info As String() = {"??", "acerca", "about"}
            Dim Ayuda As String() = {"?", "h", "help", "ayuda"}
            Dim Resumen As String() = {"resumen", "res", "entrada", "entradilla", "pageresume"}
            Dim InfoPagina As String() = {"info", "pag", "pageinfo", "infopagina", "pageinfo"}
            Dim ArchivaTodo As String() = {"archivatodo", "archiveall"}
            Dim Entra As String() = {"join", "joinroom"}
            Dim Sal As String() = {"leave", "part", "leaveroom"}
            Dim Apagar As String() = {"q", "quit"}
            Dim AgregarOP As String() = {"op", "addop"}
            Dim QuitarOP As String() = {"deop", "delop", "removeop"}
            Dim ActualizarExtractos As String() = {"updateextracts", "update", "upex", "updex", "updext"}
            Dim Archivar As String() = {"archive", "arch"}
            Dim Divide0 As String() = {"div0"}
            Dim Debug As String() = {"debug", "dbg"}
            Dim Tasks As String() = {"taskl", "tasks"}
            Dim TaskInf As String() = {"taskinf", "task", "taskinfo"}
            Dim GetLocalTime As String() = {"time", "localtime", "hora", "horalocal"}
            Dim TPause As String() = {"pause", "tpause", "taskpause", "pausetask", "pausa", "pausar"}
            Dim SetFlood As String() = {"setflood", "sflood", "setflooddelay"}
            Dim GetFlood As String() = {"getflood", "gflood", "getflooddelay"}
            Dim GenEfem As String() = {"genefe", "efe", "efem"}

            Dim _Clist As New List(Of IRCCommand)

            Dim LastEdit As New IRCCommand("GetLastEdit", Ultima, AddressOf Commands.LastEdit, "Entrega el tiempo (Aproximado) que ha pasado desde la ultima edicion del usuario.", " <usuario>")
            _Clist.Add(LastEdit)
            Dim UserInfo As New IRCCommand("GetProgUsers", Usuario, AddressOf Commands.GetProgrammedUsers, "Entrega una lista de los usuarios programados en tu lista.", "")
            _Clist.Add(UserInfo)
            Dim ProgUser As New IRCCommand("SetProgUsers", Programar, AddressOf Commands.ProgramNewUser, "Programa un aviso en caso de que un usuario no edite en un tiempo específico.", " Usuario/Dias/Horas/Minutos")
            _Clist.Add(ProgUser)
            Dim RemoveUser As New IRCCommand("RemoveProgUser", Quitar, AddressOf Commands.RemoveUser, "Quita un usuario de tu lista programada (Más info: %? %programar).", " <usuario>")
            _Clist.Add(RemoveUser)
            Dim BotInfo As New IRCCommand("About", Info, AddressOf Commands.About, "Entrega información técnica sobre el bot (limitado según el usuario).", "")
            _Clist.Add(BotInfo)
            Dim GetResume As New IRCCommand("PageResume", Resumen, AddressOf Commands.GetResume, "Entrega la entradilla de un artículo en Wikipedia.", " <Artículo>")
            _Clist.Add(GetResume)
            Dim PageInfo As New IRCCommand("PageInfo", InfoPagina, AddressOf Commands.PageInfo, "Entrega datos sobre un artículo en Wikipedia.", " <Artículo>")
            _Clist.Add(PageInfo)
            Dim ArchiveAll As New IRCCommand("ArchiveAll", ArchivaTodo, AddressOf Commands.ArchiveAll, "Archiva todas las páginas que incluyan la plantilla de archivado.", "")
            _Clist.Add(ArchiveAll)
            Dim JoinRoom As New IRCCommand("JoinRoom", Entra, AddressOf Commands.JoinRoom, "Indica al bot que entre a la sala solicitada.", " <sala>")
            _Clist.Add(JoinRoom)
            Dim LeaveRoom As New IRCCommand("LeaveRoom", Sal, AddressOf Commands.LeaveRoom, "Indica al bot que salga de la sala solicitada.", " <sala>")
            _Clist.Add(LeaveRoom)
            Dim Quit As New IRCCommand("Quit", Apagar, AddressOf Commands.Quit, "Apaga el bot.", "")
            _Clist.Add(Quit)
            Dim AddOP As New IRCCommand("AddOP", AgregarOP, AddressOf Commands.SetOp, "Añade un nuevo operador.", " <nickname!hostname>")
            _Clist.Add(AddOP)
            Dim DeOP As New IRCCommand("DeOP", QuitarOP, AddressOf Commands.DEOp, "Elimina a un operador.", " <nickname!hostname>")
            _Clist.Add(DeOP)
            Dim UpdateExtracts As New IRCCommand("UpdateExtracts", ActualizarExtractos, AddressOf Commands.UpdateExtracts, "Actualiza la plantilla de extractos.", "")
            _Clist.Add(UpdateExtracts)
            Dim Archive As New IRCCommand("Archive", Archivar, AddressOf Commands.ArchivePage, "Archiva una página específica.", " <página>")
            _Clist.Add(Archive)
            Dim DivBy0 As New IRCCommand("Div0", Divide0, AddressOf Commands.Div0, "Divide por cero.", "")
            _Clist.Add(DivBy0)
            Dim DebugMode As New IRCCommand("Debug", Debug, AddressOf Commands.SetDebug, "Activa o desactiva los registros con el tag DEBUG.", "")
            _Clist.Add(DebugMode)
            Dim GetTasks As New IRCCommand("Tasks", Tasks, AddressOf Commands.GetTasks, "Obtiene una lista de las tareas en ejecución.", "")
            _Clist.Add(GetTasks)
            Dim GetTaskInfo As New IRCCommand("TaskInfo", TaskInf, AddressOf Commands.TaskInfo, "Entrega información sobre una tarea.", "<número>")
            _Clist.Add(GetTaskInfo)
            Dim GetTime As New IRCCommand("Time", GetLocalTime, AddressOf Commands.GetTime, "Entrega la hora del sistema.", "")
            _Clist.Add(GetTime)
            Dim TaskPause As New IRCCommand("TPause", TPause, AddressOf Commands.PauseTask, "Pausa la tarea indicada.", " <número>")
            _Clist.Add(TaskPause)
            Dim Gflood As New IRCCommand("GetFloodDelay", GetFlood, AddressOf Commands.GetFloodDelay, "Establece el delay de flood.", " <delay(ms)>")
            _Clist.Add(Gflood)
            Dim SFlood As New IRCCommand("SetFloodDelay", SetFlood, AddressOf Commands.SetFloodDelay, "Obtiene el delay de flood.", "")
            _Clist.Add(SFlood)
            Dim GenEfe As New IRCCommand("GenEfe", GenEfem, AddressOf Commands.GenEfe, "Fuerza la generación del video de efemérides.", "")
            _Clist.Add(GenEfe)
            Dim CommandInfo As New IRCCommand("?", Ayuda, AddressOf CommandInfoFcn, "Entrega información sobre un comando.", " <comando>")
            _Clist.Add(CommandInfo)
            Dim Orders As New IRCCommand("Ords", Ordenes, AddressOf GetCommandsFcn, "Obtiene la lista de comandos disponibles.", "")
            _Clist.Add(Orders)
            Return _Clist
        End Function

        Private Function CommandInfoFcn(ByVal Params As IRCCommandParams) As IRCMessage
            For Each c As IRCCommand In Clist
                If c.Aliases.Contains(RemovePrefix(Params.CParam).ToLower) Then
                    Return New IRCMessage(Params.Source, "Comando: " & Utils.ColoredText(c.Name, 4) & "| Aliases: " & Utils.ColoredText(Utils.JoinTextArray(c.Aliases, "/"c), 3) & "| Descripción: " & c.Description & "| Uso: " & Utils.ColoredText(Params.CommandName & c.Usage, 10))
                End If
            Next
            If String.IsNullOrWhiteSpace(Params.CParam) Then
                For Each c As IRCCommand In Clist
                    If c.Aliases.Contains(RemovePrefix(Params.CommandName)) Then
                        Return New IRCMessage(Params.Source, "Comando: " & Utils.ColoredText(c.Name, 4) & "| Aliases: " & Utils.ColoredText(Utils.JoinTextArray(c.Aliases, "/"c), 3) & "| Descripción: " & c.Description & "| Uso: " & Utils.ColoredText(Params.CommandName & c.Usage, 10))
                    End If
                Next
            End If
            Return New IRCMessage(Params.Source, "No se ha encontrado el comando: """ & Params.CParam & """")
        End Function

        Private Function GetCommandsFcn(ByVal Params As IRCCommandParams) As IRCMessage
            Dim responsestring As String = "Comandos disponibles: "
            For Each c As IRCCommand In Clist
                responsestring = responsestring & CommandPrefixes(0) & c.Name & ", "
            Next
            Return New IRCMessage(Params.Source, responsestring)
        End Function

        Private Function Greetings(ByVal Args As IRCCommandParams) As IRCMessage
            If Args.Realname.ToLower.EndsWith("bot") Or Args.Realname.ToLower.StartsWith("bot") Or Args.MessageLine.Contains("*") Then
                Return Nothing
            End If
            Dim responsestring As String = String.Format("Hola {0}, Soy {1}, bot multipropósito de apoyo en IRC. Ordenes: '%Ord' | Ayuda con un comando %? <orden> | Más sobre mí: '%??'", Utils.ColoredText(Args.Realname, 4), Utils.ColoredText(Args.Client.NickName, 3))
            Dim mes As New IRCMessage(Args.Source, responsestring)
            Return mes
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

        Private Function RemovePrefix(ByVal command As String) As String
            Dim requestedCommand As String = command
            For Each prfx As String In CommandPrefixes
                If requestedCommand.StartsWith(prfx) Then
                    requestedCommand = Utils.ReplaceFirst(requestedCommand, prfx, "")
                    Exit For
                End If
            Next
            Return requestedCommand
        End Function
    End Class

End Namespace
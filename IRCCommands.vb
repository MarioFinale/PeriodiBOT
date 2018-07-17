Option Strict On
Option Explicit On
Imports PeriodiBOT_IRC.CommFunctions
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot

Public Class IRCCommands

    Function GetFloodDelay(ByVal Args As CommandParams) As IRCMessage
        Dim Params As String = Args.CParam
        Dim Client As IRC_Client = Args.Client
        Dim source As String = Args.Source
        If Args.OpRequest Then
            Dim responsestring As String = String.Empty
            responsestring = "Tiempo de espera entre líneas: " & ColoredText(Client._floodDelay.ToString, 4) & " milisegundos."
            Return New IRCMessage(source, responsestring.ToArray)
        Else
            Return New IRCMessage(source, "No autorizado.")
        End If
    End Function

    Function SetFloodDelay(ByVal Args As CommandParams) As IRCMessage
        Dim value As String = Args.CParam
        Dim source As String = Args.Source
        Dim client As IRC_Client = Args.Client
        If Args.OpRequest Then
            Dim responsestring As String = String.Empty
            If Not IsNumeric(value) Then
                Return New IRCMessage(source, "Ingrese un número válido.")
            End If
            Dim resdelay As Integer = Integer.Parse(value)
            If resdelay <= 0 Then
                Return New IRCMessage(source, "El valor debe ser mayor a 0.")
            End If
            client._floodDelay = resdelay
            responsestring = "Tiempo de espera entre líneas establecido a: " & ColoredText(value, 4) & " milisegundos."
            Return New IRCMessage(source, responsestring.ToArray)
        Else
            Return New IRCMessage(source, "No autorizado.")
        End If
    End Function

    Function GetTasks(ByVal Args As CommandParams) As IRCMessage
        Dim source As String = Args.Source
        Dim responsestring As New List(Of String)
        If Not ThreadList.Count >= 1 Then
            Return New IRCMessage(source, "No hay tareas ejecutándose.")
        End If
        Dim threadnames As New List(Of Tuple(Of String, String, String))
        For Each t As ThreadInfo In ThreadList
            threadnames.Add(New Tuple(Of String, String, String)(t.name, t.author, t.status))
        Next
        responsestring.Add("Hay " & ColoredText(ThreadList.Count.ToString, 4) & " tareas ejecutándose en este momento:")
        For i As Integer = 0 To threadnames.Count - 1
            responsestring.Add((i + 1).ToString & ": """ & threadnames(i).Item1 & """ por """ & threadnames(i).Item2 & """. Estado: " & ColoredText(threadnames(i).Item3, 4))
        Next
        Return New IRCMessage(source, responsestring.ToArray)
    End Function

    Function TaskInfo(ByVal Args As CommandParams) As IRCMessage
        Dim taskindex As String = Args.CParam
        Dim source As String = Args.Source
        Dim responsestring As New List(Of String)
        If Not IsNumeric(taskindex) Then
            Return New IRCMessage(source, "Ingrese el número de la tarea.")
        End If
        If Not ThreadList.Count >= 1 Then
            Return New IRCMessage(source, "No hay tareas ejecutándose.")
        End If
        If taskindex.Length > 5 Then
            Return New IRCMessage(source, "El valor es demasiado grande.")
        End If
        Dim tindex As Integer = Integer.Parse(taskindex)
        If Not tindex > 0 Then
            Return New IRCMessage(source, "El valor debe ser mayor a 0.")
        End If
        If tindex > ThreadList.Count Then
            Return New IRCMessage(source, "La tarea no existe.")
        End If
        Dim tinfo As ThreadInfo = ThreadList(tindex - 1)
        Dim tstype As String = String.Empty
        Dim timeinterval As String = String.Empty
        If tinfo.scheduledtask Then
            tstype = "Programada"
            timeinterval = tinfo.scheduledTime.ToString("c") & " GMT"
        Else
            tstype = "Periódica"
            timeinterval = "Cada " & (tinfo.interval / 1000).ToString & " segundos."
        End If
        responsestring.Add("Nombre          : " & ColoredText(tinfo.name, 4))
        responsestring.Add("Autor           : " & ColoredText(tinfo.author, 4))
        responsestring.Add("Estado          : " & ColoredText(tinfo.status, 4))
        responsestring.Add("Tipo            : " & ColoredText(tstype, 4))
        responsestring.Add("Hora/intervalo  : " & ColoredText(timeinterval, 4))
        responsestring.Add("Infinita        : " & ColoredText(tinfo.infinite.ToString, 4))
        responsestring.Add("Cancelada       : " & ColoredText(tinfo.cancelled.ToString, 4))
        responsestring.Add("Pausada         : " & ColoredText(tinfo.paused.ToString, 4))
        responsestring.Add("Ejecuciones     : " & ColoredText(tinfo.runcount.ToString, 4))
        responsestring.Add("Errores         : " & ColoredText(tinfo.excount.ToString, 4))
        responsestring.Add("Crítica         : " & ColoredText(tinfo.critical.ToString, 4))

        Return New IRCMessage(source, responsestring.ToArray)
    End Function

    Function PauseTask(ByVal Args As CommandParams) As IRCMessage
        Dim taskindex As String = Args.CParam
        Dim source As String = Args.Source
        Dim user As String = Args.Realname

        Dim responsestring As New List(Of String)
        If Not IsNumeric(taskindex) Then
            Return New IRCMessage(source, "Ingrese el número de la tarea.")
        End If
        If Not ThreadList.Count >= 1 Then
            Return New IRCMessage(source, "No hay tareas ejecutándose.")
        End If
        If taskindex.Length > 5 Then
            Return New IRCMessage(source, "El valor es demasiado grande.")
        End If
        Dim tindex As Integer = Integer.Parse(taskindex)
        If Not tindex > 0 Then
            Return New IRCMessage(source, "El valor debe ser mayor a 0.")
        End If
        If tindex > ThreadList.Count Then
            Return New IRCMessage(source, "La tarea no existe.")
        End If
        Dim tinfo As ThreadInfo = ThreadList(tindex - 1)
        If Not tinfo.critical Then
            If tinfo.paused Then
                tinfo.paused = False
                EventLogger.Log("Task """ & tinfo.name & """ unpaused.", "IRC", user)
                Return New IRCMessage(source, "Se ha renaudado la tarea.")
            Else
                tinfo.paused = True
                EventLogger.Log("Task """ & tinfo.name & """ paused.", "IRC", user)
                Return New IRCMessage(source, "Se ha pausado la tarea.")
            End If
        Else
            Return New IRCMessage(source, "No se puede pausar una tarea crítica.")
        End If

    End Function

    Function GetTime(ByVal Args As CommandParams) As IRCMessage
        Dim source As String = Args.Source
        Dim responsestring As String = String.Empty
        responsestring = "La hora del sistema es " & ColoredText(Date.Now.TimeOfDay.ToString("hh\:mm\:ss"), 4) & " (" & ColoredText(Date.UtcNow.TimeOfDay.ToString("hh\:mm\:ss"), 4) & " UTC)."
        Return New IRCMessage(source, responsestring)
    End Function

    Function SetDebug(ByVal Args As CommandParams) As IRCMessage
        Dim source As String = Args.Source

        Dim responsestring As String = String.Empty
        If EventLogger.Debug Then
            EventLogger.Debug = False
            responsestring = ColoredText("Registro de eventos con el tag ""DEBUG"" desactivado.", 4)
        Else
            EventLogger.Debug = True
            responsestring = ColoredText("Registro de eventos con el tag ""DEBUG"" activado.", 4)
        End If
        Return New IRCMessage(source, responsestring)
    End Function

    Function SetOp(ByVal Args As CommandParams) As IRCMessage
        Dim message As String = Args.Imputline
        Dim Client As IRC_Client = Args.Client
        Dim source As String = Args.Source
        Dim realname As String = Args.Realname
        Dim responsestring As String = String.Empty
        Dim param As String = message.Split(CType(" ", Char()))(4)

        If CountCharacter(param, CChar("!")) = 1 Then
            Dim requestedop As String = param.Split(CType("!", Char()))(0)
            If Client.AddOP(message, source, realname) Then
                responsestring = "El usuario " & requestedop & " se añadió como operador"

            Else
                responsestring = "El usuario " & requestedop & " no se añadió como operador"
            End If
        Else
            responsestring = "Parámetro mal ingresado."
        End If
        Return New IRCMessage(source, responsestring)
    End Function


    Function DeOp(ByVal Args As CommandParams) As IRCMessage
        Dim message As String = Args.Imputline
        Dim Client As IRC_Client = Args.Client
        Dim source As String = Args.Source
        Dim realname As String = Args.Realname
        Dim responsestring As String = String.Empty
        Dim param As String = message.Split(CType(" ", Char()))(4)

        If CountCharacter(param, CChar("!")) = 1 Then

            Dim requestedop As String = param.Split(CType("!", Char()))(0)
            If Client.DelOP(message, source, realname) Then
                responsestring = "El usuario " & requestedop & " se ha eliminado como operador"
            Else
                responsestring = "El usuario " & requestedop & " no se ha eliminado como operador"
            End If
        Else
            responsestring = "Parámetro mal ingresado."
        End If
        Return New IRCMessage(source, responsestring)
    End Function

    Function CommandInfo(source As String, MainParam As String, realname As String) As IRCMessage
        Dim responsestring As String = String.Empty
        MainParam = MainParam.ToLower
        If MainParam(0) = "%"c Then
            MainParam = MainParam.Replace("%"c, "")
        End If

        EventLogger.Log("Commandinfo: " & MainParam, "IRC", realname)
        If MainParam = ("última") Or MainParam = ("ultima") Or MainParam = ("ult") Or MainParam = ("last") Then

            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%última/%ultima/%ult/%last", 3), "Entrega el tiempo (Aproximado) que ha pasado desde la ultima edicion del usuario.", "%Ultima <usuario>")

        ElseIf MainParam = ("usuario") Or MainParam = ("usuarios") Or MainParam = ("users") Or MainParam = ("usr") Or
                       MainParam = ("usrs") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%usuario/%usuarios/%users/%usr/%usrs", 3), "Entrega una lista de los usuarios programados en tu lista (Más info: %? %programar).", "%Usuarios")

        ElseIf MainParam = ("programar") Or MainParam = ("programa") Or MainParam = ("prog") Or MainParam = ("progr") Or
                       MainParam = ("prg") Or MainParam = ("avisa") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%programar/%programa/%prog/%progr/%prg/%avisa", 3), "Programa un aviso en caso de que un usuario no edite en un tiempo específico.", "%Programar Usuario/Dias/Horas/Minutos")

        ElseIf MainParam = ("quitar") Or MainParam = ("quita") Or
                       MainParam = ("saca") Or MainParam = ("sacar") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%quitar/%quita/%saca/%sacar", 3), "Quita un usuario de tu lista programada (Más info: %? %programar).", "%Quita <usuario>")

        ElseIf MainParam = ("ord") Or MainParam = ("ordenes") Or
                       MainParam = ("órdenes") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%ord/%ordenes", 3), "Entrega una lista de las principales ordenes (Más info: %? <orden>).", "%Ordenes")

        ElseIf MainParam = "??" Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%??", 3), "Entrega información técnica sobre el bot (limitado según el usuario).", "%??")

        ElseIf MainParam = "?" Or MainParam = "h" Or MainParam = "help" Or MainParam = "ayuda" Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%?/%h/%help/%ayuda", 3), "Entrega información sobre un comando.", "%? <orden>")

        ElseIf MainParam = ("lastlog") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%lastlog", 3), "SOLO OPS: Ultimo log del bot (TOTAL).", "%lastlog")

        ElseIf MainParam = ("resumen") Or MainParam = ("res") Or
                       MainParam = ("entrada") Or MainParam = ("entradilla") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%resumen/%res/%entrada/%entradilla", 3), "Entrega la entradilla de un artículo en Wikipedia.", "%entradilla <Artículo>")

        ElseIf MainParam = ("info") Or MainParam = ("pag") Or
                       MainParam = ("pageinfo") Or MainParam = ("infopagina") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%info/%pag/%pageinfo/%infopagina", 3), "Entrega datos sobre un artículo en Wikipedia.", "%Info <Artículo>")

        ElseIf MainParam = ("updateExtracts") Or MainParam = ("update") Or
                    MainParam = ("upex") Or MainParam = ("updex") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%updateExtracts/%update/%upex/%updex", 3), "SOLO OPS, Actualiza los extractos de articulos en Wikipedia.", "%upex")

        ElseIf MainParam = ("q") Or MainParam = ("quit") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%q", 3), "SOLO OP, Solicita al bot cesar todas sus operaciones.", "%q")
        ElseIf MainParam = ("op") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%op", 3), "SOLO OP, Añade un operador.", "%op nickname!hostname")
        ElseIf MainParam = ("deop") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText(MainParam, 4), ColoredText("%deop", 3), "SOLO OP, Elimina un operador.", "%deop nickname!hostname")
        ElseIf MainParam = ("archive") Then
            responsestring = String.Format("Comando: {0}; Función:{1}; Uso:{2}",
       ColoredText(MainParam, 4), "SOLO OP, Archiva una página inmediatamente.", "%archive [página]")
        ElseIf MainParam = ("archiveall") Then
            responsestring = String.Format("Comando: {0}; Función:{1}; Uso:{2}",
       ColoredText(MainParam, 4), "SOLO OP, Archiva todas las páginas inmediatamente.", "%archiveall")
        ElseIf String.IsNullOrEmpty(MainParam) Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
        ColoredText("%?", 4), ColoredText("%?/%h/%help/%ayuda", 3), "Entrega información sobre un comando.", "%? <orden>")

        Else
            responsestring = String.Format("No se ha encontrado el comando {0}.", ColoredText(MainParam, 4))
        End If
        Return New IRCMessage(source, responsestring)

    End Function

    Function JoinRoom(ByVal Args As CommandParams) As IRCMessage
        Dim command As String = String.Format("JOIN {0}", Args.CParam)
        Args.Client.SendText(command)
        Dim responsestring As String = ColoredText("Entrando a sala solicitada", 4)
        Dim mes As New IRCMessage(Args.Source, responsestring)
        EventLogger.Log("Joined room " & Args.CParam, "IRC", Args.Realname)
        Return mes
    End Function

    Function LeaveRoom(ByVal Args As CommandParams) As IRCMessage
        Dim responsestring As String = ColoredText("Saliendo de la sala solicitada", 4)
        Dim command As String = String.Format("PART {0}", Args.CParam)
        Args.Client.SendText(command)
        Dim mes As New IRCMessage(Args.Source, responsestring)
        EventLogger.Log("Joined room " & Args.CParam, "IRC", Args.Realname)
        Return mes
    End Function

    Function Quit(ByVal Args As CommandParams) As IRCMessage
        Dim responsestring As String = ColoredText("OK, voy saliendo...", 4)
        Dim mes As New IRCMessage(Args.Source, responsestring)
        Args.Client.Sendmessage(mes)
        Dim command As String = "Solicitado por un operador."
        Args.Client.HasExited = True
        Args.Client.Quit(command)
        EventLogger.Log("QUIT", "IRC", Args.Realname)
        Return mes
    End Function

    Function Div0(ByVal Args As CommandParams) As IRCMessage
        EventLogger.Log("Div0 requested", Args.Source, Args.Realname)
        Dim responsetext As String = IrcStringBuilder(Args.Source, "OK, dividiendo 1 por 0...")
        Args.Client.SendText(responsetext)
        Dim i As Double = (1 / 0)
        Dim res As String = "Al parecer el resultado es """ & i.ToString & """"
        Dim mes As New IRCMessage(Args.Source, res)
        EventLogger.Log("Div0 completed", Args.Source, Args.Realname)
        Return mes
    End Function

    Function ArchivePage(ByVal Args As CommandParams) As IRCMessage
        EventLogger.Log("ArchivePage requested", Args.Source, Args.Realname)
        Dim PageName As String = Args.Workerbot.TitleFirstGuess(Args.CParam)
        Dim responsestring As String = ColoredText("Archivando ", 4) & """" & PageName & """"
        Task.Run(Sub()
                     Dim p As Page = ESWikiBOT.Getpage(PageName)
                     If ESWikiBOT.Archive(p) Then
                         EventLogger.Log("ArchivePage completed", Args.Source, Args.Realname)
                         Dim completedResponse As String = ColoredText("Archivado de  ", 4) & """" & PageName & """ " & ColoredText("completo", 4)
                         Args.Client.Sendmessage(New IRCMessage(Args.Source, completedResponse))
                     Else
                         Dim completedResponse As String = ColoredText("No se ha archivado ", 4) & """" & PageName & """. " & ColoredText("Verifica si hay hilos que cumplan los requisitos de archivado, o contacta a un Operador.", 4)
                         Args.Client.Sendmessage(New IRCMessage(Args.Source, completedResponse))
                     End If

                 End Sub)
        Dim mes As New IRCMessage(Args.Source, responsestring)
        Return mes
    End Function

    Private Function GetUserTime(ByVal user As String) As String()
        Dim Usertime As String = String.Empty
        For Each line As String() In EventLogger.LogUserData
            If line(0) = user Then
                Usertime = line(0)
            End If
        Next
        Return {user, Usertime}
    End Function

    Function GetResume(ByVal Args As CommandParams) As IRCMessage
        Dim PageName As String = Args.Workerbot.TitleFirstGuess(Args.CParam)
        EventLogger.Log("IRC: GetResume of " & Args.CParam, "IRC", Args.Realname)

        If Not PageName = String.Empty Then
            Dim pretext As String = "Entradilla de " & ColoredText(PageName, 3) & " en Wikipedia: " & Args.Workerbot.GetPageExtract(PageName, 390).Replace(Environment.NewLine, " ")
            Dim endtext As String = "Enlace al artículo: " & ColoredText(" " & Args.Workerbot.WikiUrl & "wiki/" & PageName.Replace(" ", "_") & " ", 10)
            Dim mes As New IRCMessage(Args.Source, {pretext, endtext})
            Return mes
        Else
            Dim nopage As String = "No se ha encontrado ninguna página llamada """ & ColoredText(Args.CParam, 3) & """ o similar."
            Dim mes As New IRCMessage(Args.Source, nopage)
            Return mes
        End If
    End Function

    Function LastLogComm(ByVal Args As CommandParams) As IRCMessage
        Dim responsestring As String = String.Empty
        If Args.Client.IsOp(Args.Imputline, Args.Source, Args.Realname) Then
            Dim lastlogdata As String() = EventLogger.Lastlog("IRC", Args.Realname)
            responsestring = String.Format("Ultimo registro de: {3} via {2} a las {0}/ Tipo: {4}/ Accion: {1}", ColoredText(lastlogdata(0), 4), lastlogdata(1), lastlogdata(2), lastlogdata(3), lastlogdata(4))
        End If
        Dim mes As New IRCMessage(Args.Realname, responsestring)
        Return mes
    End Function

    ''' <summary>
    ''' Retorna informacion sobre el estado del bot dependiendo si el solicitante es OP.
    ''' </summary>
    ''' <returns></returns>
    Function About(ByVal Args As CommandParams) As IRCMessage
        Dim elapsedtime As TimeSpan = Uptime.Subtract(DateTime.Now)
        Dim uptimestr As String = elapsedtime.ToString("d\.hh\:mm")
        Dim responsestring As String

        If Args.Client.IsOp(Args.Imputline, Args.Source, Args.Realname) Then
            If GetCurrentThreads() = 0 Then
                responsestring = String.Format("{1} Versión: {0} (Uptime: {2}; Bajo {3} (MONO)). Ordenes: %ord", ColoredText(Version, 3), Args.Client.NickName, uptimestr, ColoredText(OS, 4))
                EventLogger.Log("IRC: Requested info (%??)", "IRC", Args.Realname)
            Else
                responsestring = String.Format("{2} Versión: {0} (Bajo {1} ;Uptime: {3}; Hilos: {4}; Memoria (en uso): {6}Kb, (Privada): {5}Kb). Ordenes: %ord", ColoredText(Version, 3), ColoredText(OS, 4), Args.Client.NickName, uptimestr, GetCurrentThreads.ToString, PrivateMemory.ToString, UsedMemory.ToString)
                EventLogger.Log("IRC: Requested info (%??)", "IRC", Args.Realname)
            End If

        Else
            responsestring = String.Format("{1} Versión: {0}. Ordenes: %ord", ColoredText(Version, 3), Args.Client.NickName)
            EventLogger.Log("IRC: Requested info (%??)", "IRC", Args.Realname)
        End If
        Dim mes As New IRCMessage(Args.Source, responsestring)
        Return mes
    End Function

    Function Commands(ByVal Args As CommandParams) As IRCMessage
        If Args.Realname.ToLower.EndsWith("bot") Or Args.Realname.ToLower.StartsWith("bot") Then
            Return Nothing
        End If
        Dim responsestring As String = String.Format("Hola {0}, Soy {1}, bot multipropósito de apoyo en IRC. Ordenes: '%Ord' | Ayuda con un comando %? <orden> | Más sobre mí: '%??'", ColoredText(Args.Realname, 4), ColoredText(Args.Client.NickName, 3))
        EventLogger.Log(String.Format("IRC: {0} was mentioned, returning info", Args.Client.NickName), "IRC", Args.Realname)
        Dim mes As New IRCMessage(Args.Source, responsestring)
        Return mes
    End Function

    Function Orders(ByVal Args As CommandParams) As IRCMessage
        Dim responsestring As String = String.Format("Ordenes: %programa, %quita, %ultima, %usuarios, %info, %resumen, %??, Detalles del comando %? <orden>.")
        EventLogger.Log("IRC: Requested orders (%ord)", "IRC", Args.Realname)
        Dim mes As New IRCMessage(Args.Source, responsestring)
        Return mes
    End Function

    Function RemoveUser(ByVal Args As CommandParams) As IRCMessage
        Dim responsestring As String = String.Empty
        Dim UsersOfOP As New List(Of String)
        Dim UsersOfOPIndex As New List(Of Integer)

        Try
            If Args.CParam = String.Empty Then
                responsestring = "Uso del comando: %quita <usuario>. Quita a un usuario de tu lista programada"
            Else
                For Each Line As String() In EventLogger.LogUserData
                    If Line(0) = Args.Realname Then
                        UsersOfOP.Add(Line(1))
                        UsersOfOPIndex.Add(EventLogger.LogUserData.IndexOf(Line))
                    End If
                Next
                If UsersOfOP.Contains(Args.CParam) Then
                    Dim UserIndex As Integer = UsersOfOP.IndexOf(Args.CParam)
                    Dim UserIndexInUserdata As Integer = UsersOfOPIndex(UserIndex)
                    EventLogger.LogUserData.RemoveAt(UserIndexInUserdata)
                    responsestring = String.Format("Se ha quitado a '{0}' de tu lista", ColoredText(Args.CParam, 4))
                    EventLogger.Log(String.Format("IRC: Removed user {0} from list of {1} (%quita)", Args.CParam, Args.Realname), "IRC", Args.Realname)
                    EventLogger.SaveUsersToFile()
                Else
                    responsestring = String.Format("El usuario '{0}' no esta en tu lista", ColoredText(Args.CParam, 4))
                End If

            End If
        Catch ex As Exception
            EventLogger.Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", Args.Client.NickName)
            responsestring = String.Format("Se ha producido un error al quitar a '{0}' de tu lista", ColoredText(Args.CParam, 4))
        End Try

        Dim mes As New IRCMessage(Args.Source, responsestring)
        Return mes

    End Function

    Private Function IrcStringBuilder(ByVal Destiny As String, message As String) As String
        Return String.Format("PRIVMSG {0} :{1}", Destiny, message)
    End Function

    Private Function IrcNoticeStringBuilder(ByVal Destiny As String, message As String) As String
        Return String.Format("NOTICE {0} :{1}", Destiny, message)
    End Function

    Function ProgramNewUser(ByVal Args As CommandParams) As IRCMessage
        Dim ResponseString As String = String.Empty
        Dim UserAndTime As String = Args.CParam
        UserAndTime = UserAndTime.Trim(CType(" ", Char))

        Try
            If Not UserAndTime.Contains(CType("|", Char)) Then

                Dim requesteduser As String = UserAndTime.Split(CType("/", Char()))(0)
                Dim wuser As New WikiUser(Args.Workerbot, requesteduser)
                Dim Dias As String = UserAndTime.Split(CType("/", Char()))(1)
                Dim Horas As String = UserAndTime.Split(CType("/", Char()))(2)
                Dim Minutos As String = UserAndTime.Split(CType("/", Char()))(3)
                If IsNumeric(Dias) And IsNumeric(Horas) And IsNumeric(Minutos) And (CInt(Horas) <= 23) And (CInt(Minutos) <= 59) Then
                    If (CInt(Dias) = 0) And (CInt(Horas) = 0) And (CInt(Minutos) < 10) Then
                        ResponseString = ColoredText("Error:", 4) & " El intervalo debe ser igual o superior a 10 minutos"
                    Else
                        If wuser.Exists Then
                            If EventLogger.SetUserTime({Args.Realname, requesteduser, Dias & "." & Horas & ":" & Minutos, Args.Realname}) Then
                                ResponseString = String.Format("Hecho: Serás avisado si {0} no edita en el tiempo especificado", requesteduser)
                                EventLogger.Log(String.Format("IRC: Added user {0} to list (%prog)", requesteduser), "IRC", Args.Realname)
                            Else
                                ResponseString = ColoredText("Error", 4)
                            End If
                        Else
                            ResponseString = String.Format(ColoredText("Error", 4) & " El usuario {0} no existe o no tiene ninguna edición en el proyecto", requesteduser)
                        End If
                    End If
                Else
                    ResponseString = String.Format(ColoredText("Error:", 4) & " El parámetro de tiempo debe ser numérico, las horas no deben ser superiores a 23 ni los minutos a 59")
                End If

            Else
                ResponseString = String.Format(ColoredText("Error:", 4) & " Se ha ingresado un carácter ilegal ('|')")
            End If
        Catch ex As IndexOutOfRangeException
            ResponseString = String.Format(ColoredText("Error:", 4) & " El comando se ha ingresado de forma incorrecta (Uso: '%Programar Usuario/Dias/Horas/Minutos')")
        Catch ex As InvalidCastException
            ResponseString = String.Format(ColoredText("Error:", 4) & " El comando se ha ingresado de forma incorrecta (Uso: '%Programar Usuario/Dias/Horas/Minutos')")
        Catch ex As Exception
            EventLogger.EX_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", Args.Realname)
            Dim exmes As String = ex.Message
            ResponseString = String.Format(ColoredText("Error:", 4) & " {0}", exmes)
        End Try

        Dim mes As New IRCMessage(Args.Source, ResponseString)
        Return mes
    End Function

    Function GetProgrammedUsers(ByVal Args As CommandParams) As IRCMessage
        Dim UserList As New List(Of String)
        Dim UserString As String = String.Empty

        For Each line As String() In EventLogger.LogUserData
            If line(0) = Args.Realname Then
                UserList.Add(line(1))
            End If
        Next
        If UserList.Count = 1 Then
            UserString = Args.Realname & ": Se te avisará si no edita " & ColoredText(UserList(0), 4) & " en el tiempo especificado."
        ElseIf UserList.Count > 1 Then
            UserString = Args.Realname & ": Se te avisará si "
            For i As Integer = 0 To UserList.Count - 1
                If i = UserList.Count - 1 Then
                    UserString = UserString & "y " & ColoredText(UserList(i), 4) & " "
                Else
                    UserString = UserString & ColoredText(UserList(i), 4) & " "
                End If
            Next
            UserString = UserString & "no editan en el tiempo especificado."
        Else
            UserString = Args.Realname & ": No hay nadie en tu lista! "
        End If
        Dim mes As New IRCMessage(Args.Source, UserString)
        Return mes
    End Function


    Function LastEdit(ByVal Args As CommandParams) As IRCMessage
        Dim wuser As New WikiUser(Args.Workerbot, Args.CParam)
        Dim responsestring As String = String.Empty
        EventLogger.Log(String.Format("IRC: Requested lastedit of {0} to list (%ultima)", Args.CParam), "IRC", Args.Realname)

        If Not wuser.Exists Then
            responsestring = String.Format("El usuario {0} no tiene ninguna edición en el proyecto eswiki", ColoredText(Args.CParam, 4))
        Else
            Dim edittime As DateTime = wuser.LastEdit
            Dim actualtime As DateTime = DateTime.UtcNow
            actualtime = actualtime.AddTicks(-(actualtime.Ticks Mod TimeSpan.TicksPerSecond))

            Dim LastEditUnix As Integer = CInt(TimeToUnix(edittime))
            Dim ActualTimeUnix As Integer = CInt(TimeToUnix(actualtime))
            Dim Timediff As Integer = (ActualTimeUnix - LastEditUnix) - 3600
            Dim TimediffToHours As Integer = CInt(Math.Truncate(Timediff / 3600))
            Dim TimediffToMinutes As Integer = CInt(Math.Truncate(Timediff / 60))
            Dim TimediffToDays As Integer = CInt(Math.Truncate(Timediff / 86400))

            If TimediffToMinutes <= 1 Then
                responsestring = String.Format("¡{0} editó recién!", Args.CParam)
            Else
                If TimediffToMinutes < 60 Then
                    responsestring = String.Format("La última edición de {0} fue hace {1} minutos", ColoredText(Args.CParam, 4), ColoredText(TimediffToMinutes.ToString, 9))
                Else
                    If TimediffToMinutes < 120 Then
                        responsestring = String.Format("La última edición de {0} fue hace más de {1} hora", ColoredText(Args.CParam, 4), ColoredText(TimediffToHours.ToString, 9))
                    Else
                        If TimediffToMinutes < 1440 Then
                            responsestring = String.Format("La última edición de {0} fue hace más de {1} horas", ColoredText(Args.CParam, 4), ColoredText(TimediffToHours.ToString, 9))
                        Else
                            If TimediffToMinutes < 2880 Then
                                responsestring = String.Format("La última edición de {0} fue hace {1} día", ColoredText(Args.CParam, 4), ColoredText(TimediffToDays.ToString, 9))
                            Else
                                responsestring = String.Format("La última edición de {0} fue hace más de {1} días", ColoredText(Args.CParam, 4), ColoredText(TimediffToDays.ToString, 9))
                            End If
                        End If
                    End If
                End If
            End If
        End If

        Dim mes As New IRCMessage(Args.Source, responsestring)
        Return mes
    End Function

    Function PageInfo(ByVal Args As CommandParams) As IRCMessage
        Dim PageName As String = Args.Workerbot.TitleFirstGuess(Args.CParam)
        EventLogger.Log("IRC: Get PageInfo of " & Args.CParam, "IRC", Args.Realname)

        If Not PageName = String.Empty Then
            Dim pag As Page = Args.Workerbot.Getpage(PageName)
            Dim CatString As String = String.Empty
            If pag.Categories.Count = 10 Then
                CatString = "10+"
            Else
                CatString = pag.Categories.Count.ToString
            End If
            Dim beginmessage As String = String.Format("Información sobre {0}: Última edición por {1}; Categorias: {2}; Visitas diarias (promedio últimos dos meses): {3}; Tamaño: {5} bytes; Puntaje ORES (Última edición): {4}",
                                       ColoredText(PageName, 3), ColoredText(pag.Lastuser, 3), ColoredText(CatString, 6), ColoredText(pag.PageViews.ToString, 13),
                                       "Dañina: " & ColoredText(pag.ORESScores(0).ToString, 4) & " Buena fé: " & ColoredText(pag.ORESScores(1).ToString, 3), ColoredText(pag.Size.ToString, 3))

            Dim endmessage As String = "Enlace al artículo: " & ColoredText(" " & Args.Workerbot.WikiUrl & "wiki/" & PageName.Replace(" ", "_") & " ", 10)

            Dim mes As New IRCMessage(Args.Source, {beginmessage, endmessage})
            Return mes
        Else
            Dim notfound As String = "No se ha encontrado ninguna página llamada """ & ColoredText(Args.CParam, 3) & """ o similar."
            Dim mes As New IRCMessage(Args.Source, notfound)
            Return mes
        End If

    End Function



End Class

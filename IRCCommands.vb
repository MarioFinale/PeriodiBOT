Option Strict On
Option Explicit On
Imports System.Globalization
Imports PeriodiBOT_IRC.IRC
Imports PeriodiBOT_IRC.WikiBot

Public Class IRCCommands

    Function GetFloodDelay(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim Client As IRC_Client = args.Client
        Dim source As String = args.Source
        If args.IsOp Then
            Dim responsestring As String = String.Empty
            responsestring = "Tiempo de espera entre líneas: " & Utils.ColoredText(Client.FloodDelay.ToString, 4) & " milisegundos."
            Return New IRCMessage(source, responsestring.ToArray)
        Else
            Return New IRCMessage(source, "No autorizado.")
        End If
    End Function

    Function SetFloodDelay(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim value As String = args.CParam
        Dim source As String = args.Source
        Dim client As IRC_Client = args.Client
        If args.IsOp Then
            Dim responsestring As String = String.Empty
            If Not IsNumeric(value) Then
                Return New IRCMessage(source, "Ingrese un número válido.")
            End If
            Dim resdelay As Integer = Integer.Parse(value)
            If resdelay <= 0 Then
                Return New IRCMessage(source, "El valor debe ser mayor a 0.")
            End If
            client.FloodDelay = resdelay
            responsestring = "Tiempo de espera entre líneas establecido a: " & Utils.ColoredText(value, 4) & " milisegundos."
            Return New IRCMessage(source, responsestring.ToArray)
        Else
            Return New IRCMessage(source, "No autorizado.")
        End If
    End Function

    Function GetTasks(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim source As String = args.Source
        Dim responsestring As New List(Of String)
        If Not TaskList.Count >= 1 Then
            Return New IRCMessage(source, "No hay tareas ejecutándose.")
        End If
        Dim threadnames As New List(Of Tuple(Of String, String, String))
        For Each t As TaskInfo In TaskList
            threadnames.Add(New Tuple(Of String, String, String)(t.Name, t.Author, t.Status))
        Next
        responsestring.Add("Hay " & Utils.ColoredText(TaskList.Count.ToString, 4) & " tareas ejecutándose en este momento:")
        For i As Integer = 0 To threadnames.Count - 1
            responsestring.Add((i + 1).ToString & ": """ & threadnames(i).Item1 & """ por """ & threadnames(i).Item2 & """. Estado: " & Utils.ColoredText(threadnames(i).Item3, 4))
        Next
        Return New IRCMessage(source, responsestring.ToArray)
    End Function

    Function GenEfe(ByVal args As IRCCommandParams) As IRCMessage
        If args.IsOp Then
            If args Is Nothing Then Return Nothing

            NewThread("Generar imágenes de efemérides", args.Realname, New Func(Of Boolean)(Function()
                                                                                                Dim imggen As New VideoGen(args.Workerbot)
                                                                                                If imggen.CheckEfe Then
                                                                                                    args.Client.Sendmessage(New IRCMessage(args.Source, "Se ha generado el video."))
                                                                                                Else
                                                                                                    args.Client.Sendmessage(New IRCMessage(args.Source, "No se han generado todos los videos, ver log para más detalles."))
                                                                                                End If
                                                                                                Return True
                                                                                            End Function), 1, False)
            Return New IRCMessage(args.Source, "Generando videos.")
        End If
        Return New IRCMessage(args.Source, "No autorizado.")
    End Function


    Function TaskInfo(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim taskindex As String = args.CParam
        Dim source As String = args.Source
        Dim responsestring As New List(Of String)
        If Not IsNumeric(taskindex) Then
            Return New IRCMessage(source, "Ingrese el número de la tarea.")
        End If
        If Not TaskList.Count >= 1 Then
            Return New IRCMessage(source, "No hay tareas ejecutándose.")
        End If
        If taskindex.Length > 5 Then
            Return New IRCMessage(source, "El valor es demasiado grande.")
        End If
        Dim tindex As Integer = Integer.Parse(taskindex)
        If Not tindex > 0 Then
            Return New IRCMessage(source, "El valor debe ser mayor a 0.")
        End If
        If tindex > TaskList.Count Then
            Return New IRCMessage(source, "La tarea no existe.")
        End If
        Dim tinfo As TaskInfo = TaskList(tindex - 1)
        Dim tstype As String = String.Empty
        Dim timeinterval As String = String.Empty
        If tinfo.Scheduledtask Then
            tstype = "Programada"
            timeinterval = tinfo.ScheduledTime.ToString("c", CultureInfo.InvariantCulture()) & " GMT"
        Else
            tstype = "Periódica"
            timeinterval = "Cada " & (tinfo.Interval / 1000).ToString & " segundos."
        End If
        responsestring.Add("Nombre          : " & Utils.ColoredText(tinfo.Name, 4))
        responsestring.Add("Autor           : " & Utils.ColoredText(tinfo.Author, 4))
        responsestring.Add("Estado          : " & Utils.ColoredText(tinfo.Status, 4))
        responsestring.Add("Tipo            : " & Utils.ColoredText(tstype, 4))
        responsestring.Add("Hora/intervalo  : " & Utils.ColoredText(timeinterval, 4))
        responsestring.Add("Infinita        : " & Utils.ColoredText(tinfo.Infinite.ToString, 4))
        responsestring.Add("Cancelada       : " & Utils.ColoredText(tinfo.Canceled.ToString, 4))
        responsestring.Add("Pausada         : " & Utils.ColoredText(tinfo.Paused.ToString, 4))
        responsestring.Add("Ejecuciones     : " & Utils.ColoredText(tinfo.Runcount.ToString, 4))
        responsestring.Add("Errores         : " & Utils.ColoredText(tinfo.ExCount.ToString, 4))
        responsestring.Add("Crítica         : " & Utils.ColoredText(tinfo.Critical.ToString, 4))

        Return New IRCMessage(source, responsestring.ToArray)
    End Function

    Function PauseTask(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim taskindex As String = args.CParam
            Dim source As String = args.Source
            Dim user As String = args.Realname

            If Not IsNumeric(taskindex) Then
                Return New IRCMessage(source, "Ingrese el número de la tarea.")
            End If
            If Not TaskList.Count >= 1 Then
                Return New IRCMessage(source, "No hay tareas ejecutándose.")
            End If
            If taskindex.Length > 5 Then
                Return New IRCMessage(source, "El valor es demasiado grande.")
            End If
            Dim tindex As Integer = Integer.Parse(taskindex)
            If Not tindex > 0 Then
                Return New IRCMessage(source, "El valor debe ser mayor a 0.")
            End If
            If tindex > TaskList.Count Then
                Return New IRCMessage(source, "La tarea no existe.")
            End If
            Dim tinfo As TaskInfo = TaskList(tindex - 1)
            If Not tinfo.Critical Then
                If tinfo.Paused Then
                    tinfo.Paused = False
                    Utils.EventLogger.Log("Task """ & tinfo.Name & """ unpaused.", "IRC", user)
                    Return New IRCMessage(source, "Se ha renaudado la tarea.")
                Else
                    tinfo.Paused = True
                    Utils.EventLogger.Log("Task """ & tinfo.Name & """ paused.", "IRC", user)
                    Return New IRCMessage(source, "Se ha pausado la tarea.")
                End If
            Else
                Return New IRCMessage(source, "No se puede pausar una tarea crítica.")
            End If
        Else
            Return Nothing
        End If
    End Function

    Function GetTime(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim source As String = args.Source
        Dim responsestring As String = String.Empty
        responsestring = "La hora del sistema es " & Utils.ColoredText(Date.Now.TimeOfDay.ToString("hh\:mm\:ss", CultureInfo.InvariantCulture()), 4) & " (" & Utils.ColoredText(Date.UtcNow.TimeOfDay.ToString("hh\:mm\:ss", CultureInfo.InvariantCulture()), 4) & " UTC)."
        Return New IRCMessage(source, responsestring)
    End Function

    Function SetDebug(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim source As String = args.Source
        Dim responsestring As String = String.Empty
        If Utils.EventLogger.Debug Then
            Utils.EventLogger.Debug = False
            responsestring = Utils.ColoredText("Registro de eventos con el tag ""DEBUG"" desactivado.", 4)
        Else
            Utils.EventLogger.Debug = True
            responsestring = Utils.ColoredText("Registro de eventos con el tag ""DEBUG"" activado.", 4)
        End If
        Return New IRCMessage(source, responsestring)
    End Function

    Function SetOp(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim message As String = args.Imputline
            Dim Client As IRC_Client = args.Client
            Dim source As String = args.Source
            Dim realname As String = args.Realname
            Dim responsestring As String = String.Empty
            Dim param As String = message.Split(CType(" ", Char()))(4)

            If Utils.CountCharacter(param, CChar("!")) = 1 Then
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
        Else
            Return Nothing
        End If
    End Function


    Function DEOp(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim message As String = args.Imputline
            Dim Client As IRC_Client = args.Client
            Dim source As String = args.Source
            Dim realname As String = args.Realname
            Dim responsestring As String = String.Empty
            Dim param As String = message.Split(CType(" ", Char()))(4)

            If Utils.CountCharacter(param, CChar("!")) = 1 Then

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
        Else
            Return Nothing
        End If
    End Function

    Function UpdateExtracts(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim Upexfcn As New Func(Of Boolean)(Function() args.Workerbot.UpdatePageExtracts(True))
            NewThread("Actualizar extractos a solicitud", args.Realname, Upexfcn, 1, False)
            Return New IRCMessage(args.Source, args.Realname & ": Se ha creado la tarea.")
        Else
            Return Nothing
        End If
    End Function

    Function JoinRoom(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim command As String = String.Format("JOIN {0}", args.CParam)
            args.Client.SendText(command)
            Dim responsestring As String = Utils.ColoredText("Entrando a sala solicitada", 4)
            Dim mes As New IRCMessage(args.Source, responsestring)
            Utils.EventLogger.Log("Joined room " & args.CParam, "IRC", args.Realname)
            Return mes
        Else
            Return Nothing
        End If
    End Function

    Function LeaveRoom(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim responsestring As String = Utils.ColoredText("Saliendo de la sala solicitada", 4)
            Dim command As String = String.Format("PART {0}", args.CParam)
            args.Client.SendText(command)
            Dim mes As New IRCMessage(args.Source, responsestring)
            Utils.EventLogger.Log("Joined room " & args.CParam, "IRC", args.Realname)
            Return mes
        Else
            Return Nothing
        End If
    End Function

    Function Quit(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim responsestring As String = Utils.ColoredText("OK, voy saliendo...", 4)
            Dim mes As New IRCMessage(args.Source, responsestring)
            args.Client.Sendmessage(mes)
            Dim command As String = "Solicitado por un operador."
            args.Client.Quit(command)
            Utils.EventLogger.Log("QUIT", "IRC", args.Realname)
            Return mes
        Else
            Return Nothing
        End If
    End Function

    Function Div0(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Utils.EventLogger.Log("Div0 requested", args.Source, args.Realname)
        Dim mes1 As New IRCMessage(args.Source, "OK, dividiendo 1 por 0...")
        args.Client.Sendmessage(mes1)
        Dim i As Double = (1 / 0)
        Dim res As String = "Al parecer el resultado es """ & i.ToString & """"
        Dim mes As New IRCMessage(args.Source, res)
        Utils.EventLogger.Log("Div0 completed", args.Source, args.Realname)
        Return mes
    End Function

    Function ArchiveAll(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            NewThread("Archivado a solicitud", args.Realname, New Func(Of Boolean)(Function() args.Workerbot.ArchiveAllInclusions(True)), 1, False)
            Return New IRCMessage(args.Source, "Se realizarará el archivado en todas las páginas.")
        Else
            Return Nothing
        End If
    End Function

    Function ArchivePage(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Utils.EventLogger.Log("ArchivePage requested", args.Source, args.Realname)
        Dim PageName As String = args.Workerbot.TitleFirstGuess(args.CParam)
        Dim responsestring As String = Utils.ColoredText("Archivando ", 4) & """" & PageName & """"
        Dim archf As New Func(Of Boolean)(Function()
                                              Dim p As Page = ESWikiBOT.Getpage(PageName)
                                              If ESWikiBOT.Archive(p) Then
                                                  Utils.EventLogger.Log("ArchivePage completed", args.Source, args.Realname)
                                                  Dim completedResponse As String = Utils.ColoredText("Archivado de  ", 4) & """" & PageName & """ " & Utils.ColoredText("completo", 4)
                                                  args.Client.Sendmessage(New IRCMessage(args.Source, completedResponse))
                                              Else
                                                  Dim completedResponse As String = Utils.ColoredText("No se ha archivado ", 4) & """" & PageName & """. " & Utils.ColoredText("Verifica si hay hilos que cumplan los requisitos de archivado, o contacta a un Operador.", 4)
                                                  args.Client.Sendmessage(New IRCMessage(args.Source, completedResponse))
                                              End If
                                              Return True
                                          End Function)
        NewThread("Archivado a solicitud", args.Realname, archf, 1, False)
        Dim mes As New IRCMessage(args.Source, responsestring)
        Return mes
    End Function

    Function GetResume(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim PageName As String = args.Workerbot.TitleFirstGuess(args.CParam)
        Utils.EventLogger.Log("IRC: GetResume of " & args.CParam, "IRC", args.Realname)
        If Not PageName = String.Empty Then
            Dim pretext As String = "Entradilla de " & Utils.ColoredText(PageName, 3) & " en Wikipedia: " & args.Workerbot.GetPageExtract(PageName, 390).Replace(Environment.NewLine, " ")
            Dim endtext As String = "Enlace al artículo: " & Utils.ColoredText(" " & args.Workerbot.WikiUrl & "wiki/" & PageName.Replace(" ", "_") & " ", 10)
            Dim mes As New IRCMessage(args.Source, {pretext, endtext})
            Return mes
        Else
            Dim nopage As String = "No se ha encontrado ninguna página llamada """ & Utils.ColoredText(args.CParam, 3) & """ o similar."
            Dim mes As New IRCMessage(args.Source, nopage)
            Return mes
        End If
    End Function

    Function LastLogComm(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        If args.IsOp Then
            Dim responsestring As String = String.Empty
            Dim lastlogdata As String() = Utils.EventLogger.Lastlog("IRC", args.Realname)
            responsestring = String.Format("Ultimo registro de: {3} via {2} a las {0}/ Tipo: {4}/ Accion: {1}", Utils.ColoredText(lastlogdata(0), 4), lastlogdata(1), lastlogdata(2), lastlogdata(3), lastlogdata(4))
            Dim mes As New IRCMessage(args.Realname, responsestring)
            Return mes
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' Retorna informacion sobre el estado del bot dependiendo si el solicitante es OP.
    ''' </summary>
    ''' <returns></returns>
    Function About(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim elapsedtime As TimeSpan = Uptime.Subtract(DateTime.Now)
        Dim uptimestr As String = elapsedtime.ToString("d\.hh\:mm", CultureInfo.InvariantCulture())
        Dim responsestring As String

        If args.Client.IsOp(args.Imputline, args.Source, args.Realname) Then
            If Utils.GetCurrentThreads() = 0 Then
                responsestring = String.Format("{1} Versión: {0} (Uptime: {2}; Bajo {3} (MONO)). Ordenes: %ord", Utils.ColoredText(Version, 3), args.Client.NickName, uptimestr, Utils.ColoredText(OS, 4))
                Utils.EventLogger.Log("IRC: Requested info (%??)", "IRC", args.Realname)
            Else
                responsestring = String.Format("{2} Versión: {0} (Bajo {1} ;Uptime: {3}; Tareas en ejecución: {4}; Memoria (en uso): {6}Kb, (Privada): {5}Kb). Ordenes: %ord", Utils.ColoredText(Version, 3), Utils.ColoredText(OS, 4), args.Client.NickName, uptimestr, Utils.TaskCount, Utils.PrivateMemory.ToString, Utils.UsedMemory.ToString)
                Utils.EventLogger.Log("IRC: Requested info (%??)", "IRC", args.Realname)
            End If

        Else
            responsestring = String.Format("{1} Versión: {0}. Ordenes: %ord", Utils.ColoredText(Version, 3), args.Client.NickName)
            Utils.EventLogger.Log("IRC: Requested info (%??)", "IRC", args.Realname)
        End If
        Dim mes As New IRCMessage(args.Source, responsestring)
        Return mes
    End Function

    Function RemoveUser(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim responsestring As String = String.Empty
        Dim UsersOfOP As New List(Of String)
        Dim UsersOfOPIndex As New List(Of Integer)

        Try
            If args.CParam = String.Empty Then
                responsestring = "Uso del comando: %quita <usuario>. Quita a un usuario de tu lista programada"
            Else
                For Each Line As String() In Utils.EventLogger.LogUserData
                    If Line(0) = args.Realname Then
                        UsersOfOP.Add(Line(1))
                        UsersOfOPIndex.Add(Utils.EventLogger.LogUserData.IndexOf(Line))
                    End If
                Next
                If UsersOfOP.Contains(args.CParam) Then
                    Dim UserIndex As Integer = UsersOfOP.IndexOf(args.CParam)
                    Dim UserIndexInUserdata As Integer = UsersOfOPIndex(UserIndex)
                    Utils.EventLogger.LogUserData.RemoveAt(UserIndexInUserdata)
                    responsestring = String.Format("Se ha quitado a '{0}' de tu lista", Utils.ColoredText(args.CParam, 4))
                    Utils.EventLogger.Log(String.Format("IRC: Removed user {0} from list of {1} (%quita)", args.CParam, args.Realname), "IRC", args.Realname)
                    Utils.EventLogger.SaveUsersToFile()
                Else
                    responsestring = String.Format("El usuario '{0}' no esta en tu lista", Utils.ColoredText(args.CParam, 4))
                End If

            End If
        Catch ex As Exception
            Utils.EventLogger.Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", args.Client.NickName)
            responsestring = String.Format("Se ha producido un error al quitar a '{0}' de tu lista", Utils.ColoredText(args.CParam, 4))
        End Try

        Dim mes As New IRCMessage(args.Source, responsestring)
        Return mes

    End Function

    Function ProgramNewUser(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim ResponseString As String = String.Empty
        Dim UserAndTime As String = args.CParam
        UserAndTime = UserAndTime.Trim(CType(" ", Char))
        Try
            If Not UserAndTime.Contains(CType("|", Char)) Then

                Dim requesteduser As String = UserAndTime.Split(CType("/", Char()))(0)
                Dim wuser As New WikiUser(args.Workerbot, requesteduser)
                Dim Dias As String = UserAndTime.Split(CType("/", Char()))(1)
                Dim Horas As String = UserAndTime.Split(CType("/", Char()))(2)
                Dim Minutos As String = UserAndTime.Split(CType("/", Char()))(3)
                If IsNumeric(Dias) And IsNumeric(Horas) And IsNumeric(Minutos) And (CInt(Horas) <= 23) And (CInt(Minutos) <= 59) Then
                    If (CInt(Dias) = 0) And (CInt(Horas) = 0) And (CInt(Minutos) < 10) Then
                        ResponseString = Utils.ColoredText("Error:", 4) & " El intervalo debe ser igual o superior a 10 minutos"
                    Else
                        If wuser.Exists Then
                            If Utils.EventLogger.SetUserTime({args.Realname, requesteduser, Dias & "." & Horas & ":" & Minutos, args.Realname}) Then
                                ResponseString = String.Format("Hecho: Serás avisado si {0} no edita en el tiempo especificado", requesteduser)
                                Utils.EventLogger.Log(String.Format("IRC: Added user {0} to list (%prog)", requesteduser), "IRC", args.Realname)
                            Else
                                ResponseString = Utils.ColoredText("Error", 4)
                            End If
                        Else
                            ResponseString = String.Format(Utils.ColoredText("Error", 4) & " El usuario {0} no existe o no tiene ninguna edición en el proyecto", requesteduser)
                        End If
                    End If
                Else
                    ResponseString = String.Format(Utils.ColoredText("Error:", 4) & " El parámetro de tiempo debe ser numérico, las horas no deben ser superiores a 23 ni los minutos a 59")
                End If

            Else
                ResponseString = String.Format(Utils.ColoredText("Error:", 4) & " Se ha ingresado un carácter ilegal ('|')")
            End If
        Catch ex As IndexOutOfRangeException
            ResponseString = String.Format(Utils.ColoredText("Error:", 4) & " El comando se ha ingresado de forma incorrecta (Uso: '%Programar Usuario/Dias/Horas/Minutos')")
        Catch ex As InvalidCastException
            ResponseString = String.Format(Utils.ColoredText("Error:", 4) & " El comando se ha ingresado de forma incorrecta (Uso: '%Programar Usuario/Dias/Horas/Minutos')")
        Catch ex As Exception
            Utils.EventLogger.EX_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", args.Realname)
            Dim exmes As String = ex.Message
            ResponseString = String.Format(Utils.ColoredText("Error:", 4) & " {0}", exmes)
        End Try

        Dim mes As New IRCMessage(args.Source, ResponseString)
        Return mes
    End Function

    Function GetProgrammedUsers(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim UserList As New List(Of String)
        Dim UserString As String = String.Empty
        For Each line As String() In Utils.EventLogger.LogUserData
            If line(0) = args.Realname Then
                UserList.Add(line(1))
            End If
        Next
        If UserList.Count = 1 Then
            UserString = args.Realname & ": Se te avisará si no edita " & Utils.ColoredText(UserList(0), 4) & " en el tiempo especificado."
        ElseIf UserList.Count > 1 Then
            UserString = args.Realname & ": Se te avisará si "
            For i As Integer = 0 To UserList.Count - 1
                If i = UserList.Count - 1 Then
                    UserString = UserString & "y " & Utils.ColoredText(UserList(i), 4) & " "
                Else
                    UserString = UserString & Utils.ColoredText(UserList(i), 4) & " "
                End If
            Next
            UserString = UserString & "no editan en el tiempo especificado."
        Else
            UserString = args.Realname & ": No hay nadie en tu lista! "
        End If
        Dim mes As New IRCMessage(args.Source, UserString)
        Return mes
    End Function


    Function LastEdit(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim wuser As New WikiUser(args.Workerbot, args.CParam)
        Dim responsestring As String = String.Empty
        Utils.EventLogger.Log(String.Format("IRC: Requested lastedit of {0} to list (%ultima)", args.CParam), "IRC", args.Realname)

        If Not wuser.Exists Then
            responsestring = String.Format("El usuario {0} no tiene ninguna edición en el proyecto eswiki", Utils.ColoredText(args.CParam, 4))
        Else
            Dim edittime As DateTime = wuser.LastEdit
            Dim actualtime As DateTime = DateTime.UtcNow
            actualtime = actualtime.AddTicks(-(actualtime.Ticks Mod TimeSpan.TicksPerSecond))

            Dim LastEditUnix As Integer = CInt(Utils.TimeToUnix(edittime))
            Dim ActualTimeUnix As Integer = CInt(Utils.TimeToUnix(actualtime))
            Dim Timediff As Integer = (ActualTimeUnix - LastEditUnix) - 3600
            Dim TimediffToHours As Integer = CInt(Math.Truncate(Timediff / 3600))
            Dim TimediffToMinutes As Integer = CInt(Math.Truncate(Timediff / 60))
            Dim TimediffToDays As Integer = CInt(Math.Truncate(Timediff / 86400))

            If TimediffToMinutes <= 1 Then
                responsestring = String.Format("¡{0} editó recién!", args.CParam)
            Else
                If TimediffToMinutes < 60 Then
                    responsestring = String.Format("La última edición de {0} fue hace {1} minutos", Utils.ColoredText(args.CParam, 4), Utils.ColoredText(TimediffToMinutes.ToString, 9))
                Else
                    If TimediffToMinutes < 120 Then
                        responsestring = String.Format("La última edición de {0} fue hace más de {1} hora", Utils.ColoredText(args.CParam, 4), Utils.ColoredText(TimediffToHours.ToString, 9))
                    Else
                        If TimediffToMinutes < 1440 Then
                            responsestring = String.Format("La última edición de {0} fue hace más de {1} horas", Utils.ColoredText(args.CParam, 4), Utils.ColoredText(TimediffToHours.ToString, 9))
                        Else
                            If TimediffToMinutes < 2880 Then
                                responsestring = String.Format("La última edición de {0} fue hace {1} día", Utils.ColoredText(args.CParam, 4), Utils.ColoredText(TimediffToDays.ToString, 9))
                            Else
                                responsestring = String.Format("La última edición de {0} fue hace más de {1} días", Utils.ColoredText(args.CParam, 4), Utils.ColoredText(TimediffToDays.ToString, 9))
                            End If
                        End If
                    End If
                End If
            End If
        End If

        Dim mes As New IRCMessage(args.Source, responsestring)
        Return mes
    End Function

    Function PageInfo(ByVal args As IRCCommandParams) As IRCMessage
        If args Is Nothing Then Return Nothing
        Dim PageName As String = args.Workerbot.TitleFirstGuess(args.CParam)
        Utils.EventLogger.Log("IRC: Get PageInfo of " & args.CParam, "IRC", args.Realname)

        If Not PageName = String.Empty Then
            Dim pag As Page = args.Workerbot.Getpage(PageName)
            Dim CatString As String = String.Empty
            If pag.Categories.Count = 10 Then
                CatString = "10+"
            Else
                CatString = pag.Categories.Count.ToString
            End If
            Dim beginmessage As String = String.Format("Información sobre {0}: Última edición por {1}; Categorias: {2}; Visitas diarias (promedio últimos dos meses): {3}; Tamaño: {5} bytes; Puntaje ORES (Última edición): {4}",
                                       Utils.ColoredText(PageName, 3), Utils.ColoredText(pag.Lastuser, 3), Utils.ColoredText(CatString, 6), Utils.ColoredText(pag.PageViews.ToString, 13),
                                       "Dañina: " & Utils.ColoredText(pag.ORESScores(0).ToString, 4) & " Buena fé: " & Utils.ColoredText(pag.ORESScores(1).ToString, 3), Utils.ColoredText(pag.Size.ToString, 3))

            Dim endmessage As String = "Enlace al artículo: " & Utils.ColoredText(" " & args.Workerbot.WikiUrl & "wiki/" & PageName.Replace(" ", "_") & " ", 10)

            Dim mes As New IRCMessage(args.Source, {beginmessage, endmessage})
            Return mes
        Else
            Dim notfound As String = "No se ha encontrado ninguna página llamada """ & Utils.ColoredText(args.CParam, 3) & """ o similar."
            Dim mes As New IRCMessage(args.Source, notfound)
            Return mes
        End If

    End Function



End Class

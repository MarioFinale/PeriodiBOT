Option Strict On
Imports System.IO
Imports PeriodiBOT_IRC.WikiBot

Class IRC_Comands
    Private LastCommand As String
    Private _IrcNickName As String

    Public Function ResolveCommand(ByVal imputline As String, ByRef HasExited As Boolean, ByVal BOTIRCNickName As String) As String
        _IrcNickName = BOTIRCNickName
        Try
            If Not imputline = Nothing Then
                Dim CommandResponse As String() = {String.Empty, String.Empty}
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

                    If Params.Count >= 1 Then

                        If MainParam = ("%última") Or
                       MainParam = ("%ultima") Or MainParam = ("%ult") Or MainParam = ("%last") Then
                            CommandResponse = LastEdit(Source, Realname, Username)

                        ElseIf MainParam = ("%usuario") Or MainParam = ("%usuarios") Or MainParam = ("%users") Or MainParam = ("%usr") Or
                           MainParam = ("%usrs") Then
                            CommandResponse = GetProgrammedUsers(Source, Realname)

                        ElseIf MainParam = ("%programar") Or MainParam = ("%programa") Or MainParam = ("%prog") Or MainParam = ("%progr") Or
                           MainParam = ("%prg") Or MainParam = ("%avisa") Then
                            Dim paramarr As String() = param.Split(CType(" ", Char))

                            If paramarr.Count >= 2 Then
                                CommandResponse = ProgramNewUser(Source, Realname, paramarr(1).Trim(CType(" ", Char())))
                            End If

                        ElseIf MainParam = ("%quitar") Or MainParam = ("%quita") Or
                           MainParam = ("%saca") Or MainParam = ("%sacar") Then
                            CommandResponse = RemoveUser(Source, Realname, ParamsString)

                        ElseIf MainParam = ("%ord") Or MainParam = ("%ordenes") Or
                           MainParam = ("%órdenes") Then
                            CommandResponse = Orders(Source, Realname)

                        ElseIf MainParam = "%??" Then
                            CommandResponse = About(Source, Realname)

                        ElseIf MainParam = "%?" Or MainParam = "%h" Or MainParam = "%help" Or MainParam = "%ayuda" Then
                            CommandResponse = CommandInfo(Source, Totalparam, Realname)

                        ElseIf MainParam = ("%lastlog") Then
                            CommandResponse = LastLogComm(Source, Prefix, Realname)

                        ElseIf MainParam = ("%resumen") Or MainParam = ("%res") Or
                           MainParam = ("%entrada") Or MainParam = ("%entradilla") Then
                            Dim response As String = GetResume(Source, Totalparam, Realname)
                            If Not response = LastCommand Then
                                LastCommand = response
                                Return response
                            End If
                        ElseIf MainParam = ("%info") Or MainParam = ("%pag") Or
                           MainParam = ("%pageinfo") Or MainParam = ("%infopagina") Then
                            Dim response As String = PageInfo(Source, Totalparam, Realname)
                            If Not response = LastCommand Then
                                LastCommand = response
                                Return response
                            End If
                        ElseIf MainParam = ("%grillitusarchive") Then 'Archivado de grillitus
                            If IsOp(imputline, Source, Realname) Then
                                Task.Run(Sub()
                                             Mainwikibot.ArchiveAllInclusions(True)
                                         End Sub)
                            End If
                        ElseIf MainParam = ("%join") Then
                            If IsOp(imputline, Source, Realname) Then
                                Return JoinRoom(Source, Totalparam, Realname)
                            End If
                        ElseIf MainParam = ("%leave") Or MainParam = ("%part") Then
                            If IsOp(imputline, Source, Realname) Then
                                Return LeaveRoom(Source, Totalparam, Realname)
                            End If
                        ElseIf MainParam = ("%q") Or MainParam = ("%quit") Then
                            If IsOp(imputline, Source, Realname) Then
                                Return Quit(Source, Realname, HasExited)
                            End If

                        ElseIf MainParam = ("%op") Then
                            If IsOp(imputline, Source, Realname) Then
                                CommandResponse = SetOp(imputline, Source, Realname)
                            End If
                        ElseIf MainParam = ("%deop") Then
                            If IsOp(imputline, Source, Realname) Then
                                CommandResponse = DeOp(imputline, Source, Realname)
                            End If

                        ElseIf MainParam = ("%updateExtracts") Or MainParam = ("%update") Or
                        MainParam = ("%upex") Or MainParam = ("%updex") Then
                            If IsOp(imputline, Source, Realname) Then
                                Task.Run(Sub()
                                             Mainwikibot.UpdatePageExtracts(True)
                                         End Sub)
                            End If

                        ElseIf MainParam = ("%archive") Then
                            If IsOp(imputline, Source, Realname) Then
                                CommandResponse = ArchivePage(Source, Totalparam, Realname)
                            End If

                        ElseIf MainParam = ("%div0") Then
                            Return Div0(Source, Realname, HasExited)

                        Else
                            If param.ToLower.Contains(_IrcNickName.ToLower) And Not param.ToLower.Contains("*") And Not imputline.Contains(".freenode.net ") Then
                                CommandResponse = Commands(Source, Realname)
                            End If
                        End If

                    End If
                End If

                If Not CommandResponse(0) = String.Empty Then
                    Dim response As String = IrcStringBuilder(CommandResponse(0), CommandResponse(1))

                    If Not response = LastCommand Then
                        LastCommand = response
                        Return response
                    Else
                        Return Nothing
                    End If

                Else
                    Return Nothing
                End If
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _IrcNickName)
            Return Nothing
        End Try
    End Function

    Private Function SetOp(ByVal message As String, source As String, realname As String) As String()
        Dim responsestring As String = String.Empty
        Dim param As String = message.Split(CType(" ", Char()))(4)

        If CountCharacter(param, CChar("!")) = 1 Then
            Dim requestedop As String = param.Split(CType("!", Char()))(0)

            If AddOP(message, source, realname) Then
                responsestring = "El usuario " & requestedop & " se añadió como operador"

            Else
                responsestring = "El usuario " & requestedop & " no se añadió como operador"
            End If

        Else
            responsestring = "Parámetro mal ingresado."
        End If


        Return {source, responsestring}
    End Function


    Private Function DeOp(ByVal message As String, source As String, realname As String) As String()
        Dim responsestring As String = String.Empty
        Dim param As String = message.Split(CType(" ", Char()))(4)

        If CountCharacter(param, CChar("!")) = 1 Then

            Dim requestedop As String = param.Split(CType("!", Char()))(0)
            If DelOP(message, source, realname) Then
                responsestring = "El usuario " & requestedop & " se ha eliminado como operador"
            Else
                responsestring = "El usuario " & requestedop & " no se ha eliminado como operador"
            End If
        Else
            responsestring = "Parámetro mal ingresado."
        End If

        Return {source, responsestring}
    End Function

    Private Function CommandInfo(source As String, MainParam As String, realname As String) As String()
        Dim responsestring As String = String.Empty
        MainParam = MainParam.ToLower
        Log("Commandinfo: " & MainParam, "IRC", realname)
        If MainParam = ("%última") Or MainParam = ("%ultima") Or MainParam = ("%ult") Or MainParam = ("%last") Then

            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%última/%ultima/%ult/%last", "03"), "Entrega el tiempo (Aproximado) que ha pasado desde la ultima edicion del usuario.", "%Ultima <usuario>")

        ElseIf MainParam = ("%usuario") Or MainParam = ("%usuarios") Or MainParam = ("%users") Or MainParam = ("%usr") Or
                           MainParam = ("%usrs") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%usuario/%usuarios/%users/%usr/%usrs", "03"), "Entrega una lista de los usuarios programados en tu lista (Más info: %? %programar).", "%Usuarios")

        ElseIf MainParam = ("%programar") Or MainParam = ("%programa") Or MainParam = ("%prog") Or MainParam = ("%progr") Or
                           MainParam = ("%prg") Or MainParam = ("%avisa") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%programar/%programa/%prog/%progr/%prg/%avisa", "03"), "Programa un aviso en caso de que el usuario no edite en un tiempo específico.", "%Programar Usuario/Dias/Horas/Minutos")

        ElseIf MainParam = ("%quitar") Or MainParam = ("%quita") Or
                           MainParam = ("%saca") Or MainParam = ("%sacar") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%quitar/%quita/%saca/%sacar", "03"), "Quita un usuario de tu lista programada (Más info: %? %programar).", "%Quita <usuario>")

        ElseIf MainParam = ("%ord") Or MainParam = ("%ordenes") Or
                           MainParam = ("%órdenes") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%ord/%ordenes", "03"), "Entrega una lista de las principales ordenes (Más info: %? <orden>).", "%Ordenes")

        ElseIf MainParam = "%??" Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%??", "03"), "Entrega información técnica sobre el bot (limitado según el usuario).", "%??")

        ElseIf MainParam = "%?" Or MainParam = "%h" Or MainParam = "%help" Or MainParam = "%ayuda" Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%?/%h/%help/%ayuda", "03"), "Entrega información sobre un comando.", "%? <orden>")

        ElseIf MainParam = ("%lastlog") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%lastlog", "03"), "SOLO OPS: Ultimo log del bot (TOTAL).", "%lastlog")

        ElseIf MainParam = ("%resumen") Or MainParam = ("%res") Or
                           MainParam = ("%entrada") Or MainParam = ("%entradilla") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%resumen/%res/%entrada/%entradilla", "03"), "Entrega la entradilla de un artículo en Wikipedia.", "%entradilla <Artículo>")

        ElseIf MainParam = ("%info") Or MainParam = ("%pag") Or
                           MainParam = ("%pageinfo") Or MainParam = ("%infopagina") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%info/%pag/%pageinfo/%infopagina", "03"), "Entrega datos sobre un artículo en Wikipedia.", "%Info <Artículo>")

        ElseIf MainParam = ("%updateExtracts") Or MainParam = ("%update") Or
                        MainParam = ("%upex") Or MainParam = ("%updex") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%updateExtracts/%update/%upex/%updex", "03"), "SOLO OPS, Actualiza los extractos de articulos en Wikipedia.", "%upex")

        ElseIf MainParam = ("%q") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%q", "03"), "SOLO OP, Solicita al bot cesar todas sus operaciones.", "%q")
        ElseIf MainParam = ("%op") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%op", "03"), "SOLO OP, Añade un operador.", "%op nickname!hostname")
        ElseIf MainParam = ("%deop") Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText(MainParam, "04"), ColoredText("%deop", "03"), "SOLO OP, Elimina un operador.", "%deop nickname!hostname")
        ElseIf String.IsNullOrEmpty(MainParam) Then
            responsestring = String.Format("Comando: {0}; Aliases:{1}; Función:{2}; Uso:{3}",
            ColoredText("%?", "04"), ColoredText("%?/%h/%help/%ayuda", "03"), "Entrega información sobre un comando.", "%? <orden>")

        Else
            responsestring = String.Format("No se ha encontrado el comando {0}.", ColoredText(MainParam, "04"))
        End If

        Return {source, responsestring}
    End Function

    Private Function JoinRoom(ByVal source As String, Room As String, user As String) As String
        Dim responsestring As String = ColoredText("Entrando a sala solicitada", "04")
        Dim response As String = IrcStringBuilder(source, responsestring) & Environment.NewLine & String.Format("JOIN {0}", Room)
        Log("Joined room " & Room, "IRC", user)
        Return response
    End Function

    Private Function LeaveRoom(ByVal source As String, Room As String, user As String) As String
        Dim responsestring As String = ColoredText("Saliendo de la sala solicitada", "04")
        Dim response As String = IrcStringBuilder(source, responsestring) & Environment.NewLine & String.Format("PART {0}", Room)
        Log("Leaving room " & Room, "IRC", user)
        Return response
    End Function

    Private Function Quit(ByVal source As String, user As String, ByRef HasExited As Boolean) As String
        Dim responsetext As String = IrcStringBuilder(source, "OK, voy saliendo...") & Environment.NewLine & "QUIT :Solicitado por un operador."
        HasExited = True
        Log(String.Format("IRC: Closed via request by {0}", user), "IRC", user)
        Return responsetext
    End Function

    Private Function Div0(ByVal source As String, user As String, ByRef HasExited As Boolean) As String
        Dim responsetext As String = IrcStringBuilder(source, "OK, dividiendo por 0...")
        Dim i As Double = (1 / 0)
        responsetext = responsetext & Environment.NewLine & IrcStringBuilder(source, "Al parecer el resultado es """ & i.ToString & """")
        Return responsetext
    End Function

    Private Function ArchivePage(ByVal source As String, page As String, user As String) As String()
        Dim PageName As String = TitleFirstGuess(page)
        Dim responsestring As String = ColoredText("Archivando " & PageName, "04")
        Task.Run(Sub()
                     Dim p As Page = Mainwikibot.Getpage(PageName)
                     Mainwikibot.Archive(p)
                 End Sub)
        Return {source, responsestring}
    End Function


    Private Function GetUserTime(ByVal user As String) As String()
        Dim Usertime As String = String.Empty
        For Each line As String() In UserData
            If line(0) = user Then
                Usertime = line(0)
            End If
        Next
        Return {user, Usertime}
    End Function

    Function GetResume(ByVal source As String, Page As String, user As String) As String
        Dim responsestring As String = String.Empty
        Dim PageName As String = TitleFirstGuess(Page)

        Log("IRC: GetResume of " & Page, "IRC", user)

        If Not PageName = String.Empty Then
            Dim pretext As String = "Entradilla de " & ColoredText(PageName, "03") & " en Wikipedia: "
            responsestring = pretext & Mainwikibot.GetPageExtract(PageName, 390).Replace(Environment.NewLine, " ")
            responsestring = IrcStringBuilder(source, responsestring) & Environment.NewLine &
            IrcStringBuilder(source, "Enlace al artículo: " & ColoredText(" " & site & "wiki/" & PageName.Replace(" ", "_") & " ", "10"))
            Return responsestring
        Else
            responsestring = "No se ha encontrado ninguna página llamada """ & ColoredText(Page, "03") & """ o similar."
            responsestring = IrcStringBuilder(source, responsestring)
            Return responsestring
        End If
        Return responsestring
    End Function

    Private Function LastLogComm(ByVal source As String, Prefix As String, User As String) As String()
        Dim responsestring As String = String.Empty
        If Prefix.Contains("@wikimedia/MarioFinale") Or Prefix.Contains("@wikimedia/-jem-") Then
            Dim lastlogdata As String() = Lastlog("IRC", User)
            responsestring = String.Format("Ultimo registro de: {3} via {2} a las {0}/ Tipo: {4}/ Accion: {1}", ColoredText(lastlogdata(0), "04"), lastlogdata(1), lastlogdata(2), lastlogdata(3), lastlogdata(4))
        End If
        Return {User, responsestring}
    End Function

    Private Function About(ByVal source As String, user As String) As String()
        Dim elapsedtime As TimeSpan = Uptime.Subtract(DateTime.Now)
        Dim uptimestr As String = elapsedtime.ToString("d\.hh\:mm")
        Dim responsestring As String = String.Format("{2} Versión: {0} (Bajo {1} ;Uptime: {3}). Ordenes: %ord", ColoredText(Version, "03"), ColoredText(OS, "04"), _IrcNickName, uptimestr)
        Log("IRC: Requested info (%??)", "IRC", user)
        Return {source, responsestring}
    End Function

    Private Function Commands(ByVal source As String, user As String) As String()
        Dim responsestring As String = String.Format("Hola {0}, Soy {1}, bot multipropósito de apoyo en IRC (en pruebas). Ordenes: '%Ord' /Ayuda con un comando %? <orden> /Más sobre mí: '%??'", user, _IrcNickName)
        Log(String.Format("IRC: {0} was mentioned, returning info", _IrcNickName), "IRC", user)
        Return {source, responsestring}
    End Function

    Private Function Orders(ByVal source As String, user As String) As String()
        Dim responsestring As String = String.Format("Ordenes: %programa, %quita, %ultima, %usuarios, %info, %resumen, %??, Detalles del comando %? <orden>.")
        Log("IRC: Requested orders (%ord)", "IRC", user)
        Return {source, responsestring}
    End Function

    Private Function RemoveUser(ByVal source As String, OP As String, Requesteduser As String) As String()
        Dim responsestring As String = String.Empty
        Dim UsersOfOP As New List(Of String)
        Dim UsersOfOPIndex As New List(Of Integer)

        Try
            If Requesteduser = String.Empty Then
                responsestring = "Uso del comando: %quita <usuario>. Quita a un usuario de tu lista programada"
            Else
                For Each Line As String() In UserData
                    If Line(0) = OP Then
                        UsersOfOP.Add(Line(1))
                        UsersOfOPIndex.Add(UserData.IndexOf(Line))
                    End If
                Next
                If UsersOfOP.Contains(Requesteduser) Then
                    Dim UserIndex As Integer = UsersOfOP.IndexOf(Requesteduser)
                    Dim UserIndexInUserdata As Integer = UsersOfOPIndex(UserIndex)
                    UserData.RemoveAt(UserIndexInUserdata)
                    responsestring = String.Format("Se ha quitado a '{0}' de tu lista", ColoredText(Requesteduser, "04"))
                    Log(String.Format("IRC: Removed user {0} from list of {1} (%quita)", Requesteduser, OP), "IRC", Requesteduser)
                    SaveUsersToFile()
                Else
                    responsestring = String.Format("El usuario '{0}' no esta en tu lista", ColoredText(Requesteduser, "04"))
                End If

            End If
        Catch ex As Exception
            Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", _IrcNickName)
            responsestring = String.Format("Se ha producido un error al quitar a '{0}' de tu lista", ColoredText(Requesteduser, "04"))
        End Try

        Return {source, responsestring}

    End Function

    Private Function IrcStringBuilder(ByVal Destiny As String, message As String) As String
        Return String.Format("PRIVMSG {0} :{1}", Destiny, message)
    End Function

    Private Function IrcNoticeStringBuilder(ByVal Destiny As String, message As String) As String
        Return String.Format("NOTICE {0} :{1}", Destiny, message)
    End Function

    Private Function ProgramNewUser(ByVal source As String, user As String, UserAndTime As String) As String()
        Dim ResponseString As String = String.Empty
        UserAndTime = UserAndTime.Trim(CType(" ", Char))

        Try
            If Not UserAndTime.Contains(CType("|", Char)) Then

                Dim requesteduser As String = UserAndTime.Split(CType("/", Char()))(0)
                Dim Dias As String = UserAndTime.Split(CType("/", Char()))(1)
                Dim Horas As String = UserAndTime.Split(CType("/", Char()))(2)
                Dim Minutos As String = UserAndTime.Split(CType("/", Char()))(3)
                If IsNumeric(Dias) And IsNumeric(Horas) And IsNumeric(Minutos) And (CInt(Horas) <= 23) And (CInt(Minutos) <= 59) Then
                    If (CInt(Dias) = 0) And (CInt(Horas) = 0) And (CInt(Minutos) < 10) Then
                        ResponseString = "Error: El intervalo debe ser igual o superior a 10 minutos"
                    Else
                        If UserExist(requesteduser) Then

                            If SetUserTime({user, requesteduser, Dias & "." & Horas & ":" & Minutos, user}) Then
                                ResponseString = String.Format("Hecho: Serás avisado si {0} no edita en el tiempo especificado", requesteduser)

                                Log(String.Format("IRC: Added user {0} to list (%prog)", requesteduser), "IRC", user)
                            Else
                                ResponseString = "Error"
                            End If
                        Else
                            ResponseString = String.Format("Error: El usuario {0} no existe o no tiene ninguna edición en el proyecto", requesteduser)
                        End If
                    End If
                Else
                    ResponseString = String.Format("Error: El parámetro de tiempo debe ser numérico, las horas no deben ser superiores a 23 ni los minutos a 59")
                End If

            Else
                ResponseString = String.Format("Error: Se ha ingresado un carácter ilegal ('|')")
            End If
        Catch ex As IndexOutOfRangeException
            ResponseString = String.Format("Error: El comando se ha ingresado de forma incorrecta (Uso: '%Programar Usuario/Dias/Horas/Minutos')")
        Catch ex As InvalidCastException
            ResponseString = String.Format("Error: El comando se ha ingresado de forma incorrecta (Uso: '%Programar Usuario/Dias/Horas/Minutos')")
        Catch ex As Exception
            Debug_log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex.Message, "IRC", user)
            Dim mes As String = ex.Message
            ResponseString = String.Format("Error: {0}", mes)
        End Try

        Return {source, ResponseString}

    End Function

    Private Function GetParamString(ByVal message As String) As String
        If message.Contains(":") Then
            Try
                Dim StringToRemove As String = TextInBetweenInclusive(message, ":", " :")(0)
                Dim Paramstring As String = message.Replace(StringToRemove, String.Empty)
                Return Paramstring
            Catch ex As IndexOutOfRangeException
                Return String.Empty
            Catch ex2 As Exception
                Debug_Log(System.Reflection.MethodBase.GetCurrentMethod().Name & " EX: " & ex2.Message, "IRC", _IrcNickName)
                Return String.Empty
            End Try
        Else
            Return String.Empty
        End If

    End Function

    Private Function GetProgrammedUsers(ByVal Source As String, ByVal Op As String) As String()

        Dim UserList As New List(Of String)
        Dim UserString As String = String.Empty

        For Each line As String() In UserData
            If line(0) = Op Then
                UserList.Add(line(1))
            End If
        Next

        If UserList.Count = 1 Then
            UserString = Op & ": Se te avisará si no edita " & ColoredText(UserList(0), "04") & " en el tiempo especificado."

        ElseIf UserList.Count > 1 Then

            UserString = Op & ": Se te avisará si "
            For i As Integer = 0 To UserList.Count - 1
                If i = UserList.Count - 1 Then
                    UserString = UserString & "y " & ColoredText(UserList(i), "04") & " "
                Else
                    UserString = UserString & ColoredText(UserList(i), "04") & " "
                End If
            Next
            UserString = UserString & "no editan en el tiempo especificado."
        Else
            UserString = Op & ": No hay nadie en tu lista! "
        End If

        Return {Source, UserString}

    End Function

    Private Function GetParams(ByVal Param As String) As String()
        Return Param.Split(CType(" ", Char()))
    End Function

    Private Function LastEdit(ByVal source As String, user As String, Username As String) As String()
        Dim responsestring As String = String.Empty
        Log(String.Format("IRC: Requested lastedit of {0} to list (%ultima)", Username), "IRC", user)

        If Mainwikibot.GetLastEditTimestampUser(Username).ToString.Contains("1111") Then
            responsestring = String.Format("El usuario {0} no tiene ninguna edición en el proyecto eswiki", ColoredText(Username, "04"))
        Else
            Dim edittime As DateTime = Mainwikibot.GetLastEditTimestampUser(Username)
            Dim actualtime As DateTime = DateTime.UtcNow
            actualtime = actualtime.AddTicks(-(actualtime.Ticks Mod TimeSpan.TicksPerSecond))

            Dim LastEditUnix As Integer = CInt(TimeToUnix(edittime))
            Dim ActualTimeUnix As Integer = CInt(TimeToUnix(actualtime))
            Dim Timediff As Integer = (ActualTimeUnix - LastEditUnix) - 3600
            Dim TimediffToHours As Integer = CInt(Math.Truncate(Timediff / 3600))
            Dim TimediffToMinutes As Integer = CInt(Math.Truncate(Timediff / 60))
            Dim TimediffToDays As Integer = CInt(Math.Truncate(Timediff / 86400))

            If TimediffToMinutes <= 1 Then
                responsestring = String.Format("¡{0} editó recién!", Username)
            Else
                If TimediffToMinutes < 60 Then
                    responsestring = String.Format("La última edición de {0} fue hace {1} minutos", ColoredText(Username, "04"), ColoredText(TimediffToMinutes.ToString, "09"))
                Else
                    If TimediffToMinutes < 120 Then
                        responsestring = String.Format("La última edición de {0} fue hace más de {1} hora", ColoredText(Username, "04"), ColoredText(TimediffToHours.ToString, "09"))
                    Else
                        If TimediffToMinutes < 1440 Then
                            responsestring = String.Format("La última edición de {0} fue hace más de {1} horas", ColoredText(Username, "04"), ColoredText(TimediffToHours.ToString, "09"))
                        Else
                            If TimediffToMinutes < 2880 Then
                                responsestring = String.Format("La última edición de {0} fue hace {1} día", ColoredText(Username, "04"), ColoredText(TimediffToDays.ToString, "09"))
                            Else
                                responsestring = String.Format("La última edición de {0} fue hace más de {1} días", ColoredText(Username, "04"), ColoredText(TimediffToDays.ToString, "09"))
                            End If
                        End If
                    End If
                End If
            End If
        End If

        Return {source, responsestring}

    End Function


    Private Function UserExist(ByVal username As String) As Boolean
        Dim str As String = Mainwikibot.GetLastEditTimestampUser(username).ToString
        If str.Contains("1111") Then
            Return False
        Else
            Return True
        End If
    End Function

    Private Function PageInfo(ByVal source As String, page As String, Username As String) As String
        Dim responsestring As String = String.Empty

        Dim PageName As String = TitleFirstGuess(page)
        Log("IRC: Get PageInfo of " & page, "IRC", Username)

        If Not PageName = String.Empty Then
            Dim pag As Page = Mainwikibot.Getpage(PageName)
            Dim CatString As String = String.Empty
            If pag.Categories.Count = 10 Then
                CatString = "10+"
            Else
                CatString = pag.Categories.Count.ToString
            End If
            responsestring = String.Format("Información sobre {0}: Última edición por {1}; Categorias: {2}; Visitas diarias (promedio últimos dos meses): {3}; Tamaño: {5} bytes; Puntaje ORES (Última edición): {4}",
                                           ColoredText(PageName, "03"), ColoredText(pag.Lastuser, "03"), ColoredText(CatString, "06"), ColoredText(pag.PageViews.ToString, "13"),
                                           "Dañina: " & ColoredText(pag.ORESScores(0).ToString, "04") & " Buena fé: " & ColoredText(pag.ORESScores(1).ToString, "03"), ColoredText(pag.Size.ToString, "03"))

            responsestring = IrcStringBuilder(source, responsestring) & Environment.NewLine &
            IrcStringBuilder(source, "Enlace al artículo: " & ColoredText(" " & site & "wiki/" & PageName.Replace(" ", "_") & " ", "10"))
            Return responsestring
        Else
            responsestring = "No se ha encontrado ninguna página llamada """ & ColoredText(page, "03") & """ o similar."
            responsestring = IrcStringBuilder(source, responsestring)

            Return responsestring
        End If

    End Function






End Class

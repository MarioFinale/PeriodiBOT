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
        Public CommandPrefixes As String() = {"%", "pb%", "pepino%"}


        Public Function ResolveCommand(ByVal imputline As String, ByRef HasExited As Boolean, ByVal BOTIRCNickName As String, IRCCLient As IRC_Client, WorkerBot As Bot) As IRCMessage
            Client = IRCCLient
            _bot = WorkerBot
            _IrcNickName = BOTIRCNickName





        End Function


        Sub CommandInit()
            Dim Commands As New IRCCommands

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


            Dim LastEdit As New IRCCommand("GetLastEdit", Ultima, AddressOf Commands.LastEdit, "Entrega el tiempo (Aproximado) que ha pasado desde la ultima edicion del usuario.", Client)
            Dim UserInfo As New IRCCommand("GetProgUser", Usuario, AddressOf Commands.GetProgrammedUsers, "Entrega una lista de los usuarios programados en tu lista.", Client)
            Dim ProgUser As New IRCCommand("SetProgUser", Programar, AddressOf Commands.ProgramNewUser, "Programa un aviso en caso de que un usuario no edite en un tiempo específico.", Client)
            Dim RemoveUser As New IRCCommand("RemoveProgUser", Quitar, AddressOf Commands.RemoveUser, "Quita un usuario de tu lista programada (Más info: %? %programar).", Client)
            Dim BotInfo As New IRCCommand("Info", Info, AddressOf Commands.About, "Entrega información técnica sobre el bot (limitado según el usuario).", Client)
            Dim GetResume As New IRCCommand("Resume", Resumen, AddressOf Commands.GetResume, "Entrega la entradilla de un artículo en Wikipedia.", Client)

            Dim aa As New IRCCommand("", GetFlood, AddressOf Commands., "", Client)


            Dim Gflood As New IRCCommand("SetFloodDelay", GetFlood, AddressOf Commands.GetFloodDelay, "Establece el delay de flood", Client)
            Dim SFlood As New IRCCommand("GetFloodDelay", SetFlood, AddressOf Commands.SetFloodDelay, "Obtiene el delay de flood", Client)






        End Sub

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
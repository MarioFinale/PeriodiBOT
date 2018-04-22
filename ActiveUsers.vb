Option Explicit On
Option Strict On
Imports PeriodiBOT_IRC.CommFunctions

Namespace WikiBot

    Public Class ActiveUsers
        Private _bot As Bot
        Sub New(ByRef Workerbot As Bot)
            _bot = Workerbot
        End Sub

        Sub CheckUsersActivity(ByVal TemplatePage As Page, ByVal PageToSave As Page)

            Dim ActiveUsers As New Dictionary(Of String, WikiUser)
            Dim InactiveUsers As New Dictionary(Of String, WikiUser)
            For Each p As Page In _bot.GetallInclusionsPages(TemplatePage)

                If (p.PageNamespace = 3) Or (p.PageNamespace = 2) Then
                    Dim Username As String = p.Title.Split(":"c)(1)
                    'si es una subpágina
                    If Username.Contains("/") Then
                        Username = Username.Split("/"c)(0)
                    End If
                    'Cargar usuario
                    Dim User As New WikiUser(_bot, Username)
                    'Validar usuario
                    If Not ValidUser(User) Then
                        EventLogger.Log("Archive: The user" & User.UserName & " doesn't meet the requirements.", "LOCAL")
                        Continue For
                    End If

                    If Date.Now.Subtract(User.LastEdit) < New TimeSpan(0, 30, 0) Then
                        If Not ActiveUsers.Keys.Contains(User.UserName) Then
                            ActiveUsers.Add(User.UserName, User)
                        End If
                    Else
                        If Not InactiveUsers.Keys.Contains(User.UserName) Then
                            InactiveUsers.Add(User.UserName, User)
                        End If
                    End If
                End If
            Next


            Dim t As New Template
            t.Name = "#switch:{{{1|}}}"
            t.Parameters.Add(New Tuple(Of String, String)("", "'''Error''': No se ha indicado usuario."))
            t.Parameters.Add(New Tuple(Of String, String)("#default", "[[Archivo:WX circle red.png|10px|link=]]&nbsp;<span style=""color:red;"">'''Desconectado'''</span>"))

            For Each u As WikiUser In ActiveUsers.Values
                Dim gendertext As String = "Conectado"
                If u.Gender = "female" Then
                    gendertext = "Conectada"
                End If
                t.Parameters.Add(New Tuple(Of String, String)(u.UserName, "[[Archivo:WX circle green.png|10px|link=]]&nbsp;<span style=""color:green;"">'''" & gendertext & "'''</span>"))
            Next

            For Each u As WikiUser In InactiveUsers.Values
                Dim gendertext As String = "Desconectado"
                If u.Gender = "female" Then
                    gendertext = "Desconectada"
                End If
                Dim lastedit As Date = u.LastEdit
                t.Parameters.Add(New Tuple(Of String, String)(u.UserName, "[[Archivo:WX circle red.png|10px|link=|Última edición el " & Integer.Parse(lastedit.ToString("dd")).ToString & lastedit.ToString(" 'de' MMMM 'de' yyyy 'a las' HH:mm '(UTC)'", New System.Globalization.CultureInfo("es-ES")) & "]]&nbsp;<span style=""color:red;"">'''" & gendertext & "'''</span>"))
            Next

            Dim templatetext As String = "<div style=""position:absolute; z-index:100; right:10px; top:5px;"" class=""metadata"">" & Environment.NewLine & t.Text
            templatetext = templatetext & Environment.NewLine & "</div>" & Environment.NewLine & "<noinclude>" & "{{documentación}}" & "</noinclude>"
            PageToSave.Save(templatetext, "Bot: Actualizando lista.")

        End Sub

        ''' <summary>
        ''' Verifica si el usuario que se le pase cumple con los requisitos para verificar su actividad
        ''' </summary>
        ''' <param name="user">Usuario de Wiki</param>
        ''' <returns></returns>
        Private Function ValidUser(ByVal user As WikiUser) As Boolean
            EventLogger.Debug_Log("ValidUser: Check user", "LOCAL")
            'Verificar si el usuario existe
            If Not user.Exists Then
                EventLogger.Log("ValidUser: User " & user.UserName & " doesn't exist", "LOCAL")
                Return False
            End If

            'Verificar si el usuario está bloqueado.
            If user.Blocked Then
                EventLogger.Log("ValidUser: User " & user.UserName & " is blocked", "LOCAL")
                Return False
            End If

            'Verificar si el usuario editó hace al menos 4 días.
            If Date.Now.Subtract(user.LastEdit).Days >= 4 Then
                EventLogger.Log("ValidUser: User " & user.UserName & " is inactive", "LOCAL")
                Return False
            End If
            Return True
        End Function










    End Class

End Namespace
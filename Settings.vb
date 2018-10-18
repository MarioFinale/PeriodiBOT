Option Explicit On
Option Strict On
Imports System.IO

Public Class Settings
    Private StrSettings As New Dictionary(Of String, String)
    Private IntSettings As New Dictionary(Of String, Integer)
    Private SettingsIndex As New HashSet(Of String)
    Private _filePath As String

    Public Sub New(ByVal filepath As String)
        _filePath = filepath

        If Not File.Exists(filepath) Then
            File.Create(filepath).Close()
        End If

        For Each l As String In File.ReadLines(filepath)
            Dim vars As String() = l.Split("|"c)
            If vars.Count = 2 Then
                If Not SettingsIndex.Contains(vars(0)) Then
                    If IsNumeric(vars(1)) Then
                        IntSettings.Add(vars(0), Integer.Parse(vars(1)))
                        SettingsIndex.Add(vars(0))
                    Else
                        StrSettings.Add(vars(0), vars(1))
                        SettingsIndex.Add(vars(0))
                    End If
                End If
            End If
        Next
    End Sub

    Private Sub SaveConfig()
        Dim lines As New List(Of String)
        For Each var As String In SettingsIndex
            If StrSettings.Keys.Contains(var) Then
                lines.Add(var & "|" & StrSettings(var))
            ElseIf IntSettings.Keys.Contains(var) Then
                lines.Add(var & "|" & IntSettings(var).ToString)
            End If
        Next
        Try
            File.WriteAllLines(_filePath, lines.ToArray)
        Catch ex As IO.IOException
            Utils.EventLogger.EX_Log(ex.Message, "SaveConfig")
        End Try
    End Sub

    Public Function Contains(ByVal value As String) As Boolean
        Return SettingsIndex.Contains(value)
    End Function

    Public Function [Get](ByVal setting As String) As Object
        If SettingsIndex.Contains(setting) Then
            If StrSettings.Keys.Contains(setting) Then
                Return StrSettings(setting)
            ElseIf IntSettings.Keys.Contains(setting) Then
                Return IntSettings(setting)
            End If
        End If
        Throw New MissingFieldException
    End Function


    Public Function NewVal(ByVal setting As String, value As Integer) As Boolean
        If Not SettingsIndex.Contains(setting) Then
            IntSettings.Add(setting, value)
            SettingsIndex.Add(setting)
            SaveConfig()
            Return True
        Else
            Return False
        End If
    End Function

    Public Function NewVal(ByVal setting As String, value As String) As Boolean
        If Not SettingsIndex.Contains(setting) Then
            StrSettings.Add(setting, value)
            SettingsIndex.Add(setting)
            SaveConfig()
            Return True
        Else
            Return False
        End If
    End Function

    Public Function [Set](ByVal setting As String, value As String) As Boolean
        If SettingsIndex.Contains(setting) Then
            If IntSettings.Keys.Contains(setting) Then
                Return False
            End If
            StrSettings(setting) = value
            SaveConfig()
            Return True
        Else
            Return False
        End If
    End Function

    Public Function [Set](ByVal setting As String, value As Integer) As Boolean
        If SettingsIndex.Contains(setting) Then
            If StrSettings.Keys.Contains(setting) Then
                Return False
            End If
            IntSettings(setting) = value
            SaveConfig()
            Return True
        Else
            Return False
        End If
    End Function

    Public Function Remove(ByVal setting As String) As Boolean
        If SettingsIndex.Contains(setting) Then
            If StrSettings.Keys.Contains(setting) Then
                StrSettings.Remove(setting)
                SettingsIndex.Remove(setting)

            ElseIf IntSettings.Keys.Contains(setting) Then
                IntSettings.Remove(setting)
                SettingsIndex.Remove(setting)
            End If
        Else
            Return False
        End If
        Return True
    End Function
End Class

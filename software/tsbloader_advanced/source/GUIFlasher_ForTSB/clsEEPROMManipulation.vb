Public Class clsEEPROMManipulation
    Private sConfigPath As String
    Private f As frmGUIFlasher


    Public Sub New(sConfigFilesPath As String, ByRef fMainForm As frmGUIFlasher)
        sConfigPath = sConfigFilesPath
        f = fMainForm

        LoadAndFillEEPComboBoxes()
    End Sub

    Public Function GetServoID() As Integer

    End Function

    Public Function GetEEPAddress(iHexValue_Linenr As Integer) As Integer

    End Function

    Public Function GetEEPValue(iHexValue_Linenr As Integer) As Integer

    End Function


    Public Sub ManipulateEEPromFile(sEEPROMFileNameInput As String, ByRef sEEPROMFileNameOutput As String)

    End Sub

    Private Sub LoadAndFillEEPComboBoxes()
        If System.IO.File.Exists(sConfigPath & "EEPOptions1.mysettings") Then
            Dim sLines() As String = System.IO.File.ReadAllLines(sConfigPath & "EEPOptions1.csv")
            LoadOptions(f.cboEEPAddr1, f.cboEEPValue1, sLines, "EEP Options 1")



        End If


    End Sub

    Private Sub LoadOptions(cboAddr As ComboBox, cboValue As ComboBox, sLines() As String, sFileDescription As String)
        For Each s As String In sLines
            s = Trim(s)

            If s.Length < 3 Then Continue For
            If Left(s, 1) = ";" Or Left(s, 1) = "'" Or Left(s, 1) = "#" Or Left(s, 2) = "//" Then Continue For ' comment line

            Dim lineSplit() As String = s.Split(",")
            If lineSplit.Length <> 2 Then
                MsgBox("Error loading options for " & sFileDescription & vbCrLf & vbCrLf & "Line has invalid syntax (each line should have 2 sets of values, separated by comma):" & vbCrLf & s, vbExclamation)
                Continue For
            End If




        Next

    End Sub
End Class

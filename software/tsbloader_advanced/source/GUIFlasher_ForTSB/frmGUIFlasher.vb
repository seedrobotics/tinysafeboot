Imports System
Imports System.ComponentModel
Imports System.IO.Ports
Imports System.Xml.Serialization

Public Class frmGUIFlasher
    Delegate Sub UpdateTextBoxDelg(text As String)
    Delegate Sub ProcessEndedDelg(text As String)
    Public delegateConsoleDataReceived As UpdateTextBoxDelg = New UpdateTextBoxDelg(AddressOf UpdateTextBox)
    Public delegateProcessEnded As ProcessEndedDelg = New ProcessEndedDelg(AddressOf NotifyProcessedEnded)

    Private sTSBLoaderEXELocation As String

    Private WithEvents procTSB As Process

    Private Sub frmActuatorFlasher_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FillCommPortList()

        Dim apsettings As clsAppSettings

        If IO.File.Exists(GetSettingsFileName) Then
            Dim mySerializer As XmlSerializer = New XmlSerializer(GetType(clsAppSettings))
            Dim myFileStream As System.IO.FileStream = New System.IO.FileStream(GetSettingsFileName, System.IO.FileMode.Open)
            apsettings = CType(mySerializer.Deserialize(myFileStream), clsAppSettings)
            myFileStream.Close()

            apsettings.SetSettingsInForm(Me)
        End If


    End Sub

    Private Sub FillCommPortList()
        Dim ports As String() = SerialPort.GetPortNames

        Dim selectedPort As String
        selectedPort = Me.cboCOMMPort.Text

        cboCOMMPort.Items.Clear()
        For Each port As String In ports
            cboCOMMPort.Items.Add(port)
        Next

        If cboCOMMPort.Items.Contains(selectedPort) Then cboCOMMPort.SelectedItem = selectedPort

    End Sub

    Private Sub txtServoID_Validating(sender As Object, e As ComponentModel.CancelEventArgs) Handles txtServoID.Validating
        Dim bError As Boolean

        If Not IsNumeric(txtServoID.Text) Then
            bError = True
        ElseIf CInt(txtServoID.Text) < 1 Or CInt(txtServoID.Text) > 253 Then
            bError = True
        End If

        If bError Then
            MsgBox("Servo ID must be numeric and maximum 253." & vbCrLf & "For compatibility with EROS boards, setting ID to 0 is also not allowed here.", vbExclamation)
            e.Cancel = True
        Else
            If chkTSBMatchPasswordToServoID.Checked Then
                ' get the last number of the servo ID and assign a letter to the password
                txtTSBPassword.Text = Chr(CInt(txtServoID.Text) Mod 10 + Asc("A") - 1) ' -1 so that 1 is A
            End If
        End If
    End Sub

    Private Sub ValidateKeyPress_OnlyNumbers(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtServoID.KeyPress, txtHexAddress.KeyPress, txtTSBTimeout.KeyPress
        If Not Char.IsNumber(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub chkTSBMatchPasswordToServoID_CheckedChanged(sender As Object, e As EventArgs) Handles chkTSBMatchPasswordToServoID.CheckedChanged
        If Me.chkTSBMatchPasswordToServoID.Checked Then
            Me.txtTSBPassword.Enabled = False
            txtServoID_Validating(Nothing, New ComponentModel.CancelEventArgs)
        Else
            txtTSBPassword.Enabled = True
        End If
    End Sub

    Private Sub chkManipulateEEPROM_CheckedChanged(sender As Object, e As EventArgs) Handles chkManipulateEEPROM.CheckedChanged
        txtServoID.Enabled = chkManipulateEEPROM.Checked And chkEEPROMFile.Checked
        txtHexAddress.Enabled = chkManipulateEEPROM.Checked And chkEEPROMFile.Checked
    End Sub

    Private Sub chkTSBConfigurePassword_CheckedChanged(sender As Object, e As EventArgs) Handles chkTSBConfigurePassword.CheckedChanged
        Me.txtTSBPassword.Enabled = chkTSBConfigurePassword.Checked And Not chkTSBMatchPasswordToServoID.Checked
        Me.chkTSBMatchPasswordToServoID.Enabled = chkTSBConfigurePassword.Checked
    End Sub

    Private Sub chkTSBConfigureTimeout_CheckedChanged(sender As Object, e As EventArgs) Handles chkTSBConfigureTimeout.CheckedChanged
        txtTSBTimeout.Enabled = chkTSBConfigureTimeout.Checked
    End Sub

    Private Sub btnTSBLoaderLocation_Click(sender As Object, e As EventArgs) Handles btnTSBLoaderLocation.Click
        Dim s As String
        s = InputBox("Enter the location of the TSB Loader Advanced executable (must be the Advanced loader, released by Seed Robotics Ltd under GPL V3)",, Me.TSBLoaderExeLocation)

        s = s.Trim

        If s.Length > 0 Then
            If System.IO.File.Exists(s) Then
                Me.TSBLoaderExeLocation = s

            Else
                MsgBox("The file " & vbCrLf & s & vbCrLf & "could not be found.", vbCritical)
            End If
        End If
    End Sub

    Private Function GetSettingsFileName() As String
        Return Application.ExecutablePath & ".mysettings"
    End Function

    Private Sub frmActuatorFlasher_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Dim apsettings As New clsAppSettings

        apsettings.CollectSettingsFromForm(Me)

        Dim mySerializer As XmlSerializer = New XmlSerializer(GetType(clsAppSettings))
        Try
            Dim myWriter As System.IO.StreamWriter = New System.IO.StreamWriter(GetSettingsFileName())
            mySerializer.Serialize(myWriter, apsettings)
            myWriter.Close()

        Catch ex As Exception
            MsgBox("Unable to save program settings." & vbCrLf & vbCrLf & ex.Message, vbExclamation)
        End Try

    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefreshCOMMPorts.Click
        FillCommPortList()
    End Sub

    Private Sub btnRunTSBLoader_Click(sender As Object, e As EventArgs) Handles btnRunTSBLoader.Click
        If Not System.IO.File.Exists(Me.TSBLoaderExeLocation) Then
            MsgBox("Can't run TSB. The TSB Loader EXE Location is invalid or inaccessible!", vbCritical)
            btnTSBLoaderLocation_Click(Nothing, Nothing)
            Return
        End If

        If chkTSBConfigureTimeout.Checked Then
            If CInt(txtTSBTimeout.Text) < 50 Then
                MsgBox("TSB timeout is too small. Minimum we allow is 50 to keep the bootloader accessible.")
                Return
            End If
        End If

        Dim sParams As String = BuildTSBParameters()
        If IsNothing(sParams) Then Return

        txtOutput.AppendText(vbCrLf & "----" & vbCrLf)
        txtOutput.AppendText("Running TSB Command: " & Me.TSBLoaderExeLocation & " " & sParams & vbCrLf)

        ' prepare an stdin file to set the pwd and timeout
        ' we should PWD argument first and timeout argument after
        Dim sStdInText As String = ""
        If chkTSBConfigurePassword.Checked Then
            sStdInText &= txtTSBPassword.Text & vbCrLf & "y" & vbCrLf ' password + "y" to confirm setting pwd
        End If
        If chkTSBConfigureTimeout.Checked Then
            sStdInText &= txtTSBTimeout.Text & vbCrLf
        End If


        If Not chkFTDIHasAutoReset.Checked Then
            Dim c As MsgBoxResult = MsgBox("Power cycle the unit and press OK to initiate TSB session.", vbInformation)
        End If

        btnRunTSBLoader.Visible = False
        cmdKillTSB.Visible = True

        RunWithRedirect(Me.TSBLoaderExeLocation, sParams, sStdInText)
    End Sub
    Private Function BuildTSBParameters() As String
        Dim sCommand As String = " -I" ' to that TSB always runs and shows device data even if no action specified

        If cboCOMMPort.Text = "" Then
            MsgBox("No COMM Port specified!", vbCritical)
            Return Nothing
        End If
        sCommand &= " -port=" & cboCOMMPort.Text

        If chkFlashFile.Checked Then
            If Not System.IO.File.Exists(txtFlashFile.Text) Then
                MsgBox("Flash file can't be found!", vbCritical)
                Return Nothing
            End If
            sCommand &= " -fop=wv -ffile=""" & txtFlashFile.Text & """"
        End If

        If chkEEPROMFile.Checked Then
            If Not System.IO.File.Exists(txtEEPROMFile.Text) Then
                MsgBox("EEPROM file can't be found!", vbCritical)
                Return Nothing

            ElseIf System.IO.Path.GetExtension(txtEEPROMFile.Text).ToLower <> ".bin" Then
                MsgBox("Can't manipulate EEPROM! The EEPROM format is not supported." & vbCrLf & "Only BIN (binary) files can be manipulated.", vbCritical)
                Return Nothing
            End If

            If chkManipulateEEPROM.Checked Then
                If Not IsNumeric(txtHexAddress.Text) Then
                    MsgBox("Can't manipulate EEPROM! The HEX address to manipulate is invalid.", vbCritical)
                    Return Nothing
                End If


                ' open eeprom and manipulate the file into a temp file
                Dim bEEPROMcontents As Byte() = System.IO.File.ReadAllBytes(txtEEPROMFile.Text)
                If bEEPROMcontents.GetUpperBound(0) < CInt(txtHexAddress.Text) Then
                    MsgBox("Can't manipulate EEPROM! The HEX address to manipulate is higher than EEPROM size.", vbCritical)
                    Return Nothing
                End If

                bEEPROMcontents(CInt(txtHexAddress.Text)) = CInt(txtServoID.Text)

                ' write the file out
                Dim sTempFileName As String = System.IO.Path.GetTempPath & "manipulated_eeprom.bin"
                System.IO.File.WriteAllBytes(sTempFileName, bEEPROMcontents)

                sCommand &= " -eop=ewv -efile=""" & sTempFileName & """"

            Else
                sCommand &= " -eop=ewv -efile=""" & txtEEPROMFile.Text & """"
            End If
        End If

        If chkTSBConfigurePassword.Checked Then
            sCommand &= " -xop=p"
        End If

        If chkTSBConfigureTimeout.Checked Then
            If Not chkTSBConfigurePassword.Checked Then
                sCommand &= " -xop=t"
            Else
                sCommand &= "t"
            End If
        End If

        sCommand &= " " & txtTSBAdditionalParams.Text

        Return sCommand

    End Function

    Private Sub chkEEPROMFile_CheckedChanged(sender As Object, e As EventArgs) Handles chkEEPROMFile.CheckedChanged
        Me.chkManipulateEEPROM.Enabled = chkEEPROMFile.Checked
        chkManipulateEEPROM_CheckedChanged(Nothing, New EventArgs)
    End Sub

    Private Sub RunWithRedirect(ByVal cmdExename As String, cmdArgs As String, sStdInTextToPass As String)
        procTSB = New Process()

        procTSB.StartInfo.FileName = cmdExename
        procTSB.StartInfo.Arguments = cmdArgs


        procTSB.StartInfo.RedirectStandardOutput = True
        procTSB.StartInfo.RedirectStandardError = True
        procTSB.StartInfo.RedirectStandardInput = True
        procTSB.EnableRaisingEvents = True
        Application.DoEvents()
        procTSB.StartInfo.CreateNoWindow = True
        procTSB.StartInfo.UseShellExecute = False

        procTSB.Start()
        procTSB.BeginErrorReadLine()
        procTSB.BeginOutputReadLine()


        ' Feed the Keystrokes to the process (STDIN), this would include 
        ' any parameters that TSB expects as text such as password and timeout
        'procTSB.StandardInput.Write(sStdInTextToPass)

    End Sub

    Public Sub proc_DataReceived(ByVal sender As Object, ByVal e As DataReceivedEventArgs) Handles procTSB.ErrorDataReceived, procTSB.OutputDataReceived
        If Me.InvokeRequired = True Then
            Me.Invoke(delegateConsoleDataReceived, e.Data)
        Else
            UpdateTextBox(e.Data)
        End If
    End Sub

    Public Sub UpdateTextBox(txt As String)
        If Not String.IsNullOrEmpty(txt) Then
            txtOutput.AppendText(txt & vbCrLf)

            If txt.Contains("new Password") And chkTSBConfigurePassword.Checked Then procTSB.StandardInput.Write(txtTSBPassword.Text & vbCrLf & "y" & vbCrLf)
            If txt.Contains("new Timeout") And chkTSBConfigureTimeout.Checked Then procTSB.StandardInput.Write(txtTSBTimeout.Text & vbCrLf)
            If txt.Contains("Would you like to enter a bootloader password?") Then
                Dim sBootldrPwd = InputBox("The bootloader on the device appears to have a password." & vbCrLf & "Please enter the Bootloader password:")
                procTSB.StandardInput.Write(sBootldrPwd & vbCrLf)
            End If

        End If
    End Sub

    Public Sub NotifyProcessedEnded(txt As String)
        UpdateTextBox(txt)

        If procTSB.ExitCode > 0 Then
            MsgBox("TSB session ended with error. Please review log.", vbCritical)
        End If

        btnRunTSBLoader.Visible = True
        cmdKillTSB.Visible = False
    End Sub

    Private Sub procTSB_Exited(sender As Object, e As EventArgs) Handles procTSB.Exited
        If Me.InvokeRequired = True Then
            Me.Invoke(delegateProcessEnded, "TSB terminated. Exit code " & procTSB.ExitCode)
        Else
            NotifyProcessedEnded("TSB terminated. Exit code " & procTSB.ExitCode)
        End If

    End Sub

    Private Sub cmdKillTSB_Click(sender As Object, e As EventArgs) Handles cmdKillTSB.Click
        If Not procTSB Is Nothing Then
            If Not procTSB.HasExited Then procTSB.Kill()
        End If

        btnRunTSBLoader.Visible = True
        cmdKillTSB.Visible = False

    End Sub

    Friend Property TSBLoaderExeLocation As String
        Get
            Return sTSBLoaderEXELocation
        End Get
        Set(value As String)
            sTSBLoaderEXELocation = value
            lblStatus.Text = String.Format("   App Version {0} │ TSB Loader EXE Location: {1}", Application.ProductVersion.ToString, sTSBLoaderEXELocation)
        End Set
    End Property


End Class


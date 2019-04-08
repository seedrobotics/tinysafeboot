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
        s = GetFileNameToOpen("Select TSB Advanced Loader Console Executable", "EXE Files (*.exe)|*.exe", Me.TSBLoaderExeLocation)


        's = InputBox("Enter the location of the TSB Loader Advanced executable (must be the Advanced loader, released by Seed Robotics Ltd under GPL V3)",, Me.TSBLoaderExeLocation)
        's = s.Trim

        If Not IsNothing(s) Then
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

        Dim sParams As String = BuildTSBParameters()
        If IsNothing(sParams) Then Return

        ' prepare an stdin file to set the pwd and timeout to configure in the bootldr
        ' we should PWD argument first and timeout argument after
        'Dim sStdInText As String = ""
        'If chkTSBConfigurePassword.Checked Then
        '    sStdInText &= txtTSBPassword.Text & vbCrLf & "y" & vbCrLf ' password + "y" to confirm setting pwd
        'End If
        'If chkTSBConfigureTimeout.Checked Then
        '    sStdInText &= txtTSBTimeout.Text & vbCrLf
        'End If


        If Not chkFTDIHasAutoReset.Checked Then
            Dim c As MsgBoxResult = MsgBox("Power cycle the unit and press OK to initiate TSB session.", vbInformation)
        End If

        btnRunTSBLoader.Visible = False
        cmdKillTSB.Visible = True
        btnSeedErosEnable.Enabled = False
        btnSeedErosDisable.Enabled = False

        RunWithRedirect(Me.TSBLoaderExeLocation, sParams)
    End Sub
    Private Function BuildTSBParameters() As String
        Dim sCommand As String = " -I" ' to that TSB always runs and shows device data even if no action specified

        If chkTSBConfigureTimeout.Checked Then
            If CInt(txtTSBTimeout.Text) < 50 Then
                MsgBox("TSB timeout setting is too small. Minimum we allow using the GUI is 50, to keep the bootloader accessible.")
                Return Nothing
            End If
        End If

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

    Private Sub RunWithRedirect(ByVal cmdExename As String, cmdArgs As String)
        If Not System.IO.File.Exists(Me.TSBLoaderExeLocation) Then
            MsgBox("Can't run TSB. The TSB Loader EXE Location is invalid or inaccessible!", vbCritical)
            btnTSBLoaderLocation_Click(Nothing, Nothing)
            Return
        End If

        If cboCOMMPort.Text = "" Then
            MsgBox("No COMM Port specified!", vbCritical)
            Return
        End If
        cmdArgs &= " -port=" & cboCOMMPort.Text

        UpdateTextBox(vbCrLf & "----")
        UpdateTextBox("Running TSB Command: " & cmdExename & " " & cmdArgs)

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

    Public Sub UpdateTextBox(txt As String, Optional cColor As Color = Nothing)
        If Not String.IsNullOrEmpty(txt) Then
            'txtOutput.AppendText(txt & vbCrLf)

            If txt.ToLower.Contains("error") Or txt.ToLower.Contains("could not activate bootloader") Or
                txt.ToLower.Contains("warning") Then
                cColor = Color.Orange
            End If

            rtfStatus.SelectionStart = rtfStatus.TextLength
            rtfStatus.SelectionLength = 0 ' Len(txt)
            If Not IsNothing(cColor) Then
                rtfStatus.SelectionColor = cColor
            End If

            rtfStatus.AppendText(txt & vbCrLf)

            rtfStatus.SelectionStart = rtfStatus.TextLength
            rtfStatus.ScrollToCaret()

            If txt.Contains("new Password") And chkTSBConfigurePassword.Checked Then procTSB.StandardInput.Write(txtTSBPassword.Text & vbCrLf & "y" & vbCrLf)
            If txt.Contains("new Timeout") And chkTSBConfigureTimeout.Checked Then procTSB.StandardInput.Write(txtTSBTimeout.Text & vbCrLf)

            ' the question for entering the password changed from TSBloader_adv.exe v1.0.6 onwards
            ' we check both for compatibility with the older and newer versions
            If txt.Contains("enter the bootloader password") Or txt.Contains("Would you like to enter a bootloader password?") Then
                Dim sBootldrPwd = InputBox("The bootloader on the device appears to have a password." & vbCrLf & "Please enter the Bootloader password:")
                procTSB.StandardInput.Write(sBootldrPwd & vbCrLf)
            End If

        End If
    End Sub

    Public Sub NotifyProcessedEnded(txt As String)
        If procTSB.ExitCode > 0 Then
            UpdateTextBox(txt, Color.Red)
            My.Computer.Audio.Play(My.Resources.FlashError, AudioPlayMode.Background)
            'MsgBox("TSB session ended with error. Please review log.", vbCritical)

        ElseIf procTSB.ExitCode = 0 Then ' must explicitly set 0 bc if we abort we'll get return code -1; we don't want to do anything in that case
            UpdateTextBox(txt, Color.Green)
            My.Computer.Audio.Play(My.Resources.FlashSucess, AudioPlayMode.Background)
        End If

        btnRunTSBLoader.Visible = True
        cmdKillTSB.Visible = False

        btnSeedErosDisable.Enabled = True
        btnSeedErosEnable.Enabled = True
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

        btnSeedErosDisable.Enabled = True
        btnSeedErosEnable.Enabled = True
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

    Private Sub btnSeedErosEnable_Click(sender As Object, e As EventArgs) Handles btnSeedErosEnable.Click
        RunWithRedirect(sTSBLoaderEXELocation, " -seederos=bron")
    End Sub

    Private Sub btnSeedErosDisable_Click(sender As Object, e As EventArgs) Handles btnSeedErosDisable.Click
        RunWithRedirect(sTSBLoaderEXELocation, " -seederos=broff")
    End Sub

    Private Sub txtFlashAndEEPromFile_TextChanged(sender As Object, e As EventArgs) Handles txtFlashFile.TextChanged, txtEEPROMFile.TextChanged
        ' make sure we are NOT in Linux
        ' based on https://stackoverflow.com/questions/5116977/how-to-check-the-os-version-at-runtime-e-g-windows-or-linux-without-using-a-con
        If Environment.OSVersion.Platform <> 4 And
            Environment.OSVersion.Platform <> 6 And
            Environment.OSVersion.Platform <> 128 Then

            ' remove all quotes
            txtFlashFile.Text = txtFlashFile.Text.Replace("""", String.Empty)
        End If
    End Sub

    Private Sub cmdBrowseFlashFile_Click(sender As Object, e As EventArgs) Handles cmdBrowseFlashFile.Click
        Dim s As String = GetFileNameToOpen("Select Flash File", "HEX Files (*.hex)|*.hex|Binary Files (*.bin)|*.bin|All files|*.*", txtFlashFile.Text)

        If Not s Is Nothing Then txtFlashFile.Text = s
    End Sub


    Private Function GetFileNameToOpen(sDialogTitle As String, sExtensionsFilter As String, sCurrentFileName As String) As String

        With dlgFileOpen
            .Title = sDialogTitle
            .Filter = sExtensionsFilter

            Dim sStartPath As String = System.IO.Path.GetDirectoryName(sCurrentFileName)
            If System.IO.Directory.Exists(sStartPath) Then
                .InitialDirectory = sStartPath
            End If

            Dim result As DialogResult = .ShowDialog()

            If result = DialogResult.Cancel Then Return Nothing
        End With

        Return dlgFileOpen.FileName

    End Function

    Private Sub cmdBrowseEEPROMFile_Click(sender As Object, e As EventArgs) Handles cmdBrowseEEPROMFile.Click
        Dim s As String = GetFileNameToOpen("Select EEPROM File", "Binary Files (*.bin)|*.bin|All files|*.*", txtEEPROMFile.Text)

        If Not s Is Nothing Then txtEEPROMFile.Text = s
    End Sub
End Class


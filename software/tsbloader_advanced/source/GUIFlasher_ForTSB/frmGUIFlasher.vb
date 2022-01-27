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

    Private timeout_sent As Boolean, mbyte1_sent As Boolean, mbyte2_sent As Boolean, new_pwd_sent As Boolean, current_pwd_sent As Boolean

    Private WithEvents procTSB As Process

    Private Sub frmActuatorFlasher_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FillCommPortList()

        FillMagicBytesList()

        Dim apsettings As clsAppSettings

        If IO.File.Exists(GetSettingsFileName) Then
            Dim mySerializer As XmlSerializer = New XmlSerializer(GetType(clsAppSettings))
            Dim myFileStream As System.IO.FileStream = New System.IO.FileStream(GetSettingsFileName, System.IO.FileMode.Open)
            apsettings = CType(mySerializer.Deserialize(myFileStream), clsAppSettings)
            myFileStream.Close()

            apsettings.SetSettingsInForm(Me)
        End If




    End Sub

    Private Sub FillMagicBytesList()

        Dim mgbcbo(2) As ComboBox
        mgbcbo(0) = Me.cboMgByte1
        mgbcbo(1) = Me.cboMgByte2

        For i As Byte = 0 To 1
            mgbcbo(i).Items.Clear()

            ' set it up so that we can display text and assign a different value
            mgbcbo(i).DisplayMember = "Text"
            mgbcbo(i).ValueMember = "Value"

            If IO.File.Exists(GetMagicByteSuggestionListFileName(i + 1)) Then
                Dim sFileData As String = My.Computer.FileSystem.ReadAllText(GetMagicByteSuggestionListFileName(i + 1))

                ' replace line endings to be compatible with Win/Linux/MacOs
                sFileData = sFileData.Replace(vbCrLf, vbCr)
                sFileData = sFileData.Replace(vbLf, vbCr)

                Dim lines() As String = sFileData.Split(vbCr)
                Dim errors As String = ""

                For currLineNr As Integer = 0 To lines.Length - 1
                    lines(currLineNr) = lines(currLineNr).Trim

                    If lines(currLineNr).Length <> 0 Then
                        ' Sintax:
                        ' [HEX VALUE without 0x][space]whatever description you want
                        Dim spaceIx = lines(currLineNr).IndexOf(" ")

                        If spaceIx < 1 Or spaceIx > 2 Then ' no hex value or Hex value too big
                            errors &= String.Format("Magic Byte {0}, at Line {1}: Hex value is invalid. Hex value must be the first value in the line (whithout the 0x) followed by a space and description. Maximum value shall be FF", i, currLineNr)

                        Else
                            Dim hexValue As String = lines(currLineNr).Substring(0, spaceIx)

                            ' try parse hex
                            Dim Hvalue As Integer = 0
                            If Int16.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, Hvalue) = False Then
                                errors &= String.Format("Magic Byte {0}, at Line {1}: Hex value is invalid. Hex value should be specified without the leading 0x, followed by a space and description. Maximum value shall be FF", i, currLineNr)

                            Else
                                mgbcbo(i).Items.Add(New With {
                                                    Key .Text = String.Format("{0:X02}:", Hvalue) & lines(currLineNr).Substring(spaceIx),
                                                    Key .Value = Hvalue
                                                   })
                            End If
                            '(comboBox.SelectedItem as dynamic).Value

                        End If

                    End If
                Next

                If errors.Length > 0 Then
                    MsgBox(errors, vbExclamation Or vbOKOnly, "Error loading List of Magic Byte " & i)
                End If
            End If
        Next
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

            If chkTSBMatchByteToServoID.Checked Then
                chkTSBMatchByteToServoID_CheckedChanged(Nothing, Nothing)
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

    Private Function GetMagicByteSuggestionListFileName(b_ByteNr As Byte) As String
        Return Application.ExecutablePath & ".MgByte" & b_ByteNr.ToString & ".list"
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
        Dim sCommand As String = " -I" ' so that TSB always runs and shows device data even if no action specified
        Dim bMGbyte1 As Integer, bMGbyte2 As Integer

        If chkTSBConfigureTimeout.Checked Then
            If txtTSBTimeout.Text.Trim = "" Then
                MsgBox("TSB timeout setting is is not specified.")
                Return Nothing
            Else
                If CInt(txtTSBTimeout.Text) < 50 Then
                    MsgBox("TSB timeout setting is too small. Minimum we allow using this GUI is 50, to keep the bootloader accessible.")
                    Return Nothing
                End If
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
                    MsgBox("Can't manipulate EEPROM! The HEX address to manipulate is invalid (must be specified in Decimal).", vbCritical)
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




        Dim bXOPParam_Inserted As Boolean = False
        If chkTSBConfigurePassword.Checked Then
            If Not bXOPParam_Inserted Then sCommand &= " -xop=" : bXOPParam_Inserted = True
            sCommand &= "p"
        End If

        If chkTSBConfigureTimeout.Checked Then
            If Not bXOPParam_Inserted Then sCommand &= " -xop=" : bXOPParam_Inserted = True
            sCommand &= "t"
        End If

        If chkConfigureMagicBytes.Checked Then
            bMGbyte1 = GetMagicByteValue(cboMgByte1)
            bMGbyte2 = GetMagicByteValue(cboMgByte2)

            If bMGbyte1 < 0 Or bMGbyte1 > &HFF Then
                MsgBox("Value of Magic Byte 1 is Invalid.", vbExclamation)
                Return Nothing
            End If

            If bMGbyte2 < 0 Or bMGbyte2 > &HFF Then
                MsgBox("Value of Magic Byte 2 is Invalid.", vbExclamation)
                Return Nothing
            End If

            If Not bXOPParam_Inserted Then sCommand &= " -xop=" : bXOPParam_Inserted = True
            sCommand &= "m"
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


        ' clear text box
        rtfStatus.Clear()

        ' clear all status control flags
        timeout_sent = False
        mbyte1_sent = False
        mbyte2_sent = False
        new_pwd_sent = False
        current_pwd_sent = False

        'UpdateTextBox(vbCrLf & "----")
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

            If (Not new_pwd_sent) And (rtfStatus.Text.Contains("new Password") Or rtfStatus.Text.Contains("_new_ Password")) And chkTSBConfigurePassword.Checked Then
                procTSB.StandardInput.Write(txtTSBPassword.Text & vbCrLf)

                If rtfStatus.Text.Contains("new Password") Then ' old style where we double confirm, so we need to send a "Y enter"
                    procTSB.StandardInput.Write("y" & vbCrLf)
                End If
                new_pwd_sent = True
            End If

            If (Not timeout_sent) And rtfStatus.Text.Contains("new Timeout") And chkTSBConfigureTimeout.Checked Then
                procTSB.StandardInput.Write(txtTSBTimeout.Text & vbCrLf)
                timeout_sent = True
            End If

            If (Not mbyte1_sent) And rtfStatus.Text.Contains("First Magic Byte") And chkConfigureMagicBytes.Checked Then
                procTSB.StandardInput.Write(Conversion.Hex(GetMagicByteValue(cboMgByte1)) & vbCrLf)
                mbyte1_sent = True
            End If

            If (Not mbyte2_sent) And rtfStatus.Text.Contains("Second Magic Byte") And chkConfigureMagicBytes.Checked Then
                procTSB.StandardInput.Write(Conversion.Hex(GetMagicByteValue(cboMgByte2)) & vbCrLf)
                mbyte2_sent = True
            End If

            ' the question for entering the password changed in different Command line versions
            ' we check both for compatibility with the older and newer versions
            If (Not current_pwd_sent) And (rtfStatus.Text.Contains("enter the bootloader password") Or
                rtfStatus.Text.Contains("Would you like to enter a bootloader password?") Or
                rtfStatus.Text.Contains("_current_ bootloader password")) Then
                Dim sBootldrPwd = InputBox("The bootloader on the device appears to have a password." & vbCrLf & "Please enter the Bootloader password: ")
                procTSB.StandardInput.Write(sBootldrPwd & vbCrLf)
                current_pwd_sent = True
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

        ' verify magic byte matching
        cboMgByte_TextChanged(cboMgByte1, Nothing)
    End Sub

    Private Sub cmdBrowseFlashFile_Click(sender As Object, e As EventArgs) Handles cmdBrowseFlashFile.Click
        Dim s As String = GetFileNameToOpen("Select Flash File", "HEX Files (*.hex)|*.hex|Binary Files (*.bin)|*.bin|All files|*.*", txtFlashFile.Text)

        If Not s Is Nothing Then txtFlashFile.Text = s
    End Sub


    Private Function GetFileNameToOpen(sDialogTitle As String, sExtensionsFilter As String, sCurrentFileName As String) As String

        With dlgFileOpen
            .Title = sDialogTitle
            .Filter = sExtensionsFilter

            Dim sStartPath As String
            If sCurrentFileName Is Nothing Then
                sStartPath = ""
            ElseIf sCurrentFileName.Trim = "" Then
                sStartPath = ""
            Else
                sStartPath = System.IO.Path.GetDirectoryName(sCurrentFileName)
            End If

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

    Private Function GetMagicByteValue(MgByteCombo As ComboBox) As Integer
        Dim value As Integer

        Try
            value = MgByteCombo.SelectedItem.Value
        Catch ex As Exception
            ' value is likely nothing that has been selected
            ' see if we have text
            If MgByteCombo.Text <> "" Then
                If Int16.TryParse(MgByteCombo.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, value) = False Then
                    ' also not a hex string
                    Return -1

                    'else
                    ' fallthrough; in TryParse we have filled the contents of variable VALUE
                End If

            Else
                Return -1
            End If
        End Try

        Return IIf(value >= 0 And value <= 255, value, -1)
    End Function

    Private Sub chkConfigureMagicBytes_CheckedChanged(sender As Object, e As EventArgs) Handles chkConfigureMagicBytes.CheckedChanged
        cboMgByte1.Enabled = chkConfigureMagicBytes.Checked
        cboMgByte2.Enabled = chkConfigureMagicBytes.Checked
        chkTSBMatchByteToServoID.Enabled = chkConfigureMagicBytes.Checked

        ' force checking the values and setting colours to flag any errors
        cboMgByte_TextChanged(cboMgByte1, Nothing)
        cboMgByte_TextChanged(cboMgByte2, Nothing)
        chkTSBMatchByteToServoID_CheckedChanged(Nothing, Nothing)

    End Sub

    Private Sub cboMgByte_TextChanged(sender As Object, e As EventArgs) Handles cboMgByte1.TextChanged, cboMgByte2.TextChanged

        If chkConfigureMagicBytes.Checked = False Then
            lblMgByte1.ForeColor = SystemColors.ControlText
            lblMgByte2.ForeColor = SystemColors.ControlText
            Return
        End If

        Dim iValue As Object = GetMagicByteValue(CType(sender, ComboBox))

        If GetMagicByteValue(CType(sender, ComboBox)) < 0 Then
            CType(sender, ComboBox).ForeColor = Color.Red

        Else
            CType(sender, ComboBox).ForeColor = SystemColors.ControlText
        End If

        ' in the case of Magic Byte 1 check to see if there are any clues about the servo model and the
        ' name of the flash file to crrelate them and check for errors
        If sender Is cboMgByte1 Then

            If cboMgByte1.Text.Contains("DES") Then
                ' determine model
                Dim i = cboMgByte1.Text.IndexOf("DES") + 3 ' +3 so that we get to the letter right after the S

                Dim model As String = ""
                Dim c As Char
                While i < cboMgByte1.Text.Length
                    c = cboMgByte1.Text.ElementAt(i)

                    If c = " " Then
                        i = i + 1
                    ElseIf IsNumeric(c) Then
                        model = model & c
                        i = i + 1
                    Else
                        Exit While
                    End If
                End While

                If model.Length = 0 Then Return

                If txtFlashFile.Text.Contains(model) Then
                    ' we're ok; do nothing
                    lblMgByte1.ForeColor = SystemColors.ControlText
                    Return

                Else
                    If model.Length = 3 Then ' check using the last 2 chars
                        If txtFlashFile.Text.Contains(model.Substring(0, 2)) Then
                            'we're ok.
                            lblMgByte1.ForeColor = SystemColors.ControlText
                            Return
                        End If
                    End If
                End If

                ' fall through
                lblMgByte1.ForeColor = Color.Red
            End If
        End If

        If sender Is cboMgByte2 And Not cboMgByte2.SelectedItem Is Nothing Then
            Dim servoID As Integer = CInt(Me.txtServoID.Text) Mod 10
            If cboMgByte2.SelectedItem.Value = servoID Then
                lblMgByte2.ForeColor = SystemColors.ControlText
                Return
            End If

            ' fall through; not found
            lblMgByte2.ForeColor = Color.Red
        End If
    End Sub

    Private Sub chkTSBMatchByteToServoID_CheckedChanged(sender As Object, e As EventArgs) Handles chkTSBMatchByteToServoID.CheckedChanged

        If chkTSBMatchByteToServoID.Checked Then
            cboMgByte2.Enabled = False

            Dim servoID As Integer
            If Me.txtServoID.Text.Trim = "" Then
                servoID = 0
            Else
                servoID = CInt(Me.txtServoID.Text) Mod 10
            End If

            For i As Integer = 0 To cboMgByte2.Items.Count - 1
                If cboMgByte2.Items(i).Value = servoID Then
                    cboMgByte2.SelectedIndex = i
                    lblMgByte2.ForeColor = SystemColors.ControlText
                    Return
                End If
            Next

            ' fall through; not found
            lblMgByte2.ForeColor = Color.Red

        Else
            cboMgByte2.Enabled = chkConfigureMagicBytes.Checked
        End If
    End Sub
End Class


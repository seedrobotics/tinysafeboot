<Serializable()>
Public Class clsAppSettings
    Public Property bFlashChecked As Boolean
    Public Property sFlashFilePath As String

    Public Property bEEPROMChecked As Boolean
    Public Property sEEPROMFilePath As String

    Public Property bManipulateEEPROMData As Boolean
    Public Property sEEPROMServoID As String
    Public Property sEEPROMHexAddress As String

    Public Property bTSBTimeoutChecked As Boolean
    Public Property sTSBTimeout As String

    Public Property bTSBConfigurePasswordChecked As Boolean
    Public Property sTSBPassword As String
    Public Property bTSBPasswordMatchToServoID As Boolean

    Public Property sTSBAdditionalParams As String

    Public Property sTSBLoaderEXELocation As String

    Public Property sCOMMPort As String
    Public Property bFTDIHasAutoReset As Boolean

    Public Property bConfigureMagicBytesChecked As Boolean
    Public Property sMagicByte1 As String
    Public Property sMagicByte2 As String

    Public Sub CollectSettingsFromForm(f As frmGUIFlasher)
        bFlashChecked = f.chkFlashFile.Checked
        sFlashFilePath = f.txtFlashFile.Text

        bEEPROMChecked = f.chkEEPROMFile.Checked
        sEEPROMFilePath = f.txtEEPROMFile.Text

        bManipulateEEPROMData = f.chkManipulateEEPROM.Checked
        sEEPROMServoID = f.txtServoID.Text
        sEEPROMHexAddress = f.txtHexAddress.Text

        bTSBTimeoutChecked = f.chkTSBConfigureTimeout.Checked
        sTSBTimeout = f.txtTSBTimeout.Text

        bTSBConfigurePasswordChecked = f.chkTSBConfigurePassword.Checked
        sTSBPassword = f.txtTSBPassword.Text
        bTSBPasswordMatchToServoID = f.chkTSBMatchPasswordToServoID.Checked

        sTSBLoaderEXELocation = f.TSBLoaderExeLocation

        sTSBAdditionalParams = f.txtTSBAdditionalParams.Text

        sCOMMPort = f.cboCOMMPort.Text
        bFTDIHasAutoReset = f.chkFTDIHasAutoReset.Checked

        bConfigureMagicBytesChecked = f.chkConfigureMagicBytes.Checked
        sMagicByte1 = f.cboMgByte1.Text
        sMagicByte2 = f.cboMgByte2.Text

    End Sub

    Public Sub SetSettingsInForm(f As frmGUIFlasher)
        f.chkFlashFile.Checked = bFlashChecked
        f.txtFlashFile.Text = sFlashFilePath

        f.chkManipulateEEPROM.Checked = bManipulateEEPROMData
        f.txtServoID.Text = sEEPROMServoID
        f.txtHexAddress.Text = sEEPROMHexAddress

        f.chkEEPROMFile.Checked = bEEPROMChecked
        f.txtEEPROMFile.Text = sEEPROMFilePath

        f.chkTSBConfigureTimeout.Checked = bTSBTimeoutChecked
        f.txtTSBTimeout.Text = sTSBTimeout

        f.chkTSBConfigurePassword.Checked = bTSBConfigurePasswordChecked
        f.txtTSBPassword.Text = sTSBPassword
        f.chkTSBMatchPasswordToServoID.Checked = bTSBPasswordMatchToServoID

        f.TSBLoaderExeLocation = sTSBLoaderEXELocation

        f.txtTSBAdditionalParams.Text = sTSBAdditionalParams

        If Not String.IsNullOrEmpty(sCOMMPort) Then
            If (f.cboCOMMPort.Items.Contains(sCOMMPort)) Then f.cboCOMMPort.Text = sCOMMPort
        End If
        f.chkFTDIHasAutoReset.Checked = bFTDIHasAutoReset

        f.chkConfigureMagicBytes.Checked = bConfigureMagicBytesChecked
        f.cboMgByte1.Text = sMagicByte1
        f.cboMgByte2.Text = sMagicByte2
    End Sub

End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmGUIFlasher
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Friend WithEvents chkFTDIHasAutoReset As CheckBox
    Friend WithEvents cboCOMMPort As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents btnTSBLoaderLocation As Button
    Friend WithEvents btnRunTSBLoader As Button

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmGUIFlasher))
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.cmdBrowseEEPROMFile = New System.Windows.Forms.Button()
        Me.cmdBrowseFlashFile = New System.Windows.Forms.Button()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.txtEEPROMFile = New System.Windows.Forms.TextBox()
        Me.txtFlashFile = New System.Windows.Forms.TextBox()
        Me.txtHexAddress = New System.Windows.Forms.TextBox()
        Me.txtServoID = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.chkManipulateEEPROM = New System.Windows.Forms.CheckBox()
        Me.chkEEPROMFile = New System.Windows.Forms.CheckBox()
        Me.chkFlashFile = New System.Windows.Forms.CheckBox()
        Me.Panel3 = New System.Windows.Forms.Panel()
        Me.chkTSBMatchByteToServoID = New System.Windows.Forms.CheckBox()
        Me.cboMgByte2 = New System.Windows.Forms.ComboBox()
        Me.cboMgByte1 = New System.Windows.Forms.ComboBox()
        Me.lblMgByte2 = New System.Windows.Forms.Label()
        Me.lblMgByte1 = New System.Windows.Forms.Label()
        Me.chkConfigureMagicBytes = New System.Windows.Forms.CheckBox()
        Me.txtTSBAdditionalParams = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.txtTSBTimeout = New System.Windows.Forms.TextBox()
        Me.chkTSBMatchPasswordToServoID = New System.Windows.Forms.CheckBox()
        Me.txtTSBPassword = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.chkTSBConfigurePassword = New System.Windows.Forms.CheckBox()
        Me.chkTSBConfigureTimeout = New System.Windows.Forms.CheckBox()
        Me.chkFTDIHasAutoReset = New System.Windows.Forms.CheckBox()
        Me.cboCOMMPort = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnTSBLoaderLocation = New System.Windows.Forms.Button()
        Me.btnRunTSBLoader = New System.Windows.Forms.Button()
        Me.btnRefreshCOMMPorts = New System.Windows.Forms.Button()
        Me.cmdKillTSB = New System.Windows.Forms.Button()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.btnSeedErosEnable = New System.Windows.Forms.Button()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.btnSeedErosDisable = New System.Windows.Forms.Button()
        Me.dlgFileOpen = New System.Windows.Forms.OpenFileDialog()
        Me.rtfStatus = New System.Windows.Forms.RichTextBox()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel2.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.SuspendLayout()
        '
        'PictureBox1
        '
        Me.PictureBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), System.Drawing.Image)
        Me.PictureBox1.Location = New System.Drawing.Point(866, 8)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(141, 43)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PictureBox1.TabIndex = 1
        Me.PictureBox1.TabStop = False
        '
        'Panel2
        '
        Me.Panel2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel2.BackColor = System.Drawing.Color.White
        Me.Panel2.Controls.Add(Me.cmdBrowseEEPROMFile)
        Me.Panel2.Controls.Add(Me.cmdBrowseFlashFile)
        Me.Panel2.Controls.Add(Me.Label6)
        Me.Panel2.Controls.Add(Me.txtEEPROMFile)
        Me.Panel2.Controls.Add(Me.txtFlashFile)
        Me.Panel2.Controls.Add(Me.txtHexAddress)
        Me.Panel2.Controls.Add(Me.txtServoID)
        Me.Panel2.Controls.Add(Me.Label3)
        Me.Panel2.Controls.Add(Me.Label2)
        Me.Panel2.Controls.Add(Me.chkManipulateEEPROM)
        Me.Panel2.Controls.Add(Me.chkEEPROMFile)
        Me.Panel2.Controls.Add(Me.chkFlashFile)
        Me.Panel2.Location = New System.Drawing.Point(12, 57)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(1005, 146)
        Me.Panel2.TabIndex = 3
        '
        'cmdBrowseEEPROMFile
        '
        Me.cmdBrowseEEPROMFile.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdBrowseEEPROMFile.BackColor = System.Drawing.Color.MediumAquamarine
        Me.cmdBrowseEEPROMFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdBrowseEEPROMFile.Location = New System.Drawing.Point(948, 50)
        Me.cmdBrowseEEPROMFile.Name = "cmdBrowseEEPROMFile"
        Me.cmdBrowseEEPROMFile.Size = New System.Drawing.Size(37, 28)
        Me.cmdBrowseEEPROMFile.TabIndex = 23
        Me.cmdBrowseEEPROMFile.Text = "..."
        Me.cmdBrowseEEPROMFile.UseVisualStyleBackColor = False
        '
        'cmdBrowseFlashFile
        '
        Me.cmdBrowseFlashFile.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdBrowseFlashFile.BackColor = System.Drawing.Color.MediumAquamarine
        Me.cmdBrowseFlashFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdBrowseFlashFile.Location = New System.Drawing.Point(948, 11)
        Me.cmdBrowseFlashFile.Name = "cmdBrowseFlashFile"
        Me.cmdBrowseFlashFile.Size = New System.Drawing.Size(37, 28)
        Me.cmdBrowseFlashFile.TabIndex = 22
        Me.cmdBrowseFlashFile.Text = "..."
        Me.cmdBrowseFlashFile.UseVisualStyleBackColor = False
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(452, 109)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(168, 17)
        Me.Label6.TabIndex = 21
        Me.Label6.Text = "(HINT: Servo ID is at 0x7)"
        '
        'txtEEPROMFile
        '
        Me.txtEEPROMFile.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtEEPROMFile.Location = New System.Drawing.Point(139, 53)
        Me.txtEEPROMFile.Name = "txtEEPROMFile"
        Me.txtEEPROMFile.Size = New System.Drawing.Size(803, 22)
        Me.txtEEPROMFile.TabIndex = 20
        '
        'txtFlashFile
        '
        Me.txtFlashFile.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtFlashFile.Location = New System.Drawing.Point(139, 14)
        Me.txtFlashFile.Name = "txtFlashFile"
        Me.txtFlashFile.Size = New System.Drawing.Size(803, 22)
        Me.txtFlashFile.TabIndex = 19
        '
        'txtHexAddress
        '
        Me.txtHexAddress.Enabled = False
        Me.txtHexAddress.Location = New System.Drawing.Point(272, 106)
        Me.txtHexAddress.Name = "txtHexAddress"
        Me.txtHexAddress.Size = New System.Drawing.Size(43, 22)
        Me.txtHexAddress.TabIndex = 18
        '
        'txtServoID
        '
        Me.txtServoID.Enabled = False
        Me.txtServoID.Location = New System.Drawing.Point(403, 106)
        Me.txtServoID.Name = "txtServoID"
        Me.txtServoID.Size = New System.Drawing.Size(43, 22)
        Me.txtServoID.TabIndex = 17
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(136, 109)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(137, 17)
        Me.Label3.TabIndex = 16
        Me.Label3.Text = "At binary address 0x"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(321, 109)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(85, 17)
        Me.Label2.TabIndex = 15
        Me.Label2.Text = "set value to "
        '
        'chkManipulateEEPROM
        '
        Me.chkManipulateEEPROM.AutoSize = True
        Me.chkManipulateEEPROM.Enabled = False
        Me.chkManipulateEEPROM.Location = New System.Drawing.Point(62, 82)
        Me.chkManipulateEEPROM.Name = "chkManipulateEEPROM"
        Me.chkManipulateEEPROM.Size = New System.Drawing.Size(194, 21)
        Me.chkManipulateEEPROM.TabIndex = 14
        Me.chkManipulateEEPROM.Text = "Manipulate EEPROM data"
        Me.chkManipulateEEPROM.UseVisualStyleBackColor = True
        '
        'chkEEPROMFile
        '
        Me.chkEEPROMFile.AutoSize = True
        Me.chkEEPROMFile.Location = New System.Drawing.Point(15, 55)
        Me.chkEEPROMFile.Name = "chkEEPROMFile"
        Me.chkEEPROMFile.Size = New System.Drawing.Size(115, 21)
        Me.chkEEPROMFile.TabIndex = 13
        Me.chkEEPROMFile.Text = "EEPROM File"
        Me.chkEEPROMFile.UseVisualStyleBackColor = True
        '
        'chkFlashFile
        '
        Me.chkFlashFile.AutoSize = True
        Me.chkFlashFile.Location = New System.Drawing.Point(15, 14)
        Me.chkFlashFile.Name = "chkFlashFile"
        Me.chkFlashFile.Size = New System.Drawing.Size(90, 21)
        Me.chkFlashFile.TabIndex = 12
        Me.chkFlashFile.Text = "Flash File"
        Me.chkFlashFile.UseVisualStyleBackColor = True
        '
        'Panel3
        '
        Me.Panel3.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel3.BackColor = System.Drawing.Color.White
        Me.Panel3.Controls.Add(Me.chkTSBMatchByteToServoID)
        Me.Panel3.Controls.Add(Me.cboMgByte2)
        Me.Panel3.Controls.Add(Me.cboMgByte1)
        Me.Panel3.Controls.Add(Me.lblMgByte2)
        Me.Panel3.Controls.Add(Me.lblMgByte1)
        Me.Panel3.Controls.Add(Me.chkConfigureMagicBytes)
        Me.Panel3.Controls.Add(Me.txtTSBAdditionalParams)
        Me.Panel3.Controls.Add(Me.Label4)
        Me.Panel3.Controls.Add(Me.txtTSBTimeout)
        Me.Panel3.Controls.Add(Me.chkTSBMatchPasswordToServoID)
        Me.Panel3.Controls.Add(Me.txtTSBPassword)
        Me.Panel3.Controls.Add(Me.Label5)
        Me.Panel3.Controls.Add(Me.chkTSBConfigurePassword)
        Me.Panel3.Controls.Add(Me.chkTSBConfigureTimeout)
        Me.Panel3.Location = New System.Drawing.Point(12, 218)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(1005, 220)
        Me.Panel3.TabIndex = 4
        '
        'chkTSBMatchByteToServoID
        '
        Me.chkTSBMatchByteToServoID.AutoSize = True
        Me.chkTSBMatchByteToServoID.Enabled = False
        Me.chkTSBMatchByteToServoID.Location = New System.Drawing.Point(500, 128)
        Me.chkTSBMatchByteToServoID.Name = "chkTSBMatchByteToServoID"
        Me.chkTSBMatchByteToServoID.Size = New System.Drawing.Size(186, 21)
        Me.chkTSBMatchByteToServoID.TabIndex = 31
        Me.chkTSBMatchByteToServoID.Text = "Match Byte 2 to Servo ID"
        Me.chkTSBMatchByteToServoID.UseVisualStyleBackColor = True
        '
        'cboMgByte2
        '
        Me.cboMgByte2.Enabled = False
        Me.cboMgByte2.FormattingEnabled = True
        Me.cboMgByte2.Location = New System.Drawing.Point(345, 125)
        Me.cboMgByte2.Name = "cboMgByte2"
        Me.cboMgByte2.Size = New System.Drawing.Size(142, 24)
        Me.cboMgByte2.TabIndex = 30
        '
        'cboMgByte1
        '
        Me.cboMgByte1.Enabled = False
        Me.cboMgByte1.FormattingEnabled = True
        Me.cboMgByte1.Location = New System.Drawing.Point(126, 124)
        Me.cboMgByte1.Name = "cboMgByte1"
        Me.cboMgByte1.Size = New System.Drawing.Size(142, 24)
        Me.cboMgByte1.TabIndex = 29
        '
        'lblMgByte2
        '
        Me.lblMgByte2.AutoSize = True
        Me.lblMgByte2.Location = New System.Drawing.Point(274, 128)
        Me.lblMgByte2.Name = "lblMgByte2"
        Me.lblMgByte2.Size = New System.Drawing.Size(70, 17)
        Me.lblMgByte2.TabIndex = 28
        Me.lblMgByte2.Text = "Byte 2: 0x"
        '
        'lblMgByte1
        '
        Me.lblMgByte1.AutoSize = True
        Me.lblMgByte1.Location = New System.Drawing.Point(56, 128)
        Me.lblMgByte1.Name = "lblMgByte1"
        Me.lblMgByte1.Size = New System.Drawing.Size(70, 17)
        Me.lblMgByte1.TabIndex = 27
        Me.lblMgByte1.Text = "Byte 1: 0x"
        '
        'chkConfigureMagicBytes
        '
        Me.chkConfigureMagicBytes.AutoSize = True
        Me.chkConfigureMagicBytes.Location = New System.Drawing.Point(15, 100)
        Me.chkConfigureMagicBytes.Name = "chkConfigureMagicBytes"
        Me.chkConfigureMagicBytes.Size = New System.Drawing.Size(244, 21)
        Me.chkConfigureMagicBytes.TabIndex = 25
        Me.chkConfigureMagicBytes.Text = "Configure Bootloader Magic Bytes"
        Me.chkConfigureMagicBytes.UseVisualStyleBackColor = True
        '
        'txtTSBAdditionalParams
        '
        Me.txtTSBAdditionalParams.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtTSBAdditionalParams.Location = New System.Drawing.Point(198, 174)
        Me.txtTSBAdditionalParams.Name = "txtTSBAdditionalParams"
        Me.txtTSBAdditionalParams.Size = New System.Drawing.Size(744, 22)
        Me.txtTSBAdditionalParams.TabIndex = 24
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(12, 177)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(178, 17)
        Me.Label4.TabIndex = 23
        Me.Label4.Text = "Additional TSB Parameters"
        '
        'txtTSBTimeout
        '
        Me.txtTSBTimeout.Location = New System.Drawing.Point(225, 15)
        Me.txtTSBTimeout.Name = "txtTSBTimeout"
        Me.txtTSBTimeout.Size = New System.Drawing.Size(43, 22)
        Me.txtTSBTimeout.TabIndex = 22
        '
        'chkTSBMatchPasswordToServoID
        '
        Me.chkTSBMatchPasswordToServoID.AutoSize = True
        Me.chkTSBMatchPasswordToServoID.Enabled = False
        Me.chkTSBMatchPasswordToServoID.Location = New System.Drawing.Point(223, 72)
        Me.chkTSBMatchPasswordToServoID.Name = "chkTSBMatchPasswordToServoID"
        Me.chkTSBMatchPasswordToServoID.Size = New System.Drawing.Size(204, 21)
        Me.chkTSBMatchPasswordToServoID.TabIndex = 21
        Me.chkTSBMatchPasswordToServoID.Text = "Match password to servo ID"
        Me.chkTSBMatchPasswordToServoID.UseVisualStyleBackColor = True
        '
        'txtTSBPassword
        '
        Me.txtTSBPassword.Enabled = False
        Me.txtTSBPassword.Location = New System.Drawing.Point(161, 70)
        Me.txtTSBPassword.Name = "txtTSBPassword"
        Me.txtTSBPassword.Size = New System.Drawing.Size(58, 22)
        Me.txtTSBPassword.TabIndex = 20
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(93, 72)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(69, 17)
        Me.Label5.TabIndex = 19
        Me.Label5.Text = "Password"
        '
        'chkTSBConfigurePassword
        '
        Me.chkTSBConfigurePassword.AutoSize = True
        Me.chkTSBConfigurePassword.Location = New System.Drawing.Point(13, 48)
        Me.chkTSBConfigurePassword.Name = "chkTSBConfigurePassword"
        Me.chkTSBConfigurePassword.Size = New System.Drawing.Size(216, 21)
        Me.chkTSBConfigurePassword.TabIndex = 18
        Me.chkTSBConfigurePassword.Text = "Configure new TSB Password"
        Me.chkTSBConfigurePassword.UseVisualStyleBackColor = True
        '
        'chkTSBConfigureTimeout
        '
        Me.chkTSBConfigureTimeout.AutoSize = True
        Me.chkTSBConfigureTimeout.Location = New System.Drawing.Point(15, 15)
        Me.chkTSBConfigureTimeout.Name = "chkTSBConfigureTimeout"
        Me.chkTSBConfigureTimeout.Size = New System.Drawing.Size(206, 21)
        Me.chkTSBConfigureTimeout.TabIndex = 17
        Me.chkTSBConfigureTimeout.Text = "Configure new TSB Timeout"
        Me.chkTSBConfigureTimeout.UseVisualStyleBackColor = True
        '
        'chkFTDIHasAutoReset
        '
        Me.chkFTDIHasAutoReset.AutoSize = True
        Me.chkFTDIHasAutoReset.ForeColor = System.Drawing.Color.White
        Me.chkFTDIHasAutoReset.Location = New System.Drawing.Point(346, 22)
        Me.chkFTDIHasAutoReset.Name = "chkFTDIHasAutoReset"
        Me.chkFTDIHasAutoReset.Size = New System.Drawing.Size(224, 21)
        Me.chkFTDIHasAutoReset.TabIndex = 8
        Me.chkFTDIHasAutoReset.Text = "FTDI has Auto Reset capability"
        Me.chkFTDIHasAutoReset.UseVisualStyleBackColor = True
        '
        'cboCOMMPort
        '
        Me.cboCOMMPort.FormattingEnabled = True
        Me.cboCOMMPort.Location = New System.Drawing.Point(110, 21)
        Me.cboCOMMPort.Name = "cboCOMMPort"
        Me.cboCOMMPort.Size = New System.Drawing.Size(131, 24)
        Me.cboCOMMPort.TabIndex = 7
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.ForeColor = System.Drawing.Color.White
        Me.Label1.Location = New System.Drawing.Point(24, 24)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(80, 17)
        Me.Label1.TabIndex = 6
        Me.Label1.Text = "COMM Port"
        '
        'btnTSBLoaderLocation
        '
        Me.btnTSBLoaderLocation.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnTSBLoaderLocation.BackColor = System.Drawing.Color.MediumAquamarine
        Me.btnTSBLoaderLocation.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnTSBLoaderLocation.Location = New System.Drawing.Point(19, 653)
        Me.btnTSBLoaderLocation.Name = "btnTSBLoaderLocation"
        Me.btnTSBLoaderLocation.Size = New System.Drawing.Size(215, 38)
        Me.btnTSBLoaderLocation.TabIndex = 9
        Me.btnTSBLoaderLocation.Text = "TSB Loader Exe Location"
        Me.btnTSBLoaderLocation.UseVisualStyleBackColor = False
        '
        'btnRunTSBLoader
        '
        Me.btnRunTSBLoader.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRunTSBLoader.BackColor = System.Drawing.Color.MediumAquamarine
        Me.btnRunTSBLoader.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnRunTSBLoader.Location = New System.Drawing.Point(869, 653)
        Me.btnRunTSBLoader.Name = "btnRunTSBLoader"
        Me.btnRunTSBLoader.Size = New System.Drawing.Size(141, 38)
        Me.btnRunTSBLoader.TabIndex = 10
        Me.btnRunTSBLoader.Text = "Run TSB Loader"
        Me.btnRunTSBLoader.UseVisualStyleBackColor = False
        '
        'btnRefreshCOMMPorts
        '
        Me.btnRefreshCOMMPorts.BackColor = System.Drawing.Color.MediumAquamarine
        Me.btnRefreshCOMMPorts.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnRefreshCOMMPorts.Location = New System.Drawing.Point(247, 18)
        Me.btnRefreshCOMMPorts.Name = "btnRefreshCOMMPorts"
        Me.btnRefreshCOMMPorts.Size = New System.Drawing.Size(93, 28)
        Me.btnRefreshCOMMPorts.TabIndex = 11
        Me.btnRefreshCOMMPorts.Text = "Refresh"
        Me.btnRefreshCOMMPorts.UseVisualStyleBackColor = False
        '
        'cmdKillTSB
        '
        Me.cmdKillTSB.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdKillTSB.BackColor = System.Drawing.Color.MediumAquamarine
        Me.cmdKillTSB.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdKillTSB.Location = New System.Drawing.Point(804, 653)
        Me.cmdKillTSB.Name = "cmdKillTSB"
        Me.cmdKillTSB.Size = New System.Drawing.Size(139, 38)
        Me.cmdKillTSB.TabIndex = 12
        Me.cmdKillTSB.Text = "Kill TSB Loader"
        Me.cmdKillTSB.UseVisualStyleBackColor = False
        Me.cmdKillTSB.Visible = False
        '
        'lblStatus
        '
        Me.lblStatus.BackColor = System.Drawing.Color.DimGray
        Me.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblStatus.Location = New System.Drawing.Point(0, 716)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(1041, 21)
        Me.lblStatus.TabIndex = 13
        Me.lblStatus.Text = "Label7"
        '
        'btnSeedErosEnable
        '
        Me.btnSeedErosEnable.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSeedErosEnable.BackColor = System.Drawing.Color.MediumAquamarine
        Me.btnSeedErosEnable.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSeedErosEnable.Location = New System.Drawing.Point(695, 653)
        Me.btnSeedErosEnable.Name = "btnSeedErosEnable"
        Me.btnSeedErosEnable.Size = New System.Drawing.Size(93, 28)
        Me.btnSeedErosEnable.TabIndex = 14
        Me.btnSeedErosEnable.Text = "Enable"
        Me.btnSeedErosEnable.UseVisualStyleBackColor = False
        '
        'Label7
        '
        Me.Label7.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label7.AutoSize = True
        Me.Label7.ForeColor = System.Drawing.Color.White
        Me.Label7.Location = New System.Drawing.Point(565, 658)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(124, 17)
        Me.Label7.TabIndex = 16
        Me.Label7.Text = "Eros Board Bridge"
        '
        'btnSeedErosDisable
        '
        Me.btnSeedErosDisable.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSeedErosDisable.BackColor = System.Drawing.Color.MediumAquamarine
        Me.btnSeedErosDisable.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSeedErosDisable.Location = New System.Drawing.Point(695, 680)
        Me.btnSeedErosDisable.Name = "btnSeedErosDisable"
        Me.btnSeedErosDisable.Size = New System.Drawing.Size(93, 28)
        Me.btnSeedErosDisable.TabIndex = 17
        Me.btnSeedErosDisable.Text = "Disable"
        Me.btnSeedErosDisable.UseVisualStyleBackColor = False
        '
        'rtfStatus
        '
        Me.rtfStatus.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.rtfStatus.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.rtfStatus.DetectUrls = False
        Me.rtfStatus.Font = New System.Drawing.Font("Consolas", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.rtfStatus.Location = New System.Drawing.Point(12, 458)
        Me.rtfStatus.Name = "rtfStatus"
        Me.rtfStatus.ReadOnly = True
        Me.rtfStatus.Size = New System.Drawing.Size(1005, 178)
        Me.rtfStatus.TabIndex = 18
        Me.rtfStatus.Text = ""
        '
        'frmGUIFlasher
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(88, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(91, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1041, 737)
        Me.Controls.Add(Me.rtfStatus)
        Me.Controls.Add(Me.btnSeedErosDisable)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.btnSeedErosEnable)
        Me.Controls.Add(Me.lblStatus)
        Me.Controls.Add(Me.cmdKillTSB)
        Me.Controls.Add(Me.btnRefreshCOMMPorts)
        Me.Controls.Add(Me.btnRunTSBLoader)
        Me.Controls.Add(Me.btnTSBLoaderLocation)
        Me.Controls.Add(Me.chkFTDIHasAutoReset)
        Me.Controls.Add(Me.cboCOMMPort)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Panel3)
        Me.Controls.Add(Me.Panel2)
        Me.Controls.Add(Me.PictureBox1)
        Me.Name = "frmGUIFlasher"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Actuator Flasher for Tinysafeboot Bootloader"
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel3.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Panel2 As Panel
    Friend WithEvents txtEEPROMFile As TextBox
    Friend WithEvents txtFlashFile As TextBox
    Friend WithEvents txtHexAddress As TextBox
    Friend WithEvents txtServoID As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents chkManipulateEEPROM As CheckBox
    Friend WithEvents chkEEPROMFile As CheckBox
    Friend WithEvents chkFlashFile As CheckBox
    Friend WithEvents Panel3 As Panel
    Friend WithEvents txtTSBTimeout As TextBox
    Friend WithEvents chkTSBMatchPasswordToServoID As CheckBox
    Friend WithEvents txtTSBPassword As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents chkTSBConfigurePassword As CheckBox
    Friend WithEvents chkTSBConfigureTimeout As CheckBox
    Friend WithEvents btnRefreshCOMMPorts As Button
    Friend WithEvents txtTSBAdditionalParams As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents cmdKillTSB As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents lblStatus As Label
    Friend WithEvents btnSeedErosEnable As Button
    Friend WithEvents Label7 As Label
    Friend WithEvents btnSeedErosDisable As Button
    Friend WithEvents cmdBrowseEEPROMFile As Button
    Friend WithEvents cmdBrowseFlashFile As Button
    Friend WithEvents dlgFileOpen As OpenFileDialog
    Friend WithEvents rtfStatus As RichTextBox
    Friend WithEvents cboMgByte2 As ComboBox
    Friend WithEvents cboMgByte1 As ComboBox
    Friend WithEvents lblMgByte2 As Label
    Friend WithEvents lblMgByte1 As Label
    Friend WithEvents chkConfigureMagicBytes As CheckBox
    Friend WithEvents chkTSBMatchByteToServoID As CheckBox
End Class

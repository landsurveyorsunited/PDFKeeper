﻿'******************************************************************************
'* PDFKeeper -- Open Source PDF Document Storage Solution
'* Copyright (C) 2009-2018 Robert F. Frasca
'*
'* This file is part of PDFKeeper.
'*
'* PDFKeeper is free software: you can redistribute it and/or modify
'* it under the terms of the GNU General Public License as published by
'* the Free Software Foundation, either version 3 of the License, or
'* (at your option) any later version.
'*
'* PDFKeeper is distributed in the hope that it will be useful,
'* but WITHOUT ANY WARRANTY; without even the implied warranty of
'* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'* GNU General Public License for more details.
'*
'* You should have received a copy of the GNU General Public License
'* along with PDFKeeper.  If not, see <http://www.gnu.org/licenses/>.
'******************************************************************************
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
<System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", _
    "CA1726:UsePreferredTerms", MessageId:="Login")> _
Partial Class LoginForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub
    Friend WithEvents LogoPictureBox As System.Windows.Forms.PictureBox
    Friend WithEvents UsernameLabel As System.Windows.Forms.Label
    Friend WithEvents PasswordLabel As System.Windows.Forms.Label
    Friend WithEvents UsernameTextBox As System.Windows.Forms.TextBox
    Friend WithEvents DatasourceTextBox As System.Windows.Forms.TextBox
    Friend WithEvents OK As System.Windows.Forms.Button
    Friend WithEvents Cancel As System.Windows.Forms.Button

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(LoginForm))
        Me.LogoPictureBox = New System.Windows.Forms.PictureBox()
        Me.UsernameLabel = New System.Windows.Forms.Label()
        Me.PasswordLabel = New System.Windows.Forms.Label()
        Me.UsernameTextBox = New System.Windows.Forms.TextBox()
        Me.DatasourceTextBox = New System.Windows.Forms.TextBox()
        Me.OK = New System.Windows.Forms.Button()
        Me.Cancel = New System.Windows.Forms.Button()
        Me.DatasourceLabel = New System.Windows.Forms.Label()
        Me.HelpProvider = New System.Windows.Forms.HelpProvider()
        Me.PasswordSecureTextBox = New PDFKeeper.WindowsApplication.SecureTextBox()
        CType(Me.LogoPictureBox, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LogoPictureBox
        '
        Me.LogoPictureBox.BackColor = System.Drawing.SystemColors.ActiveBorder
        resources.ApplyResources(Me.LogoPictureBox, "LogoPictureBox")
        Me.LogoPictureBox.Name = "LogoPictureBox"
        Me.HelpProvider.SetShowHelp(Me.LogoPictureBox, CType(resources.GetObject("LogoPictureBox.ShowHelp"), Boolean))
        Me.LogoPictureBox.TabStop = False
        '
        'UsernameLabel
        '
        resources.ApplyResources(Me.UsernameLabel, "UsernameLabel")
        Me.UsernameLabel.Name = "UsernameLabel"
        Me.HelpProvider.SetShowHelp(Me.UsernameLabel, CType(resources.GetObject("UsernameLabel.ShowHelp"), Boolean))
        '
        'PasswordLabel
        '
        resources.ApplyResources(Me.PasswordLabel, "PasswordLabel")
        Me.PasswordLabel.Name = "PasswordLabel"
        Me.HelpProvider.SetShowHelp(Me.PasswordLabel, CType(resources.GetObject("PasswordLabel.ShowHelp"), Boolean))
        '
        'UsernameTextBox
        '
        Me.UsernameTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Global.PDFKeeper.WindowsApplication.My.MySettings.Default, "LoginUsername", True, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged))
        resources.ApplyResources(Me.UsernameTextBox, "UsernameTextBox")
        Me.UsernameTextBox.Name = "UsernameTextBox"
        Me.HelpProvider.SetShowHelp(Me.UsernameTextBox, CType(resources.GetObject("UsernameTextBox.ShowHelp"), Boolean))
        Me.UsernameTextBox.Text = Global.PDFKeeper.WindowsApplication.My.MySettings.Default.LoginUsername
        '
        'DatasourceTextBox
        '
        Me.DatasourceTextBox.DataBindings.Add(New System.Windows.Forms.Binding("Text", Global.PDFKeeper.WindowsApplication.My.MySettings.Default, "LoginDatasource", True, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged))
        resources.ApplyResources(Me.DatasourceTextBox, "DatasourceTextBox")
        Me.DatasourceTextBox.Name = "DatasourceTextBox"
        Me.HelpProvider.SetShowHelp(Me.DatasourceTextBox, CType(resources.GetObject("DatasourceTextBox.ShowHelp"), Boolean))
        Me.DatasourceTextBox.Text = Global.PDFKeeper.WindowsApplication.My.MySettings.Default.LoginDatasource
        '
        'OK
        '
        resources.ApplyResources(Me.OK, "OK")
        Me.OK.Name = "OK"
        Me.HelpProvider.SetShowHelp(Me.OK, CType(resources.GetObject("OK.ShowHelp"), Boolean))
        '
        'Cancel
        '
        Me.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        resources.ApplyResources(Me.Cancel, "Cancel")
        Me.Cancel.Name = "Cancel"
        Me.HelpProvider.SetShowHelp(Me.Cancel, CType(resources.GetObject("Cancel.ShowHelp"), Boolean))
        '
        'DatasourceLabel
        '
        resources.ApplyResources(Me.DatasourceLabel, "DatasourceLabel")
        Me.DatasourceLabel.Name = "DatasourceLabel"
        Me.HelpProvider.SetShowHelp(Me.DatasourceLabel, CType(resources.GetObject("DatasourceLabel.ShowHelp"), Boolean))
        '
        'PasswordSecureTextBox
        '
        resources.ApplyResources(Me.PasswordSecureTextBox, "PasswordSecureTextBox")
        Me.PasswordSecureTextBox.Name = "PasswordSecureTextBox"
        Me.PasswordSecureTextBox.ShortcutsEnabled = False
        Me.HelpProvider.SetShowHelp(Me.PasswordSecureTextBox, CType(resources.GetObject("PasswordSecureTextBox.ShowHelp"), Boolean))
        '
        'LoginForm
        '
        Me.AcceptButton = Me.OK
        resources.ApplyResources(Me, "$this")
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.Cancel
        Me.Controls.Add(Me.DatasourceLabel)
        Me.Controls.Add(Me.PasswordSecureTextBox)
        Me.Controls.Add(Me.Cancel)
        Me.Controls.Add(Me.OK)
        Me.Controls.Add(Me.DatasourceTextBox)
        Me.Controls.Add(Me.UsernameTextBox)
        Me.Controls.Add(Me.PasswordLabel)
        Me.Controls.Add(Me.UsernameLabel)
        Me.Controls.Add(Me.LogoPictureBox)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.HelpProvider.SetHelpKeyword(Me, resources.GetString("$this.HelpKeyword"))
        Me.HelpProvider.SetHelpNavigator(Me, CType(resources.GetObject("$this.HelpNavigator"), System.Windows.Forms.HelpNavigator))
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "LoginForm"
        Me.HelpProvider.SetShowHelp(Me, CType(resources.GetObject("$this.ShowHelp"), Boolean))
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.TopMost = True
        CType(Me.LogoPictureBox, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(false)
        Me.PerformLayout

End Sub
    Friend WithEvents PasswordSecureTextBox As PDFKeeper.WindowsApplication.SecureTextBox
    Friend WithEvents DatasourceLabel As System.Windows.Forms.Label
    Friend WithEvents HelpProvider As System.Windows.Forms.HelpProvider

End Class
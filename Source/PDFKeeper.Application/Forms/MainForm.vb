﻿'******************************************************************************
'*
'* PDFKeeper -- Free, Open Source PDF Capture, Upload, and Search.
'* Copyright (C) 2009-2016 Robert F. Frasca
'*
'* This file is part of PDFKeeper.
'*
'* PDFKeeper is free software: you can redistribute it and/or modify it under
'* the terms of the GNU General Public License as published by the Free
'* Software Foundation, either version 3 of the License, or (at your option)
'* any later version.
'*
'* PDFKeeper is distributed in the hope that it will be useful, but WITHOUT
'* ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
'* FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for
'* more details.
'*
'* You should have received a copy of the GNU General Public License along
'* with PDFKeeper.  If not, see <http://www.gnu.org/licenses/>.
'*
'******************************************************************************

Public Partial Class MainForm
	Dim history As New ArrayList
	Dim sortColumn As Byte
	Dim sortOrder As string = "asc"
	Dim documentCaptureFolderChanged As Boolean = True
	Dim searchLastTitleText As String = "PDFKeeper"
	Dim searchLastStatusMessage As String
	Dim capturePdfEditInput As String
	Dim capturePdfEditOutput As String
	Dim captureLastStatusMessage As String
	Dim lastPdfDocumentCheckResult As Enums.PdfPasswordType
						
	''' <summary>
	''' Class constructor.
	''' </summary>
	Public Sub New()
		Me.InitializeComponent()
		StartUpdateCheck
		fileSystemWatcherDocumentCapture.Path = _
			ApplicationProfileFolders.Capture
	End Sub
	
	#Region "Form Loading"
	
	''' <summary>
	''' This subroutine will set the font to MS Sans Serif 8pt in XP or
	''' Segoe UI 9pt in Vista or later; set the form size and position based
	''' on the values retrieved from the User Settings object properties, a
	''' default will be used for any value that is NUL; call the
	''' ResizeListViewColumns subroutine; and select the Search Text combo box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub MainFormLoad(sender As Object, e As EventArgs)
		Font = SystemFonts.MessageBoxFont
		PositionAndSizeForm
		DoNotResetZoomLevelCheck
		ResizeListViewColumns
		comboBoxSearchText.Select
	End Sub
	
	''' <summary>
	''' This subroutine will start the update check thread.
	''' </summary>
	Private Sub StartUpdateCheck
		If CDbl(UserSettings.Instance.UpdateCheck) = 1 Then
			toolStripStatusLabelUpdateStatus.Text = _
				PdfKeeper.Strings.MainFormCheckingVersion
			BackgroundWorkerUpdateCheck.RunWorkerAsync
		End If
	End Sub
	
	''' <summary>
	''' Size and position form based on the values retrieved from the
	''' UserSettings properties.
	''' </summary>
	Private Sub PositionAndSizeForm
		Me.Top = CInt(UserSettings.Instance.FormPositionTop)
		Me.Left = CInt(UserSettings.Instance.FormPositionLeft)
		If IsNothing(UserSettings.Instance.FormPositionHeight) Then
			Dim workingRectangle As System.Drawing.Rectangle = _
				Screen.PrimaryScreen.WorkingArea
			Me.Size = New System.Drawing.Size(workingRectangle.Width - 10, _
				workingRectangle.Height - 10)
			If Me.Height > UserSettings.Instance.FormPositionDefaultHeight Then
				Me.Height = UserSettings.Instance.FormPositionDefaultHeight
			End If
		Else
			Me.Height = CInt(UserSettings.Instance.FormPositionHeight)
		End If
		If IsNothing(UserSettings.Instance.FormPositionWidth) = False Then
			Me.Width = CInt(UserSettings.Instance.FormPositionWidth)
		ElseIf Me.Width > UserSettings.Instance.FormPositionDefaultWidth Then
			Me.Width = UserSettings.Instance.FormPositionDefaultWidth			
		End If
		Me.WindowState = CType(UserSettings.Instance.FormPositionWindowState, _
			Windows.Forms.FormWindowState)
	End Sub
	
	''' <summary>
	''' If user setting is enabled, check "Do not reset Zoom Level during this
	''' preview session" on the Document Preview tab. 
	''' </summary>
	Private Sub DoNotResetZoomLevelCheck
		If UserSettings.Instance.DoNotResetZoomLevel = CStr(1) Then
			checkBoxDoNotResetZoomLevel.Checked = True
		End If
	End Sub
		
	#End Region
	
	#Region "Form Menu"
	
	''' <summary>
	''' This subroutine will prompt the user for the folder and file name to
	''' save the PDF as, and then query the database to get the PDF document
	''' for the selected ID.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemSavePdfToDiskClick(sender As Object, e As EventArgs)
		SaveFileDialog.InitialDirectory = _
			UserSettings.Instance.SaveFileLastFolder
		
		' Construct the file name prefill using the title of the selected
		' list view item.
		SaveFileDialog.FileName = listViewDocs.SelectedItems(0).SubItems(1).Text & _
								".pdf"

		If SaveFileDialog.ShowDialog() = 2 Then
			Exit Sub
		End If
		Me.Cursor = Cursors.WaitCursor
		Dim targetPdfFile As String = SaveFileDialog.FileName
		Dim pdfFileInfo As New FileInfo(targetPdfFile)
		UserSettings.Instance.SaveFileLastFolder = pdfFileInfo.DirectoryName
		If Not pdfFileInfo.Extension.ToUpper(CultureInfo.InvariantCulture) = _
				".PDF" Then
			targetPdfFile = targetPdfFile & ".pdf"
		End If
		Try
			System.IO.File.Copy( _
				DocumentRecord.Instance.PdfPathName, _
				targetPdfFile)
		Catch ex As IOException
			Me.Cursor = Cursors.Default
			ShowError(ex.Message)
			Exit Sub
		End Try
		Me.Cursor = Cursors.Default
		ShowInformation(String.Format( _
			CultureInfo.CurrentCulture, _
			PdfKeeper.Strings.MainFormPdfSaved, _
			targetPdfFile))
	End Sub
	
	''' <summary>
	''' This subroutine will select the "Document Notes" tab; prompt the user
	''' to select the printer to use for printing the contents of the document
	''' notes text box; and then call the print process, if the OK button was
	''' selected.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemPrintDocumentNotesClick(sender As Object, e As EventArgs)
		tabControlDocNotesKeywords.SelectTab(0)
		Dim result As Boolean = False
		While result = False
			Try
				If printDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
					Me.Cursor = Cursors.WaitCursor
					printDocumentNotes.Print
					Me.Cursor = Cursors.Default
				End If
				result = True
			Catch ex As System.ArgumentNullException
			Catch ex As System.ComponentModel.Win32Exception
			Catch ex As System.NullReferenceException
			End Try
		End While
	End Sub
	
	''' <summary>
	''' Print the contents of the Document Notes text box using the printer
	''' chosen by the user.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub PrintDocumentNotesPrintPage( _
		sender As Object, _
		e As System.Drawing.Printing.PrintPageEventArgs)
		Dim printText As String = textBoxDocumentNotes.Text
		printText.Print(textBoxDocumentNotes.Font, e)
	End Sub
	
	''' <summary>
	''' This subroutine will exit the form and the application.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemExitClick(sender As Object, e As EventArgs)
		Me.Close
	End Sub

	''' <summary>
	''' This subroutine will select the "Document Notes" tab and append a
	''' Date/Time stamp with the database user name to the existing text in
	''' the Document Notes text box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemInsertDateTimeIntoDocumentNotesClick(sender As Object, e As EventArgs)
		tabControlDocNotesKeywords.SelectTab(0)
		TextBoxDocumentNotesScrollToEnd
		If textBoxDocumentNotes.TextLength > 0 Then
			textBoxDocumentNotes.AppendText(vbCrLf & vbCrLf)
		End If
		textBoxDocumentNotes.AppendText("--- " & Date.Now & " (" & _
			UserSettings.Instance.LastUserName & ") ---" & vbCrLf)
		TextBoxDocumentNotesScrollToEnd
	End Sub

	''' <summary>
	''' This subroutine will check all listview items.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemCheckAllClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		Dim oListViewItem As ListViewItem
		For Each oListViewItem In listViewDocs.Items
			oListViewItem.Checked = True
		Next
		UpdateListCountStatusBar
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will uncheck all listview items.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemUncheckAllClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		Dim oListViewItem As ListViewItem
		For Each oListViewItem In listViewDocs.Items
			oListViewItem.Checked = False
		Next
		UpdateListCountStatusBar
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will delete the database records for all checked
	''' listview items.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemDeleteCheckedDocumentsClick(sender As Object, e As EventArgs)
		DocumentNotesModifiedCheck
		If ShowQuestion( _
			PdfKeeper.Strings.MainFormDeleteChecked) = 7 Then ' No
			
			Exit Sub
		End If
		Me.Cursor = Cursors.WaitCursor
		Dim oListViewItem As ListViewItem
		For Each oListViewItem In listViewDocs.CheckedItems
			Dim nonQuery As New DatabaseNonQuery(oListViewItem.Text)
			Try
				nonQuery.ExecuteNonQuery
			Catch ex As DataException
				Me.Cursor = Cursors.Default
				Exit Sub
			End Try
			oListViewItem.Checked = False
			oListViewItem.Remove
		Next
		UpdateListCountStatusBar
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will open the Capture folder with Windows Explorer.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemCaptureFolderClick(sender As Object, e As EventArgs)
		OpenFolder(ApplicationProfileFolders.Capture)
	End Sub
	
	''' <summary>
	''' This subroutine will open the DirectUpload folder with Windows Explorer.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemDirectUploadFolderClick(sender As Object, e As EventArgs)
		OpenFolder(ApplicationProfileFolders.DirectUpload)
	End Sub
	
	''' <summary>
	''' This subroutine will open the specified folder with Windows Explorer.
	''' </summary>
	''' <param name="folderPath"></param>
	Private Sub OpenFolder(folderPath As String)
		Me.Cursor = Cursors.WaitCursor
		processExplorer.StartInfo.FileName = folderPath
		processExplorer.Start
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will open the Direct Upload Configuration form.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemDirectUploadConfigClick(sender As Object, e As EventArgs)
		timerDirectUpload.Stop
		DirectUploadConfigurationForm.ShowDialog()
		timerDirectUpload.Start
	End Sub
	
	''' <summary>
	''' This subroutine will check/uncheck the
	''' "Automatically Check for Newer Version" menu item.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemHelpClick(sender As Object, e As EventArgs)
		If CDbl(UserSettings.Instance.UpdateCheck) = 1 Then
			toolStripMenuItemCheckNewerVersion.Checked = True
		Else
			toolStripMenuItemCheckNewerVersion.Checked = False
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will display the help file.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemContentsClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		Dim helpTopic As String = Nothing
		If tabControlMain.SelectedIndex = 0 Then
			helpTopic = HelpFileTopics.MainFormDocumentSearchTab
		ElseIf tabControlMain.SelectedIndex = 1 Then
			helpTopic = HelpFileTopics.MainFormDocumentPreviewTab
		ElseIf tabControlMain.SelectedIndex = 2 Then
			helpTopic = HelpFileTopics.MainFormDocumentTextOnlyViewTab
		ElseIf tabControlMain.SelectedIndex = 3 Then
			helpTopic = HelpFileTopics.MainFormDocumentCaptureTab
		End If
		Help.ShowHelp(Me, HelpFileTopics.HelpFile, helpTopic)
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will enable/disable update checking.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemCheckNewerVersionClick(sender As Object, e As EventArgs)
		If toolStripMenuItemCheckNewerVersion.Checked Then
			UserSettings.Instance.UpdateCheck = CStr(0)
			toolStripMenuItemCheckNewerVersion.Checked = False
		Else
			UserSettings.Instance.UpdateCheck = CStr(1)
			toolStripMenuItemCheckNewerVersion.Checked = True
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will display the About box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripMenuItemAboutClick(sender As Object, e As EventArgs)
		AboutForm.ShowDialog()
	End Sub
	
	#End Region

	#Region "Document Search"
	
	''' <summary>
	''' This subroutine will fill the Search Text combo box with the items
	''' contained in the history array.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ComboBoxSearchTextDropDown(sender As Object, e As EventArgs)
		comboBoxSearchText.Items.Clear
		For Each historyItem As String In history
			comboBoxSearchText.Items.Add(historyItem)
		Next
	End Sub
	
	''' <summary>
	''' This subroutine will enable the Search button if the search text does
	''' not contain any syntax errors.  The user will be alerted if a syntax
	''' error was detected.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ComboBoxSearchTextTextChanged(sender As Object, e As EventArgs)
		Dim errorMessage As String
		errorProvider.Clear
		toolStripMenuItemRefresh.Enabled = False
		buttonSearch.Enabled = False
		comboBoxSearchText.Text = comboBoxSearchText.Text.TrimStart
		If comboBoxSearchText.Text.Length = 0 Then
			Exit Sub
		End If
		If comboBoxSearchText.Text.IndexOf("*", _
				StringComparison.Ordinal) <> -1 Then
			errorMessage = PdfKeeper.Strings.MainFormSearchTextUsageError
			errorProvider.SetError(comboBoxSearchText, errorMessage)
			comboBoxSearchText.Select
			Exit Sub
		End If
		If SearchTextInvalid() Then
			errorMessage = PdfKeeper.Strings.MainFormSearchImproperUsage
			errorProvider.SetError(comboBoxSearchText, errorMessage)
			comboBoxSearchText.Select
			Exit Sub
		End If
		errorProvider.Clear
		buttonSearch.Enabled = True
	End Sub
	
	''' <summary>
	''' This function will return True or False if the Search Text specified is
	''' invalid.
	''' </summary>
	''' <returns>True or False</returns>
	Private Function SearchTextInvalid() As Boolean
		If comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "MINUS" Or _
			comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "MINUS " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "NEAR" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "NEAR " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "NOT" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "NOT " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "AND" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "AND " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "EQUIV" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "EQUIV " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "WITHIN" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "WITHIN " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "OR" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "OR " Or _
 		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "ACCUM" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "ACCUM " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "FUZZY" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "FUZZY " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "ABOUT" Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "ABOUT " Or _
		   	comboBoxSearchText.Text.ToUpper(CultureInfo.InvariantCulture) = "ABOUT()" Or _
		   	comboBoxSearchText.Text.IndexOf("{}", StringComparison.Ordinal) <> -1 Or _
		   	comboBoxSearchText.Text.IndexOf("()", StringComparison.Ordinal) <> -1 Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "=" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = ";" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = ">" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "-" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "~" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "&" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "|" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "," Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "!" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "{" Or _
		   	comboBoxSearchText.Text.Substring(0,1) = "(" Or _
		   	comboBoxSearchText.Text = "?" Or _
		   	comboBoxSearchText.Text = "$" Then
			Return True
		End If
		Return False
	End Function
	
	''' <summary>
	''' This subroutine will search the database for document records that
	''' match the Search Text, and then add the matching records to the
	''' listview, sorted by the selected column. If the search returns one or
	''' more records, add the search text to the search text history, only if
	''' the search text doesn't already exist in the history.  If the listview
	''' contains a selected document, then reselect that same document after
	''' reloading the list view, if the document record still exists. 
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonSearchClick(sender As Object, e As EventArgs)
		DocumentNotesModifiedCheck
		Me.Cursor = Cursors.WaitCursor
		listViewDocs.SelectedItems.Clear
		listViewDocs.Items.Clear
		toolStripStatusLabelMessage.Text = Nothing
		Me.Refresh	' Form needed to be refreshed for status label to clear.
		
		' Build query string.
		Dim orderBy As String = Nothing
		Select Case sortColumn
			Case = 0	' ID
				orderBy = "doc_id " & sortOrder & "," & _
						  "doc_title " & sortOrder & "," & _
					  	  "doc_author " & sortOrder & "," & _
					  	  "doc_subject " & sortOrder & "," & _
					 	  "doc_added " & sortOrder
			Case = 1	' Title
				orderBy = "doc_title " & sortOrder & "," & _
						  "doc_author " & sortOrder & "," & _
				      	  "doc_subject " & sortOrder & "," & _
						  "doc_added " & sortOrder & "," & _
					  	  "doc_id " & sortOrder
			Case = 2	' Author
				orderBy = "doc_author " & sortOrder & "," & _
						  "doc_subject " & sortOrder & "," & _
						  "doc_added " & sortOrder & "," & _
					  	  "doc_id " & sortOrder & "," & _
					  	  "doc_title " & sortOrder
			Case = 3	' Subject
				orderBy = "doc_subject " & sortOrder & "," & _
						  "doc_added " & sortOrder & "," & _
					  	  "doc_id " & sortOrder & "," & _
					  	  "doc_title " & sortOrder & "," & _
						  "doc_author " & sortOrder
			Case = 4	' Added
				orderBy = "doc_added " & sortOrder & "," & _
						  "doc_id " & sortOrder & "," & _
					  	  "doc_title " & sortOrder & "," & _
						  "doc_author " & sortOrder & "," & _
						  "doc_subject " & sortOrder
		End Select
		Dim query As New DatabaseSearchQuery(comboBoxSearchText.Text, orderBy)
		Dim tableReader As DataTableReader = _
			query.ExecuteQuery.CreateDataReader
		
		' Fill listview with the results.
		Dim oListViewItem As ListViewItem
		Dim itemArray(5) As String
		Do While (tableReader.Read())
			itemArray(0) = CType(tableReader("doc_id"), String)
			itemArray(1) = CType(tableReader("doc_title"), String)
			itemArray(2) = CType(tableReader("doc_author"), String)
 			itemArray(3) = CType(tableReader("doc_subject"), String)
 			itemArray(4) = CType(tableReader("doc_added"), String)
			oListViewItem = New ListViewItem(itemArray)
 			ListViewDocs.Items.Add(oListViewItem)
		Loop
				
		UpdateListCountStatusBar
		RightJustifyListViewDocIds
		ResizeListViewColumns
		If listViewDocs.Items.Count > 0 Then
			If sortOrder = "asc" Then
				listViewDocs.EnsureVisible(listViewDocs.Items.Count - 1)
			End If
			toolStripMenuItemCheckAll.Enabled = True
			AddSearchTextToHistory
			listViewDocs.Focus
			listViewDocs.Select
			If DocumentRecord.Instance.Id > 0 Then
				For i As Integer = 0 To listViewDocs.Items.Count - 1
					If listViewDocs.Items(i).Text.Trim = _
							DocumentRecord.Instance.Id.ToString( _
							CultureInfo.InvariantCulture).Trim Then
						ListViewDocs.Items(i).Selected = True
						listViewDocs.EnsureVisible( _
							listViewDocs.SelectedItems(0).Index)
					End If
				Next
			End If
		Else
			toolStripMenuItemCheckAll.Enabled = False
		End If
		toolStripMenuItemRefresh.Enabled = True
		buttonSearch.Enabled = False
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will add the Search Text to the Search Text history.
	''' </summary>
	Private Sub AddSearchTextToHistory
		If Not history.Contains(comboBoxSearchText.Text) Then
			history.Add(comboBoxSearchText.Text)
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will set the listview sort order based on the column
	''' selected, and then refresh the listview.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ListViewDocsColumnClick(sender As Object, e As ColumnClickEventArgs)
		If comboBoxSearchText.Text.Length = 0 Then
			Exit Sub
		End If
		If e.Column = sortColumn Then
			If sortOrder = "asc" Then
				sortOrder = "desc"
			Else
				sortOrder = "asc"
			End If
		Else
			sortOrder = "asc"
		End If
		sortColumn = CByte(e.Column)
		ButtonSearchClick(Me, Nothing)
	End Sub
	
	''' <summary>
	''' This subroutine will query the Document Notes and Document Keywords
	''' of the selected listview item, load the results into the Document
	''' Notes and Document Keywords text boxes, and then enable/disable
	''' document record specific menu/control items.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ListViewDocsSelectedIndexChanged(sender As Object, e As EventArgs)
		DocumentNotesModifiedCheck
		textBoxDocumentKeywords.Text = Nothing
		textBoxDocumentNotes.Text = Nothing
		If listViewDocs.SelectedItems.Count = 0 Then
			DocumentRecord.Instance.Id = 0	' no item selected
			toolStripMenuItemSavePDFtoDisk.Enabled = False
			toolStripMenuItemInsertDateTimeIntoDocumentNotes.Enabled = False
			textBoxDocumentNotes.Enabled = False
			textBoxDocumentKeywords.Enabled = False
			buttonDocumentNotesUpdate.Enabled = False
			buttonDocumentNotesRevert.Enabled = False
			Me.Text = "PDFKeeper"
			searchLastTitleText = Me.Text
			buttonZoomIn.Enabled = False
			buttonZoomOut.Enabled = False
			buttonPreviewPrevious.Enabled = False
			buttonPreviewNext.Enabled = False
			pictureBoxPreview.Image = Nothing
			pictureBoxPreview.Enabled = False
			buttonTextOnlyPrevious.Enabled = False
			buttonTextOnlyNext.Enabled = False
			textBoxTextOnlyView.Text = Nothing
			textBoxTextOnlyView.Enabled = False
		Else
			Dim selectedIndex As Integer = listViewDocs.SelectedItems(0).Index
			Try
				Me.Cursor = Cursors.WaitCursor
				DocumentRecord.Instance.Id = _
					CInt(listViewDocs.SelectedItems(0).Text.Trim)
			Catch ex As DataException
				ShowError(ex.Message.ToString())
				Exit Sub
			Finally
				Me.Cursor = Cursors.Default
			End Try
			If DocumentRecord.Instance.Keywords IsNot Nothing Then
				textBoxDocumentKeywords.Text = DocumentRecord.Instance.Keywords
				textBoxDocumentKeywords.Enabled = True
			End If
			If DocumentRecord.Instance.Notes IsNot Nothing Then
				textBoxDocumentNotes.Text = DocumentRecord.Instance.Notes
			End If
			toolStripMenuItemSavePDFtoDisk.Enabled = True
			toolStripMenuItemInsertDateTimeIntoDocumentNotes.Enabled = True
			tabPagePreview.Enabled = True
			textBoxDocumentNotes.Enabled = True
			TextBoxDocumentNotesScrollToEnd
			Me.Text = "PDFKeeper - [" & DocumentRecord.Instance.Id & "]"
			searchLastTitleText = Me.Text
			If tabControlMain.SelectedIndex = 0 Then
				listViewDocs.Focus
				listViewDocs.Items(selectedIndex).Selected = True
			End If
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will get the title of the PDF document for the selected
	''' ID and open it.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ListViewDocsMouseDoubleClick(sender As Object, e As MouseEventArgs)
		Me.Cursor = Cursors.WaitCursor
		listViewDocs.SelectedItems(0).Checked = False
		Dim oPdfProperties As New _
			PdfProperties(DocumentRecord.Instance.PdfPathName)
		If oPdfProperties.Read = 0 Then
			Dim titleBarText As String
			If oPdfProperties.Title = Nothing Then
				titleBarText = Path.GetFileName( _
					DocumentRecord.Instance.PdfPathName) & " - SumatraPDF"
			Else
				titleBarText = Path.GetFileName( _
					DocumentRecord.Instance.PdfPathName) & " - [" & _
					oPdfProperties.Title & "] - SumatraPDF"
			End If
			If WindowFinder(titleBarText, True) = False Then
				processSearchPdfViewer.StartInfo.Arguments = chr(34) & _
					DocumentRecord.Instance.PdfPathName & chr(34)
				processSearchPdfViewer.Start
				processSearchPdfViewer.WaitForInputIdle(15000)
			End If
		End If
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will toggle "Uncheck All" and
	''' "Delete Checked Documents" menu items based on if any listview items
	''' are checked or none are checked and update the status bar with the
	''' number of checked list view items.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ListViewDocsItemChecked(sender As Object, e As ItemCheckedEventArgs)
		If listViewDocs.CheckedItems.Count > 0 Then
			toolStripMenuItemUncheckAll.Enabled = True
			toolStripMenuItemDeleteCheckedDocuments.Enabled = True
		Else
			toolStripMenuItemUncheckAll.Enabled = False
			toolStripMenuItemDeleteCheckedDocuments.Enabled = False
		End If
		UpdateListCountStatusBar
	End Sub
	
	''' <summary>
	''' This subroutine is a workaround to the listview control forcing the
	''' far left column to be left justified.  However, right justification is
	''' required to properly align the ID's in the listview.
	''' </summary>
	Private Sub RightJustifyListViewDocIds
		Dim maxLength As Integer = 0
		Dim oListViewItem As ListViewItem
		For Each oListViewItem In listViewDocs.Items
			If oListViewItem.Text.Length > maxLength Then
				maxLength = oListViewItem.Text.Length
			End If
		Next
		If maxLength > 0 Then
			Dim diff As Integer
			For Each oListViewItem In listViewDocs.Items
				If oListViewItem.Text.Length < maxLength Then
					diff = maxLength - oListViewItem.Text.Length
					oListViewItem.Text = _
					oListViewItem.Text.PadLeft(maxLength + diff)
				End If
			Next
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will resize each listview column by auto-adjusting each
	''' column to fit the header and contents, then resize columns 1-3 to a
	''' maximum width of 259 each, only if the total width of all columns
	''' exceeds the width of the form.
	''' </summary>
	Private Sub ResizeListViewColumns
		If listViewDocs.Items.Count > 0 Then
			listViewDocs.AutoResizeColumns( _
				ColumnHeaderAutoResizeStyle.ColumnContent)
			Dim totalColWidth As Integer
			totalColWidth = 0
			For j = 0 To 4
				If j > 0 And j < 4 Then
					totalColWidth = totalColWidth + _
						listViewDocs.Columns(j).Width
				End If
			Next
			If totalColWidth > Me.Width Then
				For k = 1 To 3
					If listViewDocs.Columns(k).Width > 259 Then
						listViewDocs.Columns(k).Width = 259
					End If
				Next
			End If
		Else
			listViewDocs.AutoResizeColumns( _
				ColumnHeaderAutoResizeStyle.HeaderSize)
		End If
	End Sub
	
	''' <summary>
	''' Selects the next listview item.
	''' direction = "N" or "P"; where N = Next, P = Previous
	''' </summary>
	''' <param name="direction"></param>
	Private Sub SelectNextListViewItem(ByVal direction As String)
		listViewDocs.Focus
		If direction.ToUpper(CultureInfo.CurrentCulture) = "N" Then
			listViewDocs.Items( _
				listViewDocs.SelectedItems(0).Index + 1).Selected = True
		ElseIf direction.ToUpper(CultureInfo.CurrentCulture) = "P" Then
			listViewDocs.Items( _
				listViewDocs.SelectedItems(0).Index - 1).Selected = True
		Else
			Throw New System.ArgumentException( _
				PdfKeeper.Strings.MainFormDirectionArgument)
		End If
		listViewDocs.EnsureVisible(listViewDocs.SelectedItems(0).Index)
	End Sub
		
	''' <summary>
	''' This subroutine will trim a leading space from the text in the
	''' Document Notes text box, enable the "Print Document Notes" menu item
	''' if the Document Notes text box contains text, and enable the Update
	''' and Revert buttons if the text in the Document Notes text box was
	''' modified.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub TextBoxDocumentNotesTextChanged(sender As Object, e As EventArgs)
		textBoxDocumentNotes.Text = textBoxDocumentNotes.Text.TrimStart
		If textBoxDocumentNotes.Text.Length > 0 Then
			toolStripMenuItemPrintDocumentNotes.Enabled = True
		Else
			toolStripMenuItemPrintDocumentNotes.Enabled = False
		End If
		If textBoxDocumentNotes.Modified Then
			buttonDocumentNotesUpdate.Enabled = True
			buttonDocumentNotesRevert.Enabled = True
		Else
			buttonDocumentNotesUpdate.Enabled = False
			buttonDocumentNotesRevert.Enabled = False
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will update the Document Notes for selectedId in the
	''' database with the contents of the Document Notes text box, and then
	''' disable the Update and Revert buttons.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonDocumentNotesUpdateClick(sender As Object, e As EventArgs)
		textBoxDocumentNotes.Text = textBoxDocumentNotes.Text.Trim
		Try
			Me.Cursor = Cursors.WaitCursor
			DocumentRecord.Instance.Notes = textBoxDocumentNotes.Text
		Catch ex As DataException
			ShowError(ex.Message.ToString())
			Exit Sub
		Finally
			Me.Cursor = Cursors.Default
		End Try
		buttonDocumentNotesUpdate.Enabled = False
		buttonDocumentNotesRevert.Enabled = False
		TextBoxDocumentNotesScrollToEnd
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will revert all changes made to the Document Notes
	''' text box, and then disable the Update and Revert buttons.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonDocumentNotesRevertClick(sender As Object, e As EventArgs)
		textBoxDocumentNotes.Text = DocumentRecord.Instance.Notes
		buttonDocumentNotesUpdate.Enabled = False
		buttonDocumentNotesRevert.Enabled = False
		TextBoxDocumentNotesScrollToEnd
	End Sub
	
	''' <summary>
	''' This subroutine will select the Document Notes text box, and then move
	''' the cursor to the end of the text.
	''' </summary>
	Private Sub TextBoxDocumentNotesScrollToEnd
		textBoxDocumentNotes.Select
		textBoxDocumentNotes.Select(textBoxDocumentNotes.Text.Length,0)
		textBoxDocumentNotes.ScrollToCaret
	End Sub
	
	''' <summary>
	''' This subroutine will prompt to save Document Notes if unsaved data
	''' exists and either Update or Revert the changes based on the user
	''' response.
	''' </summary>
	Private Sub DocumentNotesModifiedCheck
		If buttonDocumentNotesUpdate.Enabled Then
			tabControlDocNotesKeywords.SelectTab(0)
			If ShowQuestion( _
				PdfKeeper.Strings.MainFormDocumentNotesSavePrompt) = 6 Then 'Yes
				
				ButtonDocumentNotesUpdateClick(Me, Nothing)
			Else
				ButtonDocumentNotesRevertClick(Me, Nothing)
			End If
		End If
	End Sub
	
	#End Region
	
	#Region "Document Preview"
	
	''' <summary>
	''' "Do not reset Zoom Level during this preview session" setting checkbox.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub CheckBoxDoNotResetZoomLevelCheckedChanged(sender As Object, e As EventArgs)
		If checkBoxDoNotResetZoomLevel.Checked Then
			UserSettings.Instance.DoNotResetZoomLevel = CStr(1)
		Else
			UserSettings.Instance.DoNotResetZoomLevel = CStr(0)
		End If
	End Sub
	
	''' <summary>
	''' Increase the zoom level, and then apply to the image in the picture
	''' box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonZoomInClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		ImageZoom.Instance.IncreaseZoomLevel
		PreviewImageZoom
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Decrease the zoom level, and then apply to the image in the picture
	''' box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonZoomOutClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		ImageZoom.Instance.DecreaseZoomLevel
		PreviewImageZoom
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Select the previous listview item on the Document Search tab, and then
	''' load the preview PNG file generated from the PDF into the picture
	''' box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonPreviewPreviousClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		SelectNextListViewItem("P")
		LoadDocumentPreview
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Select the next listview item on the Document Search tab, and then
	''' load the preview PNG file generated from the PDF into the picture
	''' box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonPreviewNextClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		SelectNextListViewItem("N")
		LoadDocumentPreview
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Create an image of the first page from the PDF file for the selected
	''' listview item on the Document Search tab and load into the picture box;
	''' enable/disable controls on the Document Preview tab; set Image Zoom
	''' source file to image from picture box; and update the status bar.
	''' </summary>
	Private Sub LoadDocumentPreview
		toolStripStatusLabelMessage.Text = Nothing
		Application.DoEvents
		checkBoxDoNotResetZoomLevel.Enabled = False
		buttonZoomIn.Enabled = False
		buttonZoomOut.Enabled = False
		buttonPreviewPrevious.Enabled = False
		buttonPreviewNext.Enabled = False
		pictureBoxPreview.Image = Nothing
		Dim img As System.Drawing.Image = _
			DocumentRecord.Instance.GetPdfPreviewImage
		If img.Size.IsEmpty = False Then
			pictureBoxPreview.Image = img
			ImageZoom.Instance.SourceImage = pictureBoxPreview.Image
			pictureBoxPreview.Enabled = True
			checkBoxDoNotResetZoomLevel.Enabled = True
			buttonZoomIn.Enabled = True
			If listViewDocs.SelectedItems(0).Index > 0 Then
				buttonPreviewPrevious.Enabled = True
			End If
			If listViewDocs.SelectedItems(0).Index < _
					listViewDocs.Items.Count - 1 Then
				buttonPreviewNext.Enabled = True
			End If
			toolStripStatusLabelMessage.Text = "Previewing document: " & _
				listViewDocs.SelectedItems(0).Index + 1 & " of " & _
				listViewDocs.Items.Count
			If checkBoxDoNotResetZoomLevel.Checked = True Then
				PreviewImageZoom
			Else
				ImageZoom.Instance.ResetZoomLevel
			End If
		End If
	End Sub
	
	''' <summary>
	''' Enable/Disable the Zoom Out button, and then apply the zoom level to
	''' the image in the picture box.
	''' </summary>
	Private Sub PreviewImageZoom
		If ImageZoom.Instance.ZoomLevel > 100 Then
			buttonZoomOut.Enabled = True
		Else
			buttonZoomOut.Enabled = False
		End If
		pictureBoxPreview.Image = Nothing
		pictureBoxPreview.Image = ImageZoom.Instance.ZoomSourceImage
	End Sub
	
	#End Region
	
	#Region "Document Text-Only View"
	
	''' <summary>
	''' This subroutine will select the previous listview item on the Document
	''' Search tab, and then extract and load the document text into the text
	''' box control on the Document Text-Only View tab.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonTextOnlyPreviousClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		SelectNextListViewItem("P")
		LoadTextOnlyView
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will select the next listview item on the Document
	''' Search tab, and then extract and load the document text into the text
	''' box control on the Document Text-Only View tab.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonTextOnlyNextClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		SelectNextListViewItem("N")
		LoadTextOnlyView
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Load extracted text from the PDF document for the selected listview
	''' item on the Document Search tab into the text box control,
	''' enable/disable controls on the Document Text-Only View tab, and update
	''' the status bar.
	''' </summary>
	Private Sub LoadTextOnlyView
		toolStripStatusLabelMessage.Text = Nothing
		Application.DoEvents
		buttonTextOnlyPrevious.Enabled = False
		buttonTextOnlyNext.Enabled = False
		textBoxTextOnlyView.Text = Nothing
		textBoxTextOnlyView.Text = DocumentRecord.Instance.GetPdfText
		textBoxTextOnlyView.Enabled = True
		If listViewDocs.SelectedItems(0).Index > 0 Then
			buttonTextOnlyPrevious.Enabled = True
		End If
		If listViewDocs.SelectedItems(0).Index < _
			listViewDocs.Items.Count - 1 Then
			buttonTextOnlyNext.Enabled = True
		End If
		toolStripStatusLabelMessage.Text = "Viewing document: " & _
			listViewDocs.SelectedItems(0).Index + 1 & " of " & _
			listViewDocs.Items.Count
	End Sub
	
	#End Region
	
	#Region "Document Capture"
	
	''' <summary>
	''' Display the "Document Capture" status bar icon, if PDF files exist in
	''' the Capture folder; refresh the "Document Capture Queue" list box, if
	''' the folder change flag is set to True; and delete all empty
	''' sub-folders.  To maintain synchronization, the timer is stopped during
	''' the execution of this subroutine.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub TimerCaptureCheckTick(sender As Object, e As EventArgs)
		timerCaptureCheck.Stop
		If CountOfFilesInfolder( _
			ApplicationProfileFolders.Capture, _
			"pdf") > 0 Then
			
			toolStripStatusLabelCaptured.Visible = True
		Else
			toolStripStatusLabelCaptured.Visible = False
		End If
		If documentCaptureFolderChanged Then
			documentCaptureFolderChanged = False
			FillDocCaptureQueueList
		End If
		DeleteEmptySubfoldersFromFolder( _
			ApplicationProfileFolders.Capture)
		timerCaptureCheck.Start
	End Sub
	
	''' <summary>
	''' Set the Capture folder changed flag to True when a file added to the
	''' Capture folder is a PDF document.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub FileSystemWatcherDocumentCaptureCreated(sender As Object, e As FileSystemEventArgs)
		If Path.GetExtension(e.FullPath) = ".pdf" Then
			Me.Cursor = Cursors.WaitCursor
			FileUtil.WaitForFileCreation(e.FullPath)
			If listBoxDocCaptureQueue.FindStringExact(e.FullPath) = -1 Then
				documentCaptureFolderChanged = True
			End If
			Me.Cursor = Cursors.Default
		End If
	End Sub

	''' <summary>
	''' Set the Capture folder changed flag to True when a file is deleted from
	''' the Capture folder that exists in the Document Capture Queue list box,
	''' and then clear the form if a PDF document is selected.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub FileSystemWatcherDocumentCaptureDeleted(sender As Object, e As FileSystemEventArgs)
		If Not listBoxDocCaptureQueue.FindStringExact(e.FullPath) = -1 Then
			documentCaptureFolderChanged = True
			If textBoxPDFDocument.Text = e.FullPath Then
				ShowInformation( _
					PdfKeeper.Strings.MainFormSelectedDocDeleted)
				Me.Cursor = Cursors.WaitCursor
				TerminateCapturePdfViewer
				ClearCaptureSelection
				Me.Cursor = Cursors.Default
			End If
		End If
	End Sub

	''' <summary>
	''' Set the Capture folder changed flag to True when a file in the Capture
	''' folder is renamed and is selected; and then clear the form and select
	''' the renamed PDF document.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub FileSystemWatcherDocumentCaptureRenamed(sender As Object, e As RenamedEventArgs)
		If Not listBoxDocCaptureQueue.FindStringExact(e.OldFullPath) = -1 Then
			documentCaptureFolderChanged = False
			FillDocCaptureQueueList
			If textBoxPDFDocument.Text = e.OldFullPath Then
				ShowInformation( _
					PdfKeeper.Strings.MainFormSelectedDocRenamed)
				Me.Cursor = Cursors.WaitCursor
				TerminateCapturePdfViewer
				ClearCaptureSelection
				Dim index As Integer = _
					listBoxDocCaptureQueue.FindStringExact(e.FullPath)
				If Not index = -1 Then
					listBoxDocCaptureQueue.SetSelected(index, True)
				End If
				ListBoxDocCaptureQueueSelectedIndexChanged(Me,Nothing)
				Me.Cursor = Cursors.Default
			End If
		End If
	End Sub
	
	''' <summary>
	''' This subroutine is the "Document Capture" folder watcher error event
	''' handler that will display the error exception message and enable the
	''' folder watcher.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub FileSystemWatcherDocumentCaptureError(sender As Object, e As ErrorEventArgs)
		ShowError(e.GetException().ToString)
		fileSystemWatcherDocumentCapture.EnableRaisingEvents = True
	End Sub
		
	''' <summary>
	''' Fill the "Document Capture Queue" list box with the absolute path name
	''' for each PDF document in the Capture folder.
	''' </summary>
	Private Sub FillDocCaptureQueueList
		Me.Cursor = Cursors.WaitCursor
		listBoxDocCaptureQueue.Items.Clear
		Dim files As String() = Directory.GetFiles( _
			ApplicationProfileFolders.Capture, _
			"*.pdf", _
			SearchOption.AllDirectories)
		For Each file In files
			listBoxDocCaptureQueue.Items.Add(file)
		Next
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Read the information properties for the selected PDF document and
	''' update the form.  When a PDF document is selected, the "Document
	''' Capture Queue" list box is disabled.  If the selected PDF document is
	''' protected by an OWNER password, a password prompt will be displayed.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ListBoxDocCaptureQueueSelectedIndexChanged(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		capturePdfEditInput = CStr(listBoxDocCaptureQueue.SelectedItem)
		If IsNothing(capturePdfEditInput) = False Then
			capturePdfEditOutput = Path.Combine( _
				ApplicationProfileFolders.CaptureTemp, _
				Path.GetFileName(capturePdfEditInput))
			lastPdfDocumentCheckResult = _
				GetPdfPasswordType(capturePdfEditInput)
			If lastPdfDocumentCheckResult = _
				Enums.PdfPasswordType.Owner Then
				If PdfOwnerPasswordForm.ShowDialog() = _
						Windows.Forms.DialogResult.Cancel Then
					Me.Cursor = Cursors.Default
					Exit Sub
				End If
			ElseIf lastPdfDocumentCheckResult = _
				Enums.PdfPasswordType.User Or _
				lastPdfDocumentCheckResult = _
				Enums.PdfPasswordType.Unknown Then
				Me.Cursor = Cursors.Default
				Exit Sub
			End If
			Dim oPdfProperties As PdfProperties = Nothing
			If lastPdfDocumentCheckResult = 0 Then
				Dim oPdfProperties1 As New PdfProperties(capturePdfEditInput)
				oPdfProperties = oPdfProperties1
			ElseIf lastPdfDocumentCheckResult = 1 Then
				Dim oPdfProperties2 As New PdfProperties(capturePdfEditInput, _
									   PdfOwnerPasswordForm.ownerPassword)
				oPdfProperties = oPdfProperties2
			End If
			If oPdfProperties.Read = 0 Then
				textBoxPDFDocument.Text = capturePdfEditInput
				buttonViewOriginal.Enabled = True
				textBoxTitle.Text = oPdfProperties.Title
				buttonSetToFileName.Enabled = True
				
				' If the title is blank, default to the filename without the
				' PDF extension.
				If IsNothing(oPdfProperties.Title) Then
					textBoxTitle.Text = Path.GetFileNameWithoutExtension( _
						capturePdfEditInput)
				End If
				
				textBoxTitle.Enabled = True
				textBoxTitle.Select
				buttonSetToFileName.Enabled = True
				listBoxDocCaptureQueue.Enabled = False
				comboBoxAuthor.Text = oPdfProperties.Author
				comboBoxAuthor.Enabled = True
				comboBoxSubject.Text = oPdfProperties.Subject
				comboBoxSubject.Enabled = True
				textBoxKeywords.Text = oPdfProperties.Keywords
				textBoxKeywords.Enabled = True
				buttonDeselect.Enabled = True
				CaptureComboBoxTextChanged
			Else
				ShowError(String.Format( _
					CultureInfo.CurrentCulture, _
					PdfKeeper.Strings.MainFormUnableRead, _
					capturePdfEditInput))
			End If
			buttonRename.Enabled = True
			buttonDelete.Enabled = True
		Else
			buttonRename.Enabled = False
			buttonDelete.Enabled = False
		End If
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' Prompt for new file name, and then rename the selected PDF document.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonRenameClick(sender As Object, e As EventArgs)
		PdfFileRenameForm.pdfRenameName = _
			Path.GetFileNameWithoutExtension(capturePdfEditInput)
		If PdfFileRenameForm.ShowDialog() = Windows.Forms.DialogResult.OK Then
			Dim capturePdfFileNewName As String = _
				Path.Combine(Path.GetDirectoryName(capturePdfEditInput), _
				PdfFileRenameForm.pdfRenameName & ".pdf")
			Me.Cursor = Cursors.WaitCursor
			Try
				System.IO.File.Move(capturePdfEditInput, capturePdfFileNewName)
			Catch ex As IOException
				ShowError(ex.Message)
			Finally
				Me.Cursor = Cursors.Default
			End Try
		End If
	End Sub
	
	''' <summary>
	''' Delete the selected PDF document to the Recycle Bin.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonDeleteClick(sender As Object, e As EventArgs)
		If ShowQuestion( _
			String.Format( _
			CultureInfo.CurrentCulture, _
			PdfKeeper.Strings.MainFormDeletePrompt, _
			capturePdfEditInput)) = 6 Then ' Yes
			
			Me.Cursor = Cursors.WaitCursor
			Try
				DeleteFileToRecycleBin(capturePdfEditInput)
			Catch ex As IOException
				ShowError(ex.Message)
			Finally
				Me.Cursor = Cursors.Default
			End Try
		End If
	End Sub
	
	''' <summary>
 	''' Call the CaptureViewPdf subroutine to display the original PDF
 	''' document in a restricted Sumatra PDF process.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonViewOriginalClick(sender As Object, e As EventArgs)
		CaptureViewPdf(capturePdfEditInput)
	End Sub
	
	''' <summary>
	''' Set Title textbox text to PDF file name without the extension. 
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonSetToFilenameClick(sender As Object, e As EventArgs)
		textBoxTitle.Text = _
			Path.GetFileNameWithoutExtension(capturePdfEditInput)
	End Sub
	
	''' <summary>
	''' Fill the Author combo box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ComboBoxAuthorDropDown(sender As Object, e As EventArgs)
		FillCaptureComboBox("Author")
	End Sub
	
	''' <summary>
	''' Fill the Subject combo box.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ComboBoxSubjectDropDown(sender As Object, e As EventArgs)
		FillCaptureComboBox("Subject")
	End Sub
	
	''' <summary>
	''' Fill "comboBoxName" with either authors or subjects for the
	''' selected/specified author.  "comboBoxName" can be "Author" or "Subject.
	''' </summary>
	''' <param name="comboBoxName"></param>
	Private Sub FillCaptureComboBox(ByVal comboBoxTitle As String)
		Dim query As DatabaseAuthorsSubjectsQuery
		Me.Cursor = Cursors.WaitCursor
		If comboBoxTitle = "Author" Then
			comboBoxAuthor.Items.Clear
			query = New DatabaseAuthorsSubjectsQuery
		ElseIf comboBoxTitle = "Subject" Then
			comboBoxSubject.Items.Clear
			query = New DatabaseAuthorsSubjectsQuery(comboBoxAuthor.Text)
		Else
			Exit Sub
		End If
		Dim rows As ArrayList = query.ExecuteQuery
		Dim row As String
		For Each row In rows
			If comboBoxTitle = "Author" Then
				comboBoxAuthor.Items.Add(row)
			ElseIf comboBoxTitle = "Subject" Then
				comboBoxSubject.Items.Add(row)
			End If
		Next
		Me.Cursor = Cursors.Default
	End Sub
		
	''' <summary>
	''' Trim the leading space from the text in all of the Document Capture
	''' text and combo boxes; and enable the Save button if the length of the
	''' Title, Author, and Subject is greater than 0.
	''' </summary>
	Private Sub CaptureComboBoxTextChanged
		toolStripStatusLabelMessage.Text = Nothing
		textBoxTitle.Text = textBoxTitle.Text.TrimStart
		comboBoxAuthor.Text = comboBoxAuthor.Text.TrimStart
		comboBoxSubject.Text = comboBoxSubject.Text.TrimStart
		textBoxKeywords.Text = textBoxKeywords.Text.TrimStart
		If textBoxTitle.TextLength > 0 And _
		   comboBoxAuthor.Text.Length > 0 And _
		   comboBoxSubject.Text.Length > 0 Then
			buttonSave.Enabled = True
		Else
			buttonSave.Enabled = False
		End If
		buttonRotate.Enabled = False
		buttonView.Enabled = False
		buttonUpload.Enabled = False
	End Sub
	
	''' <summary>
	''' Call the TerminateCapturePdfViewer subroutine; save the information
	''' properties to the new PDF document; and clear the PDF password secure
	''' string, if it was specified.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonSaveClick(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		DisableCaptureControls(False)
		TerminateCapturePdfViewer
		toolStripStatusLabelMessage.Text = _
			PdfKeeper.Strings.MainFormCaptureSaving
		Application.DoEvents
		Dim oPdfProperties As PdfProperties = Nothing
		If lastPdfDocumentCheckResult = 0 Then
			Dim oPdfProperties1 As New PdfProperties(capturePdfEditInput, _
													 capturePdfEditOutput)
			oPdfProperties = oPdfProperties1
		ElseIf lastPdfDocumentCheckResult = 1 Then
			Dim oPdfProperties2 As New PdfProperties(capturePdfEditInput, _
								   capturePdfEditOutput, _
								   PdfOwnerPasswordForm.ownerPassword)
			oPdfProperties = oPdfProperties2
		End If
		oPdfProperties.Title = textBoxTitle.Text.Trim
		oPdfProperties.Author = comboBoxAuthor.Text.Trim
		oPdfProperties.Subject = comboBoxSubject.Text.Trim
		oPdfProperties.Keywords = textBoxKeywords.Text.Trim
		If oPdfProperties.Write = 0 Then
			buttonSave.Enabled = False
			buttonRotate.Enabled = True
			buttonView.Enabled = True
			buttonUpload.Enabled = True
			toolStripStatusLabelMessage.Text = _
				PdfKeeper.Strings.MainFormCaptureSaved
		Else
			buttonRotate.Enabled = False
			buttonView.Enabled = False
			buttonUpload.Enabled = False
			toolStripStatusLabelMessage.Text = Nothing
		End If
		captureLastStatusMessage = toolStripStatusLabelMessage.Text
		EnableCaptureControls(False)
		If lastPdfDocumentCheckResult = 1 Then
			PdfOwnerPasswordForm.ownerPassword.Clear
		End If
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' 
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonRotateClick(sender As Object, e As EventArgs)
		RotatePagesForm.ShowDialog()
	End Sub
	
	''' <summary>
	''' Call the CaptureViewPdf subroutine to display the modified PDF
 	''' document in a restricted Sumatra PDF process.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonViewClick(sender As Object, e As EventArgs)
		CaptureViewPdf(capturePdfEditOutput)
	End Sub
	
	''' <summary>
	''' Call the TerminateCapturePdfViewer subroutine, upload the modified PDF
	''' document to the database, delete the original PDF document to the
	''' recycle bin, call the ClearCaptureSelection and FillDocCaptureQueueList
	''' subroutines.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonUploadClick(sender As Object, e As EventArgs)
		If ShowQuestion( _
			String.Format( _
			CultureInfo.CurrentCulture, _
			PdfKeeper.Strings.MainFormUploadPrompt, _
			capturePdfEditOutput)) = 6 Then ' Yes
			
			Me.Cursor = Cursors.WaitCursor
			DisableCaptureControls(True)
			TerminateCapturePdfViewer
			toolStripStatusLabelMessage.Text = _
				PdfKeeper.Strings.MainFormCaptureUploading
			Application.DoEvents
			
			' Read properties from PDF document and confirm that the Title, 
			' Author, and Subject are not blank.
			Dim oPdfProperties As New PdfProperties(capturePdfEditOutput)
			If oPdfProperties.Read = 0 Then
				If oPdfProperties.Title = Nothing Or _
		   		   	oPdfProperties.Author = Nothing Or _
		   		   	oPdfProperties.Subject = Nothing Then
				
					Me.Cursor = Cursors.Default
					EnableCaptureControls(True)
					ShowError(PdfKeeper.Strings.PdfPropertiesBlank)
					Exit Sub
				End If
			Else
				Me.Cursor = Cursors.Default
				EnableCaptureControls(True)
				Exit Sub
			End If
			
			Dim nonQuery As New DatabaseNonQuery( _
				oPdfProperties.Title, _
				oPdfProperties.Author, _
				oPdfProperties.Subject, _
				oPdfProperties.Keywords, _
				capturePdfEditOutput)
			Try
				nonQuery.ExecuteNonQuery
			Catch ex As DataException
				EnableCaptureControls(True)
				Me.Cursor = Cursors.Default
				Exit Sub
			End Try
			toolStripStatusLabelMessage.Text = Nothing
			Application.DoEvents
			Try
				DeleteFileToRecycleBin(capturePdfEditInput)
			Catch ex As IOException
				ShowError(ex.Message)
			End Try
			ClearCaptureSelection
			FillDocCaptureQueueList
			Me.Cursor = Cursors.Default
		End If
	End Sub
	
	''' <summary>
	''' Disable all text and combo boxes, and the appropriate buttons based on
	''' the action being performed.  If performing a Save, "uploading" should
	''' be False; if performing an upload, "uploading" should be True.  This
	''' should be called at the start of a Save or Upload.
	''' </summary>
	''' <param name="uploading"></param>
	Private Sub DisableCaptureControls(uploading As Boolean)
		buttonViewOriginal.Enabled = False
		textBoxTitle.Enabled = False
		buttonSetToFileName.Enabled = False
		comboBoxAuthor.Enabled = False
		comboBoxSubject.Enabled = False
		textBoxKeywords.Enabled = False
		buttonSave.Enabled = False
		If uploading Then
			buttonRotate.Enabled = False
			buttonView.Enabled = False
			buttonUpload.Enabled = False
		End If
		buttonDeselect.Enabled = False
	End Sub
	
	''' <summary>
	''' Disable all text and combo boxes, and the appropriate buttons based on
	''' the action being performed.  If performing a Save, "uploading" should
	''' be False; if performing an upload, "uploading" should be True.  This
	''' should be called at the end of a Save or when an Upload has failed.
	''' </summary>
	''' <param name="uploading"></param>
	Private Sub EnableCaptureControls(uploading As Boolean)
		buttonViewOriginal.Enabled = True
		textBoxTitle.Enabled = True
		buttonSetToFileName.Enabled = True
		comboBoxAuthor.Enabled = True
		comboBoxSubject.Enabled = True
		textBoxKeywords.Enabled = True
		If uploading Then
			buttonRotate.Enabled = True
			buttonView.Enabled = True
			buttonUpload.Enabled = True
		End If
		buttonDeselect.Enabled = True
	End Sub
	
	''' <summary>
	''' When the user selects "Yes" to deselect the selected document; call the
	''' the TerminateCapturePdfViewer and ClearCaptureSelection subroutines, and
	''' dispose the PDF password secure string if it was specified.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ButtonDeselectClick(sender As Object, e As EventArgs)
		If ShowQuestion( _
			PdfKeeper.Strings.MainFormDeselectPrompt) = 6 Then ' Yes
			
			Me.Cursor = Cursors.WaitCursor
			TerminateCapturePdfViewer
			ClearCaptureSelection
			If lastPdfDocumentCheckResult = 1 Then
				PdfOwnerPasswordForm.ownerPassword.Clear
			End If
			Me.Cursor = Cursors.Default
		End If
	End Sub
		
	''' <summary>
	''' Display the PDF document "file" in the restricted Sumatra PDF viewer.
	''' If a Sumatra PDF process object does exist, it will be terminated.
	''' </summary>
	''' <param name="file"></param>
	Private Sub CaptureViewPdf(file As String)
		Me.Cursor = Cursors.WaitCursor

		' Get the title of the PDF document and open it.
		Dim oPdfProperties As New PdfProperties(file)
		If oPdfProperties.Read = 0 Then
			Dim titleBarText As String
			If IsNothing(oPdfProperties.Title) Then
				titleBarText = Path.GetFileName(file) & " - SumatraPDF"
			Else
				titleBarText = Path.GetFileName(file) & " - [" & _
								   oPdfProperties.Title & "] - SumatraPDF"
			End If
			If WindowFinder(titleBarText, True) = False Then
				TerminateCapturePdfViewer
				processCapturePdfViewer.StartInfo.Arguments = "-restrict " & _
												  	chr(34) & file & chr(34)
				processCapturePdfViewer.Start
				processCapturePdfViewer.WaitForInputIdle(15000)
			End If
		End If
		Me.Cursor = Cursors.Default
	End Sub
		
	''' <summary>
	''' Terminate the restricted Sumatra PDF process object.
	''' </summary>
	Private Sub TerminateCapturePdfViewer
		Try
			processCapturePdfViewer.Kill
		Catch ex As InvalidOperationException
		End Try
	End Sub
	
	''' <summary>
	''' Clear the "Document Capture" form controls, enable the "Document
	''' Capture Queue" list box, and delete the modified copy of the selected
	''' PDF document.
	''' </summary>
	Private Sub ClearCaptureSelection
		toolStripStatusLabelMessage.Text = Nothing
		captureLastStatusMessage = Nothing
		buttonRename.Enabled = False
		buttonDelete.Enabled = False
		textBoxPDFDocument.Text = Nothing
		buttonViewOriginal.Enabled = False
		textBoxTitle.Text = Nothing
		textBoxTitle.Enabled = False
		buttonSetToFileName.Enabled = False
		comboBoxAuthor.Text = Nothing
		comboBoxAuthor.Enabled = False
		comboBoxSubject.Text = Nothing
		comboBoxSubject.Enabled = False
		textBoxKeywords.Text = Nothing
		textBoxKeywords.Enabled = False
		buttonSave.Enabled = False
		buttonRotate.Enabled = False
		buttonView.Enabled = False
		buttonUpload.Enabled = False
		buttonDeselect.Enabled = False
		listBoxDocCaptureQueue.Enabled = True
		Try
			System.IO.File.Delete(capturePdfEditOutput)
		Catch ex As IOException
			ShowError(ex.Message)
		End Try
	End Sub
	
	#End Region
	
	#Region "Tab Control, Title, and Status Bar"
	
	''' <summary>
	''' This subroutine will update the form title bar text, select the last
	''' selected search list view item, enable/disable menu items, and set the
	''' status bar text to the last message displayed for the selected tab.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub TabControlMainSelectedIndexChanged(sender As Object, e As EventArgs)
		Me.Cursor = Cursors.WaitCursor
		
		' Document Search
		If tabControlMain.SelectedIndex = 0 Then
			Me.Text = searchLastTitleText
			If DocumentRecord.Instance.Id > 0 Then
				listViewDocs.Focus
				listViewDocs.SelectedItems(0).Text = _
					CStr(DocumentRecord.Instance.Id)
				toolStripMenuItemSavePDFtoDisk.Enabled = True
			Else
				toolStripMenuItemSavePDFtoDisk.Enabled = False
			End If
			If textBoxDocumentNotes.Text.Length > 0 Then
				toolStripMenuItemPrintDocumentNotes.Enabled = True
			Else
				toolStripMenuItemPrintDocumentNotes.Enabled = False
			End If
			toolStripMenuItemEdit.Enabled = True
			toolStripMenuItemView.Enabled = True
			toolStripMenuItemCaptureFolder.Enabled = False
			toolStripStatusLabelMessage.Text = searchLastStatusMessage
			
		' Document Preview or Document Text-Only View
		ElseIf tabControlMain.SelectedIndex = 1 Or _
				tabControlMain.SelectedIndex = 2 Then
			Me.Text = searchLastTitleText
			toolStripStatusLabelMessage.Text = Nothing
			If DocumentRecord.Instance.Id > 0 Then
				listViewDocs.Focus
				listViewDocs.SelectedItems(0).Text = _
					CStr(DocumentRecord.Instance.Id)
				toolStripMenuItemSavePDFtoDisk.Enabled = True
				If tabControlMain.SelectedIndex = 1 Then
					LoadDocumentPreview	
				ElseIf tabControlMain.SelectedIndex = 2 Then
					LoadTextOnlyView
				End If
			Else
				toolStripMenuItemSavePDFtoDisk.Enabled = False
			End If
			toolStripMenuItemPrintDocumentNotes.Enabled = False
			toolStripMenuItemEdit.Enabled = False
			toolStripMenuItemView.Enabled = False
			toolStripMenuItemCaptureFolder.Enabled = False
		Else ' Document Capture
			Me.Text = "PDFKeeper"
			toolStripMenuItemSavePDFtoDisk.Enabled = False
			toolStripMenuItemPrintDocumentNotes.Enabled = False
			toolStripMenuItemEdit.Enabled = False
			toolStripMenuItemView.Enabled = False
			toolStripMenuItemCaptureFolder.Enabled = True
			toolStripStatusLabelMessage.Text = captureLastStatusMessage
		End If
		Me.Cursor = Cursors.Default
	End Sub
	
	''' <summary>
	''' This subroutine will update the status bar with the number of items in
	''' the listview.
	''' </summary>
	Private Sub UpdateListCountStatusBar
		toolStripStatusLabelMessage.Text = String.Format( _
							CultureInfo.CurrentCulture, _
							PdfKeeper.Strings.MainFormListViewCountChecked, _
							listViewDocs.Items.Count, _
							listViewDocs.CheckedItems.Count)
		searchLastStatusMessage = toolStripStatusLabelMessage.Text
	End Sub
	
	''' <summary>
	''' This subroutine will open the project site using the default browser.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub StatusStripItemClicked(sender As Object, e As ToolStripItemClickedEventArgs)
		If e.ClickedItem.Text = _
				PdfKeeper.Strings.MainFormNewerVersionAvailable Then
			Me.Cursor = Cursors.WaitCursor
			Process.Start(ConfigurationManager.AppSettings("ProjectSiteUrl"))
			Me.Cursor = Cursors.Default
		End If
	End Sub
		
	''' <summary>
	''' This subroutine will select the "Document Capture" tab.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub ToolStripStatusLabelCapturedClick(sender As Object, e As EventArgs)
		tabControlMain.SelectTab(1)
	End Sub
	
	#End Region
	
	#Region "Components"
	
	''' <summary>
	''' This subroutine will process the Direct Upload folder on a worker
	''' thread by uploading all PDF documents in each configured folder,
	''' including subfolders.  To maintain synchronization, the timer is
	''' stopped during the execution of this subroutine.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub TimerDirectUploadTick(sender As Object, e As EventArgs)
		toolStripMenuItemDirectUploadConfig.Enabled = False
		timerDirectUpload.Stop
		backgroundWorkerDirectUpload.RunWorkerAsync
	End Sub
	
	''' <summary>
	''' Triggers the setting of the UpdateCheck.Instance.IsUpdateAvailable
	''' property to True or False if an update is available, and then triggers
	''' the RunWorkerCompleted event.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub BackgroundWorkerUpdateCheckDoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs)
		ApplicationUpdate.Instance.CheckForUpdate
	End Sub
	
	''' <summary>
	''' Creates a link on the status bar if an update is available.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub BackgroundWorkerUpdateCheckRunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs)
		If ApplicationUpdate.Instance.IsUpdateAvailable Then
			toolStripStatusLabelUpdateStatus.Text = _
				PdfKeeper.Strings.MainFormNewerVersionAvailable
			toolStripStatusLabelUpdateStatus.ForeColor = _
				 System.Drawing.SystemColors.ActiveCaption
			toolStripStatusLabelUpdateStatus.IsLink = True
		Else
			toolStripStatusLabelUpdateStatus.Text = Nothing
		End If
	End Sub
	
	''' <summary>
	''' This subroutine will create missing Direct Upload folders; process the
	''' Direct Upload folder by uploading all PDF documents in each configured
	''' folder, including subfolders; delete any subfolders inside of each
	''' configured folder; and automatically trigger the RunWorkerCompleted
	''' event.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub BackgroundWorkerDirectUploadDoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs)
		DirectUpload.CreateMissingFolders
		If DirectUpload.CountOfPdfFiles > 0 Then
			toolStripStatusLabelUploading.Visible = True
			Application.DoEvents
			DirectUpload.UploadAllPdfFiles
			toolStripStatusLabelUploading.Visible = False
			Application.DoEvents
		End If
		DirectUpload.DeleteAllEmptySubfolders
	End Sub
	
	''' <summary>
	''' This subroutine will start the Direct Upload timer and enable the
	''' "Direct Upload Configuration" menu item.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub BackgroundWorkerDirectUploadRunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs)
		timerDirectUpload.Start
		toolStripMenuItemDirectUploadConfig.Enabled = True
	End Sub
	
	#End Region
	
	#Region "Form Closing"
	
	''' <summary>
	''' This subroutine will allow this form to close if no background worker
	''' thread's or timers are busy and no additional application form's are
	''' open.  If a PDF document is selected on the "Document Capture" tab,
	''' then prompt the user before closing the form.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub MainFormClosing(sender As Object, e As FormClosingEventArgs)
		If BackgroundWorkersBusy Or My.Application.OpenForms.Count > 1 _
								 Or timerCaptureCheck.Enabled = False _
								 Or timerDirectUpload.Enabled = False Then
			e.Cancel = True
		Else
			If Not textBoxPDFDocument.Text = Nothing Then
				If ShowQuestion( _
					PdfKeeper.Strings.MainFormClosingPromptSelected) = 6 Then 'Yes
					
					Me.Cursor = Cursors.WaitCursor
					TerminateCapturePdfViewer
					ClearCaptureSelection
					Me.Cursor = Cursors.Default
				Else
					e.Cancel = True
				End If
			End If
		End If
	End Sub
	
	''' <summary>
	''' This function will check if any background worker thread's are busy.
	''' </summary>
	''' <returns>True or False</returns>
	Function BackgroundWorkersBusy() As Boolean
		If backgroundWorkerUpdateCheck.IsBusy Or _
				backgroundWorkerDirectUpload.IsBusy Then
			Return True
		Else
			Return False
		End If
	End Function
	
	''' <summary>
	''' Check for unsaved Document Notes; dispose the database password string;
	''' save form size and postion; delete Document Capture and Direct Upload
	''' folder shortcuts; and delete cached PDF and PNG files.
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	Private Sub MainFormFormClosed(sender As Object, e As FormClosedEventArgs)
		DocumentNotesModifiedCheck
		DatabaseConnectionForm.dbPassword.Dispose
		SaveFormPosition
		Try
			System.IO.File.Delete(Shortcuts.DocumentCaptureInLinks)
			System.IO.File.Delete(Shortcuts.DocumentCaptureInSendTo)
			System.IO.File.Delete(Shortcuts.DirectUploadInLinks)
		Catch ex As IOException
			ShowError(ex.Message)
		End Try
		FileCache.Instance.DeleteAllItemsFromFileSystem
	End Sub
	
	''' <summary>
	''' This subroutine will save the form size and postion.
	''' </summary>
	Private Sub SaveFormPosition
		If Me.WindowState.ToString = "Normal" Then
			UserSettings.Instance.FormPositionTop = _
				Me.Top.ToString(CultureInfo.InvariantCulture)
			UserSettings.Instance.FormPositionLeft = _
				Me.Left.ToString(CultureInfo.InvariantCulture)
			UserSettings.Instance.FormPositionHeight = _
				Me.Height.ToString(CultureInfo.InvariantCulture)
			UserSettings.Instance.FormPositionWidth = _
				Me.Width.ToString(CultureInfo.InvariantCulture)
			UserSettings.Instance.FormPositionWindowState = CStr(0)
		End If
		If Me.WindowState.ToString = "Maximized" Then
			UserSettings.Instance.FormPositionWindowState = CStr(2)
		End If
	End Sub
	
	#End Region
End Class
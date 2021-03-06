﻿'******************************************************************************
'* PDFKeeper -- Open Source PDF Document Storage Solution
'* Copyright (C) 2009-2019 Robert F. Frasca
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
Public Class MainForm
    Implements IMainViewUnboundSettings, IMainViewToolStripState,  _
        IMainViewSearch, IMainViewSelectedDocument, IMainViewUpload
    Private unboundSettingsPresenter As MainViewUnboundSettingsPresenter
    Private toolStripStatePresenter As MainViewToolStripStatePresenter
    Private searchPresenter As MainViewSearchPresenter
    Private documentDataPresenter As MainViewSelectedDocumentPresenter
    Private uploadPresenter As MainViewUploadPresenter
    Private refreshFlag As Boolean
    Private textToSaveAsOrPrint As String
    Private m_DocumentNotesChanged As String
    ' Message that is sent when the contents of the clipboard have changed.
    Private Const WM_CLIPBOARDUPDATE As Integer = &H31D

    Public Sub New()
        InitializeComponent()
        unboundSettingsPresenter = New MainViewUnboundSettingsPresenter(Me)
        toolStripStatePresenter = New MainViewToolStripStatePresenter(Me)
        searchPresenter = New MainViewSearchPresenter(Me)
        documentDataPresenter = New MainViewSelectedDocumentPresenter(Me)
        uploadPresenter = New MainViewUploadPresenter(Me)
        HelpProvider.HelpNamespace = HelpProviderHelper.HelpFile
        AutoUpdaterHelper.StartUpdater()    ' Also called on a timer every 30 minutes.
    End Sub

#Region "Form events and protected and private methods"
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        unboundSettingsPresenter.MainViewLoad()
        SearchStringComboBox.Select()
        NativeMethods.AddClipboardFormatListener(Me.Handle)
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If IsSaveEnabled() Then
            If documentDataPresenter.MainViewClosing = False Then
                e.Cancel = True
            End If
        End If
        unboundSettingsPresenter.MainViewClosing()
        NativeMethods.RemoveClipboardFormatListener(Me.Handle)
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        MyBase.WndProc(m)
        If m.Msg = WM_CLIPBOARDUPDATE Then
            If My.Computer.Clipboard.ContainsText Then
                If NotesTextBox.Focused Then
                    toolStripStatePresenter.SetPasteState(True)
                End If
            End If
        End If
    End Sub

    Private Sub UploadTimer_Tick(sender As Object, e As EventArgs) Handles UploadTimer.Tick
        If UploadController.UploadRunning = False Then
            Dim uploadThread As New Threading.Thread(AddressOf uploadPresenter.UploadTimerTick)
            uploadThread.Start()
        End If
    End Sub

    Private Sub AutoUpdateCheckTimer_Tick(sender As Object, e As EventArgs) Handles AutoUpdateCheckTimer.Tick
        AutoUpdaterHelper.StartUpdater()
    End Sub

    Private Function IsSaveEnabled() As Boolean
        If FileSaveToolStripMenuItem.Enabled Then
            RightTabControl.SelectedIndex = 0
            NotesTextBox.Select()
            Return True
        Else
            Return False
        End If
    End Function
#End Region

#Region "Main ToolStrip events"
    Private Sub ToolStrip_VisibleChanged(sender As Object, e As EventArgs) Handles ToolStrip.VisibleChanged
        ViewToolbarToolStripMenuItem.Checked = ToolStrip.Visible
    End Sub
#End Region

#Region "File ToolStrip events"
    Private Sub FileNewToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileNewToolStripMenuItem.Click, _
                                                                                         FileNewToolStripButton.Click
        Dim command As ICommand = New FileNewToolStripCommand
        command.Execute()
    End Sub

    Private Sub FileOpenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileOpenToolStripMenuItem.Click, _
                                                                                          FileOpenToolStripButton.Click
        Me.Cursor = Cursors.WaitCursor
        Dim command As ICommand = New FileOpenToolStripCommand(DocumentId)
        command.Execute()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub FileSaveToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileSaveToolStripMenuItem.Click, _
                                                                                          FileSaveToolStripButton.Click
        Me.Cursor = Cursors.WaitCursor
        documentDataPresenter.FileSaveToolStripMenuItemClick()
        Me.Cursor = Cursors.Default
        NotesTextBox_TextChanged(Me, Nothing)
        TextBoxScrollToEnd(NotesTextBox)
    End Sub

    Private Sub FileSaveAsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileSaveAsToolStripMenuItem.Click
        Dim command As ICommand = New FileSaveAsToolStripCommand(DocumentId, textToSaveAsOrPrint)
        command.Execute()
    End Sub

    Private Sub FilePrintToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FilePrintToolStripMenuItem.Click, _
                                                                                           FilePrintToolStripButton.Click
        Dim command As ICommand = New FilePrintToolStripCommand(textToSaveAsOrPrint, False)
        command.Execute()
    End Sub

    Private Sub FilePrintPreviewToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FilePrintPreviewToolStripMenuItem.Click
        Dim command As ICommand = New FilePrintToolStripCommand(textToSaveAsOrPrint, True, Me.Size)
        command.Execute()
    End Sub

    Private Sub FileSelectAllToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileSelectAllToolStripMenuItem.Click
        Dim command As ICommand = New FileSelectToolStripCommand(SearchResultsDataGridView, Enums.SelectionState.SelectAll)
        command.Execute()
    End Sub

    Private Sub FileSelectNoneToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileSelectNoneToolStripMenuItem.Click
        Dim command As ICommand = New FileSelectToolStripCommand(SearchResultsDataGridView, Enums.SelectionState.DeselectAll)
        command.Execute()
    End Sub

    Private Sub FileDeleteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileDeleteToolStripMenuItem.Click, _
                                                                                            FileDeleteToolStripButton.Click
        Dim command As ICommand = New FileDeleteToolStripCommand(Me, searchPresenter)
        command.Execute()
    End Sub

    Private Sub FileExportToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileExportToolStripMenuItem.Click
        Dim command As ICommand = New FileExportToolStripCommand(Me, searchPresenter)
        command.Execute()
    End Sub

    Private Sub FileExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FileExitToolStripMenuItem.Click
        Me.Close()
    End Sub
#End Region

#Region "Edit ToolStrip events"
    Private Sub EditUndoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditUndoToolStripMenuItem.Click, _
                                                                                          EditUndoToolStripButton.Click
        NotesTextBox.Undo()
    End Sub

    Private Sub EditCutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditCutToolStripMenuItem.Click, _
                                                                                         EditCutToolStripButton.Click
        NotesTextBox.Cut()
    End Sub

    Private Sub EditCopyToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditCopyToolStripMenuItem.Click, _
                                                                                          EditCopyToolStripButton.Click
        GetTextBoxWithInputFocus.Copy()
    End Sub

    Private Sub EditPasteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditPasteToolStripMenuItem.Click, _
                                                                                           EditPasteToolStripButton.Click
        NotesTextBox.Paste()
    End Sub

    Private Sub EditSelectAllToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditSelectAllToolStripMenuItem.Click
        GetTextBoxWithInputFocus.SelectAll()
        toolStripStatePresenter.SetTextBoxTextSelectionState(GetTextBoxWithInputFocus.ReadOnly, _
                                                             GetTextBoxWithInputFocus.TextLength, _
                                                             GetTextBoxWithInputFocus.SelectionLength)
    End Sub

    Private Sub EditRestoreToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditRestoreToolStripMenuItem.Click, _
                                                                                             EditRestoreToolStripButton.Click
        RightTabControl.SelectedIndex = 0
        documentDataPresenter.EditRestoreToolStripMenuItemClick()
        NotesTextBox_TextChanged(Me, Nothing)
        TextBoxScrollToEnd(NotesTextBox)
    End Sub

    Private Sub EditDateTimeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditDateTimeToolStripMenuItem.Click, _
                                                                                              EditDateTimeToolStripButton.Click
        TextBoxScrollToEnd(NotesTextBox)
        documentDataPresenter.EditDateTimeToolStripMenuItemClick()
        TextBoxScrollToEnd(NotesTextBox)
    End Sub
#End Region

#Region "View ToolStrip events"
    Private Sub ViewToggleRightPanelToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ViewToggleRightPanelToolStripMenuItem.Click,
                                                                                                      ViewToggleRightPanelToolStripButton.Click
        Dim command As ICommand = New ViewToggleRightPanelToolStripCommand(SearchResultsDataGridView, SplitContainer)
        command.Execute()
    End Sub

    Private Sub ViewRefreshToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ViewRefreshToolStripMenuItem.Click,
                                                                                             ViewRefreshToolStripButton.Click
        refreshFlag = True
        SearchOptionsTabControl_Selected(Me, Nothing)
    End Sub

    Private Sub ViewSetPreviewImageResolutionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ViewSetPreviewImageResolutionToolStripMenuItem.Click,
                                                                                                               PreviewPictureBox.DoubleClick
        PreviewImageResolutionDialog.ShowDialog()
        Me.Cursor = Cursors.WaitCursor
        documentDataPresenter.ViewSetPreviewImageResolutionToolStripMenuItemClick()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub ViewToolBarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ViewToolbarToolStripMenuItem.Click
        Dim command As ICommand = New ViewToolBarToolStripCommand(ToolStrip, ViewToolbarToolStripMenuItem)
        command.Execute()
    End Sub

    Private Sub ViewStatusBarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ViewStatusBarToolStripMenuItem.Click
        Dim command As ICommand = New ViewStatusBarToolStripCommand(StatusStrip, ViewStatusBarToolStripMenuItem)
        command.Execute()
    End Sub
#End Region

#Region "Tools ToolStrip events"
    Private Sub ToolsOptionsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ToolsOptionsToolStripMenuItem.Click,
                                                                                              ToolsOptionsToolStripButton.Click
        OptionsDialog.ShowDialog()
    End Sub

    Private Sub ToolsUploadFoldersToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ToolsUploadFoldersToolStripMenuItem.Click,
                                                                                                    ToolsUploadFoldersToolStripButton.Click
        uploadPresenter.ToolsUploadFoldersToolStripMenuItemClick()
    End Sub
#End Region

#Region "Help ToolStrip events"
    Private Sub HelpContentsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HelpContentsToolStripMenuItem.Click,
                                                                                              HelpContentsToolStripButton.Click
        HelpProviderHelper.ShowHelp(Me, "Using PDFKeeper.html")
    End Sub

    Private Sub HelpAboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HelpAboutToolStripMenuItem.Click
        AboutBox.ShowDialog()
    End Sub
#End Region

#Region "Search Options events and private members"
    Private Sub SearchOptionsTabControl_Selected(sender As Object, e As TabControlEventArgs) Handles SearchOptionsTabControl.Selected
        toolStripStatePresenter.SetPreSearchState()
        SplitContainer.Panel2Collapsed = False
        If SearchOptionsTabControl.SelectedIndex = 0 Then
            DoSearch(True)
        ElseIf SearchOptionsTabControl.SelectedIndex = 1 Then
            Author1ComboBox_DropDownClosed(Me, Nothing)
        ElseIf SearchOptionsTabControl.SelectedIndex = 2 Then
            Subject1ComboBox_DropDownClosed(Me, Nothing)
        ElseIf SearchOptionsTabControl.SelectedIndex = 3 Then
            Subject2ComboBox_DropDownClosed(Me, Nothing)
        ElseIf SearchOptionsTabControl.SelectedIndex = 4 Then
            SearchDateTimePicker_ValueChanged(Me, Nothing)
        ElseIf SearchOptionsTabControl.SelectedIndex = 5 Then
            Me.Cursor = Cursors.WaitCursor
            If refreshFlag Then
                QueryAllDocumentsButton_Click(Me, Nothing)
            Else
                searchPresenter.SearchOptionsTabControlSelected()
            End If
            Me.Cursor = Cursors.Default
        End If
        refreshFlag = False
    End Sub

    Private Sub SearchStringComboBox_TextChanged(sender As Object, e As EventArgs) Handles SearchStringComboBox.TextChanged
        toolStripStatePresenter.SetPreSearchState()
        searchPresenter.SearchStringComboBoxTextChanged()
    End Sub

    Private Sub SearchStringComboBox_Enter(sender As Object, e As EventArgs) Handles SearchStringComboBox.Enter
        searchPresenter.SearchStringComboBoxEnter()
    End Sub

    Private Sub SearchButton_Click(sender As Object, e As EventArgs) Handles SearchButton.Click
        DoSearch(False)
    End Sub

    Private Sub DoSearch(ByVal useLastSearchString As Boolean)
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.DoSearch(useLastSearchString)
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub AuthorComboBox_DropDown(sender As Object, e As EventArgs) Handles Author1ComboBox.DropDown, Author2ComboBox.DropDown
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.AuthorComboBoxDropDown()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub Author1ComboBox_DropDownClosed(sender As Object, e As EventArgs) Handles Author1ComboBox.DropDownClosed
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.Author1ComboBoxDropDownClosed()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub Author1ComboBox_KeyDown(sender As Object, e As KeyEventArgs) Handles Author1ComboBox.KeyDown
        ' ComboBox will only drop down when the down arrow is pressed.
        If e.KeyCode = 40 Then
            Author1ComboBox.DroppedDown = True
        End If
    End Sub

    Private Sub Author1ComboBox_KeyUp(sender As Object, e As KeyEventArgs) Handles Author1ComboBox.KeyUp
        ' Pressing the up arrow when ComboBox has focus will select the previous Author in the
        ' collection when the drop down is in the closed position.  When drop down is in the
        ' open position, the up arrow will move selector up the list.
        If e.KeyCode = 38 Then
            If Author1ComboBox.DroppedDown = False Then
                Author1ComboBox_DropDownClosed(Me, Nothing)
            End If
        End If
    End Sub

    Private Sub Author1ComboBox_MouseWheel(sender As Object, e As MouseEventArgs) Handles Author1ComboBox.MouseWheel
        ' This will prevent mouse wheel scrolling while drop down is closed.
        If Not Author1ComboBox.DroppedDown Then
            Dim handledMouseEventArgs As HandledMouseEventArgs = DirectCast(e, HandledMouseEventArgs)
            handledMouseEventArgs.Handled = True
        End If
    End Sub

    Private Sub Subject1ComboBox_DropDown(sender As Object, e As EventArgs) Handles Subject1ComboBox.DropDown
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.Subject1ComboBoxDropDown()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub Subject1ComboBox_DropDownClosed(sender As Object, e As EventArgs) Handles Subject1ComboBox.DropDownClosed
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.Subject1ComboBoxDropDownClosed()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub Subject1ComboBox_KeyDown(sender As Object, e As KeyEventArgs) Handles Subject1ComboBox.KeyDown
        ' ComboBox will only drop down when the down arrow is pressed.
        If e.KeyCode = 40 Then
            Subject1ComboBox.DroppedDown = True
        End If
    End Sub

    Private Sub Subject1ComboBox_KeyUp(sender As Object, e As KeyEventArgs) Handles Subject1ComboBox.KeyUp
        ' Pressing the up arrow when ComboBox has focus will select the previous Subject in the
        ' collection when the drop down is in the closed position.  When drop down is in the
        ' open position, the up arrow will move selector up the list.
        If e.KeyCode = 38 Then
            If Subject1ComboBox.DroppedDown = False Then
                Subject1ComboBox_DropDownClosed(Me, Nothing)
            End If
        End If
    End Sub

    Private Sub Subject1ComboBox_MouseWheel(sender As Object, e As MouseEventArgs) Handles Subject1ComboBox.MouseWheel
        ' This will prevent mouse wheel scrolling while drop down is closed.
        If Not Subject1ComboBox.DroppedDown Then
            Dim handledMouseEventArgs As HandledMouseEventArgs = DirectCast(e, HandledMouseEventArgs)
            handledMouseEventArgs.Handled = True
        End If
    End Sub

    Private Sub Author2ComboBox_DropDownClosed(sender As Object, e As EventArgs) Handles Author2ComboBox.DropDownClosed
        searchPresenter.Author2ComboBoxDropDownClosed()
        Subject2ComboBox_DropDownClosed(Me, Nothing)
    End Sub

    Private Sub Author2ComboBox_KeyDown(sender As Object, e As KeyEventArgs) Handles Author2ComboBox.KeyDown
        ' ComboBox will only drop down when the down arrow is pressed.
        If e.KeyCode = 40 Then
            Author2ComboBox.DroppedDown = True
        End If
    End Sub

    Private Sub Author2ComboBox_KeyUp(sender As Object, e As KeyEventArgs) Handles Author2ComboBox.KeyUp
        ' Pressing the up arrow when ComboBox has focus will select the previous Author in the
        ' collection when the drop down is in the closed position.  When drop down is in the
        ' open position, the up arrow will move selector up the list.
        If e.KeyCode = 38 Then
            If Author2ComboBox.DroppedDown = False Then
                Author2ComboBox_DropDownClosed(Me, Nothing)
            End If
        End If
    End Sub

    Private Sub Author2ComboBox_MouseWheel(sender As Object, e As MouseEventArgs) Handles Author2ComboBox.MouseWheel
        ' This will prevent mouse wheel scrolling while drop down is closed.
        If Not Author2ComboBox.DroppedDown Then
            Dim handledMouseEventArgs As HandledMouseEventArgs = DirectCast(e, HandledMouseEventArgs)
            handledMouseEventArgs.Handled = True
        End If
    End Sub

    Private Sub Subject2ComboBox_DropDown(sender As Object, e As EventArgs) Handles Subject2ComboBox.DropDown
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.Subject2ComboBoxDropDown()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub Subject2ComboBox_DropDownClosed(sender As Object, e As EventArgs) Handles Subject2ComboBox.DropDownClosed
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.Subject2ComboBoxDropDownClosed()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub Subject2ComboBox_KeyDown(sender As Object, e As KeyEventArgs) Handles Subject2ComboBox.KeyDown
        ' ComboBox will only drop down when the down arrow is pressed.
        If e.KeyCode = 40 Then
            Subject2ComboBox.DroppedDown = True
        End If
    End Sub

    Private Sub Subject2ComboBox_KeyUp(sender As Object, e As KeyEventArgs) Handles Subject2ComboBox.KeyUp
        ' Pressing the up arrow when ComboBox has focus will select the previous Subject in the
        ' collection when the drop down is in the closed position.  When drop down is in the
        ' open position, the up arrow will move selector up the list.
        If e.KeyCode = 38 Then
            If Subject2ComboBox.DroppedDown = False Then
                Subject2ComboBox_DropDownClosed(Me, Nothing)
            End If
        End If
    End Sub

    Private Sub Subject2ComboBox_MouseWheel(sender As Object, e As MouseEventArgs) Handles Subject2ComboBox.MouseWheel
        ' This will prevent mouse wheel scrolling while drop down is closed.
        If Not Subject2ComboBox.DroppedDown Then
            Dim handledMouseEventArgs As HandledMouseEventArgs = DirectCast(e, HandledMouseEventArgs)
            handledMouseEventArgs.Handled = True
        End If
    End Sub

    Private Sub SearchDateTimePicker_ValueChanged(sender As Object, e As EventArgs) Handles SearchDateTimePicker.ValueChanged
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.SearchDateTimePickerValueChanged()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub QueryAllDocumentsButton_Click(sender As Object, e As EventArgs) Handles QueryAllDocumentsButton.Click
        Me.Cursor = Cursors.WaitCursor
        searchPresenter.QueryAllDocumentsButtonClick()
        Me.Cursor = Cursors.Default
    End Sub
#End Region

#Region "Search Results DataGridView events"
    Private Sub SearchResultsDataGridView_DataSourceChanged(sender As Object, e As EventArgs) Handles SearchResultsDataGridView.DataSourceChanged
        With SearchResultsDataGridView
            .Columns(0).AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            .Columns(1).HeaderCell.Value = My.Resources.ID
            .Columns(1).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns(1).ReadOnly = True
            .Columns(2).HeaderCell.Value = My.Resources.Title
            .Columns(2).ReadOnly = True
            .Columns(3).HeaderCell.Value = My.Resources.Author
            .Columns(3).ReadOnly = True
            .Columns(4).HeaderCell.Value = My.Resources.Subject
            .Columns(4).ReadOnly = True
            .Columns(5).HeaderCell.Value = My.Resources.Added
            .Columns(5).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            .Columns(5).ReadOnly = True
        End With
        If SearchResultsDataGridView.RowCount > 0 Then
            SearchResultsDataGridView.Enabled = True
            SearchResultsDataGridView.Columns(5).AutoSizeMode = _
                DataGridViewAutoSizeColumnMode.DisplayedCells
            If SearchResultsDataGridView.Columns.GetColumnsWidth(DataGridViewElementStates.Displayed) > _
                SearchResultsDataGridView.Size.Width Then
                SplitContainer.Panel2Collapsed = True
            End If
            If SearchResultsDataGridView.Columns(5).Displayed = True Then
                SearchResultsDataGridView.Columns(5).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            End If
            SearchResultsDataGridView.Columns(5).MinimumWidth = SearchResultsDataGridView.Columns(5).FillWeight + 20
        End If
        toolStripStatePresenter.SetPostSearchState()
        SearchResultsDataGridView.Focus()
    End Sub

    Private Sub SearchResultsDataGridView_Sorted(sender As Object, e As EventArgs) Handles SearchResultsDataGridView.Sorted
        searchPresenter.SearchResultsDataGridViewSorted()
    End Sub

    Private Sub SearchResultsDataGridView_RowsAdded(sender As Object, e As DataGridViewRowsAddedEventArgs) Handles SearchResultsDataGridView.RowsAdded
        searchPresenter.SearchResultsDataGridViewRowsAdded()
        toolStripStatePresenter.SetSearchResultsViewRowCountChangedState(SearchResultsDataGridView.RowCount, _
                                                                       SplitContainer.Panel2Collapsed)
    End Sub

    Private Sub SearchResultsDataGridView_RowsRemoved(sender As Object, e As DataGridViewRowsRemovedEventArgs) Handles SearchResultsDataGridView.RowsRemoved
        searchPresenter.SearchResultsDataGridViewRowsRemoved()
        toolStripStatePresenter.SetSearchResultsViewRowCountChangedState(SearchResultsDataGridView.RowCount, _
                                                                       SplitContainer.Panel2Collapsed)
        toolStripStatePresenter.SetSearchResultsSelectedState(GetSelectedSearchResultsIds.Count)
    End Sub

    Private Sub SearchResultsDataGridView_SelectionChanged(sender As Object, e As EventArgs) Handles SearchResultsDataGridView.SelectionChanged
        Me.Cursor = Cursors.WaitCursor
        documentDataPresenter.SearchResultsDataGridViewSelectionChanged()
        Me.Cursor = Cursors.Default
    End Sub

    Private Sub SearchResultsDataGridView_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles SearchResultsDataGridView.CurrentCellDirtyStateChanged
        If SearchResultsDataGridView.IsCurrentCellDirty Then
            SearchResultsDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub SearchResultsDataGridView_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles SearchResultsDataGridView.CellValueChanged
        toolStripStatePresenter.SetSearchResultsSelectedState(GetSelectedSearchResultsIds.Count)
    End Sub

    Private Sub SearchResultsDataGridView_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles SearchResultsDataGridView.CellDoubleClick
        FileOpenToolStripMenuItem_Click(Me, Nothing)
    End Sub

    Private Function GetSelectedSearchResultsIds() As GenericList(Of String)
        Dim results As New GenericList(Of String)
        For Each row As DataGridViewRow In SearchResultsDataGridView.Rows
            If row.Cells(0).Value = True Then
                results.Add(row.Cells(1).Value)
            End If
        Next
        Return results
    End Function
#End Region

#Region "Document Data events and private methods"
    Private Sub NotesTextBox_Enter(sender As Object, e As EventArgs) Handles NotesTextBox.Enter, _
                                                                             NotesTextBox.GotFocus
        toolStripStatePresenter.SetTextBoxEnterState(NotesTextBox.ReadOnly, _
                                                     NotesTextBox.TextLength)
        If NotesTextBox.TextLength > 0 Then
            textToSaveAsOrPrint = NotesTextBox.Text
            toolStripStatePresenter.SetTextBoxPrintableState(True)
        End If
        If My.Computer.Clipboard.ContainsText Then
            toolStripStatePresenter.SetPasteState(True)
        End If
        toolStripStatePresenter.SetNotesTextBoxChangedState(m_DocumentNotesChanged, _
                                                            NotesTextBox.CanUndo)
    End Sub

    Private Sub NotesTextBox_MouseUp(sender As Object, e As MouseEventArgs) Handles NotesTextBox.MouseUp
        toolStripStatePresenter.SetTextBoxTextSelectionState(NotesTextBox.ReadOnly, _
                                                             NotesTextBox.TextLength, _
                                                             NotesTextBox.SelectionLength)
    End Sub

    Private Sub NotesTextBox_KeyPress(sender As Object, e As KeyPressEventArgs) Handles NotesTextBox.KeyPress
        toolStripStatePresenter.SetTextBoxTextSelectionState(NotesTextBox.ReadOnly, _
                                                             NotesTextBox.TextLength, _
                                                             NotesTextBox.SelectionLength)
    End Sub

    Private Sub NotesTextBox_TextChanged(sender As Object, e As EventArgs) Handles NotesTextBox.TextChanged
        documentDataPresenter.NotesTextBoxTextChanged()
        toolStripStatePresenter.SetTextBoxTextSelectionState(NotesTextBox.ReadOnly, _
                                                             NotesTextBox.TextLength, _
                                                             NotesTextBox.SelectionLength)
    End Sub

    Private Sub NotesTextBox_Leave(sender As Object, e As EventArgs) Handles NotesTextBox.Leave
        textToSaveAsOrPrint = Nothing
        toolStripStatePresenter.SetTextBoxLeaveState()
        toolStripStatePresenter.SetTextBoxPrintableState(False)
    End Sub

    Private Sub KeywordsTextBox_Enter(sender As Object, e As EventArgs) Handles KeywordsTextBox.Enter
        toolStripStatePresenter.SetTextBoxEnterState(KeywordsTextBox.ReadOnly, _
                                                     KeywordsTextBox.TextLength)
        toolStripStatePresenter.SetTextBoxPrintableState(False)
    End Sub

    Private Sub KeywordsTextBox_MouseUp(sender As Object, e As MouseEventArgs) Handles KeywordsTextBox.MouseUp
        toolStripStatePresenter.SetTextBoxTextSelectionState(KeywordsTextBox.ReadOnly, _
                                                             KeywordsTextBox.TextLength, _
                                                             KeywordsTextBox.SelectionLength)
    End Sub

    Private Sub KeywordsTextBox_KeyPress(sender As Object, e As KeyPressEventArgs) Handles KeywordsTextBox.KeyPress
        toolStripStatePresenter.SetTextBoxTextSelectionState(KeywordsTextBox.ReadOnly, _
                                                             KeywordsTextBox.TextLength, _
                                                             KeywordsTextBox.SelectionLength)
    End Sub

    Private Sub KeywordsTextBox_Leave(sender As Object, e As EventArgs) Handles KeywordsTextBox.Leave
        toolStripStatePresenter.SetTextBoxLeaveState()
    End Sub

    Private Sub TextTextBox_Enter(sender As Object, e As EventArgs) Handles TextTextBox.Enter
        toolStripStatePresenter.SetTextBoxEnterState(TextTextBox.ReadOnly, _
                                                     TextTextBox.TextLength)
        If TextTextBox.TextLength > 0 Then
            textToSaveAsOrPrint = TextTextBox.Text
            toolStripStatePresenter.SetTextBoxPrintableState(True)
        End If
    End Sub

    Private Sub TextTextBox_MouseUp(sender As Object, e As MouseEventArgs) Handles TextTextBox.MouseUp
        toolStripStatePresenter.SetTextBoxTextSelectionState(TextTextBox.ReadOnly, _
                                                             TextTextBox.TextLength, _
                                                             TextTextBox.SelectionLength)
    End Sub

    Private Sub TextTextBox_KeyPress(sender As Object, e As KeyPressEventArgs) Handles TextTextBox.KeyPress
        toolStripStatePresenter.SetTextBoxTextSelectionState(TextTextBox.ReadOnly, _
                                                             TextTextBox.TextLength, _
                                                             TextTextBox.SelectionLength)
    End Sub

    Private Sub TextTextBox_Leave(sender As Object, e As EventArgs) Handles TextTextBox.Leave
        textToSaveAsOrPrint = Nothing
        toolStripStatePresenter.SetTextBoxLeaveState()
        toolStripStatePresenter.SetTextBoxPrintableState(False)
    End Sub

    Private Function GetTextBoxWithInputFocus() As TextBox
        Dim textBox As TextBox = Nothing
        If NotesTextBox.Focused Then
            textBox = NotesTextBox
        ElseIf KeywordsTextBox.Focused Then
            textBox = KeywordsTextBox
        ElseIf TextTextBox.Focused Then
            textBox = TextTextBox
        End If
        Return textBox
    End Function

    <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", _
        "CA1822:MarkMembersAsStatic")> _
    Private Sub TextBoxScrollToEnd(ByVal textBoxControl As TextBox)
        textBoxControl.Select()
        textBoxControl.Select(textBoxControl.Text.Length, 0)
        textBoxControl.ScrollToCaret()
    End Sub
#End Region

#Region "StatusStrip events"
    Private Sub StatusStrip_VisibleChanged(sender As Object, e As EventArgs) Handles StatusStrip.VisibleChanged
        ViewStatusBarToolStripMenuItem.Checked = StatusStrip.Visible
    End Sub

    Private Sub UploadFolderErrorToolStripStatusLabel_Click(sender As Object, e As EventArgs) Handles UploadFolderErrorToolStripStatusLabel.Click
        UploadDirectory.Explore()
    End Sub

    Private Sub UploadStagingFolderErrorToolStripStatusLabel_Click(sender As Object, e As EventArgs) Handles UploadStagingFolderErrorToolStripStatusLabel.Click
        UploadStagingDirectory.Explore()
    End Sub
#End Region

#Region "IMainViewUnboundSettings members get/set by MainViewUnboundSettingsPresenter"
    Public Property ViewLocation As Point Implements IMainViewUnboundSettings.ViewLocation
        Get
            Return Me.Location
        End Get
        Set(value As Point)
            Me.Location = value
        End Set
    End Property

    Public Property ViewSize As Size Implements IMainViewUnboundSettings.ViewSize
        Get
            Return Me.Size
        End Get
        Set(value As Size)
            Me.Size = value
        End Set
    End Property

    Public ReadOnly Property ViewRestoreBoundsSize As Size Implements IMainViewUnboundSettings.ViewRestoreBoundsSize
        Get
            Return Me.RestoreBounds.Size
        End Get
    End Property

    Public Property ViewWindowsState As FormWindowState Implements IMainViewUnboundSettings.ViewWindowsState
        Get
            Return Me.WindowState
        End Get
        Set(value As FormWindowState)
            Me.WindowState = value
        End Set
    End Property

    Public Property ViewSplitterDistance As Integer Implements IMainViewUnboundSettings.ViewSplitterDistance
        Get
            Return SplitContainer.SplitterDistance
        End Get
        Set(value As Integer)
            SplitContainer.SplitterDistance = value
        End Set
    End Property
#End Region

#Region "IMainViewToolStripState member called by MainViewToolStripStateManager"
    Public Sub SetToolStripItemsState(shortName As String, enabled As Boolean) Implements IMainViewToolStripState.SetToolStripItemsState
        Dim menuStripResults = MenuStrip.Items.Find(shortName & "ToolStripMenuItem", True).ToList
        Dim toolBarResults = ToolStrip.Items.Find(shortName & "ToolStripButton", True).ToList
        For Each item As ToolStripItem In menuStripResults
            item.Enabled = enabled
        Next
        For Each item As ToolStripItem In toolBarResults
            item.Enabled = enabled
        Next
    End Sub
#End Region

#Region "IMainViewSearch members get/set by MainViewSearchPresenter"
    Public ReadOnly Property SearchOptionsSelectedIndex As Integer Implements IMainViewSearch.SearchOptionsSelectedIndex
        Get
            Return SearchOptionsTabControl.SelectedIndex
        End Get
    End Property

    Public Property SearchString As String Implements IMainViewSearch.SearchString
        Get
            Return SearchStringComboBox.Text
        End Get
        Set(value As String)
            SearchStringComboBox.Text = value
        End Set
    End Property

    Public Property SearchStringErrorProviderMessage As String Implements IMainViewSearch.SearchStringErrorProviderMessage
        Get
            Return SearchStringErrorProvider.GetError(SearchStringComboBox)
        End Get
        Set(value As String)
            If value Is Nothing Then
                SearchStringErrorProvider.Clear()
            Else
                SearchStringErrorProvider.SetError(SearchStringComboBox, value)
            End If
        End Set
    End Property

    Public Property SearchStringHistory As Object Implements IMainViewSearch.SearchStringHistory
        Get
            Return SearchStringComboBox.Items
        End Get
        Set(value As Object)
            SearchStringComboBox.Items.Clear()  ' Need to clear twice to work around duplicates
            SearchStringComboBox.Items.Clear()  ' from being displayed in the drop down.
            SearchStringComboBox.Items.AddRange(value)
        End Set
    End Property

    Public Property SearchEnabled As Boolean Implements IMainViewSearch.SearchEnabled
        Get
            Return SearchButton.Enabled
        End Get
        Set(value As Boolean)
            SearchButton.Enabled = value
        End Set
    End Property

    Public Property Authors1 As DataTable Implements IMainViewSearch.Authors1
        Get
            Return Author1ComboBox.DataSource
        End Get
        Set(value As DataTable)
            Author1ComboBox.DataSource = value
            Author1ComboBox.DisplayMember = "doc_author"
        End Set
    End Property

    Public ReadOnly Property Author1 As String Implements IMainViewSearch.Author1
        Get
            If Not Author1ComboBox.SelectedItem Is Nothing Then
                Return Convert.ToString(Author1ComboBox.SelectedItem("doc_author"), _
                                        CultureInfo.CurrentCulture)
            End If
            Return Nothing
        End Get
    End Property

    Public Property Subjects1 As DataTable Implements IMainViewSearch.Subjects1
        Get
            Return Subject1ComboBox.DataSource
        End Get
        Set(value As DataTable)
            Subject1ComboBox.DataSource = value
            Subject1ComboBox.DisplayMember = "doc_subject"
        End Set
    End Property

    Public ReadOnly Property Subject1 As String Implements IMainViewSearch.Subject1
        Get
            If Not Subject1ComboBox.SelectedItem Is Nothing Then
                Return Convert.ToString(Subject1ComboBox.SelectedItem("doc_subject"), _
                                        CultureInfo.CurrentCulture)
            End If
            Return Nothing
        End Get
    End Property

    Public Property Authors2 As DataTable Implements IMainViewSearch.Authors2
        Get
            Return Author2ComboBox.DataSource
        End Get
        Set(value As DataTable)
            Author2ComboBox.DataSource = value
            Author2ComboBox.DisplayMember = "doc_author"
        End Set
    End Property

    Public ReadOnly Property Author2 As String Implements IMainViewSearch.Author2
        Get
            If Not Author2ComboBox.SelectedItem Is Nothing Then
                Return Convert.ToString(Author2ComboBox.SelectedItem("doc_author"), _
                                        CultureInfo.CurrentCulture)
            End If
            Return Nothing
        End Get
    End Property

    Public Property Subjects2 As DataTable Implements IMainViewSearch.Subjects2
        Get
            Return Subject2ComboBox.DataSource
        End Get
        Set(value As DataTable)
            Subject2ComboBox.DataSource = value
            Subject2ComboBox.DisplayMember = "doc_subject"
        End Set
    End Property

    Public ReadOnly Property Subject2 As String Implements IMainViewSearch.Subject2
        Get
            If Not Subject2ComboBox.SelectedItem Is Nothing Then
                Return Convert.ToString(Subject2ComboBox.SelectedItem("doc_subject"), _
                                        CultureInfo.CurrentCulture)
            End If
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property SearchDate As String Implements IMainViewSearch.SearchDate
        Get
            Return SearchDateTimePicker.Value.ToString("yyyy-MM-dd", _
                                                       CultureInfo.CurrentCulture)
        End Get
    End Property

    Public Property DBDocumentRecordsCountMessage As String Implements IMainViewSearch.DBDocumentRecordsCountMessage
        Get
            Return DBDocumentRecordsCountLabel.Text
        End Get
        Set(value As String)
            DBDocumentRecordsCountLabel.Text = value
        End Set
    End Property

    Public Property QueryAllDocumentsVisible As Boolean Implements IMainViewSearch.QueryAllDocumentsVisible
        Get
            Return QueryAllDocumentsButton.Visible
        End Get
        Set(value As Boolean)
            QueryAllDocumentsButton.Visible = value
        End Set
    End Property

    Public Property QueryAllDocumentsEnabled As Boolean Implements IMainViewSearch.QueryAllDocumentsEnabled
        Get
            Return QueryAllDocumentsButton.Enabled
        End Get
        Set(value As Boolean)
            QueryAllDocumentsButton.Enabled = value
        End Set
    End Property

    Public Property SearchResultsFullView As Boolean Implements IMainViewSearch.SearchResultsFullView
        Get
            Return SplitContainer.Panel2Collapsed
        End Get
        Set(value As Boolean)
            SplitContainer.Panel2Collapsed = value
        End Set
    End Property

    Public Property SearchResultsViewEnabled As Boolean Implements IMainViewSearch.SearchResultsViewEnabled
        Get
            Return SearchResultsDataGridView.Enabled
        End Get
        Set(value As Boolean)
            SearchResultsDataGridView.Enabled = value
        End Set
    End Property

    Public Property SearchResults As DataTable Implements IMainViewSearch.SearchResults
        Get
            Return SearchResultsDataGridView.DataSource
        End Get
        Set(value As DataTable)
            SearchResultsDataGridView.DataSource = value
        End Set
    End Property

    Public ReadOnly Property SearchResultsSortedColumn As DataGridViewColumn Implements IMainViewSearch.SearchResultsSortedColumn
        Get
            Return SearchResultsDataGridView.SortedColumn
        End Get
    End Property

    Public ReadOnly Property SearchResultsSortOrder As SortOrder Implements IMainViewSearch.SearchResultsSortOrder
        Get
            Return SearchResultsDataGridView.SortOrder
        End Get
    End Property

    Public ReadOnly Property SearchResultsViewRowCount As Integer Implements IMainViewSearch.SearchResultsViewRowCount
        Get
            Return SearchResultsDataGridView.RowCount
        End Get
    End Property

    Public ReadOnly Property SelectedSearchResultsIds As Object Implements IMainViewSearch.SelectedSearchResultsIds
        Get
            Return GetSelectedSearchResultsIds.ToArray(True)
        End Get
    End Property

    Public ReadOnly Property SelectedSearchResultsIdsCount As Integer Implements IMainViewSearch.SelectedSearchResultsIdsCount
        Get
            Return GetSelectedSearchResultsIds.Count
        End Get
    End Property

    Public Property TotalRecordsCountLabel As String Implements IMainViewSearch.TotalRecordsCountLabel
        Get
            Return TotalRecordsCountToolStripStatusLabel.Text
        End Get
        Set(value As String)
            TotalRecordsCountToolStripStatusLabel.Text = value
        End Set
    End Property

    Public Property DeleteExportProgressVisible As Boolean Implements IMainViewSearch.DeleteExportProgressVisible
        Get
            Return DeleteExportToolStripProgressBar.Visible
        End Get
        Set(value As Boolean)
            DeleteExportToolStripProgressBar.Visible = value    ' Must be performed first.
            If value Then
                DeleteExportToolStripProgressBar.Value = 0
            End If
        End Set
    End Property

    Public Property DeleteExportProgressMaximum As Integer Implements IMainViewSearch.DeleteExportProgressMaximum
        Get
            Return DeleteExportToolStripProgressBar.Maximum
        End Get
        Set(value As Integer)
            DeleteExportToolStripProgressBar.Maximum = value
        End Set
    End Property
#End Region

#Region "IMainViewSearch members called by MainViewSearchPresenter"
    Public Sub ClearSubject2Selection() Implements IMainViewSearch.ClearSubject2Selection
        Subject2ComboBox.SelectedItem = Nothing
        SearchOptionsTabControl_Selected(Me, Nothing)
    End Sub

    Public Sub ResetSearchResultsViewHeader() Implements IMainViewSearch.ResetSearchResultsViewHeader
        SearchResultsDataGridView.Columns(0).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    End Sub

    Public Sub SortSearchResults(sortColumnIndex As Integer, sortDirection As ListSortDirection) Implements IMainViewSearch.SortSearchResults
        SearchResultsDataGridView.Sort(SearchResultsDataGridView.Columns(sortColumnIndex), sortDirection)
    End Sub

    Public Sub SelectSearchResultsViewLastRow() Implements IMainViewSearch.SelectSearchResultsViewLastRow
        SearchResultsDataGridView.Rows(SearchResultsDataGridView.Rows.Count - 1).Selected = True
        SearchResultsDataGridView.FirstDisplayedScrollingRowIndex = SearchResultsDataGridView.RowCount - 1
    End Sub

    Public Sub RefreshSearchResultsView() Implements IMainViewSearch.RefreshSearchResultsView
        ViewRefreshToolStripMenuItem_Click(Me, Nothing)
    End Sub

    Public Sub SelectNoneInSearchResultsView() Implements IMainViewSearch.SelectNoneInSearchResultsView
        FileSelectNoneToolStripMenuItem_Click(Me, Nothing)
    End Sub

    Public Sub DeleteExportProgressPerformStep() Implements IMainViewSearch.DeleteExportProgressPerformStep
        DeleteExportToolStripProgressBar.PerformStep()
    End Sub
#End Region

#Region "IMainViewSelectedDocument members get/set by MainViewSelectedDocumentPresenter"
    Public ReadOnly Property DocumentId As Integer Implements IMainViewSelectedDocument.DocumentId
        Get
            ' Filter out null selections that occur when the DataGridView is filled.
            If SearchResultsDataGridView.SelectedRows.Count > 0 Then
                Return SearchResultsDataGridView.SelectedRows(0).Cells(1).Value
            End If
            Return Nothing  ' Will return 0 for a null selection.
        End Get
    End Property

    Public Property RightTabControlEnabled As Boolean Implements IMainViewSelectedDocument.RightTabControlEnabled
        Get
            Return RightTabControl.Enabled
        End Get
        Set(value As Boolean)
            toolStripStatePresenter.SetDocumentSelectedState(value)
            RightTabControl.Enabled = value
        End Set
    End Property

    Public Property DocumentNotes As String Implements IMainViewSelectedDocument.DocumentNotes
        Get
            Return NotesTextBox.Text
        End Get
        Set(value As String)
            NotesTextBox.Text = value
        End Set
    End Property

    Public Property DocumentNotesChanged As Boolean Implements IMainViewSelectedDocument.DocumentNotesChanged
        Get
            Return m_DocumentNotesChanged
        End Get
        Set(value As Boolean)
            m_DocumentNotesChanged = value
            Dim controlEnabled As Boolean = False
            If m_DocumentNotesChanged = False Then
                controlEnabled = True
                toolStripStatePresenter.SetSearchResultsSelectedState(GetSelectedSearchResultsIds.Count)
            End If
            ' The "If" check is needed to prevent user from having to check/uncheck checkbox in
            ' SearchResultsDataGridView when Document Notes length > 0.
            If NotesTextBox.Focused Then
                SearchOptionsTabControl.Enabled = controlEnabled
                SearchResultsDataGridView.Enabled = controlEnabled
            End If
            toolStripStatePresenter.SetNotesTextBoxChangedState(m_DocumentNotesChanged, _
                                                                NotesTextBox.CanUndo)
        End Set
    End Property

    Public Property DocumentKeywords As String Implements IMainViewSelectedDocument.DocumentKeywords
        Get
            Return KeywordsTextBox.Text
        End Get
        Set(value As String)
            KeywordsTextBox.Text = value
        End Set
    End Property

    Public Property DocumentPreview As Image Implements IMainViewSelectedDocument.DocumentPreview
        Get
            Return PreviewPictureBox.Image
        End Get
        Set(value As Image)
            ' Dispose the PictureBox first to release memory before loading image.
            If Not PreviewPictureBox.Image Is Nothing Then
                PreviewPictureBox.Image.Dispose()
            End If
            PreviewPictureBox.Image = value
        End Set
    End Property

    Public Property DocumentText As String Implements IMainViewSelectedDocument.DocumentText
        Get
            Return TextTextBox.Text
        End Get
        Set(value As String)
            TextTextBox.Text = value
            If TextTextBox.Text.Length > 0 Then
                TextTextBox.Text = TextTextBox.Text.Trim
            End If
        End Set
    End Property
#End Region

#Region "IMainViewUpload members get/set by MainViewUploadPresenter"
    Public Property UploadRunningVisible As Boolean Implements IMainViewUpload.UploadRunningVisible
        Get
            Return UploadRunningToolStripStatusLabel.Visible
        End Get
        Set(value As Boolean)
            UploadRunningToolStripStatusLabel.Visible = value
        End Set
    End Property

    Public Property UploadFolderErrorVisible As Boolean Implements IMainViewUpload.UploadFolderErrorVisible
        Get
            Return UploadFolderErrorToolStripStatusLabel.Visible
        End Get
        Set(value As Boolean)
            UploadFolderErrorToolStripStatusLabel.Visible = value
        End Set
    End Property

    Public Property UploadStagingFolderErrorVisible As Boolean Implements IMainViewUpload.UploadStagingFolderErrorVisible
        Get
            Return UploadStagingFolderErrorToolStripStatusLabel.Visible
        End Get
        Set(value As Boolean)
            UploadStagingFolderErrorToolStripStatusLabel.Visible = value
        End Set
    End Property
#End Region

End Class

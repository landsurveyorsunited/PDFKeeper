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
Public NotInheritable Class AutoUpdaterHelper
    Private Sub New()
        ' Required by Code Analysis.
    End Sub

    Private Shared ReadOnly Property ConfigUrl As String
        Get
            Return "https://raw.githubusercontent.com/rffrasca/PDFKeeper/master/config/PDFKeeper.AutoUpdater.config.xml"
        End Get
    End Property

    Public Shared Sub StartUpdater()
        AutoUpdaterDotNET.AutoUpdater.RunUpdateAsAdmin = False
        AutoUpdaterDotNET.AutoUpdater.Start(ConfigUrl)
    End Sub
End Class

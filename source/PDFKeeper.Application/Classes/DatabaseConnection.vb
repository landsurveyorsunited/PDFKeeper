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

Public Class DatabaseConnection
	Implements IDisposable
	Private isDisposed As Boolean
	Friend oraConnection As New OracleConnection
	
	''' <summary>
	''' This function will open a database connection.
	''' </summary>
	''' <returns>0 = Successful, 1 = Failed</returns>
	Public Function Open As Integer
		Try
			oraConnection.ConnectionString = _
				DBConnectionString.Instance.ConnectionString
			oraConnection.Open
			Return 0
  		Catch ex As OracleException
  			Dispose
  			ShowError(ex.Message.ToString())
			Return 1
  		End Try
	End Function
	
	''' <summary>
	''' This subroutine will close the active database connection and will
	''' dispose the database connection object.
	''' </summary>
	''' <param name="disposing"></param>
	Protected Overridable Sub Dispose(ByVal disposing As Boolean)
		If Not Me.isDisposed Then
			If disposing Then
				oraConnection.Close
				oraConnection.Dispose
				oraConnection = Nothing
			End If
		End If
		Me.isDisposed = True
	End Sub
	
	''' <summary>
	''' This subroutine will call the protected Dispose subroutine.  It is
	''' called by the consumer of the object when a database operation has
	''' completed.
	''' </summary>
	Public Sub Dispose() Implements IDisposable.Dispose
		Dispose(True)
		GC.SuppressFinalize(Me)
	End Sub
	
	''' <summary>
	''' This subroutine will call the protected Dispose subroutine.  It is
	''' called by the garbage collector, only if the consumer of the object
	''' does not call Dispose as it should.
	''' </summary>
	Protected Overrides Sub Finalize()
		Dispose(False)
	End Sub
End Class

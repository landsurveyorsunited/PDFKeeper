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

Public NotInheritable Class DatabaseConnectionString
	Private Shared _instance As DatabaseConnectionString = _
		New DatabaseConnectionString()
	
	Private _userName As String
	Private _password As SecureString
	Private _dataSource As String
	
	Public Shared ReadOnly Property Instance As DatabaseConnectionString
		Get
			Return _instance
		End Get
	End Property
		
	''' <summary>
	''' Gets or sets the User Name for the database connection string.
	''' </summary>
	Friend Property UserName As String
		Get
			Return _userName
		End Get
		Set(ByVal value As String)
			_userName = value
		End Set
	End Property
	
	''' <summary>
	''' Gets or sets the Password for the database connection string.
	''' </summary>
	Friend Property Password As SecureString
		Get
			Return _password
		End Get
		Set(ByVal value As SecureString)
			_password = value
		End Set
	End Property
	
	''' <summary>
	''' Gets or sets the Data Source for the database connection string.
	''' </summary>
	Friend Property DataSource As String
		Get
			Return _dataSource
		End Get
		Set(ByVal value As String)
			_dataSource = value
		End Set
	End Property
	
	''' <summary>
	''' Gets the database connection string.
	''' </summary>
	Friend ReadOnly Property ConnectionString As String
		Get
			Return "User Id=" + UserName + ";" & _
				   "Password=" + Password.GetString + ";" & _
			   	   "Data Source=" + DataSource + ";" & _
			   	   "Persist Security Info=False;Pooling=True"
		End Get
	End Property
	
	''' <summary>
	''' Is the database connection string valid?
	''' </summary>
	''' <returns>True or False</returns>
	Friend Shared Function IsConnectionStringValid As Boolean
		Using connection As New OracleConnection
			Try
				connection.ConnectionString = _
					DatabaseConnectionString.Instance.ConnectionString
				connection.Open
				Return True
			Catch ex As OracleException
				ShowError(ex.Message.ToString())
				Return False
			End Try
		End Using
	End Function
End Class
@echo off
rem ***************************************************************************
rem * PDFKeeper -- Open Source PDF Document Storage Solution
rem * Copyright (C) 2009-2018  Robert F. Frasca
rem *
rem * This file is part of PDFKeeper.
rem *
rem * PDFKeeper is free software: you can redistribute it and/or modify
rem * it under the terms of the GNU General Public License as published by
rem * the Free Software Foundation, either version 3 of the License, or
rem * (at your option) any later version.
rem *
rem * PDFKeeper is distributed in the hope that it will be useful,
rem * but WITHOUT ANY WARRANTY; without even the implied warranty of
rem * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
rem * GNU General Public License for more details.
rem *
rem * You should have received a copy of the GNU General Public License
rem * along with PDFKeeper.  If not, see <http://www.gnu.org/licenses/>.
rem ***************************************************************************
rem
rem Start of localized strings
rem
set title=Oracle Database Change PDFKeeper Schema Account Password
set instructions1=Enter the database connect string in the format:
set instructions2=username@host:port
set instructions3=username: database account that must be a member of SYSDBA.
set instructions4=Note, SYSTEM is a member of SYSDBA.
set instructions5=host: host name or IP address of the computer that is
set instructions6=running Oracle Database XE. Use localhost or 127.0.0.1 if
set instructions7=logged into the same computer as Oracle Database XE.
set instructions8=port (optional): TCP port number on which the Oracle Net
set instructions9=listener is listening. The default port number is 1521.
set promptString=Enter connect string:
set errorMessage=Error: Database connect string not specified.
rem
rem End of localized strings
rem
title %title%
echo.
echo %instructions1%
echo.
echo %instructions2%
echo.
echo * %instructions3%
echo   %instructions4%
echo.
echo * %instructions5%
echo   %instructions6% 
echo   %instructions7%
echo.
echo * %instructions8%
echo   %instructions9%
echo.
set /p connectString=%promptString% 
if "%connectString%"=="" (
	echo.
	echo %errorMessage%
	echo.
	goto End
)
sqlplus %connectString% @OracleDBChangeSchemaAccountPassword.sql
:End
set connectString=
del sqlnet.log 2>nul
endlocal
pause
@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM  Builds a test package (zip) for internal testers from the
REM  already-built plugin outputs. Usage:
REM
REM    MakeTestPackage.bat [Debug^|Release] [plugin-name]
REM
REM  Requires the solution to be built first (dotnet build).
REM  Output: dist\<name>-<version>-test.zip
REM ============================================================

set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"
set "NAME=%~2"
if "%NAME%"=="" set "NAME=WelderPaint"

REM read <Version> from Version.Build.props
set "VER="
for /f "delims=" %%L in ('findstr /c:"<Version>" Version.Build.props') do (
    set "TAGLINE=%%L"
)
if not defined TAGLINE goto noversion
set "TAGLINE=!TAGLINE:*<Version>=!"
set "VER=!TAGLINE:</Version>=!"
:noversion
if not defined VER set "VER=0.0.0"

set "STAGE=dist\%NAME%-test"
set "ZIP=dist\%NAME%-%VER%-test.zip"

if not exist "ClientPlugin\bin\%CONFIG%\net48\%NAME%.dll" (
    echo ERROR: %NAME%.dll not found in ClientPlugin\bin\%CONFIG%\net48
    echo Build the solution first: dotnet build %NAME%.sln
    exit /b 1
)
if not exist "ClientPlugin\bin\%CONFIG%\net10.0\%NAME%.dll" (
    echo ERROR: %NAME%.dll not found in ClientPlugin\bin\%CONFIG%\net10.0
    echo Build the solution first: dotnet build %NAME%.sln
    exit /b 1
)
if not exist "%NAME%.xml" (
    echo ERROR: %NAME%.xml descriptor not found in the repo root.
    exit /b 1
)

set "SEVENZIP=%ProgramFiles%\7-Zip\7z.exe"
if not exist "%SEVENZIP%" (
    echo ERROR: 7-Zip not found at "%SEVENZIP%".
    echo Encrypted test packages require 7-Zip ^(https://7-zip.org^).
    exit /b 1
)

if exist "%STAGE%" rmdir /s /q "%STAGE%"
mkdir "%STAGE%\Plugin\Legacy" "%STAGE%\Plugin\Interim" || exit /b 1

echo Staging %NAME% v%VER% (%CONFIG%)...

copy /y "Package\Install-TestPlugin.bat" "%STAGE%\" >nul
copy /y "Package\Uninstall-TestPlugin.bat" "%STAGE%\" >nul
copy /y "Package\README-test.txt" "%STAGE%\" >nul

REM net48 build -> Pulsar Legacy, net10.0 build -> Pulsar Interim
copy /y "ClientPlugin\bin\%CONFIG%\net48\%NAME%.dll" "%STAGE%\Plugin\Legacy\" >nul
copy /y "%NAME%.xml" "%STAGE%\Plugin\Legacy\%NAME%.dll.xml" >nul
copy /y "ClientPlugin\bin\%CONFIG%\net10.0\%NAME%.dll" "%STAGE%\Plugin\Interim\" >nul
copy /y "%NAME%.xml" "%STAGE%\Plugin\Interim\%NAME%.dll.xml" >nul

if exist "%ZIP%" del /q "%ZIP%"

REM Generated password: 24 chars, no ambiguous characters (no pipes in the PS code -
REM the ^| escaping does not survive the for /f shell layers).
for /f "delims=" %%P in ('powershell -NoProfile -Command "$s='ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789'; $c=New-Object char[] 24; $r=New-Object System.Random; for($i=0;$i -lt 24;$i++){$c[$i]=$s[$r.Next($s.Length)]}; -join $c"') do set "PASS=%%P"
if not defined PASS (
    echo ERROR: password generation failed.
    exit /b 1
)

"%SEVENZIP%" a -tzip -mem=AES256 -r -p%PASS% "%ZIP%" "%STAGE%\*" >nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: encrypted zip creation failed.
    exit /b 1
)

REM Sanity check: the archive must decrypt with the password.
"%SEVENZIP%" t -p%PASS% "%ZIP%" >nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: archive verification failed.
    exit /b 1
)

echo %PASS%> "%ZIP%.password.txt"

echo.
echo Created: %ZIP%  ^(AES-256 encrypted^)
echo Password: %PASS%
echo Password also saved to: %ZIP%.password.txt
echo.
echo Ship the zip to the tester and send the password via a separate channel.
echo The tester needs 7-Zip or WinRAR to extract ^(Explorer cannot open AES zips^), then runs Install-TestPlugin.bat.

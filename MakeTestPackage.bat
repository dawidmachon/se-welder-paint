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
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%STAGE%\*' -DestinationPath '%ZIP%' -Force"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: zip creation failed.
    exit /b 1
)

echo.
echo Created: %ZIP%
echo Contents:
powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::OpenRead('%CD%\%ZIP%').Entries.FullName"
echo.
echo Ship this zip to the tester - they only need to extract and run Install-TestPlugin.bat.

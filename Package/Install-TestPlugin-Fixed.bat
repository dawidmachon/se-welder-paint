@echo off
setlocal enabledelayedexpansion
title Welder Paint - fixed installer

REM ============================================================
REM  FIXED installer for the WelderPaint test build.
REM
REM  Problem it fixes: your game runs on .NET 10, but the
REM  previous install placed the .NET Framework (net48) build.
REM  Pulsar listed the plugin as enabled but silently never
REM  initialized it (no "[WelderPaint]" lines in the game log).
REM
REM  This installer:
REM   - copies the .NET 10 (Interim) build of the plugin into
REM     EVERY installed Pulsar edition's Local folder
REM   - REMOVES the old wrong-flavor files first
REM   - keeps the plugin enabled in the profile (no duplicates)
REM
REM  Run with the game CLOSED. Needs no admin rights.
REM ============================================================

set "SCRIPT_DIR=%~dp0"
set "SRC=%SCRIPT_DIR%Plugin\Interim"

set "PULSAR=%~1"
if not defined PULSAR set "PULSAR=%PULSAR_DIR%"
if not defined PULSAR set "PULSAR=%AppData%\Pulsar"

echo.
echo === WelderPaint fixed installer ===
echo Pulsar folder: %PULSAR%
echo.

if not exist "%PULSAR%" (
    echo ERROR: Pulsar folder not found: %PULSAR%
    echo Pass it as argument: %~nx0 "C:\path\to\Pulsar"
    pause
    exit /b 1
)
if not exist "%SRC%\WelderPaint.dll" (
    echo ERROR: package incomplete - Plugin\Interim\WelderPaint.dll missing.
    pause
    exit /b 1
)

for %%E in (Legacy Interim) do (
    set "EDITION=%%E"
    call :install_edition "%%E"
)

echo.
echo Done. Start the game through Pulsar the same way as before,
echo take a welder, press P to pick a color, press O.
echo You MUST see "Welder paint: ON" on the HUD.
echo.
echo If it still does nothing after a minute of playing, run
echo Collect-Diagnostics.bat and send back the zip from your Desktop.
echo.
pause
exit /b 0

:install_edition
set "EDITION=%~1"
set "PROFILE=%PULSAR%\%EDITION%\Profiles\Current.xml"
set "DEST=%PULSAR%\%EDITION%\Local"

REM Skip editions that are clearly not installed (no profile AND no Local folder).
if not exist "%PROFILE%" if not exist "%DEST%" (
    echo [Pulsar %EDITION%] not installed, skipping.
    exit /b 0
)

if not exist "%DEST%" mkdir "%DEST%"

echo [Pulsar %EDITION%] removing old files...
if exist "%DEST%\WelderPaint.dll" del /q "%DEST%\WelderPaint.dll"
if exist "%DEST%\WelderPaint.dll.xml" del /q "%DEST%\WelderPaint.dll.xml"

echo [Pulsar %EDITION%] copying .NET 10 build...
copy /y "%SRC%\WelderPaint.dll" "%DEST%\" >nul
if errorlevel 1 (
    echo ERROR: could not copy to "%DEST%" - is the game running?
    pause
    exit /b 2
)
copy /y "%SRC%\WelderPaint.dll.xml" "%DEST%\" >nul

if not exist "%PROFILE%" (
    echo [Pulsar %EDITION%] no profile ^(edition folder exists only^), files placed.
    exit /b 0
)

REM Ensure enabled in the profile (idempotent).
set "PSF=%PROFILE:'=''%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$f='%PSF%';$d='WelderPaint.dll';$nl=[string][char]13+[string][char]10;$t=[IO.File]::ReadAllText($f);$e='<string>'+$d+'</string>';if($t.Contains($e)){exit 1};if($t.Contains('<Local>')){$t=$t.Replace('<Local>','<Local>'+$nl+'    '+$e)}elseif($t.Contains('<Local />')){$t=$t.Replace('<Local />','<Local>'+$nl+'    '+$e+$nl+'  </Local>')}else{exit 2};[IO.File]::WriteAllText($f,$t,(New-Object Text.UTF8Encoding($false)));exit 0"
if %ERRORLEVEL% EQU 0 (
    echo [Pulsar %EDITION%] enabled WelderPaint.dll in the profile.
) else if %ERRORLEVEL% EQU 1 (
    echo [Pulsar %EDITION%] WelderPaint.dll already enabled in the profile.
) else (
    echo [Pulsar %EDITION%] WARNING: no ^<Local^> section found in the profile -
    echo the plugin is copied but NOT enabled. Enable it in the Pulsar launcher
    ^(plugin list, Local section^) and restart the game.
)
exit /b 0

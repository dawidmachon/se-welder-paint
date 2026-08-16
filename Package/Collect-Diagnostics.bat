@echo off
setlocal enabledelayedexpansion
title SE plugin diagnostics collector

REM ============================================================
REM  Collects everything needed to debug the WelderPaint test:
REM  - newest Space Engineers game logs (all [WelderPaint] lines)
REM  - Pulsar Legacy/Interim info.log (plugin load errors)
REM  - Pulsar Profiles\Current.xml (which plugins were enabled)
REM  - WelderPaint.cfg (plugin settings)
REM  into one zip on the Desktop, ready to send back.
REM  No admin rights needed, nothing is changed or deleted.
REM ============================================================

set "SEDIR=%AppData%\SpaceEngineers"
set "PULSAR=%AppData%\Pulsar"
set "PLUGIN=WelderPaint"

set "STAGE=%TEMP%\%PLUGIN%-diagnostics"
if exist "%STAGE%" rmdir /s /q "%STAGE%"
mkdir "%STAGE%"

echo.
echo === %PLUGIN% diagnostics collector ===
echo.

echo [1/4] Game logs...
set "COUNT=0"
if exist "%SEDIR%\SpaceEngineers.log" (
    copy /y "%SEDIR%\SpaceEngineers.log" "%STAGE%\" >nul
    set /a COUNT+=1
)
set "NEWEST="
for /f "delims=" %%F in ('dir /b /o-d "%SEDIR%\SpaceEngineers_*.log" 2^>nul') do (
    if not defined NEWEST set "NEWEST=%%F"
)
if defined NEWEST (
    if not "!NEWEST!"=="SpaceEngineers.log" (
        copy /y "%SEDIR%\!NEWEST!" "%STAGE%\" >nul
        set /a COUNT+=1
    )
)
echo       !COUNT! log file^(s^) collected.

echo [2/4] Pulsar logs...
set "PCOUNT=0"
for %%E in (Legacy Interim) do (
    if exist "%PULSAR%\%%E\info.log" (
        mkdir "%STAGE%\Pulsar-%%E" 2>nul
        copy /y "%PULSAR%\%%E\info.log" "%STAGE%\Pulsar-%%E\" >nul
        if exist "%PULSAR%\%%E\Profiles\Current.xml" copy /y "%PULSAR%\%%E\Profiles\Current.xml" "%STAGE%\Pulsar-%%E\" >nul
        set /a PCOUNT+=1
    )
)
echo       !PCOUNT! Pulsar edition^(s^) collected.

echo [3/4] Plugin config...
if exist "%SEDIR%\Storage\%PLUGIN%.cfg" (
    copy /y "%SEDIR%\Storage\%PLUGIN%.cfg" "%STAGE%\" >nul
    echo       config collected.
) else (
    echo       no config file ^(defaults in use^) - fine.
)

echo [4/4] Packing...
for /f %%T in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "TS=%%T"
for /f "delims=" %%D in ('powershell -NoProfile -Command "[Environment]::GetFolderPath('Desktop')"') do set "DESKTOP=%%D"
set "OUT=%DESKTOP%\%PLUGIN%-diagnostics-%TS%.zip"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%STAGE%\*' -DestinationPath '%OUT%' -Force"
if errorlevel 1 (
    echo.
    echo ERROR: could not create the zip. The collected files are here,
    echo zip this folder manually and send it instead:
    echo   %STAGE%
    pause
    exit /b 1
)
rmdir /s /q "%STAGE%"

echo.
echo Done! Created on your Desktop:
echo   %OUT%
echo.
echo Please send this file back. Thank you!
echo.
pause

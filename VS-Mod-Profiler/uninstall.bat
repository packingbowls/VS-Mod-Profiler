@echo off
title VS Mod Profiler Uninstaller
color 0C

echo =============================================================================
echo                           VS MOD PROFILER UNINSTALL                          
echo =============================================================================
echo.
echo         This will completely remove VS-Mod-Profiler, its configs, and
echo               all stored mods inside the VS-Mod-Profiler folder
echo.
echo              All unloaded profiles and Persist folders stored in
echo     /VintagestoryData/VS-Mod-Profiler/ will be DELETED and are UNRECOVERABLE!
echo.
echo                  Make sure you have backed up any real files
echo                  you want to keep to save a migraine or two.
echo.
set /p CONFIRM="Type UNINSTALL to proceed, or anything else to exit: "

if "%CONFIRM%" NEQ "UNINSTALL" (
    echo.
    echo Uninstallation cancelled.
    pause
    exit /b
)

color 0E
echo.
echo Locating VintagestoryData directory...

set "VSDATA=%APPDATA%\VintagestoryData"
if exist "%~dp0vs_path.txt" (
    set /p VSDATA=<"%~dp0vs_path.txt"
)

echo Target Data Directory: "%VSDATA%"

if exist "%VSDATA%\Mods" (
    echo Cleaning symbolic links from "%VSDATA%\Mods"...
    for %%F in ("%VSDATA%\Mods\*") do (
        fsutil reparsepoint query "%%~fF" >nul 2>&1
        if not errorlevel 1 (
            del /f /q "%%~fF"
        )
    )
)

if exist "%VSDATA%\VS-Mod-Profiler" (
    echo Removing "%VSDATA%\VS-Mod-Profiler"...
    rmdir /s /q "%VSDATA%\VS-Mod-Profiler"
)

if exist "%~dp0vs_path.txt" del /f /q "%~dp0vs_path.txt"
if exist "%~dp0VS-Mod-Profiler.exe" del /f /q "%~dp0VS-Mod-Profiler.exe"

echo.
echo Uninstallation complete. Removing uninstaller...
pause

(goto) 2>nul & del "%~f0"
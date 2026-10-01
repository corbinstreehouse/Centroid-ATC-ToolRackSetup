@echo off
setlocal

set "PROJECT=%~dp0ToolRackSetup\ToolRackSetup.csproj"
set "SRC=%~dp0ToolRackSetup\bin\Release\net8.0-windows7.0"
set "DEST=C:\cncm"

echo ============================================
echo Building ToolRackSetup (Release)...
echo ============================================
dotnet build "%PROJECT%" -c Release
if errorlevel 1 (
    echo.
    echo *** BUILD FAILED - aborting, nothing copied. ***
    exit /b 1
)

echo.
echo ============================================
echo Copying build output to %DEST%
echo ============================================
robocopy "%SRC%" "%DEST%" /R:3 /W:2

set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
    echo.
    echo *** COPY FAILED - robocopy exit code %RC%. ***
    echo *** One or more files in %DEST% could not be overwritten. ***
    echo *** Make sure ToolRackSetup.exe ^(or CNC12^) is not running and try again. ***
    exit /b 1
)

echo.
echo ============================================
echo Done. Build deployed to %DEST%
echo ============================================
exit /b 0

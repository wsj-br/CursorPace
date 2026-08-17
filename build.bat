@echo off
REM Build and package Cursor Quota Progress for Windows

echo Building Release configuration...
dotnet build -c Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Running tests...
dotnet test -c Release --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Publishing self-contained package...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

set PUBLISH_DIR=bin\Release\net10.0-windows\win-x64\publish

echo.
echo Build complete!
echo Publish directory: %PUBLISH_DIR%
echo.
echo To create installer:
echo 1. Install Inno Setup 6.x from https://jrsoftware.org/isdl.php
echo 2. Replace Assets/icon-placeholder.txt with a proper icon.ico
echo 3. Run: iscc setup.iss
echo.
echo To test the published app:
echo cd %PUBLISH_DIR% ^&^& CursorQuotaProgress.exe

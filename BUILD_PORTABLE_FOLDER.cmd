@echo off
chcp 65001 >nul
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -FolderBundle
if errorlevel 1 (
  echo.
  echo BUILD FAILED.
  pause
  exit /b 1
)
echo.
echo Build complete. Check the dist-folder folder.
pause

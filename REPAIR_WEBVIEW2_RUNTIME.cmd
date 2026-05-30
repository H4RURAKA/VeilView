@echo off
chcp 65001 > nul
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\install-webview2-runtime.ps1"
if errorlevel 1 (
  echo.
  echo WebView2 Runtime install/repair failed.
  pause
  exit /b 1
)
echo.
echo Done.
pause

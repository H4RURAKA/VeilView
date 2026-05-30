@echo off
chcp 65001 >nul
set EXE=%~dp0dist\VeilView.exe
if not exist "%EXE%" (
  echo dist\VeilView.exe not found.
  echo Run BUILD_SINGLE_EXE.cmd first.
  pause
  exit /b 1
)
start "" "%EXE%"

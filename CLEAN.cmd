@echo off
chcp 65001 >nul
cd /d "%~dp0"
if exist dist rmdir /s /q dist
if exist dist-folder rmdir /s /q dist-folder
if exist src\VeilView\bin rmdir /s /q src\VeilView\bin
if exist src\VeilView\obj rmdir /s /q src\VeilView\obj
echo Clean complete.
pause

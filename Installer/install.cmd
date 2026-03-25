@echo off
setlocal

set "TARGET=%LocalAppData%\FFBoost"

if not exist "%TARGET%" (
    mkdir "%TARGET%"
)

copy /Y "%~dp0FFBoost.exe" "%TARGET%\FFBoost.exe" >nul
copy /Y "%~dp0config.json" "%TARGET%\config.json" >nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$desktop=[Environment]::GetFolderPath('Desktop'); $shell=New-Object -ComObject WScript.Shell; $shortcut=$shell.CreateShortcut((Join-Path $desktop 'FF Boost.lnk')); $shortcut.TargetPath=(Join-Path $env:LOCALAPPDATA 'FFBoost\\FFBoost.exe'); $shortcut.WorkingDirectory=(Join-Path $env:LOCALAPPDATA 'FFBoost'); $shortcut.IconLocation=(Join-Path $env:LOCALAPPDATA 'FFBoost\\FFBoost.exe'); $shortcut.Save()"

start "" "%TARGET%\FFBoost.exe"

endlocal

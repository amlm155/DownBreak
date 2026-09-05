@echo off
setlocal
cd /d %~dp0

set WORKSPACE=%~dp0..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0
set CODE_OUT=%WORKSPACE%\Assets\Scripts\0_Data\LubanTableData\Gen

dotnet "%LUBAN_DLL%" ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf "%CONF_ROOT%luban.conf" ^
    -x outputCodeDir=%CODE_OUT% ^
    -x outputDataDir=%WORKSPACE%\Assets\StreamingAssets\Config\Item

if %ERRORLEVEL% NEQ 0 (
    echo Luban gen failed
    exit /b %ERRORLEVEL%
)

echo Luban gen ok
endlocal

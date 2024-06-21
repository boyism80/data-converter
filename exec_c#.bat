@ECHO OFF

SET SOURCE=D:\ProjectM2\design

CALL dotnet publish -c Release -o "bin"
if ERRORLEVEL 1 GOTO END

PUSHD bin
CALL ExcelTableConverter.exe --dir=D:\ProjectM2\design --lang=c# --env=c#
POPD

if ERRORLEVEL 1 GOTO END

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
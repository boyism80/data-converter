CALL dotnet publish -c Release -o "bin"
if ERRORLEVEL 1 GOTO END

PUSHD bin
CALL ExcelTableConverter.exe --dir=../sample --lang=c#
POPD

if ERRORLEVEL 1 GOTO END

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
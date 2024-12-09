SET DEST=C:\Users\cshyeon\Desktop\node table
CALL dotnet publish -c Release -o "bin"
if ERRORLEVEL 1 GOTO END

SET SOURCE=C:\Users\cshyeon\fb\resources\table

PUSHD bin
CALL ExcelTableConverter.exe --dir=%SOURCE% --lang=node
POPD

if ERRORLEVEL 1 GOTO END

DEL /s /q "%DEST%\model.js"
RMDIR /s /q "%DEST%\json"
XCOPY bin\output\class\server\*.js "%DEST%\*.js"
XCOPY bin\output\json\\server\*.json "%DEST%\json\*.json"

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
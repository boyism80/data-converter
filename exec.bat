@ECHO OFF

SET SOURCE=C:\Users\cshyeon\fb\resources\table
SET DEST=C:\Users\cshyeon\fb

CALL dotnet publish -c Release -o "bin"
if ERRORLEVEL 1 GOTO END

PUSHD bin
CALL ExcelTableConverter.exe --dir=%SOURCE% --lang=c++
POPD

if ERRORLEVEL 1 GOTO END

DEL /s /q "%DEST%\include\fb\game\model.h"
RMDIR /s /q "%DEST%\game\json"
XCOPY "bin\output\class\server\*.h" "%DEST%\include\fb\game\*.h"
XCOPY "bin\output\json\\server\*.json" "%DEST%\game\json\*.json"

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
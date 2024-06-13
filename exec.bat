SET DEST="C:\Users\cshyeon\fb"

CALL dotnet publish -c Release -o "bin"
if ERRORLEVEL 1 GOTO END

PUSHD bin
CALL ExcelTableConverter.exe --dir=../sample --lang=c++
POPD

if ERRORLEVEL 1 GOTO END

DEL /s /q "%DEST%\include\fb\game\model.h"
XCOPY bin\output\class\server\include\*.h %DEST%\include\fb\game\*.h

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
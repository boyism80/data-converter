@ECHO OFF

SET SOURCE=D:\Users\CSHYEON\Data\git\game\c++\fb
CALL dotnet publish -c Release -o "bin"
if ERRORLEVEL 1 GOTO END

PUSHD bin
CALL ExcelTableConverter.exe --dir=%SOURCE%\resources\table --lang=c#
POPD

if ERRORLEVEL 1 GOTO END

rem DEL /s /q "%SOURCE%\include\fb\game\model.h"
rem RMDIR /s /q "%SOURCE%\game\json"
rem XCOPY "bin\output\class\server\*.h" "%SOURCE%\include\fb\game\*.h"
rem XCOPY "bin\output\json\\server\*.json" "%SOURCE%\game\json\*.json"

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
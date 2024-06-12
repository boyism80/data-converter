SET DEST="C:\Users\cshyeon\fb"

CALL dotnet publish -c Release -o "bin"
PUSHD bin
CALL ExcelTableConverter.exe --dir=../sample
POPD

if ERRORLEVEL 1 GOTO END

RMDIR /s /q "%DEST%\include\fb\model"
MKDIR "%DEST%\include\fb\model"

RMDIR /s /q "%DEST%\model"
MKDIR "%DEST%\model"

XCOPY bin\output\enum\*.h %DEST%\include\fb\model\*.h
XCOPY bin\output\const\common\*.h %DEST%\include\fb\model\common\*.h
XCOPY bin\output\const\server\*.h %DEST%\include\fb\model\*.h
XCOPY bin\output\dsl\*.h %DEST%\include\fb\model\*.h
XCOPY bin\output\class\include\*.h %DEST%\include\fb\model\*.h
XCOPY bin\output\class\common\include\*.h %DEST%\include\fb\model\common\*.h
XCOPY bin\output\class\server\include\*.h %DEST%\include\fb\model\*.h
XCOPY bin\output\bind\server\include\*.h %DEST%\include\fb\model\*.h

XCOPY bin\output\class\common\source\*.cpp %DEST%\model\common\*.cpp
XCOPY bin\output\class\server\source\*.cpp %DEST%\model\*.cpp
XCOPY bin\output\dsl\*.cpp %DEST%\model\*.cpp
XCOPY bin\output\bind\server\source\*.cpp %DEST%\model\*.cpp

GOTO SKIP_PAUSE
:END
PAUSE
:SKIP_PAUSE
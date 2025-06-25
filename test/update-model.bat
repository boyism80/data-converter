@echo off
echo Updating Go model from data-converter...

REM Run data-converter
cd ..
echo Cleaning cache...
rmdir /s /q cache 2>nul
echo Running data-converter...
dotnet build
dotnet run
if %ERRORLEVEL% neq 0 (
    echo Error: data-converter execution failed
    exit /b 1
)

REM Copy generated model.go to test/generated folder
echo Copying generated model to test directory...
copy "output\Go\server\model.go" "test\generated\"
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to copy model.go
    exit /b 1
)

REM Go back to test directory and run tests
cd test
echo Running Go tests...
go test -v -args -jsondir "..\output\json\server"
if %ERRORLEVEL% neq 0 (
    echo Error: Tests failed
    exit /b 1
)

echo All operations completed successfully!
echo Model updated and tests passed! 
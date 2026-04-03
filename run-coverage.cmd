@echo off
setlocal

set "RESULTS_PATH=%~dp0coverage\results"
set "REPORT_PATH=%~dp0coverage\html"

echo [1/5] Cleaning previous build artifacts...
dotnet clean
if %ERRORLEVEL% NEQ 0 (
    echo Clean failed.
    exit /b %ERRORLEVEL%
)

echo [2/5] Cleaning previous test results and coverage...
if exist "%RESULTS_PATH%" rd /s /q "%RESULTS_PATH%"
if exist "%REPORT_PATH%"  rd /s /q "%REPORT_PATH%"
mkdir "%RESULTS_PATH%"

echo [3/5] Building project...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo Build failed.
    exit /b %ERRORLEVEL%
)

echo [4/5] Running Core unit tests with coverage...
dotnet-coverage collect ^
    --output "%RESULTS_PATH%\coverage.xml" ^
    --output-format cobertura ^
    "dotnet test AshesofVelsingrad.Core.Tests --no-build"
if %ERRORLEVEL% NEQ 0 (
    echo Core tests failed.
    exit /b %ERRORLEVEL%
)

echo [5/5] Generating HTML coverage report...
reportgenerator ^
    "-reports:%RESULTS_PATH%\coverage.xml" ^
    "-targetdir:%REPORT_PATH%" ^
    -reporttypes:Html

if %ERRORLEVEL% NEQ 0 (
    echo Report generation failed.
    exit /b %ERRORLEVEL%
)

start "" "%REPORT_PATH%\index.html"
endlocal
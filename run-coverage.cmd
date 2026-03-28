@echo off
setlocal

set "RESULTS_PATH=%CD%\coverage\results"
set "REPORT_PATH=%CD%\coverage\html"

if exist "%RESULTS_PATH%" rd /s /q "%RESULTS_PATH%"
if exist "%REPORT_PATH%"  rd /s /q "%REPORT_PATH%"
mkdir "%RESULTS_PATH%"

echo [1/3] Running Core tests with coverage...
dotnet-coverage collect ^
    --output "%RESULTS_PATH%\coverage.xml" ^
    --output-format cobertura ^
    "dotnet test tests/Core"
if %ERRORLEVEL% NEQ 0 (
    echo Core tests failed.
    exit /b %ERRORLEVEL%
)

echo [2/3] Running Godot integration tests (no coverage)...
dotnet test --settings tests/.runsettings
if %ERRORLEVEL% NEQ 0 (
    echo Godot tests failed, continuing to generate report...
)

echo [3/3] Generating report...
reportgenerator ^
    "-reports:%RESULTS_PATH%\coverage.xml" ^
    "-targetdir:%REPORT_PATH%" ^
    -reporttypes:Html

start "%REPORT_PATH%\index.html"
endlocal
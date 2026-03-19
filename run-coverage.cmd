@echo off
setlocal enabledelayedexpansion

echo ============================================
echo    Godot 4 + .NET 9 Coverage (Microsoft Engine)
echo ============================================

set "SCRIPT_DIR=%~dp0"
set "RUNSETTINGS_PATH=%SCRIPT_DIR%tests/.runsettings"
set "RESULTS_PATH=%SCRIPT_DIR%tests\TestResults"

:: 1. Cleanup old results but NOT the .godot folder
if exist "%RESULTS_PATH%" rmdir /s /q "%RESULTS_PATH%"
if exist "coverage" rmdir /s /q "coverage"
mkdir "tests\TestResults"

:: 2. Force Build to ensure .pdb files exist for discovery
echo [1/4] Building project...
dotnet build --configuration Debug

:: 3. Run Tests
echo [2/4] Running 393 tests and collecting coverage...
:: Removed --no-build here to allow GdUnit4 to sync if needed
dotnet test --settings "%RUNSETTINGS_PATH%" --collect:"Code Coverage" --results-directory "%RESULTS_PATH%" --verbosity normal

echo.
echo [3/4] Searching for Cobertura file...
set "COVERAGE_FILE="
for /f "delims=" %%F in ('dir /b /s "%RESULTS_PATH%\*.cobertura.xml" 2^>nul') do (
    set "COVERAGE_FILE=%%F"
)

if "%COVERAGE_FILE%"=="" (
    echo [ERROR] No coverage XML found. Did tests run?
    exit /b 1
)

:: 4. Generate Report
echo [4/4] Generating HTML report...
reportgenerator "-reports:tests\TestResults\**\*.cobertura.xml" "-targetdir:coverage/html" -reporttypes:Html "-filefilters:-*.generated.cs"

if %ERRORLEVEL% equ 0 (
    echo Success! Opening report...
    start coverage\html\index.html
)
endlocal
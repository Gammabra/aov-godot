@echo off
set "RESULTS_PATH=%CD%\tests\TestResults"
if exist "%RESULTS_PATH%" rd /s /q "%RESULTS_PATH%"

echo [1/3] Building...
dotnet build

echo [2/3] Running Tests with XPlat Collector...
:: This uses the coverlet.collector NuGet package we installed earlier
dotnet test --no-build --settings tests/.runsettings --collect:"XPlat Code Coverage" --results-directory "%RESULTS_PATH%"

echo [3/3] Generating Report...
:: The collector puts the file in a random GUID folder, so we use the wildcard
reportgenerator "-reports:%RESULTS_PATH%\*\coverage.cobertura.xml" "-targetdir:coverage/html" -reporttypes:Html
 
start coverage/html/index.html
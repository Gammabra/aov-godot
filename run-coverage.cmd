@echo off
echo [1/3] Building...
dotnet build
if %errorlevel% neq 0 exit /b %errorlevel%

echo [2/3] Instrumenting assembly and running tests...
coverlet ".godot\mono\temp\bin\Debug\Ashes of Velsingrad.dll" ^
  --target "dotnet" ^
  --targetargs "test --no-build --settings tests/.runsettings" ^
  --output "coverage/results.xml" ^
  --format cobertura ^
  --include-test-assembly ^
  --exclude "[GodotSharp]*" ^
  --exclude "[GdUnit4*]*" ^
  --exclude "[gdUnit4*]*" ^
  --exclude "[Microsoft*]*" ^
  --exclude "[testhost]*"
if %errorlevel% neq 0 exit /b %errorlevel%

echo [3/3] Generating HTML report...
reportgenerator -reports:coverage/results.xml -targetdir:coverage/html -reporttypes:Html

echo Done! Opening report...
start coverage/html/index.html
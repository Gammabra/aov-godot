@echo off
echo ===========================================
echo    Ashes of Velsingrad Documentation
echo    DocFX Generation Script
echo ===========================================
echo.

REM Check if DocFX is installed
echo [1/6] Checking DocFX installation...
docfx --version >nul 2>&1
if %errorlevel% neq 0 (
    echo DocFX not found. Installing DocFX...
    dotnet tool install -g docfx
    if %errorlevel% neq 0 (
        echo ERROR: Failed to install DocFX
        pause
        exit /b 1
    )
    echo DocFX installed successfully!
) else (
    echo DocFX is already installed.
)
echo.

REM Build the project first
echo [2/6] Building C# project...
dotnet build
if %errorlevel% neq 0 (
    echo ERROR: Failed to build the project
    pause
    exit /b 1
)
echo Project built successfully!
echo.

REM Check for Godot DLLs and update docfx.json if needed
echo [3/6] Checking Godot DLL paths...
set "DEBUG_PATH=AshesofVelsingrad/.godot/mono/temp/bin/Debug"

if exist "%DEBUG_PATH%/GodotSharp.dll" (
    echo Found Debug Godot DLLs, using Debug path
    set "DLL_PATH=%DEBUG_PATH%"
) else (
    echo WARNING: Godot DLLs not found, documentation may be incomplete
    set "DLL_PATH=%DEBUG_PATH%"
)
echo.

REM Clean previous documentation
echo [4/6] Cleaning previous documentation...
if exist "documentation\_site" (
    rmdir /s /q "documentation\_site"
    echo Previous documentation cleaned.
) else (
    echo No previous documentation to clean.
)

if exist "documentation\api" (
    rmdir /s /q "documentation\api"
    echo Previous API documentation cleaned.
) else (
    echo No previous API documentation to clean.
)
echo.

REM Generate API documentation with warning suppression
echo [5/6] Generating API documentation...
echo Suppressing .NET version warnings...
docfx metadata docfx.json --warningsAsErrors false --logLevel Warning
if %errorlevel% neq 0 (
    echo ERROR: Failed to generate metadata
    pause
    exit /b 1
)
echo API metadata generated successfully!
echo.

REM Build the full documentation site
echo [6/6] Building documentation site...
docfx build docfx.json --warningsAsErrors false
if %errorlevel% neq 0 (
    echo ERROR: Failed to build documentation site
    pause
    exit /b 1
)
echo.

echo ===========================================
echo Documentation generated successfully!
echo Location: documentation/_site/index.html
echo ===========================================
pause

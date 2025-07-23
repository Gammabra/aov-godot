@echo off
echo ===========================================
echo    Ashes of Velsingrad Documentation
echo    DocFX Generation Script
echo ===========================================
echo.

REM Check if DocFX is installed
echo [1/5] Checking DocFX installation...
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
echo [2/5] Building C# project...
dotnet build
if %errorlevel% neq 0 (
    echo ERROR: Failed to build the project
    pause
    exit /b 1
)
echo Project built successfully!
echo.

REM Clean previous documentation
echo [3/5] Cleaning previous documentation...
if exist "documentation\_site" (
    rmdir /s /q "documentation\_site"
    echo Previous documentation cleaned.
) else (
    echo No previous documentation to clean.
)
echo.

REM Generate API documentation
echo [4/5] Generating API documentation...
cd documentation
docfx metadata docfx.json
if %errorlevel% neq 0 (
    echo ERROR: Failed to generate metadata
    cd ..
    pause
    exit /b 1
)
echo API metadata generated successfully!
echo.

REM Build the full documentation site
echo [5/5] Building documentation site...
docfx build docfx.json
if %errorlevel% neq 0 (
    echo ERROR: Failed to build documentation site
    cd ..
    pause
    exit /b 1
)
cd ..
echo.

echo ===========================================

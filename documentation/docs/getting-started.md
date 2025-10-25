# Getting Started with Ashes of Velsingrad

This guide will help you set up your development environment and get Ashes of Velsingrad running on your machine.

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

- **Godot 4.5.1** (Mono version) - [Download from official site](https://godotengine.org/download)
- **.NET 9.0 SDK** or higher - [Download from Microsoft](https://dotnet.microsoft.com/download)
- **Git** - [Download from git-scm.com](https://git-scm.com/)

### Recommended Tools

- **Visual Studio Code** with C# Dev Kit extension (recommended)
- **Visual Studio 2022** (alternative)
- **JetBrains Rider** (alternative)

### System Requirements

- **OS**: Windows 10/11 (64-bit)
- **RAM**: 8GB minimum, 16GB recommended
- **Storage**: 2GB free space
- **Graphics**: DirectX 11 compatible

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/Gammabra/aov-godot.git
cd aov-godot
```

### 2. Set Up Environment Variables

You need to set the `GODOT_BIN` environment variable for testing:

**Windows (Command Prompt):**
```cmd
setx GODOT_BIN "C:\path\to\Godot_v4.5.1-stable_mono_win64.exe"
```

**Windows (PowerShell):**
```powershell
[Environment]::SetEnvironmentVariable("GODOT_BIN", "C:\path\to\Godot_v4.5.1-stable_mono_win64.exe", "Machine")
```

### 3. Open the Project

1. Launch Godot
2. Click "Import" in the Project Manager
3. Navigate to the cloned repository folder
4. Select `project.godot`
5. Click "Import & Edit"

### 4. Build the Project

In Godot, go to:
- **Project** → **Tools** → **C#** → **Create C# solution**
- Wait for the build to complete
- If you see errors, try **Build** → **Rebuild Project**

### 5. Run Tests (Optional)

To verify everything is working:

```bash
dotnet test --settings tests/.runsettings
```

Or use the GdUnit4 tab in Godot to run tests within the editor.

## 🔧 Development Environment Setup

### Visual Studio Code (Recommended)

1. **Install Extensions:**
   - C# Dev Kit (v1.5.12 (pre-release) recommended) (Microsoft)
   - C# (Microsoft)
   - godot-tools (optional)
   - Conventional Commits (vivaxy) - for commit messages

2. **Configure Workspace:**
   Create `.vscode/settings.json` in your project root:

```json
{
  "dotnet.defaultSolution": "Ashes of Velsingrad.sln",
  "files.exclude": {
    "**/.godot/": true,
    "**/.import/": true,
    "**/*.cs.uid": true
  },
  "godotTools.editorPath.godot4": "path\\to\\your\\Godot_v4.5.1-stable_mono_win64.exe",
  "dotnet.unitTests.runSettingsPath": "./tests/.runsettings",
  "conventionalCommits.scopes": [
    "player", "combat", "inventory", "ui", "audio",
    "level", "ai", "save", "network", "build", "config"
  ]
}
```

3. **Set Up Debugging:**
   Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Godot",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${env:GODOT_BIN}",
      "args": ["--path", "${workspaceFolder}"],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole",
      "stopAtEntry": false
    }
  ]
}
```

### Configure Godot External Editor

1. Go to **Editor** → **Editor Settings** → **Dotnet** → **Editor**
2. Set **External Editor** to "Visual Studio Code and VSCodium"
3. Set **Exec Path** to your VS Code installation path

## 📁 Project Structure Overview

Here's how the project is organized:

```
AshesOfVelsingrad/
├── .github/                 # GitHub workflows and templates
├── .godot/                  # Godot engine files (auto-generated)
├── addons/                  # Godot addons (GdUnit4, etc.)
├── assets/                  # Game assets (sprites, audio, etc.)
├── documentation/           # Project documentation
│   ├── docfx/              # DocFX configuration and content
│   └── readme-intl/        # Internationalized README files
├── scenes/                  # Godot scene files (.tscn)
├── scripts/                 # C# source code
│   ├── player/             # Player-related systems
│   ├── combat/             # Combat mechanics
│   ├── ui/                 # User interface
│   └── utils/              # Utility classes
├── tests/                   # Unit and integration tests
│   ├── unit/               # Unit tests
│   └── integration/        # Integration tests
├── project.godot           # Godot project configuration
└── *.sln                   # C# solution file
```

## 🧪 Running Tests

### Prerequisites for Testing

Make sure GdUnit4 is properly installed:

```bash
dotnet add package gdUnit4.api --version 5.0.0
dotnet add package gdUnit4.test.adapter --version 3.0.0
dotnet add package gdUnit4.analyzers --version 1.0.0
```

### Running Tests

**From Godot (Recommended):**
1. Go to the **MSBuild** tab and rebuild
2. Switch to the **GdUnit4** tab
3. Click "Run discover tests"
4. Select and run your tests

**From Command Line:**
```bash
# Run all tests
dotnet test --settings tests/.runsettings

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test category
dotnet test --filter "Category=Unit"
```

**From VS Code:**
- Use the Test Explorer panel
- Install C# Dev Kit (v1.5.12 (pre-release) recommended) extension
- Tests will appear automatically

## ✅ Quick Validation

After setup, verify everything works:

1. **Build Test**: `dotnet build` (should complete without errors)
2. **Simple Scene Test**: Run the main scene in Godot
3. **Test Suite**: `dotnet test --settings tests/.runsettings`
4. **IDE Integration**: Open a .cs file and verify IntelliSense works

If all steps pass, you're ready to develop! 🎉

## 🌟 Your First Contribution

Ready to contribute? Here's a simple workflow:

### 1. Create a Feature Branch

```bash
git checkout develop
git checkout -b feat/your-feature-name
```

### 2. Make Your Changes

Follow our [coding standards](technical/coding-standards.md) and commit message conventions.

### 3. Test Your Changes

```bash
# Build the project
dotnet build

# Run tests
dotnet test --settings tests/.runsettings

# Test in Godot
# Open the project and run a scene
```

### 4. Commit and Push

```bash
# Follow Conventional Commits format
git commit -m "feat(player): add wall jump ability"
git push origin feat/your-feature-name
```

### 5. Create a Pull Request

- Target the `develop` branch
- Add a clear description
- Reference any related issues

## 📚 Next Steps

Now that you have the project running, here are some suggested next steps:

1. **Explore the Codebase**: Start with the `scripts/` folder to understand the architecture
2. **Read the Documentation**: Check out our [Technical Guides](technical/architecture.md)
3. **Join the Community**: Connect with us on [Discord](https://discord.gg/VCTrCrasp9)
4. **Check Issues**: Look for "good first issue" labels on GitHub

## 🚨 Troubleshooting

### Common Issues

**Build Errors:**
```bash
# Clean and rebuild
dotnet clean
dotnet build --no-restore
```

**Tests Not Running:**
- Verify `GODOT_BIN` environment variable is set correctly
- Make sure you're using the Mono version of Godot
- Restart your terminal/IDE after setting environment variables

**VS Code IntelliSense Issues:**
- Restart VS Code
- Run "Developer: Reload Window" command
- In Godot: **Project** → **Tools** → **C#** → **Sync C# Project**

**Permission Issues on Windows:**
- Run VS Code or Godot as Administrator
- Check that your antivirus isn't blocking files

### Getting Help

If you're still having issues:

1. Check our [GitHub Issues](https://github.com/Gammabra/aov-godot/issues)
2. Join our [Discord community](https://discord.gg/VCTrCrasp9)
3. Read the detailed [Technical Documentation](technical/testing.md)

## 🎯 What's Next?

- **Learn the Game Design**: Read about our [Core Gameplay](game-design/core-gameplay.md)
- **Understand the Architecture**: Check out our [Technical Documentation](technical/architecture.md)
- **Contribute**: Read our [Contributing Guide](contributing.md)

Welcome to the team! We're excited to see what you'll build with us.

---

*"Every great journey begins with a single step. Thank you for taking that step with us into the world of Velsingrad."*

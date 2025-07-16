# Godot 4.4.1 Setup Guide with C# and GdUnit4

## Table of Contents
1. [External Editor Configuration](#external-editor-configuration)
2. [Unit Testing Configuration with GdUnit4](#unit-testing-configuration-with-gdunit4)
3. [Project Structure](#project-structure)
4. [Advanced Configuration](#advanced-configuration)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

## External Editor Configuration

### Visual Studio Code (Recommended)

1. **Install required extensions:**
   - C# Dev Kit (Microsoft)
   - C# (Microsoft)
   - godot-tools (optional, for .gd file syntax highlighting)

2. **Configure in Godot:**
   - In `Editor → Editor settings → Dotnet → Editor `, set:
     - **External Editor**: `Visual Studio Code and VSCodium`
     - **Exec Path**: Path to your VS Code executable
       - Windows: `C:\Users\[username]\AppData\Local\Programs\Microsoft VS Code\Code.exe`
       - Linux: `/usr/bin/code`
       - macOS: `/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code`

3. **Configure VS Code workspace:**
   Create a `.vscode/settings.json` file at the project root:
   ```json
   {
     "dotnet.defaultSolution": "Ashes of Velsingrad.sln",
     "files.exclude": {
       "**/.godot/": true,
       "**/.import/": true
     }
   }
   ```

### Visual Studio (Alternative)

1. **Configure in Godot:**
   - **External Editor**: `Visual Studio`
   - **Exec Path**: Path to devenv.exe
     - Example: `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe`
   - **Exec Flags**: `{project} --goto {file}:{line}:{col}`

### JetBrains Rider (Alternative)

1. **Configure in Godot:**
   - **External Editor**: `JetBrains Rider and Fleet`
   - **Exec Path**: Path to rider64.exe
   - **Exec Flags**: `{project} --line {line} {file}`

## Unit Testing Configuration with GdUnit4

### Environment Setup

1. **Set the `GODOT_BIN` environment variable:**

    This variable must point to the Godot Mono executable (for example, `Godot_v4.4.1-stable_mono_win64.exe`). It is required to run C# tests with GdUnit4.
    You can set this variable:
    - System-wide (recommended): via your operating system's environment variables:

    - **Variable name:** `GODOT_BIN`
    - **Value:** Full path to the Godot Mono executable
    - **Example:** `C:\Program Files\Godot\Godot_v4.4.1-stable_mono_win64.exe`

    - Or locally for tests, by adding it to the `Tests/.runsettings` file, inside `RunConfiguration`:

    ```xml
    <EnvironmentVariables>
         <GODOT_BIN>C:\path\to\Godot_v4.4.1-stable_mono_win64.exe</GODOT_BIN>
    </EnvironmentVariables>
    ```

    > **Tip:** Prefer system-wide configuration to avoid updating the configuration file each time you change machines or installation paths.

2. **Add GdUnit4Net NuGet packages:**
   Run these commands in your project directory:
   ```bash
   dotnet add package gdUnit4.api --version 5.0.0
   dotnet add package gdUnit4.test.adapter --version 3.0.0
   dotnet add package gdUnit4.analyzers --version 1.0.0
   ```

### Installation Verification

Your project structure should look like this:

```
AshesOfVelsingrad/
├── addons/
│   └── gdUnit4/
├── Tests/
│   ├── Unit/
│   │   └── TestTemp.cs
│   ├── Integration/
│   └── .runsettings
└── project.godot
```

### C# Project Testing Configuration

1. **Verify NuGet packages** in your `.csproj` file:
   ```xml
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="gdUnit4.api" Version="5.0.0" />
    <PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />
    <PackageReference Include="gdUnit4.analyzers" Version="1.0.0">
   ```

2. **Enhanced unit test example:**
   ```csharp
   using GdUnit4;
   using Godot;
   using static GdUnit4.Assertions;

   namespace Tests.Unit
   {
       [TestSuite]
       public class UnitTestExample
       {
           [TestCase]
           public void TestBasicAssertion()
           {
               AssertThat(2 + 2).IsEqual(4);
           }

           [TestCase]
           public void TestGodotNode()
           {
               var node = new Node();
               node.Name = "TestNode";

               AssertThat(node.Name).IsEqual("TestNode");
               AssertThat(node).IsNotNull();
           }

           [TestCase]
           public void TestWithTimeout()
           {
               // Test with 2 second timeout
               AssertThat(() => SlowOperation()).IsEqual("result").WithTimeout(2000);
           }

           private string SlowOperation()
           {
               // Simulate slow operation
               return "result";
           }
       }
   }
   ```

### Running Tests

1. **From VS Code:**
   - Use the C# Dev Kit extension
   - Open the "Test Explorer" panel
   - Click "Run All Tests"

2. **From command line:**
   ```bash
   dotnet test --settings Tests/.runsettings
   ```

3. **From Godot:**
   - Go to the "GdUnit4" tab
   - Select your tests and click "Run"

## Project Structure

### Organization

```
YourProject/
├── addons/
│   └── gdUnit4/
├── Scripts/
│   ├── Player/
│   ├── Enemy/
│   ├── UI/
│   └── Utils/
├── Tests/
│   ├── Unit/
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── UI/
│   │   └── Utils/
│   ├── Integration/
│   └── .runsettings
├── Scenes/
├── Resources/
├── .editorconfig (already configured)
├── .gitignore (already configured)
├── CONTRIBUTING.md (already present)
└── project.godot
```

### File Organization Principles

1. **Mirror test structure**: Test files should mirror your main script structure
2. **Separate concerns**: Keep unit tests and integration tests in separate folders
3. **Follow naming conventions**: Use clear, descriptive names following the project's CONTRIBUTING.md guidelines

## Advanced Configuration

### Debugger Configuration

For VS Code, create `.vscode/launch.json`:

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

### Task Configuration

Create `.vscode/tasks.json` for build tasks:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build"],
      "group": "build",
      "presentation": {
        "echo": true,
        "reveal": "silent",
        "focus": false,
        "panel": "shared"
      },
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": ["test", "--settings", "Tests/.runsettings"],
      "group": "test",
      "presentation": {
        "echo": true,
        "reveal": "always",
        "focus": false,
        "panel": "shared"
      }
    }
  ]
}
```

## Best Practices

### Unit Testing

1. **Test naming:**
   - Use descriptive names: `Should_ReturnTrue_When_PlayerIsAlive`
   - Follow AAA pattern (Arrange, Act, Assert)

2. **Organization:**
   - One test file per tested class
   - Group tests by functionality

3. **Mocking:**
   ```csharp
   [TestCase]
   public void TestWithMock()
   {
       var mockNode = AutoFree(Mock(Node.class));

       // Configure mock
       When(mockNode.GetName()).ThenReturn("MockedNode");

       // Test
       AssertThat(mockNode.GetName()).IsEqual("MockedNode");
   }
   ```

### C# Code in Godot

1. **Use Godot attributes:**
   ```csharp
   [Export] public int Health { get; set; } = 100;
   [Signal] public delegate void HealthChangedEventHandler(int newHealth);
   ```

2. **Resource management:**
   ```csharp
   public override void _ExitTree()
   {
       // Clean up resources
       base._ExitTree();
   }
   ```

### Commit Messages

Follow the project's CONTRIBUTING.md guidelines for commit message conventions. The project uses Conventional Commits with specific scopes like `player`, `combat`, `inventory`, `ui`, `audio`, `level`, `ai`, `save`, `network`, `build`, and `config`.

## Troubleshooting

### Common Issues

1. **Tests don't run:**
   - Check that the `GODOT_BIN` environment variable is correctly set
   - Ensure GdUnit4 is enabled in the project plugins
   - Verify the path points to the mono version: `Godot_v4.4.1-stable_mono_win64.exe`

2. **IntelliSense not working:**
   - Regenerate project files: `Project → Tools → C# → Sync C# Project`
   - Restart your editor

3. **Build errors:**
   - Check that your .NET version is compatible (6.0+ recommended)
   - Clean and rebuild: `dotnet clean && dotnet build`

### Useful Commands

```bash
# Clean project
dotnet clean

# Complete rebuild
dotnet build --no-restore

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Add GdUnit4Net packages
dotnet add package gdUnit4.api --version 5.0.0
dotnet add package gdUnit4.test.adapter --version 3.0.0
dotnet add package gdUnit4.analyzers --version 1.0.0
```

### Logging and Debugging

1. **Enable detailed logs in Godot:**
   - `Project → Project Settings → Debug → Settings`
   - Enable "Verbose stdout"

2. **Logs in tests:**
   ```csharp
   [TestCase]
   public void TestWithLogging()
   {
       GD.Print("Debug message from test");
       AssertThat(true).IsTrue();
   }
   ```

### Environment Variable Setup (Windows)

1. **Via System Properties:**
   - Press `Win + R`, type `sysdm.cpl`
   - Go to "Advanced" tab → "Environment Variables"
   - Add new system variable:
     - Name: `GODOT_BIN`
     - Value: `C:\path\to\Godot_v4.4.1-stable_mono_win64.exe`

2. **Via Command Line:**
   ```cmd
   setx GODOT_BIN "C:\path\to\Godot_v4.4.1-stable_mono_win64.exe"
   ```

3. **Via PowerShell:**
   ```powershell
   [Environment]::SetEnvironmentVariable("GODOT_BIN", "C:\path\to\Godot_v4.4.1-stable_mono_win64.exe", "Machine")
   ```

## Additional Resources

- [GdUnit4 Documentation](https://mikeschulze.github.io/gdUnit4/)
- [Godot C# Documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)
- [GdUnit4Net Documentation](https://github.com/MikeSchulze/gdUnit4Net)
- [Project CONTRIBUTING.md](./CONTRIBUTING.md) for commit conventions and project guidelines

This documentation should help you effectively set up your development environment and tests. Don't hesitate to ask if you have specific questions about any of these aspects!

# Testing Guide: Ashes of Velsingrad

This document outlines the testing strategy for **Ashes of Velsingrad**. We utilize a dual-layer testing approach to maintain 100% logic coverage while ensuring engine stability.

## Table of Contents

1. [External Editor Configuration](#external-editor-configuration)
2. [The Two-Tier Testing Strategy](#the-two-tier-testing-strategy)
3. [Tier 1: Core Unit Testing (NUnit)](#tier-1-core-unit-testing-nunit)
4. [Tier 2: Godot Integration Testing (GdUnit4)](#tier-2-godot-integration-testing-gdunit4)
5. [Environment Configuration](#environment-configuration)
6. [Best Practices & Troubleshooting](#best-practices--troubleshooting)

## External Editor Configuration

### Visual Studio Code (Recommended)

1. **Required Extensions**:
   - **C# Dev Kit** (Microsoft)
   - **Godot Tools** (For ```.tscn``` and ```.gd``` support)
2. **Workspace Settings** (```.vscode/settings.json```):

```json
{
  "dotnet.defaultSolution": "AshesofVelsingrad.sln",
  "godotTools.editorPath.godot4": "path/to/Godot_v4.6-stable_mono.exe",
  "dotnet.unitTests.runSettingsPath": "./AshesofVelsingrad/integration_tests/.runsettings",
  "files.exclude": {
    "**/.godot/": true,
    "**/.import/": true
  }
}
```

## The Two-Tier Testing Strategy

| **Tier** | **Target** | **Framework** | **Speed** | **Requirement** |
|------|--------|-----------|-------|-------------|
| **Tier 1: Logic** | ```AshesofVelsingrad.Core,NUnit + Moq``` | **Ultra Fast** | **.NET SDK Only** |
| **Tier 2: Engine** | ```AshesofVelsingrad``` (Adapter) | **GdUnit4** | **Moderate** |

## Tier 1: Core Unit Testing (NUnit)

These tests validate your game rules, AI math, and data structures. They do **not** require Godot to run.

**Location**: ```AshesofVelsingrad.Core.Tests/```

```csharp
[TestFixture]
public class CombatLogicTests {
    [Test]
    public void Damage_ShouldBeMitigated_ByArmor() {
        // Arrange
        var defender = new HealthSystem(hp: 100, armor: 10);
        
        // Act
        defender.TakeDamage(20);
        
        // Assert
        Assert.That(defender.CurrentHealth, Is.EqualTo(90));
    }
}
```

## Tier 2: Godot Integration Testing (GdUnit4)

These tests ensure that your Godot Nodes correctly wrap the Core logic and that signals/animations trigger as expected.
**Location**: ```AshesofVelsingrad/integration_tests/```

### Environment Setup (GODOT_BIN)

GdUnit4 requires the ```GODOT_BIN``` environment variable to point to your Godot Mono executable.
- **Windows**: ```setx GODOT_BIN "C:\Path\To\Godot_v4.6_mono.exe"```
- **Linux/macOS**: ```export GODOT_BIN="/path/to/godot"```

GdUnit4 Test Example

```csharp
namespace Tests.Integration {
    [TestSuite]
    public class PlayerActorTests {
        [TestCase]
        [RequireGodotRuntime]
        public async Task Player_ShouldPlayDeathAnimation_OnZeroHealth() {
            // Arrange
            var player = AutoFree(new PlayerActor());
            var animPlayer = player.GetNode<AnimationPlayer>("Anim");
            
            // Act
            player.CoreHealth.TakeDamage(999); // Trigger core logic
            await ISceneRunner.Wait(100);     // Wait for frame
            
            // Assert
            AssertThat(animPlayer.CurrentAnimation).IsEqual("death");
        }
    }
}
```

## Project Structure

```
AshesofVelsingrad/
├── AshesofVelsingrad.Core/            # Logic (Tested via NUnit)
├── AshesofVelsingrad.Core.Tests/      # Logic Unit Tests
└── AshesofVelsingrad/                 # Godot Adapter
    ├── addons/gdUnit4/                # Plugin
    └── integration_tests/             # GdUnit4 Tests
        └── .runsettings               # Environment config
```

## Running Tests

### 1. From VS Code (All Tests)

Open the **Test Explorer** (Beaker icon). C# Dev Kit will discover both NUnit and GdUnit4 tests. Use the "Run All" button.

### 2. From Godot Editor (Integration Only)

1. Open the **GdUnit4** bottom panel.
2. Click **Run All** to see real-time execution within the engine.

### 3. Command Line (CI/CD)

```bash
# Run Core Tests
dotnet test AshesofVelsingrad.Core.Tests

# Run Integration Tests
dotnet test AshesofVelsingrad --settings AshesofVelsingrad/integration_tests/.runsettings
```

## Best Practices

- **Mock the Boundary**: When testing Godot Nodes, use ```Mock<IUnitSystem>``` to isolate the Node's behavior from the actual Core implementation.
- **AutoFree is Mandatory**: In GdUnit4, always wrap Node creation in ```AutoFree()``` to prevent memory leaks in the Godot process.
- **No Godot in Core Tests**: If a test in ```Core.Tests fails``` because it can't find ```Godot.Vector3```, the logic is incorrectly coupled. Move the logic to a custom ```struct``` or move the test to the integration folder.

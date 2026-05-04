# Testing Guide: Ashes of Velsingrad

This document outlines the testing strategy for **Ashes of Velsingrad**. We utilize a dual-layer testing approach to maintain 100% logic coverage while ensuring engine stability.

## Table of Contents

1. [External Editor Configuration](#external-editor-configuration)
2. [The Two-Tier Testing Strategy](#the-two-tier-testing-strategy)
3. [Tier 1: Core Unit Testing (NUnit)](#tier-1-core-unit-testing-nunit)
4. [Tier 2: Godot Integration Testing (GdUnit4)](#tier-2-godot-integration-testing-gdunit4)
5. [Project Structure](#project-structure)
6. [Running Tests](#running-tests)
7. [Best Practices](#best-practices)

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

### Testing Godot Resources from Core.Tests

A few Core types (e.g. ```EntityProfile```) extend Godot's ```Resource``` because designers need to author them as ```.tres``` files. They live in Core because they carry no Godot logic — but you still need to be careful in unit tests. The ```GodotObject``` finalizer tries to free a native handle through GodotSharp's runtime; from a non-Godot test process, that call crashes the host with a non-zero exit code AFTER all assertions pass (you'll see ```Test Run Aborted: Test host process crashed``` even though every test passed).

**Always wrap Resource instances in ```using```** so ```Dispose()``` runs synchronously inside the test:

```csharp
[Test]
public void EntityProfile_Defaults_AreEmpty()
{
    using var profile = new EntityProfile();
    Assert.That(profile.DisplayName, Is.EqualTo(string.Empty));
}
```

```Dispose()``` calls ```GC.SuppressFinalize(this)``` internally, so by the time the host shuts down there are no live finalizers queued and the process exits cleanly with code 0.

### Singleton Cleanup in Integration Tests

```GameManager``` and ```TurnManager``` carry a ```private new static Instance``` field that survives across scene reloads. Tests that spawn either of these into the scene tree must reset the static between cases — otherwise the second test sees the leftover from the first and ```QueueFree```s itself as a "duplicate":

```csharp
[BeforeTest]
public void Setup()
{
    typeof(TurnManager)
        .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)!
        .SetValue(null, null);
    // …
}
```

The production code now also resets the instance via an ```_ExitTree``` override, but the reflection-based reset stays as a defensive belt-and-braces for tests that don't fully exit the tree.

### Stub vs Mock for Map / Unit Interfaces

```IUnitSystem``` and ```IMapSystem``` have wide surfaces (~30 members each). Two strategies depending on the test:

- **Moq** (auto-generated) — best for AI/decision tests where you only care about a couple of properties (```mockUnit.Setup(u => u.Hp).Returns(50)```). Adding new interface members never breaks Moq-based tests because Moq auto-stubs everything.
- **Hand-written stubs** — best when you need a delegate-driven member (```StubMap``` with a ```Func<int,int,int,bool>``` walkability rule for ```Pathfinder``` tests). Adding new interface members breaks every stub at compile time, which is sometimes what you want as a compile-time canary.

# Software Architecture

This document outlines the software architecture for **Ashes of Velsingrad**, a Godot 4.6 C# (.NET 9.0) game project. The project utilizes a **Clean Architecture** approach to decouple game rules from engine implementation.

Understanding this architecture will help you navigate the codebase, make informed decisions, and contribute effectively to the project.

## Table of Contents

- [Architectural Overview](#architectural-overview)
- [Project Segmentation](#project-segmentation)
- [Dependency Flow](#dependency-flow)
- [Core Design Principles](#core-design-principles)
- [Folder Structure Legend](#folder-structure-legend)
- [Solution Structure](#solution-structure)
- [Component Patterns](#component-patterns)
- [Communication Patterns](#communication-patterns)
- [Evolution Strategy](#evolution-strategy)
- [Architectural Guidelines](#architectural-guidelines)

## Architectural Overview

### Technology Stack

- **Engine**: Godot 4.6 (Mono version)
- **Language**: C# (.NET 9.0)
- **Unit Testing**: NUnit + Moq (for pure C# testing)
- **Integration Testing**: GdUnit4
- **Build System**: MSBuild/.NET CLI
- **Version Control**: Git with Conventional Commits

### Architectural Style

We follow a **Clean Architecture (Ports and Adapters)**:
- **Framework Independence**: The core game rules do not depend on Godot. 
- **Interface-Driven**: Assemblies communicate strictly through interfaces.
- **Event-Driven**: Using C# ```Action``` to push updates from the Core to the Godot presentation layer.

## Project Segmentation

The solution is divided into three primary assemblies to maintain a strict dependency hierarchy:

### 🟢 ```AshesofVelsingrad.Core``` (The Domain)

- **Purpose**: Contains all pure C# game logic.
- **Constraints**: **Zero reference**s to the Godot engine.
- **Contents**: ```AI/```, ```Systems/```, ```interface/```, ```Data/```, ```Utilities/```

### 🔴 ```AshesofVelsingrad.Core.Tests``` (Unit Testing)

- **Purpose**: High-speed validation of the Domain logic.
- **Goal**: Maintain 100% code coverage for all logic-heavy systems.

### 🔵 ```AshesofVelsingrad``` (The Godot Adapter)

- **Purpose**: Handles rendering, input, and physics.
- **Integration**: Connects to the Core via the ```interface/``` layer.

## Dependency Flow

We follow the principle of **Dependency Inversion**:
- **Inward Flow**: ```AshesofVelsingrad``` depends on ```AshesofVelsingrad.Core```.
- **Outward Flow**: ```AshesofVelsingrad.Core``` never knows about Godot; it simply triggers events or calls interfaces.

## Core Design Principles

### 1. Engine Decoupling

Game logic and Godot nodes are strictly separated. The core logic handles the rules, while Godot acts purely as the presentation and input layer.

### 2. Testability First

By removing Godot dependencies from the Core, we achieve 100% test coverage using standard .NET unit testing frameworks.

```csharp
// AshesofVelsingrad.Core (Pure C# - 100% Coverage)
public class InventoryLogic {
    private readonly List<IItem> _items = new();
    public void AddItem(IItem item) {
        if (item.Weight + CurrentWeight <= MaxWeight)
            _items.Add(item);
    }
}
```

## Folder Structure Legend

| **Path** | **Responsibility** | **Layer** |
|------|----------------|-------|
| ```AshesofVelsingrad.Core/AI``` | Tactical math and logic | Domain |
| ```AshesofVelsingrad.Core/interface``` | Communication contracts | Domain |
| ```AshesofVelsingrad/scripts/managers``` | Godot AutoLoads (Singletons) | Adapter |
| ```AshesofVelsingrad/integration_tests``` | Engine-based testing | Testing |

## Solution Structure

The project is split into three distinct assemblies to enforce architectural boundaries:

```
AshesofVelsingrad/ (Solution Root)
│
├── AshesofVelsingrad.Core/            # Assembly 1: PURE C# LOGIC
│   ├── AI/                            # Pathfinding math & tactical logic
│   ├── Data/                          # Enums and unit data structures
│   ├── interface/                     # Boundary definitions (IMapSystem, etc)
│   ├── Systems/                       # Combat, movement, status effects
│   └── Utilities/                     # Generic C# math helpers
│
├── AshesofVelsingrad.Core.Tests/      # Assembly 2: UNIT TESTS
│   ├── AI/                            # Tests for AI logic
│   ├── Data/                          # Tests for data parsing
│   ├── Systems/                       # Tests for combat/logic rules
│   └── Utilities/                     # Tests for helpers
│
└── AshesofVelsingrad/                 # Assembly 3: GODOT ENGINE
    ├── assets/                        # 3D Models, Textures, Audio
    ├── integration_tests/             # GdUnit4 Scene/Node tests
    ├── scenes/                        # .tscn files (UI, Units, Levels)
    └── scripts/                       # Godot C# wrappers
        ├── managers/                  # AutoLoads (GameManager, etc.)
        └── ui/                        # HUD and Menu controllers
```

## Battle Layer

The battle layer is built on the same Clean-Architecture split — pure rules in `AshesofVelsingrad.Core`, Godot wrappers in `AshesofVelsingrad/scripts`.

### Faction Model

Three factions drive the turn flow and target validation:

| Faction | Source | Turn Dispatch | Renders With |
|---------|--------|---------------|--------------|
| `Player` | Player input | `OnPlayerTurn` | Blue marker, full HUD |
| `Ally` | AI (friendly guest) | `OnAllyTurn` | Green marker, HUD hidden |
| `Enemy` | AI (hostile) | `OnEnemyTurn` | Red marker, HUD hidden |

Faction relationships live in `Faction.cs` as extension methods (`IsHostileTo`, `IsFriendlyTo`, `IsAiControlled`). Every skill targeting check, AI target picker, and HUD visibility toggle reads from these. Player + Ally are mutually friendly; both are hostile to Enemy.

### Skill Catalogue + Localization

Skills are hand-coded `SkillSystem` subclasses grouped by archetype (Fighter, Swordsman, Assassin, Archer, Mage Fire/Water/Earth/Light/Dark). Names and descriptions read from `SkillStrings.cs`, a single static class of `public const string` fields. Today every constant is English; introducing a second language is a one-file swap to `L10n.Tr("skill.charge.name")` against Godot's `TranslationServer` or a custom dictionary.

Skills can opt into per-cell targeting rules by overriding `IsTargetCellValid(caster, x, y, z, map)`. `Charge` uses this to enforce cardinal alignment; the GameManager target-tile filter calls the predicate so only valid red tiles light up.

### HUD Composition

The HUD is fully programmatic — no `.tscn` resource needed. `BattleHud : CanvasLayer` exposes seven child widgets and a `Build()` method that creates them and forwards to each child's `EnsureBuilt()`. Every widget owns a `_built` flag so `Build()` is idempotent and safe to call before `_Ready` fires.

```text
BattleHud (CanvasLayer, layer=100)
├── ActionMenu        (Move / Attack / Skill / Pass / Cancel)
├── SkillSelector     (5-slot bar, hidden on AI turns)
├── PlayerStatusPanel (HP / MP, auto-refreshes each frame)
├── EnemyRoster       (per-enemy health bars)
├── TurnOrderQueue    (upcoming-turn chips with portraits)
├── ContextInfoPanel  (movement budget vs queued-skill details)
└── BattleLog         (subscribed to BattleNotifications.Posted)
```

End-screens live above the HUD: `VictoryScreen` (layer 110) and `GameOverScreen` (layer 120). Both follow the same `EnsureBuilt()` pattern and are spawned by `GameManager.CheckWinLoseCondition`.

### Indicators + Pathfinder

Three `MeshInstance3D`-backed overlays floating just above the GridMap:

- `MoveIndicator` — green tiles for valid moves
- `TargetIndicator` — red tiles for valid skill targets
- `HoverIndicator` — yellow tile under the cursor

All three are managed by `IndicatorOverlay`, parented to the map. Player movement uses `Pathfinder.FindPath` (BFS) to build an A*-style step list, then a `Tween` follows the path with `body.GlobalPosition` interpolation.

A small `FactionMarker` Node3D bobs over each unit, colour-coded by faction; the marker pulses brighter on the active turn so the player can identify whose turn it is from any camera angle.

### Critical Adapter Patterns

A few patterns earned dedicated lifecycle handling:

- **Deferred `AddChild` for CanvasLayers.** Adding a CanvasLayer programmatically *during* another node's `_Ready` can leave it in a state where Godot never dispatches its own `_Ready`, and 2D rendering quietly fails. `GameManager.EnsureHud` queues the add via `host.CallDeferred("add_child", _battleHud)` so the node enters the tree at the start of the next idle frame.
- **Idempotent `EnsureBuilt`.** Each HUD widget exposes a public `EnsureBuilt()` that runs `BuildLayout` exactly once (guarded by `_built`). `BattleHud.Build()` calls it on every child synchronously, so HUD construction is decoupled from `_Ready` timing — every widget is fully built and bindable the moment `EnsureHud` returns.
- **Main-thread turn loop.** `TurnManager.StartBattle` uses `await ProcessTurn()` directly (not `Task.Run`). Running the loop on a worker thread silently corrupts Godot's renderer state because the event handlers it fires touch scene/render APIs from off-thread.
- **Singleton cleanup on `_ExitTree`.** `GameManager` and `TurnManager` use a `private new static Instance` that hides `BaseManager.Instance`. Without an explicit `_ExitTree` override that nulls out the derived static, `ReloadCurrentScene` (used by Try Again) leaves the new instance to QueueFree itself as a "duplicate".

## Component Patterns

### The Wrapper Pattern (Godot to Core)

The Godot Node acts as a visual wrapper for the Core logic.

```csharp
// 1. Interface in Core/interface
public interface IHealthSystem {
    int CurrentHealth { get; }
    void TakeDamage(int amount);
    event Action OnDeath;
}

// 2. Implementation in Core/Systems
public class HealthSystem : IHealthSystem {
    public int CurrentHealth { get; private set; }
    public event Action OnDeath;
    public void TakeDamage(int amount) {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0) OnDeath?.Invoke();
    }
}

// 3. Godot Wrapper in AshesofVelsingrad/scripts
public partial class PlayerActor : CharacterBody3D {
    private IHealthSystem _coreHealth;
    public override void _Ready() {
        _coreHealth = new HealthSystem(100);
        _coreHealth.OnDeath += () => GetNode<AnimationPlayer>("Anim").Play("death");
    }
}
```

## Communication Patterns

### Interface Implementation

The Core defines the "What," and Godot provides the "How."

```csharp
// Core System (AshesofVelsingrad.Core/interface)
public interface IUnitSystem {
    event Action<int> OnHealthChanged; 
}

// Godot Wrapper (AshesofVelsingrad/scripts)
public partial class UnitNode : Node3D {
    [Signal] public delegate void VisualHealthChangedEventHandler(int val); 
}
```

## Evolution Strategy

This architecture is designed to evolve with the project:

1. **Phase 1 (Current)**: Clean Architecture split (Core vs Engine) with 100% Core coverage.
2. **Phase 2**: Full UI integration mapping Core events to Godot signals and tweens
3. **Phase 3**: Multiplayer synchronization by serializing the pure Core state across the network.
4. **Phase 4**: Abstracted Audio/Save implementations via interfaces.

## Architectural Guidelines

### Do's ✅
- **DO** keep the ```AshesofVelsingrad.Core``` project entirely free of ```using Godot;```.
- **DO** use interfaces to communicate between the engine and logic.
- **DO** maintain 100% code coverage in the Core.

### Don'ts ❌
- **DON'T** pass Godot nodes (like ```Node3D```) into Core functions. Pass coordinates or IDs instead.
- **DON'T** put game rules (like "how much damage a sword does") inside a Godot script.
- **DON'T** write unit tests for Godot nodes in ```AshesofVelsingrad.Core.Tests```.

The modular design ensures that individual systems can be refactored or replaced without affecting the entire codebase, supporting the project's long-term maintenance and scalability goals.

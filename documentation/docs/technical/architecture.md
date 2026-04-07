# Software Architecture

This document outlines the software architecture for **Ashes of Velsingrad**, a Godot 4.6 C# (.NET 9.0) game project. The project utilizes a **Clean Architecture** approach to decouple game rules from engine implementation.

Understanding this architecture will help you navigate the codebase, make informed decisions, and contribute effectively to the project.

## Table of Contents

- [Architectural Overview](#architectural-overview)
- [Core Design Principles](#core-design-principles)
- [Solution Structure](#solution-structure)
- [System Architecture](#system-architecture)
- [Component Patterns](#component-patterns)
- [Data Flow](#data-flow)
- [Scene Management](#scene-management)
- [Performance Considerations](#performance-considerations)
- [Integration Points](#integration-points)

- [Architectural Overview](#architectural-overview)
- [Project Segmentation](#projet-segementation)
- [Dependency Flow](#dependency-flow)
- [Core Design Principles](#core-design-principles)
- [Folder Structure Legend](#folder-structure-legend)
- [Solution Structure](#solution-structure)
- [AutoLoad Configuration](#autoload-configuration)
- [System Architecture](#system-architecture)
- [Component Patterns](#component-patterns)
- [Communication Patterns](#communication-patterns)
- [Data Flow](#data-flow)
- [Scene Management](#scene-management)
- [Performance Considerations](#performance-considerations)
- [Integration Points](#integration-points)
- [Architectural Guidelines](#architectural-guidelines)
- [Evolution Strategy](#evolution-strategy)

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

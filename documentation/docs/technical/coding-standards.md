# Coding Standards

This document outlines the coding standards and conventions for our Godot 4.4.1 C# (.NET 9.0) game project. These standards ensure code consistency, maintainability, and team collaboration.

## Table of Contents

- [General Principles](#general-principles)
- [File Organization](#file-organization)
- [C# Coding Standards](#c-coding-standards)
- [Godot-Specific Guidelines](#godot-specific-guidelines)
- [Naming Conventions](#naming-conventions)
- [Code Formatting](#code-formatting)
- [Comments and Documentation](#comments-and-documentation)
- [Error Handling](#error-handling)
- [Performance Guidelines](#performance-guidelines)

## General Principles

### Code Quality
- **Readability First**: Write code that tells a story. Code is read more often than it's written.
- **KISS (Keep It Simple, Stupid)**: Favor simple solutions over complex ones.
- **DRY (Don't Repeat Yourself)**: Avoid code duplication through proper abstraction.
- **YAGNI (You Aren't Gonna Need It)**: Don't implement features until they're actually needed.

### Consistency
- Follow the established patterns in the codebase.
- Use the same naming conventions and code structure throughout the project.
- Maintain consistent indentation and formatting as defined in `.editorconfig`.

## File Organization

### Directory Structure
```
Scripts/
├── Actors/          # Player, enemies, NPCs
├── Components/      # Reusable game components
├── Controllers/     # Input handlers, game controllers
├── Data/           # Data classes, scriptable objects
├── Managers/       # Singleton managers (GameManager, AudioManager, etc.)
├── UI/             # User interface scripts
├── Utilities/      # Helper classes and extensions
└── Systems/        # Game systems (inventory, dialogue, etc.)
```

### File Naming
- Use PascalCase for C# files: `PlayerController.cs`
- Use snake_case for GDScript files: `player_controller.gd`
- Use descriptive names that clearly indicate the file's purpose
- Avoid abbreviations unless they're widely understood

## C# Coding Standards

### Class Structure
Organize class members in the following order:
1. Constants
2. Static fields
3. Fields (private first, then protected, then public)
4. Constructors
5. Properties
6. Events
7. Methods (private first, then protected, then public)
8. Nested types

```csharp
public class PlayerController : CharacterBody2D
{
    // Constants
    private const float DefaultSpeed = 300.0f;

    // Fields
    private float _speed;
    private Vector2 _velocity;

    // Properties
    public float Speed
    {
        get => _speed;
        set => _speed = Mathf.Max(0, value);
    }

    // Godot lifecycle methods
    public override void _Ready()
    {
        _speed = DefaultSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleMovement();
    }

    // Private methods
    private void HandleMovement()
    {
        // Implementation
    }
}
```

### Access Modifiers
- Always specify access modifiers explicitly
- Use the most restrictive access level possible
- Prefer `private` for internal implementation details
- Use `protected` for members that should be accessible to derived classes
- Use `public` only for the class's interface

### Properties vs Fields
- Use properties for public data access
- Use auto-properties when no additional logic is needed
- Use full properties when validation or side effects are required

```csharp
// Good - Auto property
public int Health { get; set; }

// Good - Property with validation
public int MaxHealth
{
    get => _maxHealth;
    set => _maxHealth = Mathf.Max(1, value);
}

// Avoid - Public fields
public int health; // Don't do this
```

## Godot-Specific Guidelines

### Node References
- Use `GetNode<T>()` for type-safe node access
- Cache node references in `_Ready()` when possible
- Use `@export` for designer-configurable values

```csharp
public partial class PlayerController : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    [Export] public float JumpVelocity = -400.0f;

    private AnimationPlayer _animationPlayer;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
    }
}
```

### Signals
- Use PascalCase for signal names
- Provide clear, descriptive signal names
- Document signal parameters

```csharp
[Signal]
public delegate void HealthChangedEventHandler(int newHealth, int maxHealth);

[Signal]
public delegate void PlayerDiedEventHandler();
```

### Scene Management
- Use `GetTree().ChangeSceneToFile()` for scene transitions
- Keep scene paths in constants or configuration files
- Handle scene loading errors gracefully

## Naming Conventions

### Classes and Interfaces
- **Classes**: PascalCase (`PlayerController`, `InventorySystem`)
- **Interfaces**: PascalCase with 'I' prefix (`IInteractable`, `IDamageable`)
- **Abstract Classes**: PascalCase, consider 'Base' prefix (`BaseWeapon`)

### Methods and Properties
- **Methods**: PascalCase (`GetPlayerInput`, `ProcessMovement`)
- **Properties**: PascalCase (`CurrentHealth`, `IsGrounded`)
- **Events**: PascalCase with event-like names (`HealthChanged`, `PlayerDied`)

### Fields and Variables
- **Private fields**: camelCase with underscore prefix (`_currentHealth`, `_isGrounded`)
- **Local variables**: camelCase (`deltaTime`, `inputVector`)
- **Constants**: PascalCase (`MaxPlayerCount`, `DefaultSpeed`)
- **Static readonly**: PascalCase (`DefaultSettings`)

### Godot-Specific
- **Exported fields**: PascalCase (`Speed`, `MaxHealth`)
- **Signal names**: PascalCase (`HealthChanged`, `ItemCollected`)
- **Node paths**: Use descriptive names in PascalCase

## Code Formatting

### Braces and Indentation
- Use Allman style (braces on new lines)
- 4 spaces for indentation in C#
- Tabs for GDScript files

```csharp
// Good
if (condition)
{
    DoSomething();
}
else
{
    DoSomethingElse();
}

// Good - Single line statements can omit braces for simple cases
if (isDebug)
    GD.Print("Debug message");
```

### Line Length and Spacing
- Maximum 120 characters per line for C#
- Add spaces around operators and after keywords
- No trailing whitespace
- Empty line between logical sections

```csharp
// Good spacing
public void ProcessInput(double delta)
{
    Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

    if (direction != Vector2.Zero)
    {
        _velocity.X = direction.X * Speed;
    }
    else
    {
        _velocity.X = Mathf.MoveToward(_velocity.X, 0, Speed);
    }
}
```

## Comments and Documentation

### XML Documentation
Use XML documentation for public APIs:

```csharp
/// <summary>
/// Applies damage to the character and triggers appropriate responses.
/// </summary>
/// <param name="damage">The amount of damage to apply</param>
/// <param name="damageType">The type of damage being applied</param>
/// <returns>True if the character was killed by this damage</returns>
public bool TakeDamage(int damage, DamageType damageType)
{
    // Implementation
}
```

### Inline Comments
- Explain **why**, not **what**
- Use `//` for single-line comments
- Use `/* */` for multi-line comments
- Keep comments up-to-date with code changes

```csharp
// Calculate movement with coyote time to improve player experience
if (_timeSinceGrounded < CoyoteTime)
{
    _velocity.Y = JumpVelocity;
}

/*
 * This complex calculation handles the non-linear relationship
 * between input sensitivity and camera movement speed
 */
float sensitivity = Mathf.Pow(rawInput, SensitivityCurve);
```

### TODO Comments
Use consistent format for temporary comments:

```csharp
// TODO: Implement save/load functionality
// FIXME: Character sometimes clips through walls on steep slopes
// HACK: Temporary workaround for Godot audio bug - remove in v4.5
```

## Error Handling

### Exception Handling
- Use specific exception types when possible
- Handle exceptions at the appropriate level
- Log errors with sufficient context
- Don't catch exceptions you can't handle meaningfully

```csharp
try
{
    var saveData = SaveManager.LoadGame(saveSlot);
    ApplyGameState(saveData);
}
catch (FileNotFoundException)
{
    GD.PrintErr($"Save file not found for slot {saveSlot}");
    StartNewGame();
}
catch (JsonException ex)
{
    GD.PrintErr($"Corrupted save file: {ex.Message}");
    ShowCorruptedSaveDialog();
}
```

### Godot Error Handling
- Check for null before using node references
- Validate export variables in `_Ready()`
- Use Godot's built-in error codes when appropriate

```csharp
public override void _Ready()
{
    _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    if (_animationPlayer == null)
    {
        GD.PrintErr("AnimationPlayer node not found!");
        return;
    }

    if (Speed <= 0)
    {
        GD.PrintErr("Speed must be greater than 0");
        Speed = 300.0f; // Fallback value
    }
}
```

## Performance Guidelines

### Memory Management
- Minimize allocations in frequently called methods (`_Process`, `_PhysicsProcess`)
- Reuse objects when possible (object pooling for bullets, particles, etc.)
- Dispose of resources properly
- Avoid boxing/unboxing in hot paths

```csharp
// Good - Reuse Vector2 instances
private Vector2 _tempVector = Vector2.Zero;

public override void _PhysicsProcess(double delta)
{
    _tempVector.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
    _tempVector.Y = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");

    if (_tempVector.LengthSquared() > 0.01f) // Use LengthSquared() instead of Length()
    {
        ProcessMovement(_tempVector);
    }
}
```

### Godot Performance
- Use `_PhysicsProcess` for physics-related updates
- Use `_Process` for frame-rate dependent updates
- Cache node references instead of calling `GetNode()` repeatedly
- Use appropriate collision layers and masks
- Consider using `PackedScene.Instantiate()` with object pooling for frequently spawned objects

### General Performance Tips
- Prefer early returns to reduce nesting
- Use appropriate data structures (Dictionary vs Array)
- Profile before optimizing
- Avoid premature optimization

```csharp
// Good - Early return reduces nesting
public void ProcessCollision(KinematicCollision2D collision)
{
    if (collision == null)
        return;

    if (!collision.GetCollider().IsInGroup("interactive"))
        return;

    // Process interaction
    HandleInteraction(collision.GetCollider());
}
```

---

## Enforcement

These standards are enforced through:
- **EditorConfig**: Automatic formatting rules
- **Code Reviews**: Manual verification during pull requests
- **Static Analysis**: Consider tools like SonarQube or Rider inspections
- **Team Discussions**: Regular reviews and updates to standards

Remember: These standards are guidelines to improve code quality and team productivity. When in doubt, prioritize readability and maintainability over strict adherence to rules.

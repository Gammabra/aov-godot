using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using Godot;

namespace UnitTests;

/// <summary>
/// Concrete test-double for UnitSystem.
/// Matches the testing style and philosophy of TestConcreteMapSystem.
/// </summary>
public sealed partial class TestConcreteUnitSystem : UnitSystem
{
    public bool IsInitialized { get; private set; }
    public bool IsCleanedUp { get; private set; }
    public int InitializeCallCount { get; private set; }

    public StatusEffectSystem? InjectedStatusEffectSystem => _injectedStatusEffectSystem;
    private StatusEffectSystem? _injectedStatusEffectSystem;

    public readonly List<string> Log = [];

    public TestConcreteUnitSystem()
    {
        Name = "TestConcreteUnitSystem";
        GD.Print("[TEST] TestConcreteUnitSystem constructor called");
    }

    protected override void Initialize()
    {
        base.Initialize();
        IsInitialized = true;
        InitializeCallCount++;

        // Default test values
        UnitName = "TestUnit";
        Description = "Unit used only for unit testing.";
        MaxHp = 100;
        Hp = 100;
        BaseAtk = 10;
        BaseDef = 5;
        BaseSpeed = 4;
        ManaMax = 100;
        Mana = ManaMax;
        PossibleMovesRange = 1;
        Type = UnitType.Player;

        Log.Add("Initialized");

        GD.Print("[TEST] TestConcreteUnitSystem initialized");
    }

    public override void InjectDependencies(StatusEffectSystem statusEffectSystem)
    {
        base.InjectDependencies(statusEffectSystem);
        _injectedStatusEffectSystem = statusEffectSystem;

        Log.Add("StatusEffectSystem injected");
    }

    protected override void Cleanup()
    {
        IsCleanedUp = true;
        Log.Add("CleanedUp");

        GD.Print("[TEST] TestConcreteUnitSystem cleanup called");
    }

    // ---------- Public test helpers ----------

    public void CallReady()
    {
        _Ready();
    }

    public void CallInitialize()
    {
        Initialize();
    }

    public void CallCleanup()
    {
        Cleanup();
    }

    // Allows adding a fake sprite for Initialize() to detect
    public void AddFakeSprite()
    {
        Sprite3D sprite = new()
        {
            Name = "FakeSprite"
        };
        AddChild(sprite);
    }
}

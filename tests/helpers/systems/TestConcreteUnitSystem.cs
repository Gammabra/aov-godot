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

    public StatusEffectSystem? InjectedStatusEffectSystem => _injectedStatusEffectSystem;
    private StatusEffectSystem? _injectedStatusEffectSystem;

    public readonly List<string> Log = [];

    public TestConcreteUnitSystem(
        string unitName = "TestUnit",
        string description = "Unit used only for unit testing.",
        float maxHp = 100,
        float hp = 100,
        float baseAtk = 10,
        float baseDef = 5,
        float baseSpeed = 4,
        float manaPoint = 100,
        int possibleMovesRange = 1
    )
    {
        Name = "TestConcreteUnitSystem";

        IsInitialized = true;
        UnitName = unitName;
        Description = description;
        MaxHp = maxHp;
        Hp = hp;
        BaseAtk = baseAtk;
        BaseDef = baseDef;
        BaseSpeed = baseSpeed;
        ManaPoint = manaPoint;
        PossibleMovesRange = possibleMovesRange;
        Type = UnitType.Player;

        GD.Print("[TEST] TestConcreteUnitSystem constructor called");
    }

    protected override void Initialize()
    {
        if (IsInitialized)
            return;
        base.Initialize();
        IsInitialized = true;

        UnitName = "TestUnit";
        Description = "Unit used only for unit testing.";
        MaxHp = 100;
        Hp = 100;
        BaseAtk = 10;
        BaseDef = 5;
        BaseSpeed = 4;
        ManaPoint = 100;
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

    public void CallInitialize()
    {
        Initialize();
    }

    public void CallCleanup()
    {
        Cleanup();
    }
}

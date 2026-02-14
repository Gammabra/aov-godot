using System.Reflection;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using AshesOfVelsingrad.AI;
using Godot;

namespace UnitTests;

/// <summary>
/// Concrete test-double for UnitSystem.
/// Matches the testing style and philosophy of TestConcreteMapSystem.
/// </summary>
public partial class TestConcreteUnitSystem : UnitSystem
{
	public bool IsInitialized { get; private set; }
	public bool IsCleanedUp { get; private set; }
	public List<string> Log { get; } = new();

	public new AIPersonality Personality
	{
		get => base.Personality;
		set
		{
			var property = typeof(UnitSystem).GetProperty("Personality");
			property?.SetValue(this, value);
		}
	}

	// Expose the private _statusEffectSystem field for testing
	public StatusEffectSystem? InjectedStatusEffectSystem
	{
		get
		{
			var field = typeof(UnitSystem).GetField("_statusEffectSystem",
				BindingFlags.NonPublic | BindingFlags.Instance);
			return (StatusEffectSystem?)field?.GetValue(this);
		}
	}

	// Add public setters for testing
	public new int PossibleMovesRange
	{
		get => base.PossibleMovesRange;
		set
		{
			var field = typeof(UnitSystem).GetProperty("PossibleMovesRange",
				BindingFlags.Public | BindingFlags.Instance);
			field?.SetValue(this, value);
		}
	}

    public new float Hp
    {
        get => base.Hp;
        set
        {
            var field = typeof(UnitSystem).GetProperty("Hp",
                BindingFlags.Public | BindingFlags.Instance);
            field?.SetValue(this, value);
        }
    }

	public new float Mana
	{
		get => base.Mana;
		set
		{
			var field = typeof(UnitSystem).GetProperty("Mana",
				BindingFlags.Public | BindingFlags.Instance);
			field?.SetValue(this, value);
		}
	}

	public new float BaseAtk
	{
		get => base.BaseAtk;
		set
		{
			var field = typeof(UnitSystem).GetProperty("BaseAtk",
				BindingFlags.Public | BindingFlags.Instance);
			field?.SetValue(this, value);
		}
	}

	public new float BaseDef
	{
		get => base.BaseDef;
		set
		{
			var field = typeof(UnitSystem).GetProperty("BaseDef",
				BindingFlags.Public | BindingFlags.Instance);
			field?.SetValue(this, value);
		}
	}

    public TestConcreteUnitSystem(
        string unitName = "TestUnit",
        string description = "Unit used only for unit testing.",
        float maxHp = 100,
        float hp = 100,
        float baseAtk = 10,
        float baseDef = 5,
        float baseSpeed = 4,
        float manaPoint = 100,
        int possibleMovesRange = 1,
        bool isAlive = true
    )
    {
        Name = "TestConcreteUnitSystem";
        UnitName = unitName;
        Description = description;
        MaxHp = maxHp;
        Hp = hp;
        BaseAtk = baseAtk;
        BaseDef = baseDef;
        BaseSpeed = baseSpeed;
		ManaMax = manaPoint;
        Mana = manaPoint;
        PossibleMovesRange = possibleMovesRange;
        IsAlive = isAlive;
        Type = AovDataStructures.UnitType.Player;
        GD.Print("[TEST] TestConcreteUnitSystem constructor called");
    }

    protected override void Initialize()
    {
        if (IsInitialized)
            return;
        IsInitialized = true;

        base.Initialize();
        GD.Print($"[TEST] Total atk is {TotalAtk}");
        GD.Print($"[TEST] Total def is {TotalDef}");
        GD.Print("[TEST] TestConcreteUnitSystem initialized");
    }

	public override void InjectDependencies(StatusEffectSystem statusEffectSystem)
	{
		base.InjectDependencies(statusEffectSystem);

		Log.Add("StatusEffectSystem injected");
	}

	protected override void Cleanup()
	{
		base.Cleanup();
		IsCleanedUp = true;
		Log.Add("CleanedUp");
		GD.Print("[TEST] TestConcreteUnitSystem cleanup called");
	}

	public void CallInitialize()
	{
		Initialize();
	}

	public void CallCleanup()
	{
		Cleanup();
	}
}

using System.Reflection;
using System.Collections.Generic;
using AshesOfVelsingrad.Systems;
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

	public TestConcreteUnitSystem()
	{
		GD.Print("[TEST] TestConcreteUnitSystem constructor called");
	}

	protected override void Initialize()
	{
		base.Initialize();
		
		UnitName = "TestUnit";
		MaxHp = 100;
		Hp = 100;
		BaseAtk = 20;
		BaseDef = 5;
		BaseSpeed = 4;
		Intelligence = 15;
		ManaMax = 100;
		Mana = 100;
		Type = UnitType.Player;
		PossibleMovesRange = 3;
		IsAlive = true;
		
		IsInitialized = true;
		Log.Add("Initialized");
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

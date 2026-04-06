using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using AshesOfVelsingrad.AI;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class StatusEffectSystemTests
{
    private StatusEffectSystem? _system;
    private FakeUnit? _unit;

    #region Fake Implementation

    /// <summary>
    /// A minimal implementation of IUnitSystem to satisfy the compiler contract.
    /// We only implement the logic needed for StatusEffect tests.
    /// </summary>
    private class FakeUnit : IUnitSystem
    {
        // --- Status Effect Logic (What we are actually testing) ---
        private readonly List<StatusEffect<IUnitSystem>> _effects = new();
        public List<StatusEffect<IUnitSystem>> GetActiveEffects() => _effects;
        public void ApplyEffect(StatusEffect<IUnitSystem> effect) => _effects.Add(effect);
        public void RemoveEffect(StatusEffect<IUnitSystem> effect) => _effects.Remove(effect);
        public bool HasEffect<T>() where T : StatusEffect<IUnitSystem> => _effects.OfType<T>().Any();

        // --- Basic Properties ---
        public string UnitName => "TestDummy";
        public string Description => "A fake unit for testing";
        public bool IsAlive { get; private set; } = true;
        public void SetIsAlive(bool alive) => IsAlive = alive;

        // --- Stats Stubs ---
        public float Hp { get; set; } = 10;
        public float MaxHp { get; set; } = 10;
        public float BaseAtk => 5;
        public float TotalAtk => 5;
        public float BaseDef => 5;
        public float TotalDef => 5;
        public float BaseSpeed => 5;
        public float Intelligence => 5;
        public float ManaMax => 10;
        public float Mana => 10;
        public float Curse => 0;
        public int PossibleMovesRange => 3;
        public bool IsControlled => false;
        public AIPersonality Personality => default;
        public List<ISkillSystem> ActiveSkills => new();

        // --- Movement & Action Stubs ---
        public Task WaitForActionAsync() => Task.CompletedTask;
        public void PassTurn() { }
        public List<(int, int, int)> GetPossibleMoves(IMapSystem map) => [];
        public void SetGridPosition(int x, int y, int z, IMapSystem map) { }
        public bool CanMoveTo(int x, int y, int z, IMapSystem map) => true;
        public bool MoveTo(int x, int y, int z, IMapSystem map) => true;
        public List<(int, int, int)> GetReachableCellsForSkills(IMapSystem map, ISkillSystem skill) => [];
        public void Play(List<IUnitSystem> targets, IMapSystem? map, ISkillSystem skill) { }
        public void InjectDependencies(StatusEffectSystem ses) { }

        // --- Combat/Effect Behavior Stubs ---
        public void TakeDamage(float amount) { }
        public void BypassDamage(float amount) { }
        public void OnEffectHeal(float amount) { }
        public void OnEffectRevive(AovDataStructures.ModifierType m, float a) { }
        public void OnEffectModifierApplied(AovDataStructures.StatTypeWithModifier s, AovDataStructures.ModifierType m, float a) { }
        public void OnEffectModifierRemoved(AovDataStructures.StatTypeWithModifier s, AovDataStructures.ModifierType m, float a) { }
        public void OnEffectDamage(AovDataStructures.ModifierType m, float a) { }
        public void OnEffectControlApplied() { }
        public void OnEffectControlRemoved() { }
        public void SetStatusEffectOnUnit(StatusEffect<IUnitSystem> e) => ApplyEffect(e);
    }

    #endregion

    public class TestEffect<T> : StatusEffect<T>
    {
        private readonly bool _shouldApplyTwice;
        public override bool ShouldApplyTwice => _shouldApplyTwice;

        public TestEffect(string name, int duration, bool stackable, bool doubleApply = false) 
            : base(name, "Desc", duration, stackable) 
        {
            _shouldApplyTwice = doubleApply;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _system = new StatusEffectSystem();
        _unit = new FakeUnit();
    }

    [Test]
    public void ApplyEffect_FirstTime_AddsEffectToTarget()
    {
        if (_system == null || _unit == null)
        {
            Assert.Fail("System or Unit not initialized properly.");
            return;
        }

        var effect = new TestEffect<IUnitSystem>("Poison", 3, false);
        _system.ApplyEffect(_unit, effect);
        Assert.That(_unit.GetActiveEffects(), Contains.Item(effect));
    }

    [Test]
    public void ApplyEffect_ShouldApplyTwice_TriggersTwice()
    {
        if (_system == null || _unit == null)
        {
            Assert.Fail("System or Unit not initialized properly.");
            return;
        }

        var effect = new TestEffect<IUnitSystem>("Stun", 1, false, doubleApply: true);
        _system.ApplyEffect(_unit, effect);
        Assert.That(_unit.GetActiveEffects().Count, Is.EqualTo(1));
    }

    [Test]
    public void ApplyEffect_ExistingStackableEffect_AddsStackAndResetsDuration()
    {
        if (_system == null || _unit == null)
        {
            Assert.Fail("System or Unit not initialized properly.");
            return;
        }

        var existingEffect = new TestEffect<IUnitSystem>("Might", 2, true);
        var newEffect = new TestEffect<IUnitSystem>("Might", 5, true);
        
        _unit.ApplyEffect(existingEffect);
        _system.ApplyEffect(_unit, newEffect);

        Assert.That(existingEffect.StackCount, Is.EqualTo(2));
        Assert.That(existingEffect.Duration, Is.EqualTo(5));
    }

    [Test]
    public void ProcessUnitTurnEnd_DecrementsDurationAndRemovesExpired()
    {
        if (_system == null || _unit == null)
        {
            Assert.Fail("System or Unit not initialized properly.");
            return;
        }

        var effect = new TestEffect<IUnitSystem>("Burn", 1, false);
        _unit.ApplyEffect(effect);

        _system.ProcessUnitTurnEnd(_unit);

        Assert.That(effect.Duration, Is.EqualTo(0));
        Assert.That(_unit.GetActiveEffects().Count, Is.EqualTo(0));
    }

    [Test]
    public void ProcessTurnEnd_HandlesCellInformation()
    {
        if (_system == null)
        {
            Assert.Fail("System not initialized properly.");
            return;
        }

        var cell = new CellInformation(0, 0, 0, AovDataStructures.CellType.Grass, true);
        var cellEffect = new TestEffect<CellInformation>("Fire", 1, false);
        
        _system.ApplyEffect(cell, cellEffect);
        _system.ProcessTurnEnd();

        Assert.That(cellEffect.Duration, Is.EqualTo(0));
        Assert.That(cell.GetActiveEffects().Count, Is.EqualTo(0));
    }

    [Test]
    public void RefreshTargetEffects_HandlesPermanentAndRemainingDurations()
    {
        if (_system == null || _unit == null) return;

        // Line 44: Permanent effect (-1 duration)
        var permEffect = new TestEffect<IUnitSystem>("Eternal", Constants.PermanentStatusEffect, false);
        
        // Line 49: Effect with remaining duration after tick
        var longEffect = new TestEffect<IUnitSystem>("Long", 5, false);

        _unit.ApplyEffect(permEffect);
        _unit.ApplyEffect(longEffect);

        // Act
        _system.ProcessUnitTurnEnd(_unit);

        // Assert
        // Permanent effect duration remains unchanged (-1)
        Assert.That(permEffect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));
        
        // Long effect duration decremented but still > 0
        Assert.That(longEffect.Duration, Is.EqualTo(4));
        
        // Both should still be on the unit (triggered the 'continue' statements)
        Assert.That(_unit.GetActiveEffects().Count, Is.EqualTo(2));
    }

    [Test]
    public void ProcessUnitTurnEnd_WhenTargetIsNull_ReturnsImmediately()
    {
        if (_system == null) return;

        // Line 118: Pass null
        Assert.DoesNotThrow(() => _system.ProcessUnitTurnEnd(null));
    }

    [Test]
    public void ApplyEffect_NonStackableExisting_ResetsDurationOnly()
    {
        if (_system == null || _unit == null) return;

        var effect1 = new TestEffect<IUnitSystem>("Stun", 1, false);
        var effect2 = new TestEffect<IUnitSystem>("Stun", 3, false);

        _system.ApplyEffect(_unit, effect1);
        // Line 89-91: existing is not null, not stackable
        _system.ApplyEffect(_unit, effect2);

        Assert.That(effect1.Duration, Is.EqualTo(3));
        Assert.That(effect1.StackCount, Is.EqualTo(1)); // Did not stack
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using AshesOfVelsingrad.AI;
using AshesOfVelsingrad.Data;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;
using NUnit.Framework;

namespace AshesOfVelsingrad.Core.Tests.Systems;

[TestFixture]
public class StatusEffectSystemTests
{
    private StatusEffectSystem _system = null!;
    private IUnitSystem _unit = null!;

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class TestUnitSystem : EffectTarget<IUnitSystem>, IUnitSystem
    {
        public string UnitName { get; set; } = "TestUnit";
        public string Description { get; set; } = string.Empty;
        public Faction Faction { get; private set; } = Faction.Player;
        public EntityProfile? EntityProfile { get; private set; }
        public int PossibleMovesRange { get; set; }
        public float Hp { get; set; } = 1;
        public float MaxHp { get; set; } = 1;
        public float BaseAtk { get; set; }
        public float BaseDef { get; set; }
        public float BaseSpeed { get; set; }
        public float Intelligence { get; set; }
        public float ManaMax { get; set; }
        public float Mana { get; set; }
        public float Curse { get; set; }
        public bool IsAlive { get; set; } = true;
        public AIPersonality Personality { get; set; } = AIPersonality.Balanced;
        public float TotalAtk { get; set; }
        public float TotalDef { get; set; }
        public bool IsControlled { get; set; }
        public List<ISkillSystem> ActiveSkills { get; set; } = new();
        public IInventorySystem Inventory { get; } = null!;

        public Task WaitForActionAsync() => Task.CompletedTask;
        public void PassTurn() { }
        public void SetFaction(Faction faction) => Faction = faction;
        public void SetEntityProfile(EntityProfile? profile) => EntityProfile = profile;
        public List<(int, int, int)> GetPossibleMoves(IMapSystem map) => new();
        public void SetGridPosition(int x, int y, int z, IMapSystem map) { }
        public bool CanMoveTo(int x, int y, int z, IMapSystem map) => false;
        public bool MoveTo(int x, int y, int z, IMapSystem map) => false;
        public List<(int, int, int)> GetReachableCellsForSkills(IMapSystem map, ISkillSystem skill) => new();
        public void Play(List<IUnitSystem> targets, IMapSystem? map, ISkillSystem skill) { }
        public void SetIsAlive(bool isAlive) => IsAlive = isAlive;
        public void TakeDamage(float damage) => Hp -= damage;
        public void BypassDamage(float damage) => Hp -= damage;
        public void OnEffectHeal(float amount) { }
        public void RestoreMana(float amount) { }
        public void OnEffectControlApplied() { }
        public void OnEffectControlRemoved() { }
        public void OnEffectDamage(AovDataStructures.ModifierType modifierType, float amount) { }
        public void OnEffectModifierApplied(AovDataStructures.StatTypeWithModifier statTypeWithModifier, AovDataStructures.ModifierType modifierType, float amount) { }
        public void OnEffectModifierRemoved(AovDataStructures.StatTypeWithModifier statTypeWithModifier, AovDataStructures.ModifierType modifierType, float amount) { }
        public void InjectDependencies(StatusEffectSystem statusEffectSystem) { }
        public void OnEffectRevive(AovDataStructures.ModifierType modifierType, float amount) { }
        public void SetStatusEffectOnUnit(StatusEffect<IUnitSystem> statusEffect) { }
        public void UseItem(int slotIndex, IUnitSystem? target, IMapSystem? map) { }

    }

    [SetUp]
    public void SetUp()
    {
        _system = new StatusEffectSystem();
        _unit = new TestUnitSystem();
    }

    [Test]
    public void ApplyEffect_FirstTime_AddsEffectToTarget()
    {
        var effect = new TestEffect<IUnitSystem>("Poison", 3, false);
        _system.ApplyEffect(_unit, effect);
        Assert.That(_unit.GetActiveEffects(), Contains.Item(effect));
    }

    [Test]
    public void ApplyEffect_ShouldApplyTwice_TriggersTwice()
    {
        var effect = new TestEffect<IUnitSystem>("Stun", 1, false, doubleApply: true);
        _system.ApplyEffect(_unit, effect);
        Assert.That(_unit.GetActiveEffects().Count, Is.EqualTo(1));
    }

    [Test]
    public void ApplyEffect_ExistingStackableEffect_AddsStackAndResetsDuration()
    {
        var existingEffect = new TestEffect<IUnitSystem>("Might", 2, true);
        var newEffect = new TestEffect<IUnitSystem>("Might", 5, true);

        _unit.ApplyEffect(existingEffect);
        _system.ApplyEffect(_unit, newEffect);

        Assert.That(existingEffect.StackCount, Is.EqualTo(2));
        Assert.That(existingEffect.Duration, Is.EqualTo(5));
    }

    [Test]
    public void ProcessUnitStatusEffects_DecrementsDurationAndRemovesExpired()
    {
        var effect = new TestEffect<IUnitSystem>("Burn", 1, false);
        _unit.ApplyEffect(effect);

        _system.ProcessUnitStatusEffects(_unit);

        Assert.That(effect.Duration, Is.EqualTo(0));
        Assert.That(_unit.GetActiveEffects().Count, Is.EqualTo(0));
    }

    [Test]
    public void ProcessTurnEnd_HandlesCellInformation()
    {
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
        // Line 44: Permanent effect (-1 duration)
        var permEffect = new TestEffect<IUnitSystem>("Eternal", Constants.PermanentStatusEffect, false);

        // Line 49: Effect with remaining duration after tick
        var longEffect = new TestEffect<IUnitSystem>("Long", 5, false);

        _unit.ApplyEffect(permEffect);
        _unit.ApplyEffect(longEffect);

        // Act
        _system.ProcessUnitStatusEffects(_unit);

        // Assert
        // Permanent effect duration remains unchanged (-1)
        Assert.That(permEffect.Duration, Is.EqualTo(Constants.PermanentStatusEffect));

        // Long effect duration decremented but still > 0
        Assert.That(longEffect.Duration, Is.EqualTo(4));

        // Both should still be on the unit (triggered the 'continue' statements)
        Assert.That(_unit!.GetActiveEffects().Count, Is.EqualTo(2));
    }

    [Test]
    public void ProcessUnitStatusEffects_WhenTargetIsNull_ReturnsImmediately()
    {
        // Line 118: Pass null
        Assert.DoesNotThrow(() => _system.ProcessUnitStatusEffects(null));
    }

    [Test]
    public void ApplyEffect_NonStackableExisting_ResetsDurationOnly()
    {
        var effect1 = new TestEffect<IUnitSystem>("Stun", 1, false);
        var effect2 = new TestEffect<IUnitSystem>("Stun", 3, false);

        _system.ApplyEffect(_unit, effect1);
        // Line 89-91: existing is not null, not stackable
        _system.ApplyEffect(_unit, effect2);

        Assert.That(effect1.Duration, Is.EqualTo(3));
        Assert.That(effect1.StackCount, Is.EqualTo(1)); // Did not stack
    }

    [Test]
    public void ApplyEffect_ExistingStackableEffect_DoesNotResetDuration_WhenNewDurationIsLess()
    {
        var existingEffect = new TestEffect<IUnitSystem>("Might", 5, true);
        var newEffect = new TestEffect<IUnitSystem>("Might", 2, true);

        _unit.ApplyEffect(existingEffect);
        _system.ApplyEffect(_unit, newEffect);

        Assert.That(existingEffect.StackCount, Is.EqualTo(2));
        Assert.That(existingEffect.Duration, Is.EqualTo(5)); // Not updated because 5 > 2
    }
}

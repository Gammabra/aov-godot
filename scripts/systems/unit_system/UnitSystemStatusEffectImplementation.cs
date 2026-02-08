using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public abstract partial class UnitSystem : IEffectTarget<UnitSystem>, IStatusEffectBehavior
{
    #region Private fields

    private readonly EffectTarget<UnitSystem> _effectTarget = new();
    private StatusEffectSystem? _statusEffectSystem;

    private Dictionary<AovDataStructures.StatTypeWithModifier, Func<float>> _baseStats =>
        new()
        {
            { AovDataStructures.StatTypeWithModifier.Atk, () => BaseAtk },
            { AovDataStructures.StatTypeWithModifier.Def, () => BaseDef }
        };

    private Dictionary<AovDataStructures.StatTypeWithModifier, Action<float>> _applyModifiers =>
        new()
        {
            { AovDataStructures.StatTypeWithModifier.Atk, v => AtkModifierAmount += v },
            { AovDataStructures.StatTypeWithModifier.Def, v => DefModifierAmount += v }
        };

    private Dictionary<AovDataStructures.StatTypeWithModifier, Action<float>> _removeModifiers =>
        new()
        {
            { AovDataStructures.StatTypeWithModifier.Atk, v => AtkModifierAmount -= v },
            { AovDataStructures.StatTypeWithModifier.Def, v => DefModifierAmount -= v }
        };

    #endregion

    #region Public Properties

    public float AtkModifierAmount { get; protected set; }
    public float DefModifierAmount { get; protected set; }

    /// <summary>
    /// Tell if the unit is controlled or not
    /// </summary>
    public bool IsControlled { get; protected set; }

    #endregion

    /// <summary>
    /// Applies a status effect to this unit.
    /// </summary>
    /// <param name="statusEffect">The status effect to apply.</param>
    public virtual void SetStatusEffectOnUnit(StatusEffect<UnitSystem> statusEffect)
    {
        _statusEffectSystem?.ApplyEffect(this, statusEffect);
    }

    #region IEffectTarget Implementation

    /// <inheritdoc />
    public void ApplyEffect(StatusEffect<UnitSystem> statusEffect)
    {
        _effectTarget.ApplyEffect(statusEffect);
    }

    /// <inheritdoc />
    public void RemoveEffect(StatusEffect<UnitSystem> statusEffect)
    {
        _effectTarget.RemoveEffect(statusEffect);
    }

    /// <inheritdoc />
    public bool HasEffect<T>()
        where T : StatusEffect<UnitSystem>
    {
        return _effectTarget.HasEffect<T>();
    }

    /// <inheritdoc />
    public List<StatusEffect<UnitSystem>> GetActiveEffects()
    {
        return _effectTarget.GetActiveEffects();
    }

    #endregion

    #region IStatusEffect Implementation

    public virtual void OnEffectDamage(AovDataStructures.ModifierType modifierType, float amount)
    {
        float value = modifierType == AovDataStructures.ModifierType.Flat
            ? amount
            : MaxHp * amount / 100f;

        Hp -= value;

        if (Hp <= 0)
        {
            Hp = 0;
            IsAlive = false;
        }
    }

    public virtual void OnEffectHeal(float amount)
    {
        Hp += amount;
        if (Hp > MaxHp)
            Hp = MaxHp;
    }

    public virtual void OnEffectRevive(AovDataStructures.ModifierType modifierType, float amount)
    {
        float value = modifierType == AovDataStructures.ModifierType.Flat
            ? amount
            : MaxHp * amount / 100f;

        Hp += value;
        IsAlive = true;
    }

    public virtual void OnEffectModifierApplied(
        AovDataStructures.StatTypeWithModifier statType,
        AovDataStructures.ModifierType modifierType,
        float amount
    )
    {
        float value = modifierType == AovDataStructures.ModifierType.Flat
            ? amount
            : _baseStats[statType]() * amount / 100f;

        _applyModifiers[statType](value);
    }

    public virtual void OnEffectModifierRemoved(
        AovDataStructures.StatTypeWithModifier statType,
        AovDataStructures.ModifierType modifierType,
        float amount
    )
    {
        float value = modifierType == AovDataStructures.ModifierType.Flat
            ? amount
            : _baseStats[statType]() * amount / 100f;

        _removeModifiers[statType](value);
    }

    public virtual void OnEffectControlApplied()
    {
        IsControlled = true;
    }

    public virtual void OnEffectControlRemoved()
    {
        IsControlled = false;
    }

    #endregion
}

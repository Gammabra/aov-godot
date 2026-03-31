using System;
using System.Collections.Generic;
using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public abstract partial class UnitSystem
{
    #region Private fields

    private readonly EffectTarget<IUnitSystem> _effectTarget = new();
    private StatusEffectSystem? _statusEffectSystem;

    private float _atkModifierAmount;
    private float _defModifierAmount;

    private Dictionary<AovDataStructures.StatTypeWithModifier, Func<float>> _baseStats =>
        new()
        {
            { AovDataStructures.StatTypeWithModifier.Atk, () => BaseAtk },
            { AovDataStructures.StatTypeWithModifier.Def, () => BaseDef }
        };

    private Dictionary<AovDataStructures.StatTypeWithModifier, Action<float>> _applyModifiers =>
        new()
        {
            {
                AovDataStructures.StatTypeWithModifier.Atk, v =>
                {
                    _atkModifierAmount += v;
                    TotalAtk = BaseAtk + _atkModifierAmount;
                }
            },
            {
                AovDataStructures.StatTypeWithModifier.Def, v =>
                {
                    _defModifierAmount += v;
                    TotalDef = BaseDef + _defModifierAmount;
                }
            }
        };

    private Dictionary<AovDataStructures.StatTypeWithModifier, Action<float>> _removeModifiers =>
        new()
        {
            {
                AovDataStructures.StatTypeWithModifier.Atk, v =>
                {
                    _atkModifierAmount -= v;
                    TotalAtk = BaseAtk + _atkModifierAmount;
                }
            },
            {
                AovDataStructures.StatTypeWithModifier.Def, v =>
                {
                    _defModifierAmount -= v;
                    TotalDef = BaseDef + _defModifierAmount;
                }
            }
        };

    #endregion

    #region Public Properties

    public float TotalAtk { get; private set; }
    public float TotalDef { get; private set; }

    /// <summary>
    ///     Tell if the unit is controlled or not
    /// </summary>
    public bool IsControlled { get; private set; }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Applies a status effect to this unit.
    /// </summary>
    /// <param name="statusEffect">The status effect to apply.</param>
    public virtual void SetStatusEffectOnUnit(StatusEffect<IUnitSystem> statusEffect)
{
    _statusEffectSystem?.ApplyEffect(this, statusEffect);
}

    #endregion

    #region IEffectTarget Implementation

    /// <inheritdoc />
    public void ApplyEffect(StatusEffect<IUnitSystem> statusEffect)
    {
        _effectTarget.ApplyEffect(statusEffect);
    }

    /// <inheritdoc />
    public void RemoveEffect(StatusEffect<IUnitSystem> statusEffect)
    {
        _effectTarget.RemoveEffect(statusEffect);
        Console.WriteLine($"{UnitName} had {statusEffect.Name} removed.");

        if (statusEffect.ShouldApplyTwice)
        {
            Console.WriteLine($"{UnitName} is no longer stunned.");
            statusEffect.OnRemove(this);
        }
    }

    /// <inheritdoc />
    public bool HasEffect<T>()
        where T : StatusEffect<IUnitSystem>
    {
        return _effectTarget.HasEffect<T>();
    }

    /// <inheritdoc />
    public List<StatusEffect<IUnitSystem>> GetActiveEffects()
    {
        return _effectTarget.GetActiveEffects();
    }

    #endregion

    #region IStatusEffect Implementation

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public virtual void OnEffectHeal(float amount)
    {
        if (!IsAlive)
            return;
        Hp += amount;
        if (Hp > MaxHp)
            Hp = MaxHp;
    }

    /// <inheritdoc/>
    public virtual void OnEffectRevive(AovDataStructures.ModifierType modifierType, float amount)
    {
        float value = modifierType == AovDataStructures.ModifierType.Flat
            ? amount
            : MaxHp * amount / 100f;

        if (IsAlive)
            return;

        Hp = value;
        IsAlive = true;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public virtual void OnEffectControlApplied()
    {
        IsControlled = true;
    }

    /// <inheritdoc/>
    public virtual void OnEffectControlRemoved()
    {
        IsControlled = false;
    }

    #endregion
}

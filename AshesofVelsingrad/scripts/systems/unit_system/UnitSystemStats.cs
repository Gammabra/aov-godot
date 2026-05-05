using System;
using AshesOfVelsingrad.AI;

namespace AshesOfVelsingrad.Systems;

public abstract partial class UnitSystem
{
    #region Public Properties

    /// <summary>The current health points of the unit.</summary>
    public float Hp { get; protected set; }

    /// <summary>The maximum health points of the unit.</summary>
    public float MaxHp { get; protected set; }

    /// <summary>The base physical attack value of the unit.</summary>
    public float BaseAtk { get; protected set; }

    /// <summary>The base physical defense value of the unit.</summary>
    public float BaseDef { get; protected set; }

    /// <summary>The unit's base speed (used for turn order or initiative).</summary>
    public float BaseSpeed { get; protected set; }

    /// <summary>The unit's intelligence stat, affecting magical power or effects.</summary>
    public float Intelligence { get; protected set; }

    /// <summary>The unit's available mana points for casting skills.</summary>
    public float ManaMax { get; protected set; }

    /// <summary>The current mana points of the unit.</summary>
    public float Mana { get; protected set; }

    /// <summary>The unit’s curse value (used for status mechanics or debuffs).</summary>
    public float Curse { get; protected set; }

    /// <summary>The default AI personality type of the unit.</summary>
    public AIPersonality Personality { get; protected set; } = AIPersonality.Defensive;

    /// <summary>Indicates whether the unit is alive.</summary>
    public bool IsAlive { get; protected set; } = true;

    /// <summary>
    ///     Set if the unit is alive or not.
    /// </summary>
    /// <param name="isAlive">A boolean to set unit <see cref="IsAlive" /> value</param>
    public void SetIsAlive(bool isAlive)
    {
        if (!isAlive && Hp <= 0)
        {
            IsAlive = isAlive;
            // Hide the body so the corpse stops occluding the map. We don't QueueFree the
            // node because revive skills (e.g. Resurrection) need the unit reference to
            // come back. Re-show happens in OnEffectRevive.
            Visible = false;
            // Disable collision so click-targeting doesn't hit a dead unit.
            SetCollisionLayerValue(1, false);
            SetCollisionLayerValue(2, false);
        }
        else if (isAlive && Hp > 0)
        {
            IsAlive = isAlive;
            Visible = true;
            SetCollisionLayerValue(1, true);
            SetCollisionLayerValue(2, true);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Applies incoming damage to the unit and updates HP.
    /// </summary>
    /// <param name="damage">The amount of damage received.</param>
    public void TakeDamage(float damage)
    {
        float realDamage = damage - TotalDef;

        if (realDamage < 0)
            realDamage = 0;

        Hp -= realDamage;
        Console.WriteLine($"{UnitName} took {realDamage} damage (raw: {damage}), HP: {Hp}/{MaxHp}");

        if (Hp <= 0)
        {
            Hp = 0;
            SetIsAlive(false);
            Console.WriteLine($"{UnitName} has died.");
        }
    }

    /// <summary>
    ///      Applies damage that bypasses defense to the unit and updates HP.
    /// </summary>
    /// <param name="damage">The amount of damage to apply.</param>
    public virtual void BypassDamage(float damage)
    {
        Hp -= damage;

        if (Hp <= 0)
        {
            Hp = 0;
            SetIsAlive(false);
            Console.WriteLine($"{UnitName} has died.");
        }
    }

    #endregion
}

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
    public float ManaPoint { get; protected set; }

    /// <summary>The unit’s curse value (used for status mechanics or debuffs).</summary>
    public float Curse { get; protected set; }

    /// <summary>Indicates whether the unit is alive.</summary>
    public bool IsAlive { get; protected set; } = true;

    /// <summary>
    ///     Set if the unit is alive or not.
    /// </summary>
    /// <param name="isAlive">A boolean to set unit <see cref="IsAlive" /> value</param>
    public void SetIsAlive(bool isAlive)
    {
        if (!isAlive)
        {
            if (Hp <= 0)
                IsAlive = isAlive;
        }
        else
        {
            if (Hp >= 0)
                IsAlive = isAlive;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Applies incoming damage to the unit and updates HP.
    /// </summary>
    /// <param name="damage">The amount of damage received.</param>
    public virtual void TakeDamage(float damage)
    {
        float realDamage = damage - TotalDef;

        if (realDamage < 0)
            realDamage = 0;
        Hp -= realDamage;
    }

    #endregion
}

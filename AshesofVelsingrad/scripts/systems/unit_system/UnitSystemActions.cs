namespace AshesOfVelsingrad.Systems;

/// <summary>
///     <see cref="UnitSystem" /> partial that exposes the direct gameplay actions
///     called by skill / item behaviours (heal, revive, spend mana, restore mana).
/// </summary>
/// <remarks>
///     These complement the status-effect-driven <c>OnEffectHeal</c> / <c>OnEffectRevive</c>
///     hooks. Behaviours invoke these instead of the OnEffect* variants because the
///     OnEffect* hooks are reserved for the status-effect pipeline (which fires
///     <c>HpChanged</c> / lifecycle events through a different code path).
/// </remarks>
public abstract partial class UnitSystem
{
    /// <summary>
    ///     Restore HP by <paramref name="amount" />, clamped to <see cref="UnitSystem.MaxHp" />.
    ///     A no-op when the unit is dead.
    /// </summary>
    /// <param name="amount">Amount of HP to restore. Negative values are ignored.</param>
    public virtual void Heal(float amount)
    {
        if (!IsAlive) return;
        if (amount <= 0f) return;

        Hp += amount;
        if (Hp > MaxHp)
            Hp = MaxHp;
    }

    /// <summary>
    ///     Revive a dead unit, restoring HP up to <paramref name="hpRestored" />
    ///     (clamped to <see cref="UnitSystem.MaxHp" />). A no-op when the unit is alive.
    /// </summary>
    /// <param name="hpRestored">Absolute HP to set after revival (e.g. <c>MaxHp * 0.5f</c>).</param>
    public virtual void Revive(float hpRestored)
    {
        if (IsAlive) return;
        if (hpRestored <= 0f) return;

        Hp = hpRestored > MaxHp ? MaxHp : hpRestored;
        SetIsAlive(true);
    }

    /// <summary>
    ///     Deduct <paramref name="amount" /> from the unit's mana pool, clamped at 0.
    /// </summary>
    /// <param name="amount">Mana cost. Negative values are ignored.</param>
    public virtual void SpendMana(float amount)
    {
        if (amount <= 0f) return;

        Mana -= amount;
        if (Mana < 0f) Mana = 0f;
    }

    /// <summary>
    ///     Restore <paramref name="amount" /> mana, clamped to <see cref="UnitSystem.ManaMax" />.
    /// </summary>
    /// <param name="amount">Mana to restore. Negative values are ignored.</param>
    public virtual void RestoreMana(float amount)
    {
        if (amount <= 0f) return;

        Mana += amount;
        if (Mana > ManaMax) Mana = ManaMax;
    }

    /// <summary>
    ///     Adjust the unit's base attack by <paramref name="delta" /> (signed).
    ///     Used by curse / buff effects that shift raw stats persistently.
    /// </summary>
    /// <param name="delta">Signed delta. Negative reduces, positive increases.</param>
    public virtual void AdjustAttack(float delta)
    {
        BaseAtk += delta;
        if (BaseAtk < 0f) BaseAtk = 0f;
    }

    /// <summary>
    ///     Adjust the unit's base defense by <paramref name="delta" /> (signed).
    /// </summary>
    /// <param name="delta">Signed delta. Negative reduces, positive increases.</param>
    public virtual void AdjustDefense(float delta)
    {
        BaseDef += delta;
        if (BaseDef < 0f) BaseDef = 0f;
    }

    /// <summary>
    ///     Adjust the unit's base speed by <paramref name="delta" /> (signed).
    /// </summary>
    /// <param name="delta">Signed delta. Negative reduces, positive increases.</param>
    public virtual void AdjustSpeed(float delta)
    {
        BaseSpeed += delta;
        if (BaseSpeed < 0f) BaseSpeed = 0f;
    }

    /// <summary>
    ///     Replace the unit's display name. Called by spawners that want to give a unit
    ///     a designer-authored override instead of the data-class default.
    /// </summary>
    /// <param name="newName">The override name. Empty strings are ignored.</param>
    public virtual void OverrideDisplayName(string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        UnitName = newName;
    }
}

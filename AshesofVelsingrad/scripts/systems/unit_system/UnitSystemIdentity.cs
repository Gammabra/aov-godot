using AshesOfVelsingrad.Data;

namespace AshesOfVelsingrad.Systems;

/// <summary>
///     <see cref="UnitSystem" /> partial that adds Faction + EntityProfile alongside the
///     stat / movement / skill / status partials.
/// </summary>
/// <remarks>
///     <para>
///         Faction is <see cref="Systems.Faction.Player" /> by default; set it from the unit's
///         <c>Initialize()</c> override (<c>Faction = Faction.Enemy</c>) or via
///         <see cref="AssignFaction" /> from <c>GameManager.LoadUnits</c> based on which
///         container the unit lives in.
///     </para>
///     <para>
///         <see cref="EntityProfile" /> is optional. When null, HUD widgets fall back to
///         <see cref="UnitSystem.UnitName" /> + a "Lv 1" placeholder.
///     </para>
/// </remarks>
public abstract partial class UnitSystem
{
    /// <summary>Faction this unit fights for.</summary>
    public Faction Faction { get; protected set; } = Faction.Player;

    /// <summary>Designer-authored display metadata (portrait, class, level). Optional.</summary>
    public EntityProfile? EntityProfile { get; protected set; }

    /// <summary>
    ///     Set the faction at runtime — called by <c>GameManager.LoadUnits</c> based on which
    ///     scene-tree container the unit was found in.
    /// </summary>
    /// <param name="faction">Faction to assign.</param>
    public void AssignFaction(Faction faction)
    {
        Faction = faction;
    }

    /// <summary>
    ///     Replace the entity profile at runtime (e.g. when level-up changes the displayed level).
    /// </summary>
    /// <param name="profile">New profile, or null to clear.</param>
    public void SetEntityProfile(EntityProfile? profile)
    {
        EntityProfile = profile;
    }
}

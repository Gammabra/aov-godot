namespace AshesOfVelsingrad.Data;

/// <summary>
///     Designer-authored display metadata attached to a combatant.
/// </summary>
/// <remarks>
///     <para>
///         Pure C# data — name, portrait <em>path</em>, class, level, flavour text — used by
///         HUD widgets (<c>PlayerStatusPanel</c>, <c>TurnOrderQueue</c>, <c>VictoryScreen</c>)
///         to render unit identity. Does NOT carry gameplay state; that lives on
///         <see cref="Systems.IUnitSystem" />.
///     </para>
///     <para>
///         The portrait is stored as a <c>res://</c> path string rather than a
///         <c>Godot.Texture2D</c>: this keeps <c>AshesofVelsingrad.Core</c> Godot-free and
///         unit-testable. The Godot adapter (HUD widgets, in-world sprites) calls
///         <c>ResourceLoader.Load&lt;Texture2D&gt;(profile.PortraitPath)</c> at render time
///         to materialise the texture. Lazy loading also avoids forcing every saved profile
///         to drag a texture handle around.
///     </para>
///     <para>
///         Build one inline in a unit's <c>Initialize()</c>:
///         <code>
///             SetEntityProfile(new EntityProfile
///             {
///                 DisplayName = "Pikachu",
///                 ClassName = "Combattant",
///                 Level = 1,
///                 PortraitPath = "res://assets/portraits/Pikachu.png",
///             });
///         </code>
///     </para>
/// </remarks>
public sealed class EntityProfile
{
    /// <summary>Name shown in HUD widgets and dialogue prompts.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     Godot resource path to the portrait texture (e.g. <c>res://assets/portraits/Pikachu.png</c>).
    ///     The Godot adapter loads it lazily — Core stays free of <c>Godot.Texture2D</c> so
    ///     this class is testable without a Godot runtime. Empty string means no portrait;
    ///     HUD widgets fall back to a coloured placeholder.
    /// </summary>
    public string PortraitPath { get; set; } = string.Empty;

    /// <summary>Level shown next to the name.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Class label ("Combattant", "Mage Feu", ...) for tooltips and roster screens.</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Optional flavour text for hover tooltips.</summary>
    public string Bio { get; set; } = string.Empty;
}

using Godot;

namespace AshesOfVelsingrad.systems;

/// <summary>
///     Designer-authored display metadata attached to any combatant.
/// </summary>
/// <remarks>
///     <para>
///         Distinct from <see cref="progression.CharacterProfile" /> which holds gameplay state
///         (XP, equipped skills, karma, corruption). <see cref="EntityProfile" /> is presentation
///         data — what the HUD shows alongside the unit's bars.
///     </para>
///     <para>
///         Author one in the editor as a <c>.tres</c> resource and assign it via
///         <see cref="UnitSystem.EntityProfile" /> in your unit's <c>Initialize</c> override
///         (or via an <c>[Export]</c> on a custom unit subclass).
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class EntityProfile : Resource
{
    /// <summary>Name shown in HUD widgets and dialogue prompts.</summary>
    [Export] public string DisplayName { get; set; } = string.Empty;

    /// <summary>Portrait icon. Recommended size 96×96 px.</summary>
    [Export] public Texture2D? Portrait { get; set; }

    /// <summary>The level shown next to the name. Override at runtime if you scale per-encounter.</summary>
    [Export] public int Level { get; set; } = 1;

    /// <summary>
    ///     Class label ("Combattant", "Mage Fire", etc.) used in tooltips and roster screens.
    /// </summary>
    [Export] public string ClassName { get; set; } = string.Empty;

    /// <summary>Optional flavour text for hover tooltips.</summary>
    [Export(PropertyHint.MultilineText)]
    public string Bio { get; set; } = string.Empty;
}

using Godot;

namespace AshesOfVelsingrad.Data;

/// <summary>
///     Designer-authored display metadata attached to a combatant.
/// </summary>
/// <remarks>
///     <para>
///         Pure presentation data — name, portrait, class, level, flavour text — used by HUD
///         widgets (<c>PlayerStatusPanel</c>, <c>TurnOrderQueue</c>) to render unit identity.
///         Does NOT carry gameplay state; that lives on <see cref="Systems.IUnitSystem" />.
///     </para>
///     <para>
///         Author one as a <c>.tres</c> resource in the editor and assign it to a unit's
///         <c>EntityProfile</c> field — or build it inline inside the unit's
///         <c>Initialize()</c> if you prefer code-only data.
///     </para>
/// </remarks>
[GlobalClass]
public sealed partial class EntityProfile : Resource
{
    /// <summary>Name shown in HUD widgets and dialogue prompts.</summary>
    [Export] public string DisplayName { get; set; } = string.Empty;

    /// <summary>Portrait icon. Recommended 96×96 px.</summary>
    [Export] public Texture2D? Portrait { get; set; }

    /// <summary>Level shown next to the name.</summary>
    [Export] public int Level { get; set; } = 1;

    /// <summary>Class label ("Combattant", "Mage Feu", ...) for tooltips and roster screens.</summary>
    [Export] public string ClassName { get; set; } = string.Empty;

    /// <summary>Optional flavour text for hover tooltips.</summary>
    [Export(PropertyHint.MultilineText)]
    public string Bio { get; set; } = string.Empty;
}

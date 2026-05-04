using System.Collections.Generic;
using Godot;

namespace AshesOfVelsingrad.systems.skills;

/// <summary>
///     Singleton lookup that resolves skill ids to <see cref="SkillDefinition" />
///     and behaviour ids to <see cref="ISkillBehaviour" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         The registry is populated at boot time, typically by:
///         <list type="number">
///             <item>Loading every <c>.tres</c> definition under <c>res://data/skills/</c>.</item>
///             <item>Falling back to <c>DefaultDatabaseSeeder</c> for any skill not yet authored.</item>
///         </list>
///     </para>
///     <para>
///         Both maps are case-sensitive. Lookups return null with an error log if
///         the id is missing — never throw — so a misspelled id breaks one skill
///         but does not crash the battle loop.
///     </para>
/// </remarks>
public sealed partial class SkillRegistry : Node
{
    private readonly Dictionary<string, SkillDefinition> _definitions = new();
    private readonly Dictionary<string, ISkillBehaviour> _behaviours = new();

    /// <summary>The active singleton instance, set in <c>_Ready</c>.</summary>
    public static SkillRegistry? Instance { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"Multiple instances of {nameof(SkillRegistry)} detected. Removing duplicate.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    /// <summary>Register a skill definition.</summary>
    /// <param name="definition">The definition to register. Its <c>SkillId</c> is the key.</param>
    public void RegisterDefinition(SkillDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.SkillId))
        {
            GD.PrintErr("SkillRegistry: cannot register a SkillDefinition with empty SkillId.");
            return;
        }

        _definitions[definition.SkillId] = definition;
    }

    /// <summary>Register a behaviour under a string id.</summary>
    /// <param name="behaviourId">The id (matches <see cref="SkillDefinition.BehaviourId" />).</param>
    /// <param name="behaviour">The behaviour implementation.</param>
    public void RegisterBehaviour(string behaviourId, ISkillBehaviour behaviour)
    {
        if (string.IsNullOrEmpty(behaviourId))
        {
            GD.PrintErr("SkillRegistry: cannot register a behaviour with empty id.");
            return;
        }

        _behaviours[behaviourId] = behaviour;
    }

    /// <summary>Resolve a skill id to its definition. Returns null with a log on miss.</summary>
    public SkillDefinition? GetDefinition(string skillId)
    {
        if (_definitions.TryGetValue(skillId, out SkillDefinition? def))
            return def;
        GD.PrintErr($"SkillRegistry: unknown skill id '{skillId}'.");
        return null;
    }

    /// <summary>Resolve a behaviour id. Returns null with a log on miss.</summary>
    public ISkillBehaviour? GetBehaviour(string behaviourId)
    {
        if (_behaviours.TryGetValue(behaviourId, out ISkillBehaviour? b))
            return b;
        GD.PrintErr($"SkillRegistry: unknown behaviour id '{behaviourId}'.");
        return null;
    }

    /// <summary>All registered definitions, keyed by skill id.</summary>
    public IReadOnlyDictionary<string, SkillDefinition> AllDefinitions => _definitions;
}

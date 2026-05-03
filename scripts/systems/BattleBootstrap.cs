using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.systems.items;
using AshesOfVelsingrad.systems.skills;
using AshesOfVelsingrad.ui.hud;
using Godot;

namespace AshesOfVelsingrad.systems.battle;

/// <summary>
///     Stateless helper that creates or finds every combat-layer singleton and the HUD.
/// </summary>
/// <remarks>
///     <para>
///         Replaces the inline bootstrap that used to live in <c>GameManager.Initialize</c>.
///         A single call to <see cref="EnsureSingletons" /> guarantees that
///         <see cref="BattleEventBus" />, <see cref="KarmaManager" />, <see cref="SkillRegistry" />,
///         <see cref="ItemRegistry" />, <see cref="PartyInventory" />, <see cref="BattleLauncher" />
///         and <see cref="DefaultDatabaseSeeder" /> are alive and have run their <c>_Ready</c>.
///     </para>
///     <para>
///         <see cref="EnsureBattleHud" /> finds an existing HUD anywhere in the scene tree
///         (e.g. authored in <c>.tscn</c>) before spawning a programmatic fallback. The HUD is
///         parented to the current scene rather than the engine root so resource lifecycles stay
///         scoped to the active battle.
///     </para>
/// </remarks>
public static class BattleBootstrap
{
    /// <summary>
    ///     Create / find every combat singleton and force-run their <c>_Ready</c> synchronously
    ///     so dependent systems (e.g. <see cref="DefaultDatabaseSeeder" />) can rely on
    ///     <c>Instance</c> statics being populated.
    /// </summary>
    /// <param name="host">Any <see cref="Node" /> already in the tree (typically the GameManager).</param>
    public static void EnsureSingletons(Node host)
    {
        EnsureSingleton<BattleEventBus>(host, "BattleEventBus");
        EnsureSingleton<KarmaManager>(host, "KarmaManager");
        EnsureSingleton<SkillRegistry>(host, "SkillRegistry");
        EnsureSingleton<ItemRegistry>(host, "ItemRegistry");
        EnsureSingleton<PartyInventory>(host, "PartyInventory");
        EnsureSingleton<BattleLauncher>(host, "BattleLauncher");

        // Seeder must come AFTER the registries so it has somewhere to register into.
        EnsureSingleton<DefaultDatabaseSeeder>(host, "DefaultDatabaseSeeder");
    }

    /// <summary>
    ///     Add a node of <typeparamref name="T" /> to the scene root if none exists, and force
    ///     its <c>_Ready</c> to run synchronously.
    /// </summary>
    /// <typeparam name="T">A <see cref="Node" /> subclass with a parameterless constructor.</typeparam>
    /// <param name="host">Any node currently in the tree.</param>
    /// <param name="name">Name to give the new node.</param>
    /// <remarks>
    ///     Godot 4 .NET defers <c>_Ready</c> for nodes added during another node's <c>_Ready</c>.
    ///     Force-calling it makes the bootstrap deterministic; each singleton's <c>_Ready</c>
    ///     body is idempotent (the duplicate-detection short-circuits) so a later automatic
    ///     <c>_Ready</c> doesn't break anything.
    /// </remarks>
    public static void EnsureSingleton<T>(Node host, string name) where T : Node, new()
    {
        Node root = host.GetTree().Root;
        foreach (Node child in root.GetChildren())
            if (child is T) return;

        T node = new() { Name = name };
        root.AddChild(node);

        if (!node.IsNodeReady())
        {
            try
            {
                node._Ready();
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"BattleBootstrap: forced _Ready threw on {typeof(T).Name}: {e.Message}");
            }
        }

        GD.Print($"BattleBootstrap: bootstrapped {typeof(T).Name} (NodeReady={node.IsNodeReady()}).");
    }

    /// <summary>
    ///     Find an existing <see cref="BattleHud" /> in the scene tree, or build one.
    /// </summary>
    /// <param name="host">Any node currently in the tree (the host's <c>CurrentScene</c> hosts the HUD).</param>
    /// <param name="scenePath">Optional <c>.tscn</c> path; falls back to a programmatic HUD when empty.</param>
    /// <returns>The HUD instance, freshly added if one didn't already exist.</returns>
    public static BattleHud EnsureBattleHud(Node host, string scenePath)
    {
        Node sceneHost = host.GetTree().CurrentScene ?? host.GetTree().Root;

        BattleHud? found = FindBattleHudIn(sceneHost);
        if (found is not null)
        {
            GD.Print("BattleBootstrap: found existing BattleHud, reusing.");
            return found;
        }

        BattleHud? hud = null;
        if (!string.IsNullOrEmpty(scenePath))
        {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(scenePath);
            if (scene is not null)
            {
                hud = scene.Instantiate<BattleHud>();
                GD.Print($"BattleBootstrap: instantiated BattleHud from '{scenePath}'.");
            }
            else
            {
                GD.PrintErr($"BattleBootstrap: could not load HUD scene '{scenePath}'.");
            }
        }

        hud ??= new BattleHud { Name = "BattleHud" };
        if (string.IsNullOrEmpty(hud.Name))
            hud.Name = "BattleHud";

        sceneHost.AddChild(hud);
        GD.Print($"BattleBootstrap: added BattleHud under '{sceneHost.Name}'.");
        return hud;
    }

    /// <summary>Recursively search a subtree for the first <see cref="BattleHud" />.</summary>
    /// <param name="root">Subtree root.</param>
    /// <returns>The HUD or null.</returns>
    private static BattleHud? FindBattleHudIn(Node root)
    {
        if (root is BattleHud hud) return hud;
        foreach (Node child in root.GetChildren())
        {
            BattleHud? found = FindBattleHudIn(child);
            if (found is not null) return found;
        }
        return null;
    }
}

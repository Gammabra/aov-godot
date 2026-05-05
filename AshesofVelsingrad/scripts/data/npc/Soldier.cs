using AshesOfVelsingrad.Managers;
using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Systems.Battle;
using Godot;

namespace AshesOfVelsingrad.data.npc;

/// <summary>
///     Hostile NPC that triggers a battle when the player interacts with them.
/// </summary>
/// <remarks>
///     <para>
///         Hands off to <see cref="BattleLauncher" /> rather than calling
///         <c>ChangeSceneToFile</c> directly — that's what gives Forfeit something to
///         return to. The launcher captures the current scene path + the interacting
///         player's <c>GlobalPosition</c> at the moment of <see cref="Interact" />, then
///         restores both when the player presses Forfeit on the GameOverScreen (or
///         Continue on the VictoryScreen).
///     </para>
/// </remarks>
public partial class Soldier : CharacterBody3D, IInteractable
{
    [Export]
    private NodePath? _interactTextPath;

    /// <summary>
    ///     Path to the battle scene's <c>.tscn</c>. Kept as a string for backward compat
    ///     with the existing scene authoring; <see cref="Interact" /> loads it as a
    ///     <see cref="PackedScene" /> at trigger time.
    /// </summary>
    [Export]
    private string? _battleSceneToCharge;

    private Label3D? InteractText;

    public override void _Ready()
    {
        InteractText = GetNode<Label3D>(_interactTextPath);
    }

    public void Interact(IInteractor interactor)
    {
        if (string.IsNullOrEmpty(_battleSceneToCharge))
        {
            GD.PrintErr($"Soldier '{Name}': _battleSceneToCharge is empty; cannot start a battle.");
            return;
        }

        PackedScene? battleScene = ResourceLoader.Load<PackedScene>(_battleSceneToCharge);
        if (battleScene is null)
        {
            GD.PrintErr($"Soldier '{Name}': failed to load battle scene at '{_battleSceneToCharge}'.");
            return;
        }

        // Capture the player's current scene + world position so Forfeit / Victory
        // Continue can drop them right back here, no save needed.
        SceneTree tree = GetTree();
        string returnScenePath = tree.CurrentScene?.SceneFilePath ?? string.Empty;
        Vector3 returnPosition = interactor is Node3D playerNode
            ? playerNode.GlobalPosition
            : GlobalPosition; // fallback to NPC's own position if interactor isn't a Node3D

        BattleSetup setup = new()
        {
            BattleScene = battleScene,
            ReturnScenePath = returnScenePath,
            ReturnPosition = returnPosition,
            EncounterName = Name.ToString(),
        };

        if (BattleLauncher.Instance is null)
        {
            GD.PrintErr("Soldier: BattleLauncher autoload is not registered. Falling back to direct ChangeSceneToFile (no return state).");
            tree.ChangeSceneToFile(_battleSceneToCharge);
            return;
        }

        BattleLauncher.Instance.Launch(setup);
    }

    public bool CanInteract() => true;

    public void ShowPrompt()
    {
        if (InteractText is not null) InteractText.Visible = true;
    }

    public void HidePrompt()
    {
        if (InteractText is not null) InteractText.Visible = false;
    }
}

using AshesOfVelsingrad.Systems.Battle;
using Godot;

namespace AshesOfVelsingrad.Managers;

/// <summary>
///     Autoload that owns the exploration ↔ battle transition.
/// </summary>
/// <remarks>
///     <para>
///         The exploration layer (an <see cref="NpcBattleTrigger" /> on an NPC, an
///         encounter zone, scripted intro fight, …) builds a <see cref="BattleSetup" />
///         and calls <see cref="Launch" />. <c>BattleLauncher</c> stores the setup, swaps
///         to the battle scene, and exposes the setup to <see cref="GameManager" /> via
///         <see cref="PendingSetup" /> so the GameManager can spawn the configured units.
///     </para>
///     <para>
///         When the battle ends the player can:
///         <list type="bullet">
///             <item><description><b>Try Again</b> — handled inside <see cref="GameManager" /> via <c>ReloadCurrentScene</c>; the launcher's pending setup is preserved.</description></item>
///             <item><description><b>Forfeit</b> — calls <see cref="Forfeit" /> here, which loads <see cref="BattleSetup.ReturnScenePath" /> and queues the player respawn at <see cref="BattleSetup.ReturnPosition" />.</description></item>
///             <item><description><b>Continue (victory)</b> — calls <see cref="ReturnToExploration" /> with the same flow.</description></item>
///         </list>
///     </para>
///     <para>
///         Register as an autoload (Project Settings → AutoLoad):
///         <code>
///             Path: res://scripts/managers/BattleLauncher.cs
///             Name: BattleLauncher
///             Singleton: ✓
///         </code>
///         If you don't register the autoload, <see cref="Instance" /> stays null and
///         exploration code that calls <c>BattleLauncher.Instance?.Launch(...)</c>
///         silently no-ops — Test.tscn keeps working standalone.
///     </para>
/// </remarks>
public sealed partial class BattleLauncher : Node
{
    /// <summary>Singleton instance, set in <see cref="_Ready" />.</summary>
    public static BattleLauncher? Instance { get; private set; }

    /// <summary>
    ///     The setup currently being launched, or null when no battle is active.
    ///     <see cref="GameManager" /> reads this in <c>InitializeGameManager</c> and uses
    ///     it to populate the unit containers from <see cref="BattleSetup" />'s
    ///     PackedScene lists.
    /// </summary>
    public BattleSetup? PendingSetup { get; private set; }

    private string _returnScenePath = string.Empty;
    private Vector3 _returnPosition = Vector3.Zero;

    /// <summary>
    ///     Player-respawn position pending consumption by the next exploration scene.
    ///     Read once via <see cref="ConsumePendingReturnPosition" /> by whatever script
    ///     owns the player on the world map (probably a player controller's <c>_Ready</c>).
    /// </summary>
    private Vector3? _pendingReturnPosition;

    /// <inheritdoc />
    public override void _Ready()
    {
        Instance = this;
        GD.Print("BattleLauncher autoload ready.");
    }

    /// <summary>
    ///     Launch a new battle. Stores the setup and swaps to the battle scene.
    ///     <see cref="GameManager.InitializeGameManager" /> will read <see cref="PendingSetup" />
    ///     to populate the unit containers.
    /// </summary>
    /// <param name="setup">Encounter description.</param>
    public void Launch(BattleSetup setup)
    {
        if (setup.BattleScene is null)
        {
            GD.PrintErr("BattleLauncher.Launch: setup has no BattleScene.");
            return;
        }

        PendingSetup = setup;
        _returnScenePath = setup.ReturnScenePath;
        _returnPosition = setup.ReturnPosition;
        GD.Print($"BattleLauncher: launching '{setup.EncounterName}' (return → {_returnScenePath} @ {_returnPosition})");

        // Use MainManager shell if active; otherwise fallback to standard tree swapping for standalone tests
        if (MainManager.Instance != null)
        {
            MainManager.Instance.LoadScene(setup.BattleScene.ResourcePath, showHud: false);
        }
        else
        {
            GD.Print("[BattleLauncher] MainManager instance missing. Running standalone fallback layout.");
            Error err = GetTree().ChangeSceneToPacked(setup.BattleScene);
            if (err != Error.Ok)
                GD.PrintErr($"BattleLauncher: Standalone fallback ChangeSceneToPacked failed with {err}");
        }
    }

    /// <summary>
    ///     Player pressed Forfeit on the GameOverScreen. Tear down the battle and return
    ///     the player to the exploration scene at the saved return position.
    /// </summary>
    public void Forfeit()
    {
        GD.Print("BattleLauncher: Forfeit — returning to exploration.");
        ReturnToExploration();
    }

    /// <summary>
    ///     Player pressed Continue on the VictoryScreen. Same scene transition as
    ///     forfeit, but callers can layer reward / progression hooks here later.
    /// </summary>
    public void VictoryReturn()
    {
        GD.Print("BattleLauncher: Victory continue — returning to exploration.");
        // TODO: hand off rewards (xp, loot) to a progression system before returning.
        ReturnToExploration();
    }

    /// <summary>
    ///     Common return path: load the saved exploration scene and queue the player
    ///     position so the new scene can position the player on its first frame.
    /// </summary>
    private void ReturnToExploration()
    {
        if (string.IsNullOrEmpty(_returnScenePath))
        {
            GD.PrintErr("BattleLauncher: no return scene set; staying on battle scene.");
            return;
        }

        _pendingReturnPosition = _returnPosition;
        PendingSetup = null;

        // Route through MainManager shell if available
        if (MainManager.Instance != null)
            MainManager.Instance.LoadScene(_returnScenePath, showHud: false);
        else
        {
            GD.Print("[BattleLauncher] MainManager instance missing. Running standalone return fallback.");
            Error err = GetTree().ChangeSceneToFile(_returnScenePath);
            if (err != Error.Ok)
                GD.PrintErr($"BattleLauncher: Standalone fallback ChangeSceneToFile failed with {err}");
        }
    }

    /// <summary>
    ///     The player controller in the exploration scene calls this in <c>_Ready</c> to
    ///     find out whether it should snap to a return position from a finished battle.
    ///     Returns <c>null</c> when there's no pending return — the player keeps whatever
    ///     position the exploration scene gave them by default. Consuming clears the
    ///     stored value so subsequent reads return null.
    /// </summary>
    public Vector3? ConsumePendingReturnPosition()
    {
        Vector3? value = _pendingReturnPosition;
        _pendingReturnPosition = null;
        return value;
    }
}

using Godot;

public partial class ButtonMenu : Button
{
    /// <summary>
    ///     Wired to <c>ButtonPlay.pressed</c> on the beta main menu. Drops the player
    ///     into the prison exploration scene where they can walk around and trigger
    ///     encounters via NPCs (e.g. the Soldier → <c>BattleLauncher.Launch</c>).
    /// </summary>
    public void OnPlayButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Level/Prison.tscn");
    }

    public void OnOptionsButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/settings_beta.tscn");
    }

    public void OnOptionsExitButtonPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/menu_beta.tscn");
    }

    public void OnExitButtonPressed()
    {
        GetTree().Quit();
    }
}

using AshesOfVelsingrad.Audio;
using AshesOfVelsingrad.Managers;
using Godot;

public partial class ButtonMenu : Button
{
    /// <summary>
    ///     The beta main menu (<c>menu_beta.tscn</c>) and settings screen
    ///     (<c>settings_beta.tscn</c>) don't have <c>MainMenu.cs</c> attached, so the
    ///     scene root never declares its own music context. Hooking on
    ///     <see cref="_Ready" /> here covers it: every <c>ButtonMenu</c> on those
    ///     scenes pings the audio manager when the scene loads and
    ///     <c>SetMusicContext</c> is idempotent, so multiple buttons declaring the
    ///     same context cost nothing.
    /// </summary>
    public override void _Ready()
    {
        AudioManager.Instance?.SetMusicContext(MusicContext.MainMenu);
    }

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

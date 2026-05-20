using AshesOfVelsingrad;
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
        const string prisonScenePath = "res://scenes/Level/Prison.tscn";

        if (MainManager.Instance != null)
        {
            MainManager.Instance.LoadScene(prisonScenePath, showHud: false);
        }
        else
        {
            GD.Print("[ButtonMenu] MainManager instance missing. Swapping scene tree directly.");
            GetTree().ChangeSceneToFile(prisonScenePath);
        }
    }

    public void OnOptionsButtonPressed()
    {
        MenuManager.Instance?.ShowMenu(MenuManager.OPTIONS_MENU);
    }

    public void OnOptionsExitButtonPressed()
    {
        MenuManager.Instance?.GoBack();
    }

    public void OnExitButtonPressed()
    {
        GetTree().Quit();
    }
}
using System;
using AshesOfVelsingrad.Managers;
using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not found!");
            return;
        }
    }

    private void OnOptionsButtonButtonUp()
    {
        GD.Print("LOAD OPTIONS MENU");

        if (MenuManager.Instance == null)
        {
            GD.PrintErr("MenuManager not found!");
            return;
        }

        MenuManager.Instance.ShowMenu(MenuManager.OPTIONS_MENU);
    }

    private void OnExitButtonButtonUp()
    {
        GD.Print("EXIT GAME");
        GetTree().Quit();
    }
}

using System;
using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("OptionsButton").ButtonUp += OnOptionsButtonButtonUp;
        GetNode<Button>("ExitButton").ButtonUp += OnExitButtonButtonUp;
    }

    private void OnOptionsButtonButtonUp()
    {
        GD.Print("LOAD OPTIONS MENU");
        GetTree().ChangeSceneToFile("res://scenes/menu/options_menu.tscn");
    }

    private void OnExitButtonButtonUp()
    {
        GD.Print("EXIT GAME");
        GetTree().Quit();
    }
}

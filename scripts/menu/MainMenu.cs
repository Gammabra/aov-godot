using System;
using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("OptionsButton").ButtonUp += OnOptionsButtonButtonUp;
    }

    private void OnOptionsButtonButtonUp()
    {
        GD.Print("LOAD OPTIONS MENU");
        GetTree().ChangeSceneToFile("res://scenes/menu/options_menu.tscn");
    }
}

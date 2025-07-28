using Godot;
using System;

public partial class OptionsMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("BackButton").ButtonUp += OnBackButtonButtonUp;
    }

    private void OnBackButtonButtonUp()
    {
        GD.Print("LOAD MAIN MENU");
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }
}

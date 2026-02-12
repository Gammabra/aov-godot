using Godot;

public partial class button_menu : Button
{
	public void _on_OptionsButton_pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/settings_beta.tscn");
	}

	public void _on_ExitButton_pressed()
	{
		GetTree().Quit();
	}
}

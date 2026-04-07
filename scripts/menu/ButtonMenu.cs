using Godot;

public partial class ButtonMenu : Button
{
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

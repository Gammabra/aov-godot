using Godot;
using System.Collections.Generic;

public partial class settings_page_manager : Node
{
	private List<Button> titles = new();
	private List<CanvasItem> pages = new();

	public int currentPage = 1;

	public override void _Ready()
	{
		// Boutons titres
		var hbox = GetNode("VBoxContainer/HBoxContainer");

		foreach (Node child in hbox.GetChildren())
		{
			if (child is Button btn)
				titles.Add(btn);
		}

		// Pages dans MainContent
		var mainContent = GetParent().GetNode("MainContent");

		pages.Add(mainContent.GetNode<CanvasItem>("PageSubtitle"));
		pages.Add(mainContent.GetNode<CanvasItem>("PageVideo"));
		pages.Add(mainContent.GetNode<CanvasItem>("PageVisual"));
		pages.Add(mainContent.GetNode<CanvasItem>("PageCommand"));
		pages.Add(mainContent.GetNode<CanvasItem>("PageAudio"));

		UpdateAll();
	}

	public void ChangePage(int pageIndex)
	{
		currentPage = pageIndex;
		UpdateAll();
	}

	private void UpdateAll()
	{
		// Couleur boutons
		for (int i = 0; i < titles.Count; i++)
			titles[i].Modulate = Colors.White;

		if (currentPage - 1 >= 0 && currentPage - 1 < titles.Count)
			titles[currentPage - 1].Modulate = Colors.Red;

		// Visibilité pages
		for (int i = 0; i < pages.Count; i++)
			pages[i].Visible = false;

		if (currentPage - 1 >= 0 && currentPage - 1 < pages.Count)
			pages[currentPage - 1].Visible = true;
	}

	// === Connexions boutons ===

	public void _on_button_subtitle_pressed() => ChangePage(1);
	public void _on_button_video_pressed() => ChangePage(2);
	public void _on_button_visual_pressed() => ChangePage(3);
	public void _on_button_command_pressed() => ChangePage(4);
	public void _on_button_audio_pressed() => ChangePage(5);
}

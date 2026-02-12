using Godot;

public partial class button_settings_font : OptionButton
{
	public override void _Ready()
	{
		Clear();
		LoadFonts("res://font");
	}

	private void LoadFonts(string path)
	{
		var dir = DirAccess.Open(path);
		if (dir == null)
			return;

		dir.ListDirBegin();
		string fileName = dir.GetNext();

		while (fileName != "")
		{
			string fullPath = path + "/" + fileName;

			if (dir.CurrentIsDir())
			{
				if (fileName != "." && fileName != "..")
					LoadFonts(fullPath);
			}
			else
			{
				if (fileName.EndsWith(".ttf") || fileName.EndsWith(".otf"))
				{
					string cleanName = fileName.GetFile();
					AddItem(cleanName);
				}
			}

			fileName = dir.GetNext();
		}
	}
}

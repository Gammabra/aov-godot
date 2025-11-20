extends OptionButton

func _ready():
	clear()
	load_fonts("res://font")

func load_fonts(path):
	var dir = DirAccess.open(path)
	if dir == null:
		return
	
	dir.list_dir_begin()
	var file_name = dir.get_next()

	while file_name != "":
		var full_path = path + "/" + file_name

		if dir.current_is_dir():
			if file_name != "." and file_name != "..":
				load_fonts(full_path)
		else:
			if file_name.ends_with(".ttf") or file_name.ends_with(".otf"):
				var clean_name = file_name.get_file()
				add_item(clean_name)
		file_name = dir.get_next()

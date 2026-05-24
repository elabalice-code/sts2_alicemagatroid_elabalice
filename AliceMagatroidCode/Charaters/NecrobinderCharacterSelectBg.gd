extends Control

const PORTRAIT_PATH := "res://AliceMagatroid/Images/CharSelect/skin1_select.png"

func _ready() -> void:
	z_index = 0
	var portrait := get_node_or_null("Portrait") as TextureRect
	if portrait != null:
		ensure_portrait_texture(portrait)
		layout_portrait(portrait)

	log_state("ready")
	call_deferred("log_state", "deferred")

func ensure_portrait_texture(portrait: TextureRect) -> void:
	if portrait.texture != null:
		return

	for candidate_path in get_portrait_candidate_paths():
		if candidate_path.is_empty() or not FileAccess.file_exists(candidate_path):
			continue

		var bytes := FileAccess.get_file_as_bytes(candidate_path)
		if bytes.is_empty():
			continue

		var image := Image.new()
		var error := image.load_png_from_buffer(bytes)
		if error == OK and not image.is_empty():
			portrait.texture = ImageTexture.create_from_image(image)
			return

func get_portrait_candidate_paths() -> PackedStringArray:
	var executable_dir := OS.get_executable_path().get_base_dir()
	var game_root := executable_dir

	return PackedStringArray([
		ProjectSettings.globalize_path(PORTRAIT_PATH),
		game_root.path_join("Mods").path_join("AliceMagatroid_Mod").path_join("AliceMagatroid").path_join("Images").path_join("CharSelect").path_join("skin1_select.png"),
		game_root.path_join("mods").path_join("AliceMagatroid_Mod").path_join("AliceMagatroid").path_join("Images").path_join("CharSelect").path_join("skin1_select.png")
	])

func layout_portrait(portrait: TextureRect) -> void:
	portrait.z_index = 0
	portrait.clip_contents = false
	portrait.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	portrait.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	portrait.anchor_left = 0.0
	portrait.anchor_top = 0.0
	portrait.anchor_right = 1.0
	portrait.anchor_bottom = 1.0
	portrait.offset_left = 0.0
	portrait.offset_top = 0.0
	portrait.offset_right = 0.0
	portrait.offset_bottom = 0.0

func log_state(stage: String) -> void:
	var stamp := Time.get_datetime_string_from_system(true, true)
	var portrait := get_node_or_null("Portrait") as TextureRect
	if portrait == null:
		print("[AliceMagatroid_Mod][CharSelectBg][%s][%s] Portrait node missing" % [stamp, stage])
		return

	ensure_portrait_texture(portrait)
	layout_portrait(portrait)

	var texture_size := Vector2.ZERO
	var texture_type := "<null>"
	var texture_path := "<null>"
	if portrait.texture != null:
		texture_size = portrait.texture.get_size()
		texture_type = portrait.texture.get_class()
		texture_path = portrait.texture.resource_path

	print("[AliceMagatroid_Mod][CharSelectBg][%s][%s] visible=%s viewport=%s control_size=%s texture_size=%s texture_type=%s texture_path=%s" % [stamp, stage, visible, get_viewport_rect().size, portrait.size, texture_size, texture_type, texture_path])

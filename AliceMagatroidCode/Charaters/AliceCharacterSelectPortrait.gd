extends Control

const CONFIG_PATH := "user://alice_magatroid.cfg"
const CONFIG_SECTION := "appearance"
const CONFIG_SKIN_KEY := "selected_skin"
const MIN_SKIN := 1
const MAX_SKIN := 4
const SKIN1_TEXTURE := preload("res://AliceMagatroid/Images/CharSelect/skin1_select.png")
const SKIN2_TEXTURE := preload("res://AliceMagatroid/Images/CharSelect/skin2_select.png")
const SKIN3_TEXTURE := preload("res://AliceMagatroid/Images/CharSelect/skin3_select.png")
const SKIN4_TEXTURE := preload("res://AliceMagatroid/Images/CharSelect/skin4_select.png")

func _ready() -> void:
	set_process_input(true)

	var left_button := get_node_or_null("SkinPicker/HBox/LeftButton") as TextureButton
	var right_button := get_node_or_null("SkinPicker/HBox/RightButton") as TextureButton

	if left_button != null:
		left_button.pressed.connect(func() -> void: change_skin(-1))

	if right_button != null:
		right_button.pressed.connect(func() -> void: change_skin(1))

	print("[TestActor][AliceMagatroid_Mod][CharSelect] Ready left=%s right=%s" % [left_button != null, right_button != null])
	update_visuals()

func _input(event: InputEvent) -> void:
	if event is not InputEventMouseButton:
		return

	var mouse_event := event as InputEventMouseButton
	if not mouse_event.pressed or mouse_event.button_index != MOUSE_BUTTON_LEFT:
		return

	var left_button := get_node_or_null("SkinPicker/HBox/LeftButton") as Control
	var right_button := get_node_or_null("SkinPicker/HBox/RightButton") as Control
	var mouse_position := mouse_event.position

	if left_button != null and left_button.get_global_rect().has_point(mouse_position):
		print("[TestActor][AliceMagatroid_Mod][CharSelect] Click left at %s" % [mouse_position])
		change_skin(-1)
		get_viewport().set_input_as_handled()
		return

	if right_button != null and right_button.get_global_rect().has_point(mouse_position):
		print("[TestActor][AliceMagatroid_Mod][CharSelect] Click right at %s" % [mouse_position])
		change_skin(1)
		get_viewport().set_input_as_handled()

func change_skin(direction: int) -> void:
	var current := get_selected_skin()
	var next := current + direction
	if next < MIN_SKIN:
		next = MAX_SKIN
	elif next > MAX_SKIN:
		next = MIN_SKIN

	set_selected_skin(next)
	print("[TestActor][AliceMagatroid_Mod][CharSelect] ChangeSkin current=%s next=%s" % [current, next])
	update_visuals()

func update_visuals() -> void:
	var portrait := get_node_or_null("Portrait") as TextureRect
	var label := get_node_or_null("SkinPicker/HBox/SkinLabel") as Label
	var skin := get_selected_skin()
	var path := get_portrait_path(skin)
	var texture := get_portrait_texture(skin)

	if portrait != null and texture != null:
		portrait.texture = texture
		portrait.position = get_portrait_position(skin)
		portrait.size = get_portrait_size(texture, skin)
		portrait.visible = true
		print("[TestActor][AliceMagatroid_Mod][CharSelect] UpdateVisuals skin=%s path=%s position=%s size=%s" % [skin, path, portrait.position, portrait.size])
	else:
		print("[TestActor][AliceMagatroid_Mod][CharSelect] UpdateVisuals missing path=%s" % [path])

	if label != null:
		label.text = "皮肤%s" % skin

func get_portrait_path(skin: int) -> String:
	match skin:
		2:
			return "res://AliceMagatroid/Images/CharSelect/skin2_select.png"
		3:
			return "res://AliceMagatroid/Images/CharSelect/skin3_select.png"
		4:
			return "res://AliceMagatroid/Images/CharSelect/skin4_select.png"
		_:
			return "res://AliceMagatroid/Images/CharSelect/skin1_select.png"

func get_portrait_texture(skin: int) -> Texture2D:
	match skin:
		2:
			return SKIN2_TEXTURE
		3:
			return SKIN3_TEXTURE
		4:
			return SKIN4_TEXTURE
		_:
			return SKIN1_TEXTURE

func get_portrait_position(skin: int) -> Vector2:
	return Vector2.ZERO

func get_portrait_scale(skin: int) -> Vector2:
	return Vector2.ONE

func get_portrait_size(texture: Texture2D, skin: int) -> Vector2:
	return Vector2(1920, 1080)

func get_selected_skin() -> int:
	var config := ConfigFile.new()
	if config.load(CONFIG_PATH) != OK:
		return MIN_SKIN

	var value = config.get_value(CONFIG_SECTION, CONFIG_SKIN_KEY, MIN_SKIN)
	return clampi(int(value), MIN_SKIN, MAX_SKIN)

func set_selected_skin(skin: int) -> void:
	var config := ConfigFile.new()
	config.load(CONFIG_PATH)
	config.set_value(CONFIG_SECTION, CONFIG_SKIN_KEY, clampi(skin, MIN_SKIN, MAX_SKIN))
	config.save(CONFIG_PATH)

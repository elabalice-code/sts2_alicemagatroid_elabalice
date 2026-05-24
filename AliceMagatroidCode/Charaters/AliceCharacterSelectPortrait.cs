using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace AliceMagatroid_Mod.Character;

public partial class AliceCharacterSelectPortrait : Control
{
	private const string ConfigPath = "user://alice_magatroid.cfg";
	private const string ConfigSection = "appearance";
	private const string ConfigSkinKey = "selected_skin";
	private const int MinSkin = 1;
	private const int MaxSkin = 3;

	public override void _Ready()
	{
		var leftButton = GetNodeOrNull<Button>("SkinPicker/HBox/LeftButton");
		var rightButton = GetNodeOrNull<Button>("SkinPicker/HBox/RightButton");

		if (leftButton != null)
		{
			leftButton.Pressed += () => ChangeSkin(-1);
			leftButton.MouseFilter = MouseFilterEnum.Ignore;
		}

		if (rightButton != null)
		{
			rightButton.Pressed += () => ChangeSkin(1);
			rightButton.MouseFilter = MouseFilterEnum.Ignore;
		}

		Log.Info($"[AliceMagatroid_Mod][CharSelect] Ready left={(leftButton != null)} right={(rightButton != null)}", 2);
		UpdateVisuals();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseEvent)
		{
			return;
		}

		var leftButton = GetNodeOrNull<Control>("SkinPicker/HBox/LeftButton");
		var rightButton = GetNodeOrNull<Control>("SkinPicker/HBox/RightButton");
		var mousePosition = mouseEvent.Position;

		if (leftButton != null && leftButton.GetGlobalRect().HasPoint(mousePosition))
		{
			Log.Info($"[AliceMagatroid_Mod][CharSelect] Click left at {mousePosition}", 2);
			ChangeSkin(-1);
			GetViewport().SetInputAsHandled();
			return;
		}

		if (rightButton != null && rightButton.GetGlobalRect().HasPoint(mousePosition))
		{
			Log.Info($"[AliceMagatroid_Mod][CharSelect] Click right at {mousePosition}", 2);
			ChangeSkin(1);
			GetViewport().SetInputAsHandled();
		}
	}

	private void ChangeSkin(int direction)
	{
		var current = GetSelectedSkin();
		var next = current + direction;
		if (next < MinSkin)
		{
			next = MaxSkin;
		}
		else if (next > MaxSkin)
		{
			next = MinSkin;
		}

		SetSelectedSkin(next);
		Log.Info($"[AliceMagatroid_Mod][CharSelect] ChangeSkin current={current} next={next}", 2);
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		var portrait = GetNodeOrNull<TextureRect>("Portrait");
		var label = GetNodeOrNull<Label>("SkinPicker/HBox/SkinLabel");
		var skin = GetSelectedSkin();
		var path = GetPortraitPath(skin);

		if (portrait != null && ResourceLoader.Exists(path))
		{
			var texture = ResourceLoader.Load<Texture2D>(path);
			portrait.Texture = texture;
			portrait.Position = GetPortraitPosition(skin);
			portrait.Size = GetPortraitSize(texture, skin);
			portrait.Visible = true;
			Log.Info($"[AliceMagatroid_Mod][CharSelect] UpdateVisuals skin={skin} path={path} position={portrait.Position} size={portrait.Size}", 2);
		}
		else
		{
			Log.Info($"[AliceMagatroid_Mod][CharSelect] UpdateVisuals missing path={path}", 2);
		}

		if (label != null)
		{
			label.Text = $"皮肤{skin}";
		}
	}

	private static string GetPortraitPath(int skin) => skin switch
	{
		2 => "res://AliceMagatroid/Images/CharSelect/skin2_select.png",
		3 => "res://AliceMagatroid/Images/CharSelect/skin3_select.png",
		_ => "res://AliceMagatroid/Images/CharSelect/skin1_select.png"
	};

	private static Vector2 GetPortraitPosition(int skin) => skin switch
	{
		2 => new Vector2(1160, 186),
		3 => new Vector2(1065, 240),
		_ => new Vector2(1170, 158)
	};

	private static Vector2 GetPortraitScale(int skin) => skin switch
	{
		2 => new Vector2(1.7f, 1.7f),
		3 => new Vector2(1.35f, 1.35f),
		_ => new Vector2(1.35f, 1.35f)
	};

	private static Vector2 GetPortraitSize(Texture2D texture, int skin)
	{
		var scale = GetPortraitScale(skin);
		return texture.GetSize() * scale;
	}

	private static int GetSelectedSkin()
	{
		var config = new ConfigFile();
		if (config.Load(ConfigPath) != Error.Ok)
		{
			return MinSkin;
		}

		var value = config.GetValue(ConfigSection, ConfigSkinKey, MinSkin);
		var skin = value.VariantType == Variant.Type.Int ? value.AsInt32() : MinSkin;
		return Mathf.Clamp(skin, MinSkin, MaxSkin);
	}

	private static void SetSelectedSkin(int skin)
	{
		var config = new ConfigFile();
		config.Load(ConfigPath);
		config.SetValue(ConfigSection, ConfigSkinKey, Mathf.Clamp(skin, MinSkin, MaxSkin));
		config.Save(ConfigPath);
	}
}

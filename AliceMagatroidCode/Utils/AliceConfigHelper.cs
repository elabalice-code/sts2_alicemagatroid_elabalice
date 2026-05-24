using Godot;

namespace AliceMagatroid_Mod.Utils;

public static class AliceConfigHelper
{
	public const string ShouldOpenTutorialKey = "shouldOpenTutorial";
	public const string ShouldOpenMedicineTutorialKey = "shouldOpenMedicineTutorial";
	public const string EnableSpellCardSignDisplayKey = "enableSpellCardSignDisplay";
	public const string EnableCardTargetIconsKey = "enableCardTargetIcons";
	public const string EnableAlternativeMarisaCardImageKey = "enableAlternativeMarisaCardImage";
	public const string UsePackMasterStyleMarisaCardsKey = "usePackMasterStyleMarisaCards";
	public const string EnableShanghaiDollEventForOtherCharactersKey = "enableShanghaiDollEventForOtherCharacters";
	public const string EnableMedicineForOtherCharactersKey = "enableMedicineForOtherCharacters";
	public const string EnableDebuggingKey = "enableDebugging";
	public const string SkinChosenKey = "skinChosen";
	public const string SignatureCheckedKey = "signatureChecked";
	public const string SunglassesUnlockedKey = "sunglassesUnlocked";
	public const string SunglassesEnabledKey = "sunglassesEnabled";

	private const string ConfigPath = "user://alice_magatroid.cfg";
	private const string ConfigSection = "settings";

	private static readonly Dictionary<string, Variant> Defaults = new()
	{
		[ShouldOpenTutorialKey] = true,
		[ShouldOpenMedicineTutorialKey] = true,
		[EnableSpellCardSignDisplayKey] = true,
		[EnableCardTargetIconsKey] = true,
		[EnableAlternativeMarisaCardImageKey] = true,
		[UsePackMasterStyleMarisaCardsKey] = true,
		[EnableShanghaiDollEventForOtherCharactersKey] = true,
		[EnableMedicineForOtherCharactersKey] = false,
		[EnableDebuggingKey] = false,
		[SkinChosenKey] = "ORIGINAL",
		[SignatureCheckedKey] = false,
		[SunglassesUnlockedKey] = false,
		[SunglassesEnabledKey] = false
	};

	private static ConfigFile? _config;

	public static void LoadConfig()
	{
		_config = new ConfigFile();
		_config.Load(ConfigPath);

		foreach (var (key, value) in Defaults)
		{
			if (!_config.HasSectionKey(ConfigSection, key))
			{
				_config.SetValue(ConfigSection, key, value);
			}
		}

		Save();
	}

	public static bool ShouldOpenTutorial()
	{
		return GetBool(ShouldOpenTutorialKey);
	}

	public static void SetShouldOpenTutorial(bool value)
	{
		SetValue(ShouldOpenTutorialKey, value);
	}

	public static bool ShouldOpenMedicineTutorial()
	{
		return GetBool(ShouldOpenMedicineTutorialKey);
	}

	public static void SetShouldOpenMedicineTutorial(bool value)
	{
		SetValue(ShouldOpenMedicineTutorialKey, value);
	}

	public static bool EnableSpellCardSignDisplay()
	{
		return GetBool(EnableSpellCardSignDisplayKey);
	}

	public static void SetEnableSpellCardSignDisplay(bool value)
	{
		SetValue(EnableSpellCardSignDisplayKey, value);
	}

	public static bool EnableCardTargetIcons()
	{
		return GetBool(EnableCardTargetIconsKey);
	}

	public static void SetEnableCardTargetIcons(bool value)
	{
		SetValue(EnableCardTargetIconsKey, value);
	}

	public static bool EnableAlternativeMarisaCardImage()
	{
		return GetBool(EnableAlternativeMarisaCardImageKey);
	}

	public static void SetEnableAlternativeMarisaCardImage(bool value)
	{
		SetValue(EnableAlternativeMarisaCardImageKey, value);
	}

	public static bool UsePackMasterStyleMarisaCards()
	{
		return GetBool(UsePackMasterStyleMarisaCardsKey);
	}

	public static void SetUsePackMasterStyleMarisaCards(bool value)
	{
		SetValue(UsePackMasterStyleMarisaCardsKey, value);
	}

	public static bool EnableShanghaiDollEventForOtherCharacters()
	{
		return GetBool(EnableShanghaiDollEventForOtherCharactersKey);
	}

	public static void SetEnableShanghaiDollEventForOtherCharacters(bool value)
	{
		SetValue(EnableShanghaiDollEventForOtherCharactersKey, value);
	}

	public static bool EnableMedicineForOtherCharacters()
	{
		return GetBool(EnableMedicineForOtherCharactersKey);
	}

	public static void SetEnableMedicineForOtherCharacters(bool value)
	{
		SetValue(EnableMedicineForOtherCharactersKey, value);
	}

	public static bool EnableDebugging()
	{
		return GetBool(EnableDebuggingKey);
	}

	public static void SetEnableDebugging(bool value)
	{
		SetValue(EnableDebuggingKey, value);
	}

	public static string GetAliceSkinChosen()
	{
		return GetString(SkinChosenKey);
	}

	public static void SetAliceSkinChosen(string skin)
	{
		SetValue(SkinChosenKey, skin);
	}

	public static bool HasSignatureChecked()
	{
		return GetBool(SignatureCheckedKey);
	}

	public static void SetSignatureChecked(bool value)
	{
		SetValue(SignatureCheckedKey, value);
	}

	public static bool IsSunglassesUnlocked()
	{
		return GetBool(SunglassesUnlockedKey);
	}

	public static void SetSunglassesUnlocked(bool value)
	{
		SetValue(SunglassesUnlockedKey, value);
	}

	public static bool IsSunglassesEnabled()
	{
		return GetBool(SunglassesEnabledKey);
	}

	public static void SetSunglassesEnabled(bool value)
	{
		SetValue(SunglassesEnabledKey, value);
	}

	public static void Save()
	{
		EnsureLoaded().Save(ConfigPath);
	}

	private static bool GetBool(string key)
	{
		return EnsureLoaded().GetValue(ConfigSection, key, Defaults[key]).AsBool();
	}

	private static string GetString(string key)
	{
		return EnsureLoaded().GetValue(ConfigSection, key, Defaults[key]).AsString();
	}

	private static void SetValue(string key, Variant value)
	{
		EnsureLoaded().SetValue(ConfigSection, key, value);
		Save();
	}

	private static ConfigFile EnsureLoaded()
	{
		if (_config == null)
		{
			LoadConfig();
		}

		return _config!;
	}
}

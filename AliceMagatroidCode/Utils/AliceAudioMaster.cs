namespace AliceMagatroid_Mod.Utils;

public static class AliceAudioMaster
{
	public const string CharSelect1 = "AliceMargatroid:CHAR_SELECT_1";
	public const string CharSelect2 = "AliceMargatroid:CHAR_SELECT_2";
	public const string CharSelect3 = "AliceMargatroid:CHAR_SELECT_3";
	public const string ArisSelect1 = "AliceMargatroid:ARIS_SELECT_1";
	public const string ArisSelect2 = "AliceMargatroid:ARIS_SELECT_2";
	public const string ArisMaidSelect1 = "AliceMargatroid:ARIS_MAID_SELECT_1";
	public const string ArisMaidSelect2 = "AliceMargatroid:ARIS_MAID_SELECT_2";
	public const string EasterEgg = "AliceMargatroid:EASTER_EGG";
	public const string ArisCommonSkill = "AliceMargatroid:ARIS_COMMON_SKILL";
	public const string AliceInWonderland = "AliceMargatroid:ALICE_IN_WONDERLAND";
	public const string Resurgence = "AliceMargatroid:RESURGENCE";

	public static readonly IReadOnlyDictionary<string, string> AudioPaths = new Dictionary<string, string>
	{
		[CharSelect1] = "AliceMagatroid/Audio/charSelect/SELECT_ALICE1.wav",
		[CharSelect2] = "AliceMagatroid/Audio/charSelect/SELECT_ALICE2.wav",
		[CharSelect3] = "AliceMagatroid/Audio/charSelect/SELECT_ALICE3.wav",
		[ArisSelect1] = "AliceMagatroid/Audio/charSelect/ARIS_1.mp3",
		[ArisSelect2] = "AliceMagatroid/Audio/charSelect/ARIS_2.ogg",
		[ArisMaidSelect1] = "AliceMagatroid/Audio/charSelect/ARIS_MAID_1.wav",
		[ArisMaidSelect2] = "AliceMagatroid/Audio/charSelect/ARIS_MAID_2.ogg",
		[EasterEgg] = "AliceMagatroid/Audio/charSelect/panpankapan.ogg",
		[ArisCommonSkill] = "AliceMagatroid/Audio/charSelect/CommonSkill.ogg",
		[AliceInWonderland] = "AliceMagatroid/Audio/effects/AliceInWonderland.wav",
		[Resurgence] = "AliceMagatroid/Audio/effects/Resurgence.wav"
	};
}

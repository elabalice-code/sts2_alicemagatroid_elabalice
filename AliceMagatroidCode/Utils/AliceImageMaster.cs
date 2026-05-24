using AliceMagatroid_Mod.Extensions;

namespace AliceMagatroid_Mod.Utils;

public static class AliceImageMaster
{
	public static readonly string TipTop = "UI/tip/tipTop.png".ImagePath();
	public static readonly string TipMid = "UI/tip/tipMid.png".ImagePath();
	public static readonly string TipBot = "UI/tip/tipBot.png".ImagePath();
	public static readonly string ShanghaiDollCharge = "orbs/Sparkle.png".ImagePath();
	public static readonly string GoldEndTurnButtonGlow = "UI/goldEndTurnButtonGlow.png".ImagePath();
	public static readonly string AliceArrow = "UI/AliceArrow.png".ImagePath();
	public static readonly string MedicineArrow = "UI/AliceArrowRed.png".ImagePath();
	public static readonly string DessertIcon = "vfx/Dessert.png".ImagePath();
	public static readonly string MotifIcon = "vfx/PomMotif.png".ImagePath();
	public static readonly string IntentPoison = "UI/intents/IntentPoison.png".ImagePath();

	public static readonly IReadOnlyList<string> Pokers = new[]
	{
		"vfx/poker/spade.png".ImagePath(),
		"vfx/poker/heart.png".ImagePath(),
		"vfx/poker/diamond.png".ImagePath(),
		"vfx/poker/club.png".ImagePath(),
		"vfx/poker/joker_small.png".ImagePath(),
		"vfx/poker/joker_big.png".ImagePath()
	};

	public static IEnumerable<string> ExistingImages()
	{
		foreach (var path in new[]
		{
			TipTop,
			TipMid,
			TipBot,
			ShanghaiDollCharge,
			GoldEndTurnButtonGlow,
			AliceArrow,
			MedicineArrow,
			DessertIcon,
			MotifIcon,
			IntentPoison
		}.Concat(Pokers))
		{
			if (Godot.ResourceLoader.Exists(path))
			{
				yield return path;
			}
		}
	}
}

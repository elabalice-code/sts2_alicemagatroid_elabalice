using System.Threading;
using AliceMagatroid_Mod.Cards;
using AliceMagatroid_Mod.Dolls;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AliceMagatroid_Mod.Patch;

[HarmonyPatch(typeof(NMouseCardPlay), "Start")]
public static class DollPlacementInterceptPatch
{
	private static void Prefix(NMouseCardPlay __instance)
	{
		var card = Traverse.Create(__instance).Property("Card").GetValue() as CardModel;
		GD.Print("[AliceMagatroid_Mod][PlacementIntercept] Prefix called, card=", card?.GetType().Name ?? "null", " confirming=", DollState.IsConfirmingPlacement);
		if (card is not DollPlacement) return;

		if (DollState.IsConfirmingPlacement)
		{
			GD.Print("[AliceMagatroid_Mod][PlacementIntercept] Skipping intercept - confirming placement");
			return;
		}

		Traverse.Create(__instance).Field("_skipStartCardDrag").SetValue(true);
		GD.Print("[AliceMagatroid_Mod][PlacementIntercept] Set _skipStartCardDrag=true");
	}

	private static void Postfix(NMouseCardPlay __instance)
	{
		var card = Traverse.Create(__instance).Property("Card").GetValue() as CardModel;
		if (card is not DollPlacement) return;

		if (DollState.IsConfirmingPlacement)
		{
			return;
		}

		GD.Print("[AliceMagatroid_Mod][PlacementIntercept] Postfix called for DollPlacement");

		var cts = Traverse.Create(__instance).Field("_cancellationTokenSource").GetValue() as CancellationTokenSource;
		cts?.Cancel();
		GD.Print("[AliceMagatroid_Mod][PlacementIntercept] Cancelled CTS=", cts != null);

		DollState.EnterPlacementTargeting(__instance);
		GD.Print("[AliceMagatroid_Mod][PlacementIntercept] EnterPlacementTargeting done, IsPlacementTargeting=", DollState.IsPlacementTargeting);
	}
}

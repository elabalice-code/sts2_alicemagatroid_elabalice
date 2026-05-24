using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Dolls;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;

namespace AliceMagatroid_Mod.Patch;

[HarmonyPatch(typeof(Hook), "BeforeDamageReceived")]
public static class CaptureAttackerPatch
{
	[HarmonyPrefix]
	private static void CaptureDealer(Creature target, decimal amount, Creature? dealer)
	{
		if (dealer != null && amount > 0m && target.Player?.Character is AliceMagatroid)
		{
			DollState.SetPendingDamageAttacker(dealer);
		}
	}
}

[HarmonyPatch(typeof(Creature), "DamageBlockInternal")]
public static class DollBlockRedirectPatch
{
	[HarmonyPrefix]
	private static bool RedirectBlockDamage(Creature __instance, ref decimal amount, ValueProp props, ref decimal __result)
	{
		if (amount <= 0m || !IsAlice(__instance))
		{
			return true;
		}

		DollState.RegisterCombatContextFrom(__instance);

		if (DollState.DollCount == 0)
		{
			return true;
		}

		var originalAmount = (int)amount;
		var remaining = DollState.AbsorbDamageForAttacker(originalAmount);

		if (remaining <= 0)
		{
			__result = originalAmount;
			amount = 0;
			return false;
		}

		if (remaining < originalAmount)
		{
			amount = remaining;
		}

		return true;
	}

	internal static bool IsAlice(Creature creature)
	{
		try
		{
			return creature.Player?.Character is AliceMagatroid;
		}
		catch
		{
			return false;
		}
	}
}

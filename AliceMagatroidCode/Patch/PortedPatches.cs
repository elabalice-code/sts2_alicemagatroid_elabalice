using AliceMagatroid_Mod.Dolls;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

namespace AliceMagatroid_Mod.Patch;

[HarmonyPatch(typeof(CombatManager), "SetUpCombat")]
public static class CombatInitializePatch
{
	[HarmonyPostfix]
	private static void OnCombatSetUp()
	{
		Log.Info("[AliceMagatroid_Mod] Combat initialized via CombatManager.SetUpCombat", 2);
		DollState.RecycleAll();
		BonusScorePatch.Reset();
	}
}

public static class BonusScorePatch
{
	private static bool _shanghaiReturned;

	public static void MarkShanghaiReturned()
	{
		_shanghaiReturned = true;
	}

	public static void Reset()
	{
		_shanghaiReturned = false;
	}
}

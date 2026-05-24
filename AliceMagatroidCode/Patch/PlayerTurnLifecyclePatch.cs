using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Dolls;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace AliceMagatroid_Mod.Patch;

[HarmonyPatch(typeof(Creature), "BeforeTurnStart")]
public static class PlayerTurnStartPatch
{
	[HarmonyPostfix]
	private static void OnBeforeTurnStart(Creature __instance, int roundNumber, CombatSide side)
	{
		if (side != CombatSide.Player) return;
		if (!IsAlice(__instance)) return;

		DollState.TryHandlePlayerTurnStart(__instance, "BeforeTurnStart");
	}

	private static bool IsAlice(Creature creature)
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

[HarmonyPatch(typeof(Hook), "BeforeTurnEnd")]
public static class PlayerTurnEndPatch
{
	[HarmonyPrefix]
	private static void OnBeforeTurnEnd(CombatState combatState, CombatSide side)
	{
		if (side != CombatSide.Player) return;

		var player = combatState.Players.FirstOrDefault();
		if (player == null) return;
		if (player.Character is not AliceMagatroid) return;

		DollState.RegisterCombatContext(combatState);
		DollState.TryHandlePlayerTurnEnd(player, "BeforeTurnEnd");
	}
}

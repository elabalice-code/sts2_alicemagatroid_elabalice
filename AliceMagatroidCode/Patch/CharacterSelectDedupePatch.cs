using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace AliceMagatroid_Mod.Patch;

[HarmonyPatch(typeof(ModelDb), "AllCharacters", MethodType.Getter)]
public static class CharacterSelectDedupePatch
{
	[HarmonyPostfix]
	private static IEnumerable<CharacterModel> DeduplicateCharacters(IEnumerable<CharacterModel> __result)
	{
		var seen = new HashSet<System.Type>();
		foreach (var c in __result)
		{
			if (seen.Add(c.GetType()))
			{
				yield return c;
			}
			else
			{
				MainFile.Logger.Info($"[CharSelectDedupe] Skipping duplicate {c.GetType().FullName}");
			}
		}
	}
}

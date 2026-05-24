using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Dolls;

[HarmonyPatch(typeof(NCreature), "SetAnimationTrigger")]
public static class AliceAnimationPatch
{
	public static void Postfix(NCreature __instance, string trigger)
	{
		if (__instance.Entity == null || !__instance.Entity.IsPlayer)
			return;

		if (__instance.Entity.ModelId.ToString() == "CHARACTER.ALICEMAGATROID_MOD-ALICE_MAGATROID")
		{
			Log.Info("[AliceMagatroid_Mod] AnimationTrigger=" + trigger, 2);
			switch (trigger)
			{
				case "Hit":
					PlayAnim(__instance, "Hit", false);
					break;

				case "Attack":
					PlayAnim(__instance, "Attack", false);
					break;

				case "Cast":
					PlayAnim(__instance, "Attack", true);
					break;

				case "Dead":
					PlayAnim(__instance, "Dead", false);
					break;

				default:
					PlayAnim(__instance, "Idle", false);
					break;
			}
		}
	}

	private static void PlayAnim(NCreature node, string animName, bool fromEnd)
	{
		var visual = DollVisuals.AttachTo(node);
		if (visual == null) return;

		visual.PrepareAnimation(animName);

		var anim = visual.GetAnimatedSprite();
		if (anim == null) return;

		anim.Frame = 0;
		anim.Play(animName, 1f, fromEnd);

		anim.Connect("animation_finished", Callable.From(() =>
		{
			visual.PrepareAnimation("Idle");
			anim.Play("Idle");
		}), 4u);
	}
}

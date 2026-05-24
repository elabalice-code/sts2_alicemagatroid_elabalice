using Godot;
using HarmonyLib;
	using AliceMagatroid_Mod.Dolls;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System;
using System.Collections.Generic;

[HarmonyPatch(typeof(CharacterModel), "CreateVisuals")]
public static class CombatVisualTracePatch
{
	public static void Postfix(CharacterModel __instance, NCreatureVisuals __result)
	{
		try
		{
			var modelType = __instance.GetType().FullName ?? __instance.GetType().Name;
			if (!ShouldTrace(modelType))
			{
				return;
			}

			if (modelType.Contains("AliceMagatroid", StringComparison.OrdinalIgnoreCase))
			{
				DollVisuals.AttachTo(__result);
			}

			var summary = DescribeVisualNode(__result);
			Log.Info($"[AliceMagatroid_Mod][CombatVisualTrace] modelType={modelType} result={summary}", 2);
		}
		catch (Exception ex)
		{
			Log.Info($"[AliceMagatroid_Mod][CombatVisualTrace] error={ex.GetType().Name}:{ex.Message}", 2);
		}
	}

	private static bool ShouldTrace(string modelType)
	{
		return modelType.Contains("Ironclad", StringComparison.OrdinalIgnoreCase)
			|| modelType.Contains("Silent", StringComparison.OrdinalIgnoreCase)
			|| modelType.Contains("AliceMagatroid", StringComparison.OrdinalIgnoreCase);
	}

	private static string DescribeVisualNode(Node node)
	{
		if (node == null)
		{
			return "null";
		}

		var parts = new List<string>
		{
			$"{node.Name}:{node.GetType().Name}",
			$"children={node.GetChildCount()}"
		};

		var childSummaries = new List<string>();
		for (var i = 0; i < node.GetChildCount(); i++)
		{
			var child = node.GetChild(i);
			childSummaries.Add(DescribeChild(child));
		}

		parts.Add($"childList=[{string.Join(", ", childSummaries)}]");

		var visualsNode = node.GetNodeOrNull<Node>("%Visuals");
		parts.Add($"visualsNode={(visualsNode == null ? "<missing>" : $"{visualsNode.Name}:{visualsNode.GetType().Name}")}");

		return string.Join(" | ", parts);
	}

	private static string DescribeChild(Node child)
	{
		var summary = $"{child.Name}:{child.GetType().Name}";

		if (child is AnimatedSprite2D animatedSprite)
		{
			summary += $":animation={animatedSprite.Animation}";
			var frames = animatedSprite.SpriteFrames;
			if (frames != null && frames.HasAnimation(animatedSprite.Animation))
			{
				var frameTexture = frames.GetFrameTexture(animatedSprite.Animation, 0);
				summary += $":frame0={DescribeTexture(frameTexture)}";
			}
		}
		else if (child is Sprite2D sprite)
		{
			summary += $":texture={DescribeTexture(sprite.Texture)}";
		}

		return summary;
	}

	private static string DescribeTexture(Texture2D texture)
	{
		if (texture == null)
		{
			return "<null>";
		}

		return $"{texture.GetClass()}:{texture.ResourcePath}:size={texture.GetSize()}";
	}
}

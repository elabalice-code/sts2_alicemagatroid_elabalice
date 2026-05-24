using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using System;
using System.Collections.Generic;

[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
public static class CharacterSelectTracePatch
{
	public static void Prefix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
	{
		Log.Info($"[AliceMagatroid_Mod][CharSelectTrace][{Stamp()}][Before] button={DescribeButton(charSelectButton)} | {DescribeCharacter(characterModel)} | bgContainer={DescribeBgContainer(__instance)}", 2);
	}

	public static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel)
	{
		Log.Info($"[AliceMagatroid_Mod][CharSelectTrace][{Stamp()}][After] button={DescribeButton(charSelectButton)} | {DescribeCharacter(characterModel)} | bgContainer={DescribeBgContainer(__instance)}", 2);
	}

	private static string Stamp()
	{
		return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
	}

	private static string DescribeButton(NCharacterSelectButton button)
	{
		if (button == null)
		{
			return "null";
		}

		return $"{button.Name}:{button.GetType().Name}";
	}

	private static string DescribeCharacter(CharacterModel characterModel)
	{
		if (characterModel == null)
		{
			return "character=null";
		}

		var values = new List<string>
		{
			$"modelType={characterModel.GetType().FullName}",
			$"modelId={ReadProperty(characterModel, "ModelId")}",
			$"title={ReadProperty(characterModel, "Title")}",
			$"description={ReadProperty(characterModel, "Description")}",
		};

		var bgPath = ReadProperty(characterModel, "CharacterSelectBg");
		values.Add($"characterSelectBg={bgPath}");
		values.Add($"characterSelectBgExists={ResourceExists(bgPath)}");

		var visualPath = ReadProperty(characterModel, "VisualPath");
		values.Add($"visualPath={visualPath}");
		values.Add($"visualPathExists={ResourceExists(visualPath)}");

		if (characterModel is PlaceholderCharacterModel placeholder)
		{
			values.Add($"placeholderId={placeholder.PlaceholderID}");
			values.Add($"customCharacterSelectBg={placeholder.CustomCharacterSelectBg}");
			values.Add($"customCharacterSelectBgExists={ResourceExists(placeholder.CustomCharacterSelectBg)}");
			values.Add($"customVisualPath={placeholder.CustomVisualPath}");
			values.Add($"customVisualPathExists={ResourceExists(placeholder.CustomVisualPath)}");
		}

		return string.Join(" | ", values);
	}

	private static string DescribeBgContainer(NCharacterSelectScreen screen)
	{
		var field = AccessTools.Field(screen.GetType(), "_bgContainer");
		var bgContainer = field?.GetValue(screen) as Node;
		if (bgContainer == null)
		{
			return "null";
		}

		var children = new List<string>();
		for (var i = 0; i < bgContainer.GetChildCount(); i++)
		{
			var child = bgContainer.GetChild(i);
			children.Add($"{child.Name}:{child.GetType().Name}:visible={ReadVisible(child)}");
		}

		return $"{bgContainer.Name}:{bgContainer.GetType().Name}:children={bgContainer.GetChildCount()}[{string.Join(", ", children)}]";
	}

	private static string ReadProperty(object target, string propertyName)
	{
		try
		{
			var property = AccessTools.Property(target.GetType(), propertyName);
			var value = property?.GetValue(target);
			return value?.ToString() ?? "<null>";
		}
		catch (Exception ex)
		{
			return $"<error:{ex.GetType().Name}>";
		}
	}

	private static bool ResourceExists(string path)
	{
		return !string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path);
	}

	private static string ReadVisible(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			return canvasItem.Visible.ToString();
		}

		return "n/a";
	}
}

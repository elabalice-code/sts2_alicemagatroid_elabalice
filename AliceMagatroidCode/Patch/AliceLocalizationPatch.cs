using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace AliceMagatroid_Mod.Patch;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Preload))]
public static class AliceLocalizationPatch
{
	private static readonly FieldInfo LocDictionaryField =
		AccessTools.Field(typeof(LocTable), "_translations");

	private static readonly string[] LocTables = ["cards", "relics", "characters", "events", "potions", "ancients"];

	[HarmonyPostfix]
	private static void InjectChineseLoc()
	{
		var basePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		if (basePath == null) return;

		var locDir = System.IO.Path.Join(basePath, "localization", "zhs");
		if (!Directory.Exists(locDir)) return;

		foreach (var tableName in LocTables)
		{
			var filePath = System.IO.Path.Join(locDir, $"{tableName}.json");
			if (!File.Exists(filePath)) continue;

			var entries = LoadJson(filePath);
			if (entries != null)
			{
				InjectToTable(tableName, entries);
			}
		}
	}

	private static Dictionary<string, string>? LoadJson(string path)
	{
		try
		{
			var json = File.ReadAllText(path);
			return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
		}
		catch
		{
			return null;
		}
	}

	private const string Prefix = "ALICEMAGATROID_MOD-";

	private static void InjectToTable(string tableName, Dictionary<string, string> entries)
	{
		var table = LocManager.Instance.GetTable(tableName);
		if (table == null) return;

		var dict = LocDictionaryField?.GetValue(table) as Dictionary<string, string>;
		if (dict == null) return;

		foreach (var kv in entries)
		{
			dict[kv.Key] = kv.Value;
			if (kv.Key.StartsWith(Prefix))
			{
				dict[Prefix + kv.Key] = kv.Value;
			}
		}
	}
}

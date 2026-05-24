using Godot;
using System.Text.Json;

namespace AliceMagatroid_Mod.Utils;

public static class AliceHelper
{
	public const string Sts1IdPrefix = "AliceMargatroid";
	public const string Sts2IdPrefix = "ALICEMAGATROID_MOD";
	public const string DefaultLanguage = "zhs";

	private const string MarisaModId = "TS05_Marisa";
	private const string RandomPredictionModId = "RandomNumberPredictionMaster";

	private static readonly Dictionary<string, bool> ModAvailability = new(StringComparer.OrdinalIgnoreCase);

	public static string MakeId(string name)
	{
		return $"{Sts1IdPrefix}:{name}";
	}

	public static string MakeLocalizationId(string name)
	{
		return $"{Sts2IdPrefix}-{ToSnakeUpper(name)}";
	}

	public static string AssetPath(params string[] parts)
	{
		return Path.Join(new[] { MainFile.AssetRoot }.Concat(parts).ToArray());
	}

	public static string ResPath(params string[] parts)
	{
		return $"res://{AssetPath(parts).Replace('\\', '/')}";
	}

	public static string ImagePath(string type, string name)
	{
		return AssetPath("Images", type, WithPngExtension(name));
	}

	public static string CardImagePath(string name)
	{
		return ImagePath("Cards", name);
	}

	public static string RelicImagePath(string name)
	{
		return ImagePath("Relics", name);
	}

	public static string RelicLargeImagePath(string name)
	{
		return ImagePath("Relics", $"{name}_big");
	}

	public static string PowerImagePath(string name)
	{
		return ImagePath("Powers", name);
	}

	public static string LocalizationPath(string name, string language = DefaultLanguage)
	{
		return Path.Join("localization", language, WithJsonExtension(name));
	}

	public static bool ResourceExists(params string[] parts)
	{
		return ResourceLoader.Exists(AssetPath(parts));
	}

	public static bool IsModAvailable(string modId)
	{
		if (ModAvailability.TryGetValue(modId, out var available))
		{
			return available;
		}

		available = ProbeModAvailability(modId);
		ModAvailability[modId] = available;
		return available;
	}

	public static bool IsMarisaModAvailable()
	{
		return IsModAvailable(MarisaModId);
	}

	public static bool IsRandomPredictionModAvailable()
	{
		return IsModAvailable(RandomPredictionModId);
	}

	public static string ToSnakeUpper(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return value;
		}

		var chars = new List<char>(value.Length + 8);
		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];
			if (c is '-' or ':' or ' ' or '.')
			{
				AddSeparator(chars);
				continue;
			}

			if (char.IsUpper(c) && i > 0 && chars.Count > 0 && chars[^1] != '_')
			{
				var previous = value[i - 1];
				var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
				if (char.IsLower(previous) || char.IsDigit(previous) || nextIsLower)
				{
					AddSeparator(chars);
				}
			}

			chars.Add(char.ToUpperInvariant(c));
		}

		return new string(chars.ToArray()).Trim('_');
	}

	private static bool ProbeModAvailability(string modId)
	{
		var modsRoot = Path.Join(ProjectWorkspaceRoot(), "Steam", "steamapps", "common", "Slay the Spire 2", "Mods");
		var manifestPath = Path.Join(modsRoot, modId, $"{modId}.json");
		if (File.Exists(manifestPath))
		{
			return true;
		}

		if (!Directory.Exists(modsRoot))
		{
			return false;
		}

		foreach (var manifest in Directory.EnumerateFiles(modsRoot, "*.json", SearchOption.AllDirectories))
		{
			try
			{
				using var stream = File.OpenRead(manifest);
				using var json = JsonDocument.Parse(stream);
				if (json.RootElement.TryGetProperty("id", out var id) &&
					string.Equals(id.GetString(), modId, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			catch
			{
				// Broken manifests should not block character initialization.
			}
		}

		return false;
	}

	private static string ProjectWorkspaceRoot()
	{
		var current = Directory.GetCurrentDirectory();
		var parent = Directory.GetParent(current);
		return parent?.FullName ?? current;
	}

	private static string WithPngExtension(string name)
	{
		return Path.HasExtension(name) ? name : $"{name}.png";
	}

	private static string WithJsonExtension(string name)
	{
		return Path.HasExtension(name) ? name : $"{name}.json";
	}

	private static void AddSeparator(List<char> chars)
	{
		if (chars.Count > 0 && chars[^1] != '_')
		{
			chars.Add('_');
		}
	}
}

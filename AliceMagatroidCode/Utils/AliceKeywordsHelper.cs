using System.Text.Json;
using System.Text.Json.Serialization;

namespace AliceMagatroid_Mod.Utils;

public static class AliceKeywordsHelper
{
	private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase);

	public static IReadOnlyDictionary<string, string> DollDescriptions => Descriptions;

	public static void AddDollKeywords(IEnumerable<AliceKeyword> dollKeywords)
	{
		Descriptions.Clear();

		foreach (var keyword in dollKeywords)
		{
			var name = keyword.Names.Skip(1).FirstOrDefault() ?? keyword.Names.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			Descriptions[name] = keyword.Description;
		}
	}

	public static IReadOnlyList<AliceKeyword> LoadKeywords(string language = AliceHelper.DefaultLanguage)
	{
		var path = AliceHelper.LocalizationPath("keywords", language);
		if (!File.Exists(path))
		{
			return Array.Empty<AliceKeyword>();
		}

		var json = File.ReadAllText(path);
		var keywords = JsonSerializer.Deserialize<List<AliceKeyword>>(json, JsonOptions);
		if (keywords == null)
		{
			return Array.Empty<AliceKeyword>();
		}

		return keywords;
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};
}

public sealed class AliceKeyword
{
	[JsonPropertyName("NAMES")]
	public string[] Names { get; init; } = Array.Empty<string>();

	[JsonPropertyName("DESCRIPTION")]
	public string Description { get; init; } = string.Empty;
}

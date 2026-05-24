using System;
using System.Collections.Generic;

namespace AliceMagatroid_Mod.Dolls;

internal static class DollRegistry
{
	private static readonly Dictionary<string, Func<Sts2Doll>> Factories = new(StringComparer.OrdinalIgnoreCase)
	{
		[DollState.ShanghaiDollId] = static () => new ShanghaiDoll(),
		[DollState.FranceDollId] = static () => new FranceDoll(),
		[DollState.NetherlandsDollId] = static () => new NetherlandsDoll(),
		[DollState.KyotoDollId] = static () => new KyotoDoll(),
		[DollState.HouraiDollId] = static () => new HouraiDoll(),
		["Susan"] = static () => new SusanDoll(),
		["SusanReplica"] = static () => new SusanReplicaDoll()
	};

	public static void Register(string dollId, Func<Sts2Doll> factory)
	{
		if (string.IsNullOrWhiteSpace(dollId))
		{
			throw new ArgumentException("Doll id must not be empty.", nameof(dollId));
		}

		Factories[dollId] = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public static Sts2Doll Create(string dollId)
	{
		if (Factories.TryGetValue(dollId, out var factory))
		{
			return factory();
		}

		return new ShanghaiDoll();
	}
}

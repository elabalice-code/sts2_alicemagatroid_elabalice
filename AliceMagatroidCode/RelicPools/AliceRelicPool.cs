using BaseLib.Abstracts;
using AliceMagatroid_Mod.Extensions;
using AliceMagatroid_Mod.Relics;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;

namespace AliceMagatroid_Mod.Character;

public class AliceRelicPool : CustomRelicPoolModel
{
	public override string EnergyColorName => AliceMagatroid.CharacterId;
	public override string? BigEnergyIconPath => "Charui/big_energy.png".ImagePath();
	public override string? TextEnergyIconPath => "Charui/text_energy.png".ImagePath();

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return [
			ModelDb.Relic<AlicesGrimoire>(),
			ModelDb.Relic<AlicesDarkGrimoire>(),
			ModelDb.Relic<BlackTeaRelic>(),
			ModelDb.Relic<ColorContacts>(),
			ModelDb.Relic<MagicPotion>(),
			ModelDb.Relic<SacrificialDoll>(),
			ModelDb.Relic<ShanghaiDollRelic>(),
			ModelDb.Relic<StringRing>(),
			ModelDb.Relic<SuspiciousCard>(),
			ModelDb.Relic<SwordOfLight_Supernova>(),
		];
	}
}

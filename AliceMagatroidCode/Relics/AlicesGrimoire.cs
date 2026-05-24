using BaseLib.Abstracts;
using BaseLib.Utils;
using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Dolls;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Relics;

[Pool(typeof(AliceRelicPool))]
public class AlicesGrimoire : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Starter;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/alices_grimoire.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/alices_grimoire.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/alices_grimoire_big.png";

	public override Task BeforeCombatStart()
	{
		Flash();
		DollState.RegisterCombatContextFrom(this);
		DollState.SpawnShanghai();
		return Task.CompletedTask;
	}
}

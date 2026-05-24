using BaseLib.Abstracts;
using BaseLib.Utils;
using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Dolls;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Potions;

[Pool(typeof(AlicePotionPool))]
public class BottledDoll : CustomPotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;
	public override PotionUsage Usage => PotionUsage.CombatOnly;
	public override TargetType TargetType => TargetType.None;
	public override string? CustomPackedImagePath => "res://AliceMagatroid/Images/Potions/bottled_doll.png";

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		DollState.SpawnRandomBaseDollAt(DollState.FindFirstEmptySlot());
		await Task.CompletedTask;
	}
}

[Pool(typeof(AlicePotionPool))]
public class ConcentrationPotion : CustomPotionModel
{
	public override PotionRarity Rarity => PotionRarity.Rare;
	public override PotionUsage Usage => PotionUsage.CombatOnly;
	public override TargetType TargetType => TargetType.None;
	public override string? CustomPackedImagePath => "res://AliceMagatroid/Images/Potions/concentration_potion.png";

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		DollState.RegisterCombatContextFrom(this);
		var sourceCard = CreateVirtualSourceCard();
		if (sourceCard != null)
		{
			await DollState.ActAll(choiceContext, sourceCard);
		}
	}

	private static CardModel? CreateVirtualSourceCard()
	{
		return null; // Will use DollState's stored context
	}
}

[Pool(typeof(AlicePotionPool))]
public class WeavingPotion : CustomPotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;
	public override PotionUsage Usage => PotionUsage.CombatOnly;
	public override TargetType TargetType => TargetType.None;
	public override string? CustomPackedImagePath => "res://AliceMagatroid/Images/Potions/weaving_potion.png";

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		DollState.Spawn(null, 2, randomPlacement: true);
		await Task.CompletedTask;
	}
}

public class AlicePotionPool : CustomPotionPoolModel
{
	protected override IEnumerable<PotionModel> GenerateAllPotions()
	{
		return
		[
			ModelDb.Potion<BottledDoll>(),
			ModelDb.Potion<ConcentrationPotion>(),
			ModelDb.Potion<WeavingPotion>(),
		];
	}
}

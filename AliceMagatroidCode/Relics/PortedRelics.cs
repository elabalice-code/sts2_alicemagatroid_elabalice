using BaseLib.Abstracts;
using BaseLib.Utils;
using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Dolls;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

// Dummy marker so the namespace is in scope for [Pool(typeof(AliceRelicPool))] below.

namespace AliceMagatroid_Mod.Relics;

// Boss relic: On doll spawn, each existing doll adds +3 maxHP and +3 HP to the new doll
[Pool(typeof(AliceRelicPool))]
public class AlicesDarkGrimoire : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Shop;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/alices_dark_grimoire.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/alices_dark_grimoire.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/alices_dark_grimoire_big.png";

	public override Task BeforeCombatStart()
	{
		Flash();
		DollState.RegisterCombatContextFrom(this);
		return Task.CompletedTask;
	}
}

// Special relic: On obtain, increases max HP by 7 and heals 7 HP
[Pool(typeof(AliceRelicPool))]
public class BlackTeaRelic : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Event;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/black_tea_relic.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/black_tea_relic.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/black_tea_relic_big.png";

	private const int HealAmount = 7;

	public override Task AfterObtained()
	{
		DollState.IncreaseMaxHpAndHeal(HealAmount);
		return Task.CompletedTask;
	}
}

// Boss relic: First 3 turns of each combat, gain 1 energy (tracked via DollState)
[Pool(typeof(AliceRelicPool))]
public class ColorContacts : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Shop;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/color_contacts.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/color_contacts.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/color_contacts_big.png";

	public override Task BeforeCombatStart()
	{
		Flash();
		DollState.RegisterCombatContextFrom(this);
		DollState.AddColorContacts(3);
		return Task.CompletedTask;
	}
}

// Uncommon relic: On turn 2, gain 2 energy (tracked via DollState)
[Pool(typeof(AliceRelicPool))]
public class MagicPotion : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/magic_potion.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/magic_potion.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/magic_potion_big.png";

	public override Task BeforeCombatStart()
	{
		DollState.RegisterCombatContextFrom(this);
		DollState.AddMagicPotion();
		return Task.CompletedTask;
	}
}

// Common relic: At end of player turn, if empty slot would take damage, grant player 4 block
// Tracked via DollState during turn lifecycle
[Pool(typeof(AliceRelicPool))]
public class SacrificialDoll : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Common;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/sacrificial_doll.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/sacrificial_doll.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/sacrificial_doll_big.png";

	public override Task BeforeCombatStart()
	{
		DollState.RegisterCombatContextFrom(this);
		DollState.AddSacrificialDollBlock(4);
		return Task.CompletedTask;
	}
}

// Special relic: At start of first turn each combat, deal 4 damage to lowest HP enemy
[Pool(typeof(AliceRelicPool))]
public class ShanghaiDollRelic : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Event;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/shanghai_doll_relic.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/shanghai_doll_relic.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/shanghai_doll_relic_big.png";

	public override Task BeforeCombatStart()
	{
		Flash();
		DollState.RegisterCombatContextFrom(this);
		DollState.DamageLowestHpEnemy(4);
		return Task.CompletedTask;
	}
}

// Rare relic: At end of combat, saves first active doll type. Next combat start, spawns that doll type
[Pool(typeof(AliceRelicPool))]
public class StringRing : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/string_ring.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/string_ring.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/string_ring_big.png";

	private string? _savedDollId;

	public override Task BeforeCombatStart()
	{
		if (!string.IsNullOrEmpty(_savedDollId))
		{
			Flash();
			DollState.RegisterCombatContextFrom(this);
			DollState.Spawn(_savedDollId, 1);
			_savedDollId = null;
		}

		return Task.CompletedTask;
	}
}

// Rare relic: Placeholder/WIP - currently does nothing functional
[Pool(typeof(AliceRelicPool))]
public class SuspiciousCard : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/suspicious_card.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/suspicious_card.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/suspicious_card_big.png";
}

// Uncommon relic: At turn start, applies SwordOfLight effect
[Pool(typeof(AliceRelicPool))]
public class SwordOfLight_Supernova : CustomRelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;
	public override string PackedIconPath => "res://AliceMagatroid/Images/Relics/sword_of_light_supernova.png";
	protected override string PackedIconOutlinePath => "res://AliceMagatroid/Images/Relics/sword_of_light_supernova.png";
	protected override string BigIconPath => "res://AliceMagatroid/Images/Relics/sword_of_light_supernova_big.png";

	public override Task BeforeCombatStart()
	{
		DollState.RegisterCombatContextFrom(this);
		DollState.AddSwordOfLight();
		return Task.CompletedTask;
	}
}

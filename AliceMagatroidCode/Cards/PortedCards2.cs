using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using AliceMagatroid_Mod.Dolls;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Cards;

// =====================================================================
// BASIC CARDS
// =====================================================================

public class Strike_AliceMargatroid() : AliceCard(1,
	CardType.Attack, CardRarity.Basic,
	TargetType.AnyEnemy)
{
	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}

public class Defend_AliceMargatroid() : AliceCard(1,
	CardType.Skill, CardRarity.Basic,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ApplyBlockToAliceOrSelectedDoll(choiceContext, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}

// =====================================================================
// COMMON CARDS
// =====================================================================

// STS1: 0-cost skill, upgrades top N cards in draw pile
public class SpectreMystery() : AliceCard(0,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _magicAmount = 5;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 5)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.UpgradeTopCardsInDrawPile(_magicAmount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 3;
	}
}

// STS1: doll-targeting command attack - damage doll's opponents, doll gains block/heal/maxHP
public class SuicideSquad() : AliceCard(1,
	CardType.Attack, CardRarity.Common,
	TargetType.Self)
{
	private int _damageAmount = 4;
	private int _blockAmount = 4;
	private int _healAmount = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(4, ValueProp.Move),
		new BlockVar(4, ValueProp.Move),
		new CardsVar("Magic", 4)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		TriggerAttackCardThreadTension();
		await DollState.AttackDollsOpponents(choiceContext, this, target.SlotIndex, _damageAmount);
		DollState.TryApplyBlockToSlotFromCard(target.SlotIndex, _blockAmount);
		DollState.TryHealAt(target.SlotIndex, _healAmount, out _);
		DollState.TryIncreaseMaxHpAt(target.SlotIndex, _healAmount);
	}

	protected override void OnUpgrade()
	{
		_damageAmount += 1;
		_blockAmount += 1;
		_healAmount += 1;
	}
}

// STS1: 0-cost attack, deal damage then move+upgrade an attack card from draw pile to hand
public class TheSouthernCross() : AliceCard(0,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	private int _magicAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(3, ValueProp.Move),
		new CardsVar("Magic", 1)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		await DollState.MoveAttackFromDrawToHandAndUpgrade(choiceContext, this, _magicAmount);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
		_magicAmount += 1;
	}
}

// =====================================================================
// UNCOMMON CARDS
// =====================================================================

// STS1: unplayable retain skill, gains 1 energy per turn, exhausts after N turns
// STS2 simplified: 0-cost skill, gain 1 energy
public class BlackTea() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.TryGainPlayerEnergy(1);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 1;
	}
}

// STS1: auto-plays when leftmost/rightmost card in hand is played, gains energy
// STS2 simplified: 1-cost skill, gain N energy
public class Dessert() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _magicAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.TryGainPlayerEnergy(_magicAmount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 1;
	}
}

// STS1: apply Vulnerable to all enemies + apply DevilryLightPower
public class DevilryLight() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.AllEnemies)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.ApplyVulnerableToAllEnemies(_magicAmount);
		DollState.AddDevilryLight(_magicAmount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 1;
	}
}

// STS1: discover 1 of 3 Marisa-themed cards at 0 cost (cross-mod)
// STS2 simplified: discover from preset cards
public class FriendsHelp() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		await DollState.DiscoverPresetCard(choiceContext, this, IsUpgraded);
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 2-cost power (1 upgraded), applies GeyserPower with threshold 60
public class Geyser() : AliceCard(2,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _magicAmount = 60;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 60)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddGeyserThreshold(_magicAmount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(1);
	}
}

// STS1: 2-cost attack, deal 7 dmg to single target + 6 AoE x magicNumber times
public class IllusoryMoon() : AliceCard(2,
	CardType.Attack, CardRarity.Uncommon,
	TargetType.AnyEnemy)
{
	private int _aoeDamage = 6;
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(7, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		for (var i = 0; i < _magicAmount; i++)
		{
			await DollState.AttackAllOpponents(choiceContext, this, _aoeDamage);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}

// STS1: 1-cost attack, 8 dmg, discover from top 3 of draw pile, exhaust
public class Lantern() : AliceCard(1,
	CardType.Attack, CardRarity.Uncommon,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		await DollState.DiscoverFromDrawPile(choiceContext, this, 3);
		ExhaustOnNextPlay = true;
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
	}
}

// STS1: 1-cost attack, 4 dmg, gain magicNumber strength + MagicianRayPower
public class MagicianRay() : AliceCard(1,
	CardType.Attack, CardRarity.Uncommon,
	TargetType.AnyEnemy)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(4, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		DollState.AddPlayerStrength(_magicAmount);
		DollState.AddMagicianRay(_magicAmount);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		_magicAmount += 1;
	}
}

// STS1: discard cards at doll-position indices in hand, then draw magicNumber cards
public class Motif() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _magicAmount = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 4)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		await DollState.DiscardCardsAtDollIndices(choiceContext, this);
		for (var i = 0; i < _magicAmount; i++)
		{
			await CommonActions.Draw(this, choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 1;
	}
}

// STS1: 2-cost AoE attack, 18 dmg, multiplied by magicNumber if card stats are modified
public class Perihelion() : AliceCard(2,
	CardType.Attack, CardRarity.Rare,
	TargetType.AllEnemies)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(18, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		var multiplier = DollState.IsPerihelionModified(this) ? _magicAmount : 1;
		for (var i = 0; i < multiplier; i++)
		{
			await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(6m);
	}
}

// STS1: 3-cost skill (2 upgraded), exhaust, gain energy equal to missing energy + draw
public class PolarNight() : AliceCard(3,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		var energyGain = DollState.GetMissingEnergyCount();
		if (energyGain > 0)
		{
			DollState.TryGainPlayerEnergy(energyGain);
			for (var i = 0; i < energyGain; i++)
			{
				await CommonActions.Draw(this, choiceContext);
			}
		}
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(2);
	}
}

// STS1: 0-cost skill, exhaust, upgrade random attack in hand + boost its damage
public class Revelation() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		DollState.UpgradeRandomAttackCardInHand(_magicAmount, IsUpgraded);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 1-cost attack, 8 dmg, exhaust, applies Weak+Vulnerable to target and same-name enemies
public class Ultimatum() : AliceCard(1,
	CardType.Attack, CardRarity.Uncommon,
	TargetType.AnyEnemy)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(8, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		ExhaustOnNextPlay = true;
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		DollState.ApplyWeakAndVulnerableToTargetAndDuplicates(cardPlay.Target, _magicAmount);
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 1;
	}
}

// STS1: 2-cost power (1 upgraded), applies UnlockMysticPower
public class UnlockMystic() : AliceCard(2,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddUnlockMystic();
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(1);
	}
}

// =====================================================================
// RARE CARDS
// =====================================================================

// STS1: 1-cost power, ethereal, exhausts hand, replaces each with random 0-cost combat card
public class AliceInWonderland() : AliceCard(1,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var handSize = DollState.GetHandCardCountExcluding(this);
		await DollState.ExhaustHandExcept(choiceContext, this);
		for (var i = 0; i < handSize; i++)
		{
			await DollState.AddRandomCombatCardToHand(choiceContext, this, IsUpgraded, true);
		}
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 1-cost skill (0 upgraded), exhaust, remembers hand cards and brings them back from piles
public class Bookmark() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		DollState.RestoreRememberedCards(choiceContext, this);
		DollState.RememberHandCards(choiceContext, this);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(0);
	}
}

// STS1: 1-cost attack, 16 dmg, exhaust, if fatal grants extra turn
public class CollapsingWorlds() : AliceCard(1,
	CardType.Attack, CardRarity.Rare,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		var wasFatal = await DollState.FatalAttack(choiceContext, this, cardPlay.Target);
		if (wasFatal)
		{
			DollState.GainExtraTurn();
		}
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 3-cost power, ethereal (not ethereal upgraded), applies PrincessFormPower
public class PrincessForm() : AliceCard(3,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddPrincessForm();
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 2-cost power, applies SeaOfSubconsciousnessPower, upgrade makes it innate
public class SeaOfSubconsciousness() : AliceCard(2,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	private int _magicAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddSeaOfSubconsciousness(_magicAmount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 7-cost attack, cost reduced by 1 per doll placed this combat, 7x AoE 7 dmg
// Can only be played when any doll acted 7+ times. Upgrade adds Retain.
public class SevenColoredPuppeteer() : AliceCard(7,
	CardType.Attack, CardRarity.Rare,
	TargetType.AllEnemies)
{
	private int _damageAmount = 7;
	private int _hitCount = 7;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		for (var i = 0; i < _hitCount; i++)
		{
			await DollState.AttackAllOpponents(choiceContext, this, _damageAmount);
		}
	}

	protected override void OnUpgrade()
	{
	}
}

// STS1: 0-cost skill, exhaust, HEALING tag, heal+gain energy+draw, upgrade drawn cards
public class SurpriseSpring() : AliceCard(0,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	private int _healAmount = 2;
	private int _drawAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new CardsVar("Magic", 2),
		new CardsVar("Cards", 1)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		var energyGain = IsUpgraded ? 2 : 1;
		DollState.TryGainPlayerEnergy(energyGain);
		DollState.TryHealPlayer(_healAmount);
		for (var i = 0; i < _drawAmount; i++)
		{
			await CommonActions.Draw(this, choiceContext);
		}

		DollState.UpgradeAllDrawnCards();
	}

	protected override void OnUpgrade()
	{
		_healAmount += 1;
		_drawAmount += 1;
	}
}

// STS1: 1-cost power, applies UsokaePower, upgrade makes it innate
public class Usokae() : AliceCard(1,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddUsokae();
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

// ---- Colorless Cards ----

// STS1: Special skill, spawns a random base doll
public class CreateDoll() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.None)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.SpawnRandomBaseDollAt(DollState.FindFirstEmptySlot());
		await Task.CompletedTask;
	}

	protected override void OnUpgrade() { }
}

// STS1: 1-cost skill, spawns Su_san doll, exhaust
public class CreateSusan() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.None)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		await DollState.PlaceDoll(choiceContext, this, "Susan");
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(0);
	}
}

// STS1: 0-cost skill, spawns SusanReplica doll, exhaust
public class CreateSusanReplica() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.None)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		await DollState.PlaceDoll(choiceContext, this, "SusanReplica");
	}

	protected override void OnUpgrade() { }
}

// STS1: Curse card, applies 2 poison to self at end of turn
public class PoisonousSweet() : AliceCard(-2,
	CardType.Curse, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.TryAdjustPlayerStatus(2, "Poison");
		await Task.CompletedTask;
	}

	protected override void OnUpgrade() { }
}

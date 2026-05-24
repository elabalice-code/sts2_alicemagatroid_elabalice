using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using AliceMagatroid_Mod.Dolls;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Cards;

public class FailedExperiment() : AliceCard(1,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];

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

public class CallToDolls() : AliceCard(1,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	private int _magicAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(7, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		DollState.ArmCallToDolls(cardPlay.Target, _magicAmount);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
		_magicAmount += 1;
	}
}

public class Thread() : AliceCard(1,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(6, ValueProp.Move),
		new CardsVar("Cards", 1)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		await CommonActions.Draw(this, choiceContext);
		DollState.SpawnShanghai();
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}

public class MysteriousChallenger() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ApplyCardBlockIfAllowed(cardPlay);
		if (DollState.DollCount >= 3)
		{
			await DollState.ActAll(choiceContext, this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}

public class DollMagic() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new BlockVar(7, ValueProp.Move),
		new CardsVar("Cards", 1)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ApplyCardBlockIfAllowed(cardPlay);
		if (DollState.DollCount >= 2)
		{
			await CommonActions.Draw(this, choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}

public class ArtfulChanter() : AliceCard(1,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddArtfulChanterActs(1);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(0);
	}
}

public class MistyLondonDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Cards", 1)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.SpawnShanghai();
		await CommonActions.Draw(this, choiceContext);
	}

	protected override void OnUpgrade()
	{
		DollState.SpawnShanghai();
	}
}

public class CharismaticOrleansDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.SpawnShanghai();
		await DollState.ActAll(choiceContext, this);
	}

	protected override void OnUpgrade()
	{
		DollState.SpawnShanghai();
	}
}

public class PokerTrick() : AliceCard(1,
	CardType.Attack, CardRarity.Rare,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(10, ValueProp.Move),
		new CardsVar("Cards", 1)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		await CommonActions.Draw(this, choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}

public class RefreshingSpringWater() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Cards", 1)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.Draw(this, choiceContext);
		if (DollState.DollCount >= 5)
		{
			await CommonActions.Draw(this, choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(1);
	}
}

public class SnowSweeping() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _blockAmount = 5;
	private int _drawAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!DollState.TryApplyBlockToSelectedTarget(this, _blockAmount))
		{
			DollState.TryGrantBlockToPlayerFromCard(_blockAmount);
		}

		for (var i = 0; i < _drawAmount; i++)
		{
			await CommonActions.Draw(this, choiceContext);
		}

		if (IsUpgraded && target.IsDoll)
		{
			await DollState.ActAt(choiceContext, this, target.SlotIndex);
		}
	}

	protected override void OnUpgrade()
	{
	}
}

public class VisitOfThreeFairies() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Cards", 3)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.Draw(this, choiceContext);
		await CommonActions.Draw(this, choiceContext);
		await CommonActions.Draw(this, choiceContext);
	}

	protected override void OnUpgrade()
	{
	}
}

public class WitchsTeaParty() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Cards", 2)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.Draw(this, choiceContext);
		await CommonActions.Draw(this, choiceContext);
		if (DollState.DollCount > 0)
		{
			await DollState.ActAll(choiceContext, this);
		}
	}

	protected override void OnUpgrade()
	{
	}
}

public class Pause() : AliceCard(2,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _blockAmount = 14;
	private int _drawNextTurn = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(14, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		if (!DollState.TryApplyBlockToSelectedTarget(this, _blockAmount))
		{
			DollState.TryGrantBlockToPlayerFromCard(_blockAmount);
		}

		DollState.TrySchedulePlayerDrawNextTurn(_drawNextTurn);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_blockAmount += 2;
		_drawNextTurn += 1;
	}
}

public class ForbiddenMagic() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _magicAmount = 1;

	internal override bool AllowsForbiddenMagic => false;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddForbiddenMagicActs(_magicAmount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_magicAmount += 1;
	}
}

public class CharitableFranceDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _weakAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		var slotIndex = target.IsEmptySlot ? target.SlotIndex : DollState.FindFirstEmptySlot();
		if (slotIndex < 0)
		{
			return;
		}

		await DollState.PlaceDoll(choiceContext, this, DollState.FranceDollId);
		DollState.ApplyWeakToCorrespondingOpponents(slotIndex, _weakAmount);
	}

	protected override void OnUpgrade()
	{
		_weakAmount += 1;
	}
}

public class RedHairedNetherlandsDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DollState.PlaceDoll(choiceContext, this, DollState.NetherlandsDollId);
	}

	protected override void OnUpgrade()
	{
	}
}

public class SpringKyotoDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _energyGain;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DollState.PlaceDoll(choiceContext, this, DollState.KyotoDollId);
		if (_energyGain > 0)
		{
			DollState.TryGainPlayerEnergy(_energyGain);
		}
	}

	protected override void OnUpgrade()
	{
		_energyGain = 1;
	}
}

public class QuietHouraiDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _bonusActs;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 0)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		var slotIndex = target.IsEmptySlot ? target.SlotIndex : DollState.FindFirstEmptySlot();
		if (slotIndex < 0)
		{
			return;
		}

		await DollState.PlaceDoll(choiceContext, this, DollState.HouraiDollId);
		for (var i = 0; i < _bonusActs; i++)
		{
			await DollState.ActAll(choiceContext, this);
		}

		DollState.TrySchedulePlayerDrawNextTurn(1);
	}

	protected override void OnUpgrade()
	{
		_bonusActs += 1;
	}
}

public class Housework() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.HasSlot)
		{
			return;
		}

		await DollState.ActAroundSlot(choiceContext, this, target.SlotIndex);
	}

	protected override void OnUpgrade()
	{
	}
}

public class DollActivation() : AliceCard(1,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.AddDollActivation(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class DollWar() : AliceCard(1,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 3)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.AddDollWarBonus(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class MagicConduit() : AliceCard(2,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddMagicConduit(1);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

public class DollAmbush() : AliceCard(1,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.AddDollAmbush(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class DollJudge() : AliceCard(1,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 40;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 40)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.AddDollJudgePercent(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 20;
	}
}

public class ReturningDolls() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _drawAmount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Cards", 2)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		if (DollState.RecycleAt(target.SlotIndex))
		{
			for (var i = 0; i < _drawAmount; i++)
			{
				await CommonActions.Draw(this, choiceContext);
			}

			DollState.TryAddCardToHand(new Retrace());
		}
	}

	protected override void OnUpgrade()
	{
		_drawAmount += 1;
	}
}

public class LuminousShanghaiDoll() : AliceCard(2,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	private int _amount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.AddLuminousShanghaiActs(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

public class DollOrchestra() : AliceCard(1,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.AddDollOrchestraDraw(1);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

public class MaidensBunraku() : AliceCard(1,
	CardType.Power, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DollState.AddMaidensBunrakuBlock(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class SemiAutomaton() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	private int _amount = 6;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 6)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;
		DollState.ArmSemiAutomaton(_amount);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		TrySetCardBaseCost(0);
	}
}

public class DollArrangement() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _blockAmount = 4;
	private bool _grantPlayerBlockOnUpgrade;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		var slotIndex = target.HasSlot ? target.SlotIndex : DollState.FindFirstEmptySlot();
		if (slotIndex >= 0)
		{
			var choices = DollState.GetMissingBaseDollIds().ToList();
			if (choices.Count > 0)
			{
				var first = choices[Random.Shared.Next(choices.Count)];
				choices.Remove(first);
				DollState.SpawnAt(first, slotIndex);

				while (DollState.DollCount < 3 && choices.Count > 0)
				{
					var next = choices[Random.Shared.Next(choices.Count)];
					choices.Remove(next);
					DollState.Spawn(next);
				}
			}
		}

		if (_grantPlayerBlockOnUpgrade)
		{
			await ApplyCardBlockIfAllowed(cardPlay);
		}

		for (var i = 0; i < DollState.MaxSlots; i++)
		{
			DollState.TryApplyBlockToSlot(i, _blockAmount);
		}
	}

	protected override void OnUpgrade()
	{
		_grantPlayerBlockOnUpgrade = true;
	}
}

public class DollArmy() : AliceCard(2,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _copyCount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 3)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return Task.CompletedTask;
		}

		for (var i = 0; i < _copyCount; i++)
		{
			var emptySlotIndex = DollState.FindFirstEmptySlot();
			if (emptySlotIndex < 0)
			{
				break;
			}

			DollState.SpawnCopyAt(target.SlotIndex, emptySlotIndex);
		}

		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

public class DollCrusader() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var occupiedSlots = DollState.GetOccupiedSlotIndices().ToList();
		if (occupiedSlots.Count == 0)
		{
			return;
		}

		foreach (var slotIndex in occupiedSlots)
		{
			DollState.RecycleAt(slotIndex);
		}

		foreach (var slotIndex in occupiedSlots)
		{
			DollState.SpawnRandomBaseDollAt(slotIndex);
		}

		await DollState.ActAll(choiceContext, this);
	}

	protected override void OnUpgrade()
	{
	}
}

public class DollCremation() : AliceCard(2,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(22, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		DollState.RecycleRightmostDoll();
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(8m);
	}
}

public class WarFlag() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		await DollState.ActAt(choiceContext, this, target.SlotIndex, _amount);
		var doll = DollState.GetDollAt(target.SlotIndex);
		if (doll == null)
		{
			return;
		}

		var blockGain = doll.Block / 2;
		if (blockGain <= 0)
		{
			return;
		}

		DollState.TryGrantBlockToPlayerFromCard(blockGain);
		if (DollState.CanGainBlockFromCards())
		{
			DollState.GrantBlockToOtherDolls(target.SlotIndex, blockGain);
		}
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class AliceGame() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		var recycledCount = DollState.RecycleAllExcept(target.SlotIndex);
		await DollState.ActAt(choiceContext, this, target.SlotIndex, recycledCount + 1);
	}

	protected override void OnUpgrade()
	{
	}
}

public class Relay() : AliceCard(0,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _drawAmount;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Cards", 0)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (_drawAmount > 0)
		{
			for (var i = 0; i < _drawAmount; i++)
			{
				await CommonActions.Draw(this, choiceContext);
			}
		}

		if (!target.IsDoll)
		{
			return;
		}

		var targetDoll = DollState.GetDollAt(target.SlotIndex);
		if (targetDoll == null)
		{
			return;
		}

		var totalTransferred = 0;
		foreach (var slotIndex in DollState.GetOccupiedSlotIndices())
		{
			if (slotIndex == target.SlotIndex)
			{
				continue;
			}

			var otherDoll = DollState.GetDollAt(slotIndex);
			if (otherDoll == null)
			{
				continue;
			}

			totalTransferred += otherDoll.LoseAllBlock();
		}

		if (totalTransferred > 0)
		{
			targetDoll.GainBlock(totalTransferred);
		}
	}

	protected override void OnUpgrade()
	{
		_drawAmount = 1;
	}
}

public class Masterpiece() : AliceCard(0,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _blockAmount = 4;
	private int _drawAmount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new BlockVar(4, ValueProp.Move),
		new CardsVar("Cards", 1)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		var doll = DollState.GetDollAt(target.SlotIndex);
		if (doll == null)
		{
			return;
		}

		var hpBeforeRecycle = doll.Hp;
		if (!DollState.RecycleAt(target.SlotIndex))
		{
			return;
		}

		DollState.TryGrantBlockToPlayerFromCard(_blockAmount + hpBeforeRecycle);
		for (var i = 0; i < _drawAmount; i++)
		{
			await CommonActions.Draw(this, choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		_blockAmount += 3;
	}
}

public class ReturnInanimateness() : AliceCard(2,
	CardType.Attack, CardRarity.Rare,
	TargetType.Self)
{
	private int _damageAmount = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var multiplier = 1 << DollState.GetPresentBaseDollIds().Count;
		DollState.RecycleAll();
		TriggerAttackCardThreadTension();
		await DollState.AttackAllOpponents(choiceContext, this, _damageAmount * multiplier);
	}

	protected override void OnUpgrade()
	{
		_damageAmount += 1;
	}
}

public class Sale() : AliceCard(0,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _goldAmount = 16;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 16)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		ExhaustOnNextPlay = true;

		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return Task.CompletedTask;
		}

		if (DollState.RecycleAt(target.SlotIndex))
		{
			DollState.TryGainPlayerGold(_goldAmount);
		}

		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_goldAmount += 8;
	}
}

public class DollInSea() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _maxHpAmount = 4;
	private bool _healToFullOnUpgrade;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 4)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (var slotIndex in DollState.GetOccupiedSlotIndices())
		{
			var doll = DollState.GetDollAt(slotIndex);
			if (doll == null)
			{
				continue;
			}

			var healAmount = _healToFullOnUpgrade ? doll.MaxHp : Math.Max(1, doll.MaxHp / 2);
			DollState.TryHealAt(slotIndex, healAmount, out _);

			if (doll.Id is DollState.ShanghaiDollId or DollState.KyotoDollId)
			{
				DollState.TryIncreaseMaxHpAt(slotIndex, _maxHpAmount);
			}
		}

		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_maxHpAmount += 2;
		_healToFullOnUpgrade = true;
	}
}

public class ClawMachine() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private static readonly string[] ChoiceOrder =
	[
		DollState.NetherlandsDollId,
		DollState.FranceDollId,
		DollState.HouraiDollId
	];

	private List<string> _remainingChoices = [.. ChoiceOrder];
	private object? _trackedCombatState;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		EnsureChoiceStateForCurrentCombat();
		ExhaustOnNextPlay = _remainingChoices.Count <= 1;
		if (_remainingChoices.Count == 0)
		{
			return;
		}

		var target = DollState.SelectedTarget;
		var slotIndex = target.IsEmptySlot ? target.SlotIndex : DollState.FindFirstEmptySlot();
		if (slotIndex < 0)
		{
			return;
		}

		var dollId = SelectDollId();
		if (string.IsNullOrEmpty(dollId))
		{
			return;
		}

		if (DollState.SpawnAt(dollId, slotIndex))
		{
			_remainingChoices.Remove(dollId);
		}

		await Task.CompletedTask;
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		EnsureChoiceStateForCurrentCombat();
		description.Add("SelectionBehavior", IsUpgraded ? "按剩余顺序" : "随机");
		description.Add("Choices", DescribeRemainingChoices());
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		_remainingChoices = [.. _remainingChoices];
	}

	protected override void OnUpgrade()
	{
	}

	private void EnsureChoiceStateForCurrentCombat()
	{
		if (!IsInCombat || CombatState == null)
		{
			if (_trackedCombatState != null)
			{
				_trackedCombatState = null;
				_remainingChoices = [.. ChoiceOrder];
			}

			return;
		}

		if (!ReferenceEquals(_trackedCombatState, CombatState))
		{
			_trackedCombatState = CombatState;
			_remainingChoices = [.. ChoiceOrder];
		}
	}

	private string SelectDollId()
	{
		if (_remainingChoices.Count == 0)
		{
			return string.Empty;
		}

		return IsUpgraded
			? _remainingChoices[0]
			: _remainingChoices[Random.Shared.Next(_remainingChoices.Count)];
	}

	private string DescribeRemainingChoices()
	{
		if (_remainingChoices.Count == 0)
		{
			return "无剩余候选";
		}

		return string.Join("、", _remainingChoices.Select(GetDollDisplayName));
	}

	private static string GetDollDisplayName(string dollId)
	{
		return dollId switch
		{
			DollState.NetherlandsDollId => "荷兰人偶",
			DollState.FranceDollId => "法兰西人偶",
			DollState.HouraiDollId => "蓬莱人偶",
			_ => dollId
		};
	}
}

public class SoldierOfCross() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _blockAmount = 9;
	private int _tauntTurns = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(9, ValueProp.Move),
		new CardsVar("Magic", 1)
	];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		if (!DollState.TryApplyBlockToSelectedTarget(this, _blockAmount))
		{
			DollState.TryGrantBlockToPlayerFromCard(_blockAmount);
			return Task.CompletedTask;
		}

		var target = DollState.SelectedTarget;
		if (target.IsDoll)
		{
			DollState.TauntAllOpponentsToSlot(target.SlotIndex, _tauntTurns);
		}

		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
		_blockAmount += 3;
		_tauntTurns += 1;
	}
}

public class Collector() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _amount = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(0, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var blockAmount = DollState.GetPlacedBaseDollTypeCountThisCombat() * _amount;
		if (!DollState.TryApplyBlockToSelectedTarget(this, blockAmount))
		{
			DollState.TryGrantBlockToPlayerFromCard(blockAmount);
		}

		await Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class SeekerDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _bonusActs = 1;
	private int _baseActs = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new CardsVar("Magic", 1),
		new CardsVar("Magic2", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var playCount = DollState.GetCardPlayCount(Id.Entry);

		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		var acts = _baseActs + (playCount <= 1 ? _bonusActs : 0);
		await DollState.ActAt(choiceContext, this, target.SlotIndex, acts);
	}

	protected override void OnUpgrade()
	{
		_baseActs += 1;
	}
}

public class VivaciousShanghaiDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _actCount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DollState.PlaceDoll(choiceContext, this, DollState.ShanghaiDollId);
		var slotIndex = DollState.GetOccupiedSlotIndices()
			.Where(index => DollState.GetDollAt(index)?.Id == DollState.ShanghaiDollId)
			.LastOrDefault(-1);
		if (slotIndex < 0)
		{
			return;
		}

		await DollState.ActAt(choiceContext, this, slotIndex, _actCount);
	}

	protected override void OnUpgrade()
	{
		_actCount += 1;
	}
}

public class SPDoll() : AliceCard(1,
	CardType.Skill, CardRarity.Common,
	TargetType.Self)
{
	private int _amount = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		var emptySlots = Enumerable.Range(0, DollState.MaxSlots)
			.Where(index => DollState.GetSlotTarget(index).IsEmptySlot)
			.OrderBy(_ => Random.Shared.Next())
			.Take(_amount)
			.ToArray();

		foreach (var slotIndex in emptySlots)
		{
			DollState.SpawnAt(Random.Shared.Next(2) == 0 ? DollState.ShanghaiDollId : DollState.KyotoDollId, slotIndex);
		}

		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class DollLances() : AliceCard(1,
	CardType.Attack, CardRarity.Uncommon,
	TargetType.AnyEnemy)
{
	private int _shanghaiActs = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(6, ValueProp.Move),
		new CardsVar("Magic", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);

		var shanghaiCount = DollState.GetOccupiedSlotIndices()
			.Count(index => DollState.GetDollAt(index)?.Id == DollState.ShanghaiDollId);
		var halfDamage = 2m;
		for (var i = 0; i < shanghaiCount * _shanghaiActs; i++)
		{
			dynamic cmd = DamageCmd.Attack(halfDamage).FromCard(this);
			if (cardPlay.Target != null)
			{
				cmd = cmd.Targeting((dynamic)cardPlay.Target);
			}

			await cmd.Execute(choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		_shanghaiActs += 1;
	}
}

public class DollMiraCeti() : AliceCard(-1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _actMultiplier = 2;
	private int _upgradeBonus;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (!target.IsDoll)
		{
			return;
		}

		var energyUsed = ResolveXCostAmount(cardPlay);
		var acts = Math.Max(0, energyUsed + _upgradeBonus) * _actMultiplier;
		await DollState.ActAt(choiceContext, this, target.SlotIndex, acts);
	}

	protected override void OnUpgrade()
	{
		_upgradeBonus = 1;
	}
}

public class DollOfRoundTable() : AliceCard(2,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _blockAmount = 18;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(18, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		await ApplyCardBlockIfAllowed(cardPlay);
		for (var i = 0; i < DollState.MaxSlots; i++)
		{
			DollState.TryApplyBlockToSlotFromCard(i, _blockAmount);
		}

		DollState.EnableRoundTableNoCardBlock();
	}

	protected override void OnUpgrade()
	{
	}
}

public class FuturisticBunraku() : AliceCard(1,
	CardType.Skill, CardRarity.Rare,
	TargetType.Self)
{
	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var target = DollState.SelectedTarget;
		if (target.IsDoll)
		{
			DollState.ArmFutureBunraku(target.SlotIndex);
		}

		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

public class StrawDollKamikaze() : AliceCard(1,
	CardType.Skill, CardRarity.Uncommon,
	TargetType.Self)
{
	private int _amount = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 1)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		DollState.TryAdjustPlayerStatus(_amount, "Blur");
		DollState.TryAddCardToHand(new Retrace());
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		_amount += 1;
	}
}

public class Tripwire() : AliceCard(1,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		var repeat = Math.Max(0, DollState.EmptySlots);
		for (var i = 0; i < repeat; i++)
		{
			await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
	}
}

public class Tyrant() : AliceCard(-1,
	CardType.Power, CardRarity.Rare,
	TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar("Magic", 2)];

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		var x = ResolveXCostAmount(cardPlay);
		if (x <= 0)
		{
			return Task.CompletedTask;
		}

		var bonus = 2 * x;
		DollState.AddDollWarBonus(bonus);
		DollState.IncreaseAllDollsMaxHp(bonus, healToMatch: true);
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
	}
}

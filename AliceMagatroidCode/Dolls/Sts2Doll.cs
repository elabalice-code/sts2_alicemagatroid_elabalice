using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AliceMagatroid_Mod.Dolls;

internal readonly record struct DollDamageResolution(int DamageToDoll, int BlockedByDoll, int OverflowDamage, bool Destroyed);
public readonly record struct DollHealResolution(int Healed, int Overflow);
internal enum DollRemovalReason
{
	Recycle,
	Destroy,
	Clear
}

internal abstract class Sts2Doll(string id, int maxHp)
{
	public string Id { get; } = id;
	public int MaxHp { get; protected set; } = maxHp;
	public int Hp { get; protected set; } = maxHp;
	public int Block { get; protected set; }
	public virtual bool IsEmptySlot => false;

	public DollSnapshot CreateSnapshot(int slotIndex)
	{
		return new DollSnapshot(slotIndex, Id, Hp, MaxHp, Block);
	}

	public virtual int GainBlock(int amount)
	{
		var applied = Math.Max(0, amount);
		Block += applied;
		if (applied > 0)
		{
			OnBlockGained(applied);
		}

		return applied;
	}

	public virtual DollDamageResolution TakeDamage(int incomingDamage)
	{
		var damage = Math.Max(0, incomingDamage);
		var blockedByDoll = Math.Min(Block, damage);
		Block -= blockedByDoll;

		var remainingAfterBlock = damage - blockedByDoll;
		var damageToDoll = Math.Min(Hp, remainingAfterBlock);
		Hp -= damageToDoll;

		var overflowDamage = remainingAfterBlock - damageToDoll;
		return new DollDamageResolution(damageToDoll, blockedByDoll, overflowDamage, Hp <= 0);
	}

	public virtual DollHealResolution Heal(int amount)
	{
		var healAmount = Math.Max(0, amount);
		if (healAmount <= 0)
		{
			return new DollHealResolution(0, 0);
		}

		var missingHp = Math.Max(0, MaxHp - Hp);
		var healed = Math.Min(missingHp, healAmount);
		Hp += healed;
		return new DollHealResolution(healed, healAmount - healed);
	}

	public virtual int IncreaseMaxHp(int amount, bool healToMatch)
	{
		var maxHpIncrease = Math.Max(0, amount);
		if (maxHpIncrease <= 0)
		{
			return 0;
		}

		MaxHp += maxHpIncrease;
		if (healToMatch)
		{
			Hp = Math.Min(MaxHp, Hp + maxHpIncrease);
		}

		return maxHpIncrease;
	}

	public virtual int LoseAllBlock()
	{
		var blockLost = Block;
		Block = 0;
		return blockLost;
	}

	public Sts2Doll MakeStatEquivalentCopy()
	{
		var copy = CreateBaseCopy();
		copy.MaxHp = MaxHp;
		copy.Hp = Math.Min(copy.MaxHp, Hp);
		copy.Block = Block;
		return copy;
	}

	protected virtual void OnBlockGained(int amount)
	{
	}

	public virtual void OnSpawn(int slotIndex)
	{
	}

	public virtual void OnRecycled(int slotIndex)
	{
	}

	public virtual void OnDestroyed(int slotIndex)
	{
	}

	public virtual void OnRemoved(int slotIndex, DollRemovalReason reason)
	{
	}

	public virtual void OnStartOfTurn()
	{
	}

	public virtual void OnEndOfTurn()
	{
	}

	public virtual void PostOtherDollSpawn(Sts2Doll otherDoll, int otherSlotIndex)
	{
	}

	public virtual void PostOtherDollAct(Sts2Doll otherDoll, int otherSlotIndex)
	{
	}

	public virtual void PostOtherDollRemoved(string otherDollId, int otherSlotIndex, DollRemovalReason reason)
	{
	}

	protected abstract Sts2Doll CreateBaseCopy();
	public abstract Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard);
}

internal sealed class EmptyDollSlot() : Sts2Doll(string.Empty, 0)
{
	public override bool IsEmptySlot => true;

	protected override Sts2Doll CreateBaseCopy()
	{
		return new EmptyDollSlot();
	}

	public override Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		return Task.CompletedTask;
	}
}

internal sealed class ShanghaiDoll() : Sts2Doll(DollState.ShanghaiDollId, 4)
{
	private const decimal ActDamage = 4m;

	protected override Sts2Doll CreateBaseCopy()
	{
		return new ShanghaiDoll();
	}

	public override async Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		DollState.RegisterCombatContext(sourceCard.CombatState);
		var target = DollState.FindLowestHealthOpponent();
		var totalDamage = ActDamage + DollState.GetCallToDollsBonus(target);
		dynamic cmd = DamageCmd.Attack(totalDamage).FromCard(sourceCard);
		if (target != null)
		{
			cmd = cmd.Targeting((dynamic)target);
		}
		else if (sourceCard.CombatState != null)
		{
			cmd.TargetingRandomOpponents(sourceCard.CombatState);
		}

		await cmd.Execute(choiceContext);
	}
}

internal sealed class FranceDoll() : Sts2Doll(DollState.FranceDollId, 10)
{
	private const int ActBlock = 5;

	protected override Sts2Doll CreateBaseCopy()
	{
		return new FranceDoll();
	}

	public override Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		GainBlock(ActBlock);
		return Task.CompletedTask;
	}

	public override void OnStartOfTurn()
	{
		LoseAllBlock();
		var playerBlock = DollState.GetCurrentPlayerBlockValue();
		if (playerBlock > 0)
		{
			GainBlock(playerBlock);
		}
	}
}

internal sealed class NetherlandsDoll() : Sts2Doll(DollState.NetherlandsDollId, 3)
{
	private int _strengthAmount = 1;
	private int _dexterityAmount = 1;
	private bool _grantDexterityNext;

	protected override Sts2Doll CreateBaseCopy()
	{
		return new NetherlandsDoll
		{
			_strengthAmount = _strengthAmount,
			_dexterityAmount = _dexterityAmount,
			_grantDexterityNext = _grantDexterityNext
		};
	}

	public override Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		if (_grantDexterityNext)
		{
			DollState.TryAdjustPlayerDexterity(_dexterityAmount);
			_dexterityAmount += 1;
		}
		else
		{
			DollState.TryAdjustPlayerStrength(_strengthAmount);
			_strengthAmount += 1;
		}

		_grantDexterityNext = !_grantDexterityNext;
		return Task.CompletedTask;
	}

	public override void OnSpawn(int slotIndex)
	{
		DollState.TryAdjustPlayerStrength(_strengthAmount);
		DollState.TryAdjustPlayerDexterity(_dexterityAmount);
	}

	public override void OnRecycled(int slotIndex)
	{
		RemoveBuffs();
	}

	public override void OnDestroyed(int slotIndex)
	{
		RemoveBuffs();
	}

	public override void OnRemoved(int slotIndex, DollRemovalReason reason)
	{
		RemoveBuffs();
	}

	private void RemoveBuffs()
	{
		DollState.TryAdjustPlayerStrength(-_strengthAmount);
		DollState.TryAdjustPlayerDexterity(-_dexterityAmount);
	}
}

internal sealed class KyotoDoll() : Sts2Doll(DollState.KyotoDollId, 8)
{
	protected override Sts2Doll CreateBaseCopy()
	{
		return new KyotoDoll();
	}

	public override async Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		var healAmount = Math.Max(1, MaxHp / 2);
		var resolution = Heal(healAmount);
		if (resolution.Overflow > 0)
		{
			await DollState.AttackAllOpponents(choiceContext, sourceCard, resolution.Overflow);
		}
	}
}

internal sealed class HouraiDoll() : Sts2Doll(DollState.HouraiDollId, 4)
{
	private const int RecycleEnergy = 1;

	protected override Sts2Doll CreateBaseCopy()
	{
		return new HouraiDoll();
	}

	public override async Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		await DollState.DrawCards(choiceContext, sourceCard, 1);
	}

	public override void OnRecycled(int slotIndex)
	{
		DollState.TryGainPlayerEnergy(RecycleEnergy);
	}
}

// Su_san doll: Attacks the strongest enemy (most HP)
internal sealed class SusanDoll() : Sts2Doll("Susan", 6)
{
	protected override Sts2Doll CreateBaseCopy() => new SusanDoll();

	public override async Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		await DollState.AttackAllOpponents(choiceContext, sourceCard, 4);
	}
}

// Susan Replica: Copy of Susan, same stats
internal sealed class SusanReplicaDoll() : Sts2Doll("SusanReplica", 5)
{
	protected override Sts2Doll CreateBaseCopy() => new SusanReplicaDoll();

	public override async Task Act(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		await DollState.AttackAllOpponents(choiceContext, sourceCard, 3);
	}
}

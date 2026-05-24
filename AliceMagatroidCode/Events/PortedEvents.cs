using BaseLib.Abstracts;
using AliceMagatroid_Mod.Dolls;
using AliceMagatroid_Mod.Patch;
using AliceMagatroid_Mod.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Events;

public class PuppeteersHouse : CustomEventModel
{
	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new[]
		{
			new EventOption(this, OnKnock, "PUPPETEERS_HOUSE.pages.INITIAL.options.KNOCK"),
		};
	}

	private async Task OnKnock()
	{
		SetEventState(L10NLookup("PUPPETEERS_HOUSE.pages.CONTINUE.description"), new[]
		{
			new EventOption(this, OnContinue, "PUPPETEERS_HOUSE.pages.CONTINUE.options.CONTINUE"),
		});
		await Task.CompletedTask;
	}

	private async Task OnContinue()
	{
		var options = new List<EventOption>();

		var hasShanghai = Owner != null && HasRelic<ShanghaiDollRelic>();
		if (hasShanghai)
		{
			options.Add(new EventOption(this, OnReturnDoll, "PUPPETEERS_HOUSE.pages.CHOICE.options.RETURN_DOLL")
				.WithRelic<ShanghaiDollRelic>(Owner));
		}

		options.Add(new EventOption(this, OnRefuse, "PUPPETEERS_HOUSE.pages.CHOICE.options.REFUSE"));

		SetEventState(L10NLookup("PUPPETEERS_HOUSE.pages.CHOICE.description"), options);
		await Task.CompletedTask;
	}

	private async Task OnReturnDoll()
	{
		BonusScorePatch.MarkShanghaiReturned();
		SetEventState(L10NLookup("PUPPETEERS_HOUSE.pages.RETURN.description"), new[]
		{
			new EventOption(this, OnCardReward, "PUPPETEERS_HOUSE.pages.RETURN.options.CARD_REWARD"),
			new EventOption(this, OnBlackTea, "PUPPETEERS_HOUSE.pages.RETURN.options.BLACK_TEA")
				.WithRelic<BlackTeaRelic>(Owner),
		});
		await Task.CompletedTask;
	}

	private async Task OnCardReward()
	{
		SetEventFinished(L10NLookup("PUPPETEERS_HOUSE.pages.CARD_REWARD.description"));
		await Task.CompletedTask;
	}

	private async Task OnBlackTea()
	{
		if (Owner != null)
		{
			await MegaCrit.Sts2.Core.Commands.RelicCmd.Obtain<BlackTeaRelic>(Owner);
		}
		SetEventFinished(L10NLookup("PUPPETEERS_HOUSE.pages.TEA.description"));
	}

	private async Task OnRefuse()
	{
		SetEventState(L10NLookup("PUPPETEERS_HOUSE.pages.REFUSE.description"), new[]
		{
			new EventOption(this, OnLeave, "PUPPETEERS_HOUSE.pages.REFUSE.options.BYE"),
		});
		await Task.CompletedTask;
	}

	private async Task OnLeave()
	{
		SetEventFinished(L10NLookup("PUPPETEERS_HOUSE.pages.LEAVE.description"));
		await Task.CompletedTask;
	}

	private bool HasRelic<T>() where T : RelicModel
	{
		if (Owner == null) return false;
		foreach (var relic in Owner.Relics)
		{
			if (relic is T) return true;
		}
		return false;
	}
}

public class LilyOfTheValleyFlowerField : CustomEventModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new MaxHpVar("HpGain", 10),
		new HealVar("HpLoss", 7),
	};

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new[]
		{
			new EventOption(this, OnEnter, "LILY_OF_THE_VALLEY.pages.INITIAL.options.ENTER"),
			new EventOption(this, OnLeaveEarly, "LILY_OF_THE_VALLEY.pages.INITIAL.options.LEAVE"),
		};
	}

	private async Task OnEnter()
	{
		var options = new List<EventOption>
		{
			new EventOption(this, OnPickFlowers, "LILY_OF_THE_VALLEY.pages.CHOICE.options.PICK_FLOWERS"),
		};

		var hpLoss = 7;
		var canHelp = Owner != null && Owner.Creature.CurrentHp > hpLoss;
		if (canHelp)
		{
			options.Add(new EventOption(this, OnHelpSusan, "LILY_OF_THE_VALLEY.pages.CHOICE.options.HELP_SUSAN"));
		}
		else
		{
			options.Add(new EventOption(this, null, "LILY_OF_THE_VALLEY.pages.CHOICE.options.HELP_SUSAN"));
		}

		SetEventState(L10NLookup("LILY_OF_THE_VALLEY.pages.CHOICE.description"), options);
		await Task.CompletedTask;
	}

	private async Task OnPickFlowers()
	{
		if (Owner != null)
		{
			await MegaCrit.Sts2.Core.Commands.CreatureCmd.GainMaxHp(Owner.Creature, 10);
			await MegaCrit.Sts2.Core.Commands.CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
		}
		SetEventFinished(L10NLookup("LILY_OF_THE_VALLEY.pages.FLOWERS.description"));
	}

	private async Task OnHelpSusan()
	{
		if (Owner != null)
		{
			await MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(
				default, Owner.Creature, 7,
				MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null, null);
		}
		SetEventFinished(L10NLookup("LILY_OF_THE_VALLEY.pages.SUSAN.description"));
	}

	private async Task OnLeaveEarly()
	{
		SetEventFinished(L10NLookup("LILY_OF_THE_VALLEY.pages.LEAVE.description"));
		await Task.CompletedTask;
	}
}

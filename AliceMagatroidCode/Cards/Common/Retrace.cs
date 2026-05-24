using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using AliceMagatroid_Mod.Dolls;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Cards;

public class Retrace() : AliceCard(0,
	CardType.Skill, CardRarity.Rare,
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
		}
	}

	protected override void OnUpgrade()
	{
		_drawAmount += 1;
	}
}

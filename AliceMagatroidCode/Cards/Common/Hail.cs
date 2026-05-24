using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Cards;

public class Hail() : AliceCard(0,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(4, ValueProp.Move),
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
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}

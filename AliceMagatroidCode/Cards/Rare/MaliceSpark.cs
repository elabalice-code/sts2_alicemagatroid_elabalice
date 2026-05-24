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

public class MaliceSpark() : AliceCard(1,
	CardType.Attack, CardRarity.Rare,
	TargetType.AnyEnemy)
{
	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new DamageVar(12, ValueProp.Move),
		new CardsVar("Cards", 2)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay.Target).Execute(choiceContext);
		await CommonActions.Draw(this, choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
	}
}

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

public class DollPlacement() : AliceCard(1,
	CardType.Attack, CardRarity.Basic,
	TargetType.AllEnemies)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		TriggerAttackCardThreadTension();
		await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

		var selected = DollState.SelectedSlotIndex;
		if (selected >= 0 && DollState.IsSlotEmpty(selected))
			await DollState.PlaceDollAt(choiceContext, this, DollState.ShanghaiDollId, selected);
		else
			await DollState.PlaceShanghai(choiceContext, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}

using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using AliceMagatroid_Mod.Extensions;
using AliceMagatroid_Mod.Cards;
using AliceMagatroid_Mod.Relics;
using System;

namespace AliceMagatroid_Mod.Character;

public class AliceMagatroid : PlaceholderCharacterModel
{
	public const string CharacterId = "AliceMagatroid";

	public override string PlaceholderID => "necrobinder";

	public static readonly Color Color = new Color("c4278a");

	public override Color NameColor => Color;
	public override CharacterGender Gender => CharacterGender.Feminine;
	public override int StartingHp => 77;

	public override IEnumerable<CardModel> StartingDeck => [
		ModelDb.Card<Throw>(),
		ModelDb.Card<Throw>(),
		ModelDb.Card<Throw>(),
		ModelDb.Card<Throw>(),
		ModelDb.Card<ProtectiveSpell>(),
		ModelDb.Card<ProtectiveSpell>(),
		ModelDb.Card<ProtectiveSpell>(),
		ModelDb.Card<ProtectiveSpell>(),
		ModelDb.Card<DollPlacement>(),
		ModelDb.Card<Chant>()
	];

	public override IReadOnlyList<RelicModel> StartingRelics => new List<RelicModel> { ModelDb.Relic<AlicesGrimoire>() }.AsReadOnly();

	public override CardPoolModel CardPool => ModelDb.CardPool<AliceCardPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<AliceRelicPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<SharedPotionPool>();

	/*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
		override all the other methods that define those assets.
		These are just some of the simplest assets, given some placeholders to differentiate your character with.
		You don't have to, but you're suggested to rename these images. */
	public override string CustomVisualPath => "res://AliceMagatroid/Scenes/AliceVisual.tscn";
	public override string CustomCharacterSelectBg => "res://scenes/screens/char_select/char_select_bg_necrobinder.tscn";
	public override string CustomRestSiteAnimPath => "res://AliceMagatroid/Scenes/AliceRestSite.tscn";
	public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
	public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
	public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
	public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}

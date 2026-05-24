using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using AliceMagatroid_Mod.Dolls;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using AliceMagatroid_Mod.Character;
using AliceMagatroid_Mod.Extensions;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AliceMagatroid_Mod.Cards;

[Pool(typeof(AliceCardPool))]
public abstract class AliceCard(int cost, CardType type, CardRarity rarity, TargetType target) :
	CustomCardModel(cost, type, rarity, target)
{
	internal virtual bool AllowsForbiddenMagic => true;

	private string ResolvePortraitPath()
	{
		var fileName = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";
		var directPath = fileName.CardImagePath();
		if (ResourceLoader.Exists(directPath))
		{
			return directPath;
		}

		return "card.png".CardImagePath();
	}

	//Image size:
	//Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
	//Full art: 606x852
	public override string CustomPortraitPath
	{
		get
		{
			var path = ResolvePortraitPath();
			Log.Info(">>>[AliceMagatroidMod]CardPath=" + path, 2);
			return path;
		}
	}

	//Smaller variants of card images for efficiency:
	//Smaller variant of fullart: 250x350
	//Smaller variant of normalart: 250x190

	//Uses card_portraits/card_name.png as image path. These should be smaller images.
	public override string PortraitPath
	{
		get
		{
			var path = ResolvePortraitPath();
			Log.Info(">>>[AliceMagatroidMod]CardPath=" + path, 2);
			return path;
		}
	}

	//Optional and I'm not sure it's functional yet.
	public override string BetaPortraitPath
	{
		get
		{
			return PortraitPath;
		}
	}

	protected static void TriggerAttackCardThreadTension()
	{
		DollVisuals.RequestThreadTension();
	}

	protected void RegisterDollCombatContext(PlayerChoiceContext? choiceContext = null)
	{
		DollState.RegisterCombatContext(CombatState);
		if (choiceContext != null)
		{
			DollState.NotifyAliceCardPlayed(choiceContext, this, Id.Entry);
		}
	}

	protected int ResolveXCostAmount(CardPlay cardPlay, int fallback = -1)
	{
		var resolved = ReadObjectNumeric(cardPlay,
			"EnergyOnUse",
			"EnergySpent",
			"SpentEnergy",
			"XCost",
			"PlayCost",
			"CostPaid",
			"Energy");
		if (resolved > 0)
		{
			return resolved;
		}

		var currentEnergy = DollState.GetCurrentPlayerEnergyValue();
		if (currentEnergy > 0)
		{
			return currentEnergy;
		}

		return Math.Max(0, fallback);
	}

	protected async Task ApplyBlockToAliceOrSelectedDoll(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		RegisterDollCombatContext(choiceContext);
		if (!DollState.CanGainBlockFromCards())
		{
			return;
		}

		var blockAmount = ReadCardNumeric("Block", "CurrentBlock", "_block");
		if (DollState.TryApplyBlockToSelectedTarget(this, blockAmount))
		{
			return;
		}

		await CommonActions.CardBlock(this, cardPlay);
	}

	protected async Task ApplyCardBlockIfAllowed(CardPlay cardPlay)
	{
		if (!DollState.CanGainBlockFromCards())
		{
			return;
		}

		await CommonActions.CardBlock(this, cardPlay);
	}

	protected void TrySetCardBaseCost(int cost)
	{
		foreach (var memberName in new[] { "Cost", "BaseCost", "CurrentCost", "_cost", "_baseCost", "_currentCost" })
		{
			var property = GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (property != null && property.CanRead && property.CanWrite)
			{
				var currentValue = property.GetValue(this);
				if (currentValue == null)
				{
					continue;
				}

				property.SetValue(this, ConvertToNumeric(property.PropertyType, cost));
			}

			var field = GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (field != null)
			{
				var currentValue = field.GetValue(this);
				if (currentValue == null)
				{
					continue;
				}

				field.SetValue(this, ConvertToNumeric(field.FieldType, cost));
			}
		}
	}

	protected int ReadCardNumeric(params string[] memberNames)
	{
		foreach (var memberName in memberNames)
		{
			var property = GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (property?.GetValue(this) is { } propertyValue)
			{
				return Convert.ToInt32(Math.Round(Convert.ToDecimal(propertyValue)));
			}

			var field = GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (field?.GetValue(this) is { } fieldValue)
			{
				return Convert.ToInt32(Math.Round(Convert.ToDecimal(fieldValue)));
			}
		}

		return 0;
	}

	private static int ReadObjectNumeric(object? instance, params string[] memberNames)
	{
		if (instance == null)
		{
			return 0;
		}

		foreach (var memberName in memberNames)
		{
			var property = instance.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (property?.GetValue(instance) is { } propertyValue)
			{
				return Convert.ToInt32(Math.Round(Convert.ToDecimal(propertyValue)));
			}

			var field = instance.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (field?.GetValue(instance) is { } fieldValue)
			{
				return Convert.ToInt32(Math.Round(Convert.ToDecimal(fieldValue)));
			}
		}

		return 0;
	}

	private static object ConvertToNumeric(Type numericType, int value)
	{
		if (numericType == typeof(int))
		{
			return value;
		}

		if (numericType == typeof(float))
		{
			return (float)value;
		}

		if (numericType == typeof(double))
		{
			return (double)value;
		}

		if (numericType == typeof(decimal))
		{
			return (decimal)value;
		}

		if (numericType == typeof(long))
		{
			return (long)value;
		}

		return Convert.ChangeType(value, numericType);
	}
}

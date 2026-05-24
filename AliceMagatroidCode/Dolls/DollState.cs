using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using AliceMagatroid_Mod.Cards;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace AliceMagatroid_Mod.Dolls;

public readonly record struct DollSnapshot(int SlotIndex, string Id, int Hp, int MaxHp, int Block);
public readonly record struct DollIntentPreview(int SlotIndex, int TotalDamage, int OverflowDamage);
public readonly record struct ResolutionPopup(int? SlotIndex, int Damage, int Blocked);
internal readonly record struct EnemyTauntState(int SlotIndex, int RemainingTurns);

public static class DollState
{
	public const int MaxSlots = 7;
	public const string ShanghaiDollId = "Shanghai";
	public const string FranceDollId = "France";
	public const string NetherlandsDollId = "Netherlands";
	public const string KyotoDollId = "Kyoto";
	public const string HouraiDollId = "Hourai";

	private static readonly string[] _slots = new string[MaxSlots];
	private static readonly Sts2Doll?[] _dolls = new Sts2Doll?[MaxSlots];
	private static readonly DollSnapshot?[] _snapshots = new DollSnapshot?[MaxSlots];
	private static readonly Dictionary<object, int> _enemyDamageTargets = new(ReferenceEqualityComparer.Instance);
	private static readonly Dictionary<object, EnemyTauntState> _enemyTaunts = new(ReferenceEqualityComparer.Instance);
	private static readonly HashSet<string> _placedThisCombat = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> _recycledOrDestroyedThisCombat = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, int> _cardPlayCounts = new(StringComparer.OrdinalIgnoreCase);

	[ThreadStatic]
	private static bool _handlingDamageRedirect;
	[ThreadStatic]
	private static object? _pendingDamageAttacker;

	private static object? _activeCombatState;
	private static object? _activePlayer;
	private static int _dollActivationCount;
	private static int _dollWarMaxHpBonus;
	private static int _dollAmbushCount;
	private static int _dollJudgePercent;
	private static int _artfulChanterActs;
	private static int _forbiddenMagicActs;
	private static object? _callToDollsTarget;
	private static int _callToDollsBonus;
	private static int _maidensBunrakuBlock;
	private static int _luminousShanghaiActs;
	private static int _semiAutomatonActs;
	private static int _dollOrchestraDraw;
	private static int _magicConduitAmount;
	private static int _futureBunrakuSlotIndex = -1;
	private static bool _roundTableNoCardBlock;
	private static bool _playerTurnActive;
	private static int _turnNumber;
	private static int _colorContactsTurns;
	private static bool _magicPotionActive;
	private static int _sacrificialDollBlockAmount;
	private static bool _swordOfLightActive;
	private static List<string>? _bookmarkRememberedIds;

	[ThreadStatic]
	private static bool _handlingFutureBunrakuPlay;

	private static int _geyserThreshold;
	private static int _devilryLightAmount;
	private static int _magicianRayAmount;
	private static int _unlockMysticCount;
	private static int _princessFormCount;
	private static int _seaOfSubconsciousnessCount;
	private static int _usokaeCount;
	private static int _playerStrengthBonus;
	private static bool _upgradeAllDrawn;
	private static TaskCompletionSource<int>? _slotSelectionTcs;
	private static bool _selectionEmptyOnly;
	private static bool _selectionIncludeAlice;

	private static Node? _pendingCardPlayNode;
	private static bool _placementTargeting;
	private static Vector2? _frozenCardPosition;
	private static DollIntentPreview[] _cachedIntentPreviews = [];
	private static bool _isConfirmingPlacement;

	public static bool IsPlacementTargeting => _placementTargeting;
	public static bool IsConfirmingPlacement => _isConfirmingPlacement;

	public static int DollCount { get; private set; }

	public static int EmptySlots => 7 - DollCount;

	public static int SelectedSlotIndex { get; private set; }

	public static DollTargetContext SelectedTarget => GetSelectedTarget();

	public static IReadOnlyList<string> Slots => _slots;

	public static IReadOnlyList<DollSnapshot?> Snapshots => _snapshots;

	public static bool IsInSelectionMode => _slotSelectionTcs != null;

	public static bool IsAliceValidSelection => _selectionIncludeAlice;

	public static event Action<int> DollCountChanged;
	public static event Action<IReadOnlyList<string>> SlotsChanged;
	public static event Action<IReadOnlyList<DollSnapshot?>> StatsChanged;
	public static event Action<int?> SelectionChanged;
	public static event Action<ResolutionPopup> ResolutionPopupRequested;
	public static event Action<bool, bool>? SelectionModeChanged;
	public static event Action<bool>? PlacementTargetingChanged;
	public static event Action<IReadOnlyList<DollIntentPreview>>? IntentPreviewsChanged;

	static DollState()
	{
		_slots = new string[7];
		_dolls = new Sts2Doll[7];
		_snapshots = new DollSnapshot?[7];
		_enemyDamageTargets = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
		_enemyTaunts = new Dictionary<object, EnemyTauntState>(ReferenceEqualityComparer.Instance);
		_placedThisCombat = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_recycledOrDestroyedThisCombat = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_cardPlayCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		_futureBunrakuSlotIndex = -1;
		SelectedSlotIndex = -1;
		DollState.DollCountChanged = delegate { };
		DollState.SlotsChanged = delegate { };
		DollState.StatsChanged = delegate { };
		DollState.SelectionChanged = delegate { };
		DollState.ResolutionPopupRequested = delegate { };
		DollState.SelectionModeChanged = delegate { };
		DollState.PlacementTargetingChanged = delegate { };
		FillWithEmptySlots();
	}

	public static void RegisterCombatContext(object? combatState)
	{
		if (combatState != null)
		{
			_activeCombatState = combatState;
			_activePlayer = TryReadMember(combatState, "LocalPlayer", "Player", "_localPlayer", "_player");
			RefreshEnemyDamageTargets();
		}
	}

	public static void RegisterCombatContextFrom(object? owner)
	{
		if (owner != null)
		{
			RegisterCombatContext(TryReadMember(owner, "CombatState", "_combatState"));
		}
	}

	public static void SelectAlice()
	{
		SelectTarget(DollTargetContext.Alice);
	}

	public static void SelectSlot(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < 7)
		{
			SelectTarget(GetSlotTarget(slotIndex));
		}
	}

	public static void SelectTarget(DollTargetContext target)
	{
		if (target.IsAlice || !target.HasSlot)
		{
			SetSelection(-1);
		}
		else if (target.SlotIndex >= 0 && target.SlotIndex < 7)
		{
			SetSelection(target.SlotIndex);
		}
	}

	public static DollTargetContext GetSelectedTarget()
	{
		return (SelectedSlotIndex < 0) ? DollTargetContext.Alice : GetSlotTarget(SelectedSlotIndex);
	}

	public static DollTargetContext GetSlotTarget(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= 7)
		{
			return DollTargetContext.None;
		}
		return IsSlotEmpty(slotIndex) ? DollTargetContext.ForSlot(DollTargetKind.EmptySlot, slotIndex) : DollTargetContext.ForSlot(DollTargetKind.Doll, slotIndex);
	}

	public static int SpawnShanghai(int amount = 1)
	{
		return SpawnDoll("Shanghai", amount, randomPlacement: false);
	}

	public static int SpawnShanghaiRandom(int amount = 1)
	{
		return SpawnDoll("Shanghai", amount, randomPlacement: true);
	}

	public static async Task<int> PlaceShanghai(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		return await PlaceDoll(choiceContext, sourceCard, "Shanghai");
	}

	public static int Spawn(string dollId, int amount = 1, bool randomPlacement = false)
	{
		return SpawnDoll(dollId, amount, randomPlacement);
	}

	public static bool SpawnAt(string dollId, int slotIndex)
	{
		return SpawnInternal(dollId, slotIndex);
	}

	public static async Task<int> PlaceDoll(PlayerChoiceContext choiceContext, CardModel sourceCard, string dollId)
	{
		RegisterCombatContext(sourceCard.CombatState);
		int slotIndex = ResolveManualPlacementSlot();
		if (!SpawnInternal(dollId, slotIndex))
		{
			return DollCount;
		}
		await ActSlot(choiceContext, sourceCard, slotIndex);
		return DollCount;
	}

	public static int Recycle(int amount = 1)
	{
		if (amount <= 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = 6;
		while (num2 >= 0 && num < amount)
		{
			if (!IsSlotEmpty(num2))
			{
				RemoveDoll(num2, DollRemovalReason.Recycle);
				num++;
			}
			num2--;
		}
		if (num > 0)
		{
			RefreshSnapshots();
			RecountAndNotify($"Recycle -{num}");
		}
		return num;
	}

	public static int RecycleAll()
	{
		int dollCount = DollCount;
		for (int i = 0; i < _dolls.Length; i++)
		{
			RemoveDoll(i, DollRemovalReason.Clear);
		}
		_enemyDamageTargets.Clear();
		_enemyTaunts.Clear();
		_activeCombatState = null;
		_activePlayer = null;
		_dollActivationCount = 0;
		_dollWarMaxHpBonus = 0;
		_dollAmbushCount = 0;
		_dollJudgePercent = 0;
		_artfulChanterActs = 0;
		_forbiddenMagicActs = 0;
		_callToDollsTarget = null;
		_callToDollsBonus = 0;
		_maidensBunrakuBlock = 0;
		_dollOrchestraDraw = 0;
		_luminousShanghaiActs = 0;
		_semiAutomatonActs = 0;
		_magicConduitAmount = 0;
		_geyserThreshold = 0;
		_devilryLightAmount = 0;
		_magicianRayAmount = 0;
		_unlockMysticCount = 0;
		_princessFormCount = 0;
		_seaOfSubconsciousnessCount = 0;
		_usokaeCount = 0;
		_playerStrengthBonus = 0;
		_futureBunrakuSlotIndex = -1;
		_roundTableNoCardBlock = false;
		_cachedIntentPreviews = [];
		IntentPreviewsChanged?.Invoke(_cachedIntentPreviews);
		_playerTurnActive = false;
		_turnNumber = 0;
		_colorContactsTurns = 0;
		_magicPotionActive = false;
		_sacrificialDollBlockAmount = 0;
		_swordOfLightActive = false;
		_bookmarkRememberedIds = null;
		_placedThisCombat.Clear();
		_recycledOrDestroyedThisCombat.Clear();
		_cardPlayCounts.Clear();
		SetSelection(-1, force: true);
		RefreshSnapshots();
		RecountAndNotify("RecycleAll");
		return dollCount;
	}

	public static bool RecycleAt(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= 7 || IsSlotEmpty(slotIndex))
		{
			return false;
		}
		RemoveDoll(slotIndex, DollRemovalReason.Recycle);
		RefreshSnapshots();
		RecountAndNotify($"Recycle slot {slotIndex}");
		return true;
	}

	public static void OnStartOfTurn()
	{
		_turnNumber++;
		foreach (Sts2Doll item in EnumerateDolls())
		{
			item.OnStartOfTurn();
		}
		if (_colorContactsTurns > 0 && _turnNumber <= _colorContactsTurns)
		{
			TryGainPlayerEnergy(1);
		}
		if (_magicPotionActive && _turnNumber == 2)
		{
			TryGainPlayerEnergy(2);
		}
		if (_swordOfLightActive)
		{
			TryAdjustPlayerStrength(1);
		}
		if (_devilryLightAmount > 0)
		{
			foreach (object aliveOpponent in GetAliveOpponents())
			{
				TryAdjustNamedStatusAmount(aliveOpponent, _devilryLightAmount, "Weak");
			}
			_devilryLightAmount = 0;
		}
		if (_magicianRayAmount > 0)
		{
			TryAdjustPlayerStrength(_magicianRayAmount);
		}
		int num = EnumerateDolls().Count((Sts2Doll d) => d.Id == "Susan" || d.Id == "SusanReplica");
		if (num > 0)
		{
			foreach (object aliveOpponent2 in GetAliveOpponents())
			{
				int num2 = TryReadInt(aliveOpponent2, "Poison", "PoisonAmount", "_poison", "CurrentPoison");
				if (num2 > 0)
				{
					TryChangeNumericMember(aliveOpponent2, -(num2 * num), "CurrentHp", "Hp", "_currentHp");
				}
			}
		}
		RefreshSnapshots();
	}

	public static void TryHandlePlayerTurnStart(object? owner, string hookName)
	{
		RegisterCombatContextFrom(owner);
		if (IsLikelyPlayerLifecycleOwner(owner))
		{
			if (_playerTurnActive)
			{
				Log.Info("[AliceMagatroid_Mod] Skip duplicate start hook " + hookName, 2);
				return;
			}
			_playerTurnActive = true;
			Log.Info("[AliceMagatroid_Mod] Player turn start via " + hookName, 2);
			OnStartOfTurn();
			RefreshIntentPreviews();
		}
	}

	public static void OnEndOfTurn()
	{
		foreach (Sts2Doll item in EnumerateDolls())
		{
			item.OnEndOfTurn();
		}
		if (_futureBunrakuSlotIndex >= 0)
		{
			RecycleAt(_futureBunrakuSlotIndex);
			_futureBunrakuSlotIndex = -1;
		}
		if (_sacrificialDollBlockAmount > 0 && HasEmptySlotTakingDamage())
		{
			TryGrantBlockToPlayer(_sacrificialDollBlockAmount);
		}
		if (_swordOfLightActive)
		{
			TryAdjustPlayerStrength(-1);
		}
		if (_magicianRayAmount > 0)
		{
			TryAdjustPlayerStrength(-_magicianRayAmount);
		}
		_forbiddenMagicActs = 0;
		_roundTableNoCardBlock = false;
		_callToDollsTarget = null;
		_callToDollsBonus = 0;
		AdvanceEnemyTaunts();
		if (_dollActivationCount > 0 && _activeCombatState != null)
		{
			ActivateLeadingDollsAtEndOfTurn(_dollActivationCount);
		}
		RefreshSnapshots();
	}

	public static void TryHandlePlayerTurnEnd(object? owner, string hookName)
	{
		RegisterCombatContextFrom(owner);
		if (IsLikelyPlayerLifecycleOwner(owner))
		{
			if (!_playerTurnActive)
			{
				Log.Info("[AliceMagatroid_Mod] Skip duplicate end hook " + hookName, 2);
				return;
			}
			_playerTurnActive = false;
			Log.Info("[AliceMagatroid_Mod] Player turn end via " + hookName, 2);
			OnEndOfTurn();
		}
	}

	public static async Task<int> ActAll(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		RegisterCombatContext(sourceCard.CombatState);
		int acted = 0;
		for (int i = 0; i < _dolls.Length; i++)
		{
			if (!IsSlotEmpty(i))
			{
				await ActSlot(choiceContext, sourceCard, i);
				acted++;
			}
		}
		Log.Info($"[AliceMagatroid_Mod] ActAll x{acted}", 2);
		return acted;
	}

	public static bool SpawnShanghaiAt(int slotIndex)
	{
		return SpawnInternal("Shanghai", slotIndex);
	}

	public static void AddDollActivation(int amount)
	{
		_dollActivationCount = Math.Clamp(_dollActivationCount + Math.Max(0, amount), 0, 7);
	}

	public static void AddDollWarBonus(int amount)
	{
		_dollWarMaxHpBonus += Math.Max(0, amount);
	}

	public static void AddDollAmbush(int amount)
	{
		_dollAmbushCount += Math.Max(0, amount);
	}

	public static void AddDollJudgePercent(int amount)
	{
		_dollJudgePercent += Math.Max(0, amount);
	}

	public static void AddArtfulChanterActs(int amount)
	{
		_artfulChanterActs += Math.Max(0, amount);
	}

	public static void AddForbiddenMagicActs(int amount)
	{
		_forbiddenMagicActs += Math.Max(0, amount);
	}

	public static void AddMaidensBunrakuBlock(int amount)
	{
		_maidensBunrakuBlock += Math.Max(0, amount);
	}

	public static void AddDollOrchestraDraw(int amount)
	{
		_dollOrchestraDraw += Math.Max(0, amount);
	}

	public static void AddLuminousShanghaiActs(int amount)
	{
		_luminousShanghaiActs += Math.Max(0, amount);
	}

	public static void ArmSemiAutomaton(int amount)
	{
		_semiAutomatonActs += Math.Max(0, amount);
	}

	public static void AddMagicConduit(int amount)
	{
		_magicConduitAmount += Math.Max(0, amount);
	}

	public static void AddGeyserThreshold(int threshold)
	{
		_geyserThreshold = Math.Max(0, threshold);
	}

	public static void AddDevilryLight(int amount)
	{
		_devilryLightAmount += Math.Max(0, amount);
	}

	public static void AddMagicianRay(int amount)
	{
		_magicianRayAmount += Math.Max(0, amount);
	}

	public static void AddPlayerStrength(int amount)
	{
		_playerStrengthBonus += Math.Max(0, amount);
	}

	public static void AddUnlockMystic()
	{
		_unlockMysticCount++;
	}

	public static void AddPrincessForm()
	{
		_princessFormCount++;
	}

	public static void AddSeaOfSubconsciousness(int amount)
	{
		_seaOfSubconsciousnessCount += Math.Max(0, amount);
	}

	public static void AddUsokae()
	{
		_usokaeCount++;
	}

	public static void ApplyVulnerableToAllEnemies(int amount)
	{
		foreach (object aliveOpponent in GetAliveOpponents())
		{
			TryAdjustNamedStatusAmount(aliveOpponent, amount, "Vulnerable");
		}
	}

	public static void ApplyWeakAndVulnerableToTargetAndDuplicates(object? target, int amount)
	{
		if (target == null)
		{
			return;
		}
		string text = TryReadString(target, "Name", "Id", "Title");
		foreach (object aliveOpponent in GetAliveOpponents())
		{
			if (aliveOpponent == target || (!string.IsNullOrEmpty(text) && text == TryReadString(aliveOpponent, "Name", "Id", "Title")))
			{
				TryAdjustNamedStatusAmount(aliveOpponent, -amount, "Weak");
				TryAdjustNamedStatusAmount(aliveOpponent, amount, "Vulnerable");
			}
		}
	}

	public static void UpgradeTopCardsInDrawPile(int count)
	{
		object collection = TryReadMember(_activePlayer, "DrawPile", "Draw", "Deck", "_drawPile", "_deck");
		int num = 0;
		foreach (object item in EnumerateObjects(collection))
		{
			if (num >= count)
			{
				break;
			}
			if (TryUpgradeCard(item))
			{
				num++;
			}
		}
	}

	public static async Task MoveAttackFromDrawToHandAndUpgrade(PlayerChoiceContext choiceContext, CardModel sourceCard, int count)
	{
		RegisterCombatContext(sourceCard.CombatState);
		object drawPile = TryReadMember(sourceCard.Owner, "DrawPile", "Draw", "Deck", "_drawPile", "_deck");
		int moved = 0;
		foreach (object card in EnumerateObjects(drawPile).ToList())
		{
			if (moved >= count)
			{
				break;
			}
			CardModel cm = (CardModel)((card is CardModel) ? card : null);
			if (cm != null && !TryReadBool(cm, "IsUpgraded", "Upgraded"))
			{
				string typeStr = TryReadString(cm, "Type", "CardType");
				object typeVal = TryReadMember(cm, "Type", "CardType");
				if ((typeStr != null && typeStr.Contains("Attack")) || (typeVal is int tv && tv == 0))
				{
					TryUpgradeCard(cm);
					TryAddCardToHand(cm);
					moved++;
				}
			}
		}
		await Task.CompletedTask;
	}

	public static async Task AttackDollsOpponents(PlayerChoiceContext choiceContext, CardModel sourceCard, int slotIndex, int damage)
	{
		RegisterCombatContext(sourceCard.CombatState);
		RefreshEnemyDamageTargets();
		foreach (object opponent in GetAliveOpponents())
		{
			if (!_enemyDamageTargets.TryGetValue(opponent, out var mappedSlot) || mappedSlot != slotIndex)
			{
				continue;
			}
			dynamic cmd = DamageCmd.Attack((decimal)damage).FromCard(sourceCard);
			cmd = ((dynamic)cmd).Targeting((dynamic)opponent);
			await ((dynamic)cmd).Execute(choiceContext);
		}
	}

	public static async Task DiscoverFromDrawPile(PlayerChoiceContext choiceContext, CardModel sourceCard, int count)
	{
		RegisterCombatContext(sourceCard.CombatState);
		object drawPile = TryReadMember(sourceCard.Owner, "DrawPile", "Draw", "Deck", "_drawPile", "_deck");
		List<CardModel> cards = EnumerateObjects(drawPile).Take(count).OfType<CardModel>().ToList();
		if (cards.Count == 0)
		{
			await Task.CompletedTask;
			return;
		}
		try
		{
			Type selectCmdType = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("MegaCrit.Sts2.Core.Commands.CardSelectCmd", throwOnError: false)).FirstOrDefault((Type t) => t != null);
			if (selectCmdType == null)
			{
				await Task.CompletedTask;
				return;
			}
			MethodInfo screenMethod = selectCmdType.GetMethod("FromChooseACardScreen", BindingFlags.Static | BindingFlags.Public, null, new Type[4]
			{
				typeof(PlayerChoiceContext),
				typeof(IReadOnlyList<CardModel>),
				typeof(object),
				typeof(bool)
			}, null);
			if (screenMethod == null)
			{
				await Task.CompletedTask;
				return;
			}
			object result = screenMethod.Invoke(null, new object[4] { choiceContext, cards, sourceCard.Owner, true });
			Task<CardModel?> selectTask = result as Task<CardModel>;
			if (selectTask != null)
			{
				CardModel selected = await selectTask;
				if (selected != null)
				{
					TryAddCardToHand(selected);
				}
			}
		}
		catch
		{
		}
		await Task.CompletedTask;
	}

	public static async Task DiscoverPresetCard(PlayerChoiceContext choiceContext, CardModel sourceCard, bool upgraded)
	{
		RegisterCombatContext(sourceCard.CombatState);
		try
		{
			Player owner = sourceCard.Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				CharacterModel character = owner.Character;
				obj = ((character != null) ? character.CardPool : null);
			}
			CardPoolModel cardPool = (CardPoolModel)obj;
			if (cardPool == null)
			{
				await Task.CompletedTask;
				return;
			}
			Type selectCmdType = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("MegaCrit.Sts2.Core.Commands.CardSelectCmd", throwOnError: false)).FirstOrDefault((Type t) => t != null);
			Type factoryType = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("MegaCrit.Sts2.Core.Entities.Cards.CardFactory", throwOnError: false)).FirstOrDefault((Type t) => t != null);
			if (selectCmdType == null || factoryType == null)
			{
				await Task.CompletedTask;
				return;
			}
			MethodInfo getDistinct = factoryType.GetMethod("GetDistinctForCombat", BindingFlags.Static | BindingFlags.Public);
			MethodInfo getUnlocked = ((object)cardPool).GetType().GetMethod("GetUnlockedCards", BindingFlags.Instance | BindingFlags.Public);
			if (getDistinct == null || getUnlocked == null)
			{
				await Task.CompletedTask;
				return;
			}
			object allCards = getUnlocked.Invoke(cardPool, null);
			IReadOnlyList<CardModel> cards = getDistinct.Invoke(null, new object[4]
			{
				sourceCard.Owner,
				allCards,
				3,
				new Random()
			}) as IReadOnlyList<CardModel>;
			if (cards == null || cards.Count == 0)
			{
				await Task.CompletedTask;
				return;
			}
			MethodInfo screenMethod = selectCmdType.GetMethod("FromChooseACardScreen", BindingFlags.Static | BindingFlags.Public, null, new Type[4]
			{
				typeof(PlayerChoiceContext),
				typeof(IReadOnlyList<CardModel>),
				typeof(object),
				typeof(bool)
			}, null);
			if (screenMethod == null)
			{
				await Task.CompletedTask;
				return;
			}
			object result = screenMethod.Invoke(null, new object[4] { choiceContext, cards, sourceCard.Owner, true });
			Task<CardModel?> selectTask = result as Task<CardModel>;
			if (selectTask != null)
			{
				CardModel selected = await selectTask;
				if (selected != null)
				{
					if (upgraded)
					{
						TryUpgradeCard(selected);
					}
					TryAddCardToHand(selected);
				}
			}
		}
		catch
		{
		}
		await Task.CompletedTask;
	}

	public static async Task DiscardCardsAtDollIndices(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		RegisterCombatContext(sourceCard.CombatState);
		object hand = TryReadMember(sourceCard.Owner, "Hand", "HandCards", "_hand", "_handCards");
		if (hand == null)
		{
			await Task.CompletedTask;
			return;
		}
		List<object> handList = EnumerateObjects(hand).ToList();
		CardModel cm = default(CardModel);
		foreach (int slotIndex in GetOccupiedSlotIndices())
		{
			int num;
			if (slotIndex < handList.Count)
			{
				object obj = handList[slotIndex];
				cm = (CardModel)((obj is CardModel) ? obj : null);
				num = ((cm != null) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				TryAddCardToDiscard(cm);
			}
			cm = null;
		}
		await Task.CompletedTask;
	}

	public static bool IsPerihelionModified(CardModel card)
	{
		if (card == null)
		{
			return false;
		}
		try
		{
			object obj = TryReadMember(card, "DynamicVars", "_dynamicVars");
			if (obj == null)
			{
				return false;
			}
			int num = TryReadInt(obj, "DamageBase", "BaseDamage");
			int num2 = TryReadInt(obj, "Damage", "CurrentDamage");
			if (num != num2 && num2 != 0)
			{
				return true;
			}
			int num3 = TryReadInt(obj, "BlockBase", "BaseBlock");
			int num4 = TryReadInt(obj, "Block", "CurrentBlock");
			if (num3 != num4 && num4 != 0)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static int GetMissingEnergyCount()
	{
		int num = TryReadInt(_activePlayer, "MasterEnergy", "MaxEnergy", "EnergyCap");
		int currentPlayerEnergyValue = GetCurrentPlayerEnergyValue();
		return Math.Max(0, num - currentPlayerEnergyValue);
	}

	public static void UpgradeRandomAttackCardInHand(int damageBonus, bool applyRevelationModifier)
	{
		object obj = TryReadMember(_activePlayer, "Hand", "HandCards", "_hand", "_handCards");
		if (obj == null)
		{
			return;
		}
		List<CardModel> list = (from c in EnumerateObjects(obj).OfType<CardModel>()
			where !TryReadBool(c, "IsUpgraded", "Upgraded")
			select c).ToList();
		if (list.Count != 0)
		{
			CardModel val = list[Random.Shared.Next(list.Count)];
			TryUpgradeCard(val);
			if (damageBonus > 0)
			{
				TryChangeNumericMember(TryReadMember(val, "DynamicVars", "_dynamicVars"), damageBonus, "Damage", "BaseDamage");
			}
		}
	}

	public static int GetHandCardCountExcluding(CardModel exclude)
	{
		object obj = TryReadMember(_activePlayer, "Hand", "HandCards", "_hand", "_handCards");
		if (obj == null)
		{
			return 0;
		}
		return EnumerateObjects(obj).OfType<CardModel>().Count((CardModel c) => c != exclude);
	}

	public static async Task ExhaustHandExcept(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		RegisterCombatContext(sourceCard.CombatState);
		object hand = TryReadMember(sourceCard.Owner, "Hand", "HandCards", "_hand", "_handCards");
		if (hand == null)
		{
			await Task.CompletedTask;
			return;
		}
		foreach (CardModel card in (from c in EnumerateObjects(hand).OfType<CardModel>()
			where c != sourceCard
			select c).ToList())
		{
			TrySetBoolMember(card, true, "ExhaustOnNextPlay", "Exhaust", "_exhaust", "PurgeOnUse");
		}
		await Task.CompletedTask;
	}

	public static async Task AddRandomCombatCardToHand(PlayerChoiceContext choiceContext, CardModel sourceCard, bool upgraded, bool zeroCost)
	{
		RegisterCombatContext(sourceCard.CombatState);
		try
		{
			Player owner = sourceCard.Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				CharacterModel character = owner.Character;
				obj = ((character != null) ? character.CardPool : null);
			}
			CardPoolModel cardPool = (CardPoolModel)obj;
			if (cardPool == null)
			{
				await Task.CompletedTask;
				return;
			}
			MethodInfo getUnlocked = ((object)cardPool).GetType().GetMethod("GetUnlockedCards", BindingFlags.Instance | BindingFlags.Public);
			if (getUnlocked == null)
			{
				await Task.CompletedTask;
				return;
			}
			List<CardModel> allCards = (getUnlocked.Invoke(cardPool, null) as IEnumerable<object>)?.OfType<CardModel>().ToList();
			if (allCards == null || allCards.Count == 0)
			{
				await Task.CompletedTask;
				return;
			}
			CardModel selected = allCards[Random.Shared.Next(allCards.Count)];
			if (upgraded)
			{
				TryUpgradeCard(selected);
			}
			if (zeroCost)
			{
				object costObj = ((object)selected).GetType().GetProperty("EnergyCost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(selected);
				costObj?.GetType().GetMethod("SetThisTurnOrUntilPlayed", BindingFlags.Instance | BindingFlags.Public)?.Invoke(costObj, new object[1] { 0 });
			}
			TryAddCardToHand(selected);
		}
		catch
		{
		}
		await Task.CompletedTask;
	}

	public static void RestoreRememberedCards(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		RegisterCombatContext(sourceCard.CombatState);
		if (_bookmarkRememberedIds == null || _bookmarkRememberedIds.Count == 0)
		{
			return;
		}
		string[] array = new string[7] { "DrawPile", "Draw", "Deck", "DiscardPile", "Discard", "_drawPile", "_discardPile" };
		foreach (string text in array)
		{
			object obj = TryReadMember(sourceCard.Owner, text);
			if (obj == null)
			{
				continue;
			}
			foreach (CardModel item in EnumerateObjects(obj).OfType<CardModel>().ToList())
			{
				string text2 = TryReadString(item, "Id", "CardId", "Name");
				if (text2 != null && _bookmarkRememberedIds.Contains(text2))
				{
					TryAddCardToHand(item);
				}
			}
		}
		_bookmarkRememberedIds = null;
	}

	public static void RememberHandCards(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		RegisterCombatContext(sourceCard.CombatState);
		object obj = TryReadMember(sourceCard.Owner, "Hand", "HandCards", "_hand", "_handCards");
		if (obj != null)
		{
			_bookmarkRememberedIds = (from c in EnumerateObjects(obj).OfType<CardModel>()
				where c != sourceCard
				select TryReadString(c, "Id", "CardId", "Name") into id
				where !string.IsNullOrEmpty(id)
				select id).ToList();
		}
	}

	public static async Task<bool> FatalAttack(PlayerChoiceContext choiceContext, CardModel sourceCard, object? target)
	{
		RegisterCombatContext(sourceCard.CombatState);
		if (target == null)
		{
			return false;
		}
		int hpBefore = TryReadInt(target, "CurrentHp", "Hp", "_currentHp");
		dynamic cmd = DamageCmd.Attack(999m).FromCard(sourceCard);
		cmd = ((dynamic)cmd).Targeting((dynamic)target);
		await ((dynamic)cmd).Execute(choiceContext);
		int hpAfter = TryReadInt(target, "CurrentHp", "Hp", "_currentHp");
		return hpAfter <= 0;
	}

	public static void GainExtraTurn()
	{
		if (_activeCombatState != null)
		{
			TryChangeNumericMember(_activeCombatState, 1, "ExtraTurns", "BonusTurns", "_extraTurns");
		}
	}

	public static bool TryHealPlayer(int amount)
	{
		if (_activePlayer == null || amount <= 0)
		{
			return false;
		}
		int num = TryReadInt(_activePlayer, "CurrentHp", "Hp", "_currentHp");
		int num2 = TryReadInt(_activePlayer, "MaxHp", "_maxHp");
		int num3 = Math.Min(amount, Math.Max(0, num2 - num));
		if (num3 <= 0)
		{
			return false;
		}
		return TryChangeNumericMember(_activePlayer, num3, "CurrentHp", "Hp", "_currentHp");
	}

	public static void UpgradeAllDrawnCards()
	{
		_upgradeAllDrawn = true;
	}

	public static void IncreaseMaxHpAndHeal(int amount)
	{
		if (_activePlayer != null && amount > 0)
		{
			TryChangeNumericMember(_activePlayer, amount, "MaxHp", "_maxHp");
			TryChangeNumericMember(_activePlayer, amount, "CurrentHp", "Hp", "_currentHp");
		}
	}

	public static bool HasEmptySlotTakingDamage()
	{
		RefreshEnemyDamageTargets();
		foreach (KeyValuePair<object, int> enemyDamageTarget in _enemyDamageTargets)
		{
			if (IsSlotEmpty(enemyDamageTarget.Value))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGrantBlockToPlayerFromRelic(int amount)
	{
		return TryGrantBlockToPlayer(amount);
	}

	public static void DamageLowestHpEnemy(int damage)
	{
		object obj = FindLowestHealthOpponent();
		if (obj != null)
		{
			TryChangeNumericMember(obj, -damage, "CurrentHp", "Hp", "_currentHp");
		}
	}

	public static string? GetFirstOccupiedDollId()
	{
		foreach (int occupiedSlotIndex in GetOccupiedSlotIndices())
		{
			Sts2Doll dollAt = GetDollAt(occupiedSlotIndex);
			if (dollAt != null)
			{
				return dollAt.Id;
			}
		}
		return null;
	}

	public static void AddSwordOfLight()
	{
		_swordOfLightActive = true;
	}

	public static void AddColorContacts(int turns)
	{
		_colorContactsTurns = turns;
	}

	public static void AddMagicPotion()
	{
		_magicPotionActive = true;
	}

	public static void AddSacrificialDollBlock(int blockAmount)
	{
		_sacrificialDollBlockAmount = blockAmount;
	}

	private static bool TryUpgradeCard(object? card)
	{
		if (card == null)
		{
			return false;
		}
		try
		{
			if (TryReadBool(card, "IsUpgraded", "Upgraded"))
			{
				return false;
			}
			MethodInfo methodInfo = card.GetType().GetMethod("Upgrade", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? card.GetType().GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (methodInfo != null)
			{
				methodInfo.Invoke(card, null);
				return true;
			}
			TrySetBoolMember(card, true, "IsUpgraded", "Upgraded", "_upgraded");
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static async Task<int> ActAroundSlot(PlayerChoiceContext choiceContext, CardModel sourceCard, int centerSlotIndex)
	{
		RegisterCombatContext(sourceCard.CombatState);
		int acted = 0;
		foreach (int slotIndex in new int[3]
		{
			centerSlotIndex - 1,
			centerSlotIndex,
			centerSlotIndex + 1
		}.Distinct())
		{
			if (slotIndex >= 0 && slotIndex < 7 && !IsSlotEmpty(slotIndex))
			{
				await ActSlot(choiceContext, sourceCard, slotIndex);
				acted++;
			}
		}
		return acted;
	}

	public static async Task<int> ActAt(PlayerChoiceContext choiceContext, CardModel sourceCard, int slotIndex, int amount = 1)
	{
		RegisterCombatContext(sourceCard.CombatState);
		if (slotIndex < 0 || slotIndex >= 7 || IsSlotEmpty(slotIndex) || amount <= 0)
		{
			return 0;
		}
		int acted = 0;
		for (int i = 0; i < amount; i++)
		{
			await ActSlot(choiceContext, sourceCard, slotIndex);
			acted++;
		}
		return acted;
	}

	public static async Task<int> ReActRandomShanghai(PlayerChoiceContext choiceContext, CardModel sourceCard, int amount)
	{
		int[] shanghaiSlots = (from index in Enumerable.Range(0, 7)
			where _dolls[index] is ShanghaiDoll
			select index).ToArray();
		if (shanghaiSlots.Length == 0 || amount <= 0)
		{
			return 0;
		}
		int acted = 0;
		for (int i = 0; i < amount; i++)
		{
			int slotIndex = shanghaiSlots[Random.Shared.Next(shanghaiSlots.Length)];
			await ActSlot(choiceContext, sourceCard, slotIndex);
			acted++;
		}
		return acted;
	}

	public static IReadOnlyList<string> GetPresentBaseDollIds()
	{
		return (from doll in EnumerateDolls()
			select doll.Id).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	public static int GetPlacedBaseDollTypeCountThisCombat()
	{
		return _placedThisCombat.Count;
	}

	public static IReadOnlyList<int> GetOccupiedSlotIndices()
	{
		return (from index in Enumerable.Range(0, 7)
			where !IsSlotEmpty(index)
			select index).ToArray();
	}

	public static IReadOnlyList<string> GetMissingBaseDollIds()
	{
		HashSet<string> present = new HashSet<string>(GetPresentBaseDollIds(), StringComparer.OrdinalIgnoreCase);
		return new string[5] { "Shanghai", "France", "Netherlands", "Kyoto", "Hourai" }.Where((string id) => !present.Contains(id)).ToArray();
	}

	internal static Sts2Doll? GetDollAt(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= 7)
		{
			return null;
		}
		Sts2Doll sts2Doll = _dolls[slotIndex];
		return (sts2Doll == null || sts2Doll.IsEmptySlot) ? null : sts2Doll;
	}

	public static bool SpawnCopyAt(int sourceSlotIndex, int destinationSlotIndex)
	{
		Sts2Doll dollAt = GetDollAt(sourceSlotIndex);
		if (dollAt == null || destinationSlotIndex < 0 || destinationSlotIndex >= 7 || !IsSlotEmpty(destinationSlotIndex))
		{
			return false;
		}
		Sts2Doll doll = dollAt.MakeStatEquivalentCopy();
		return SpawnInternal(doll, destinationSlotIndex);
	}

	public static bool SpawnRandomBaseDollAt(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= 7 || !IsSlotEmpty(slotIndex))
		{
			return false;
		}
		string[] array = new string[5] { "Shanghai", "France", "Netherlands", "Kyoto", "Hourai" };
		string dollId = array[Random.Shared.Next(array.Length)];
		return SpawnInternal(dollId, slotIndex);
	}

	public static int GetCardPlayCount(string cardId)
	{
		return (!string.IsNullOrWhiteSpace(cardId)) ? _cardPlayCounts.GetValueOrDefault(cardId, 0) : 0;
	}

	public static bool CanGainBlockFromCards()
	{
		return !_roundTableNoCardBlock;
	}

	public static int RegisterCardPlay(string cardId)
	{
		if (string.IsNullOrWhiteSpace(cardId))
		{
			return 0;
		}
		int num = GetCardPlayCount(cardId) + 1;
		_cardPlayCounts[cardId] = num;
		return num;
	}

	public static void EnableRoundTableNoCardBlock()
	{
		_roundTableNoCardBlock = true;
	}

	public static bool ArmFutureBunraku(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= 7 || IsSlotEmpty(slotIndex))
		{
			return false;
		}
		_futureBunrakuSlotIndex = slotIndex;
		return true;
	}

	public static void NotifyAliceCardPlayed(PlayerChoiceContext choiceContext, CardModel sourceCard, string cardId)
	{
		RegisterCombatContext(sourceCard.CombatState);
		RegisterCardPlay(cardId);
		DollTargetContext selectedTarget = GetSelectedTarget();
		if (_forbiddenMagicActs > 0 && selectedTarget.IsDoll && !IsSlotEmpty(selectedTarget.SlotIndex) && IsForbiddenMagicAllowed(sourceCard))
		{
			TriggerForbiddenMagicAct(choiceContext, sourceCard, selectedTarget.SlotIndex, _forbiddenMagicActs);
			TryMarkCardExhaust(sourceCard);
		}
		if (_futureBunrakuSlotIndex < 0 || _handlingFutureBunrakuPlay || IsSlotEmpty(_futureBunrakuSlotIndex))
		{
			return;
		}
		TriggerFutureBunrakuAct(choiceContext, sourceCard);
		if (_unlockMysticCount > 0)
		{
			int cardPlayCount = GetCardPlayCount(cardId);
			if (cardPlayCount == 1)
			{
				TryGainPlayerEnergy(1);
				DrawCards(choiceContext, sourceCard, 1);
			}
		}
		if (_princessFormCount > 0)
		{
			int num = TryReadInt(sourceCard, "Cost", "EnergyCost", "BaseCost");
			if (num == 1)
			{
				_princessFormCount++;
				if (_princessFormCount % 3 == 0)
				{
					object? obj = ((object)sourceCard).GetType().GetMethod("MakeCopy", BindingFlags.Instance | BindingFlags.Public)?.Invoke(sourceCard, null);
					CardModel val = (CardModel)((obj is CardModel) ? obj : null);
					if (val != null)
					{
						object obj2 = ((object)val).GetType().GetProperty("EnergyCost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(val);
						obj2?.GetType().GetMethod("SetThisTurnOrUntilPlayed", BindingFlags.Instance | BindingFlags.Public)?.Invoke(obj2, new object[1] { 0 });
						TryAddCardToHand(val);
					}
				}
			}
		}
		if (_seaOfSubconsciousnessCount <= 0)
		{
			return;
		}
		object obj3 = TryReadMember(sourceCard.Owner, "Hand", "HandCards", "_hand", "_handCards");
		if (obj3 != null)
		{
			List<object> list = EnumerateObjects(obj3).ToList();
			int num2 = list.IndexOf(sourceCard);
			if (num2 >= 0 && num2 < 7 && !IsSlotEmpty(num2))
			{
				ActAt(choiceContext, sourceCard, num2, _seaOfSubconsciousnessCount);
			}
		}
	}

	public static int RecycleAllExcept(int preservedSlotIndex)
	{
		if (preservedSlotIndex < 0 || preservedSlotIndex >= 7 || IsSlotEmpty(preservedSlotIndex))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			if (i != preservedSlotIndex && !IsSlotEmpty(i))
			{
				RemoveDoll(i, DollRemovalReason.Recycle);
				num++;
			}
		}
		if (num > 0)
		{
			RefreshSnapshots();
			RecountAndNotify($"RecycleExcept slot {preservedSlotIndex} x{num}");
		}
		return num;
	}

	public static bool RecycleRightmostDoll()
	{
		for (int i = 0; i < 7; i++)
		{
			if (RecycleAt(i))
			{
				return true;
			}
		}
		return false;
	}

	public static int GrantBlockToOtherDolls(int excludedSlotIndex, int blockAmount)
	{
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			if (i != excludedSlotIndex && TryApplyBlockToSlot(i, blockAmount))
			{
				num++;
			}
		}
		return num;
	}

	public static bool TryApplyBlockToSelectedTarget(CardModel sourceCard, int blockAmount)
	{
		RegisterCombatContext(sourceCard.CombatState);
		if (!CanGainBlockFromCards())
		{
			return false;
		}
		DollTargetContext selectedTarget = GetSelectedTarget();
		if (!selectedTarget.IsDoll)
		{
			return false;
		}
		int slotIndex = selectedTarget.SlotIndex;
		Sts2Doll sts2Doll = _dolls[slotIndex];
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return false;
		}
		int num = sts2Doll.GainBlock(blockAmount);
		RefreshSnapshots();
		RequestResolutionPopup(new ResolutionPopup(slotIndex, 0, num));
		Log.Info($"[AliceMagatroid_Mod] Block +{num} -> doll slot {slotIndex}", 2);
		return true;
	}

	public static int TauntAllOpponentsToSlot(int slotIndex, int turns)
	{
		if (slotIndex < 0 || slotIndex >= 7 || turns <= 0)
		{
			return 0;
		}
		int num = 0;
		foreach (object aliveOpponent in GetAliveOpponents())
		{
			_enemyTaunts[aliveOpponent] = new EnemyTauntState(slotIndex, turns);
			num++;
		}
		RefreshEnemyDamageTargets();
		return num;
	}

	public static bool TryApplySelectedBlock(CardModel sourceCard, int blockAmount)
	{
		return TryApplyBlockToSelectedTarget(sourceCard, blockAmount);
	}

	public static bool TryApplyBlockToSlot(int slotIndex, int blockAmount)
	{
		Sts2Doll sts2Doll = ((slotIndex >= 0 && slotIndex < 7) ? _dolls[slotIndex] : null);
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return false;
		}
		sts2Doll.GainBlock(blockAmount);
		RefreshSnapshots();
		return true;
	}

	public static bool TryApplyBlockToSlotFromCard(int slotIndex, int blockAmount)
	{
		return CanGainBlockFromCards() && TryApplyBlockToSlot(slotIndex, blockAmount);
	}

	public static int FindFirstEmptySlot()
	{
		for (int i = 0; i < _slots.Length; i++)
		{
			if (IsSlotEmpty(i))
			{
				return i;
			}
		}
		return -1;
	}

	public static bool TryHealAt(int slotIndex, int amount, out DollHealResolution resolution)
	{
		resolution = new DollHealResolution(0, 0);
		Sts2Doll sts2Doll = ((slotIndex >= 0 && slotIndex < 7) ? _dolls[slotIndex] : null);
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return false;
		}
		resolution = sts2Doll.Heal(amount);
		RefreshSnapshots();
		return true;
	}

	public static bool TryIncreaseMaxHpAt(int slotIndex, int amount, bool healToMatch = true)
	{
		Sts2Doll sts2Doll = ((slotIndex >= 0 && slotIndex < 7) ? _dolls[slotIndex] : null);
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return false;
		}
		if (sts2Doll.IncreaseMaxHp(amount, healToMatch) <= 0)
		{
			return false;
		}
		RefreshSnapshots();
		return true;
	}

	public static int IncreaseAllDollsMaxHp(int amount, bool healToMatch = true)
	{
		if (amount <= 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			if (TryIncreaseMaxHpAt(i, amount, healToMatch))
			{
				num++;
			}
		}
		return num;
	}

	public static bool TryMove(int fromSlotIndex, int toSlotIndex)
	{
		if (fromSlotIndex < 0 || fromSlotIndex >= 7 || toSlotIndex < 0 || toSlotIndex >= 7)
		{
			return false;
		}
		if (fromSlotIndex == toSlotIndex || IsSlotEmpty(fromSlotIndex) || !IsSlotEmpty(toSlotIndex))
		{
			return false;
		}
		Sts2Doll[] dolls = _dolls;
		Sts2Doll?[] dolls2 = _dolls;
		Sts2Doll sts2Doll = _dolls[toSlotIndex];
		Sts2Doll sts2Doll2 = _dolls[fromSlotIndex];
		dolls[fromSlotIndex] = sts2Doll;
		dolls2[toSlotIndex] = sts2Doll2;
		RefreshSnapshots();
		return true;
	}

	public static void TryRedirectIncomingDamage(object __instance, object[] __args, string hookName)
	{
		if (_handlingDamageRedirect || DollCount == 0 || _activePlayer == null || __instance != _activePlayer)
		{
			return;
		}
		int num = FindDamageArgumentIndex(__args);
		if (num < 0)
		{
			return;
		}
		int num2 = Convert.ToInt32(Math.Round(Convert.ToDecimal(__args[num])));
		if (num2 <= 0)
		{
			return;
		}
		RefreshEnemyDamageTargets();
		object attacker = ResolveAttacker(__args);
		int num3 = ResolveDamageTargetSlot(attacker);
		if (num3 < 0)
		{
			return;
		}
		Sts2Doll sts2Doll = _dolls[num3];
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return;
		}
		_handlingDamageRedirect = true;
		try
		{
			DollDamageResolution dollDamageResolution = sts2Doll.TakeDamage(num2);
			RequestResolutionPopup(new ResolutionPopup(num3, dollDamageResolution.DamageToDoll, dollDamageResolution.BlockedByDoll));
			if (dollDamageResolution.Destroyed)
			{
				RemoveDoll(num3, DollRemovalReason.Destroy);
				RecountAndNotify($"Destroy slot {num3}");
			}
			RefreshSnapshots();
			__args[num] = ConvertDamageValue(__args[num], dollDamageResolution.OverflowDamage);
			if (dollDamageResolution.OverflowDamage > 0)
			{
				int num4 = Math.Min(GetCurrentPlayerBlock(), dollDamageResolution.OverflowDamage);
				int damage = Math.Max(0, dollDamageResolution.OverflowDamage - num4);
				RequestResolutionPopup(new ResolutionPopup(null, damage, num4));
			}
			Log.Info($"[AliceMagatroid_Mod] {hookName} redirect {num2} -> slot {num3} dollDamage={dollDamageResolution.DamageToDoll} dollBlock={dollDamageResolution.BlockedByDoll} remaining={dollDamageResolution.OverflowDamage}", 2);
		}
		finally
		{
			_handlingDamageRedirect = false;
		}
	}

	internal static async Task AttackAllOpponents(PlayerChoiceContext choiceContext, CardModel sourceCard, int damage)
	{
		RegisterCombatContext(sourceCard.CombatState);
		List<object> opponents = GetAliveOpponents();
		foreach (object opponent in opponents)
		{
			int adjustedDamage = damage + GetCallToDollsBonus(opponent);
			dynamic cmd = DamageCmd.Attack((decimal)adjustedDamage).FromCard(sourceCard).Targeting((dynamic)opponent);
			await ((dynamic)cmd).Execute(choiceContext);
		}
	}

	public static void ArmCallToDolls(object? opponent, int bonusDamage)
	{
		if (opponent != null && bonusDamage > 0)
		{
			_callToDollsTarget = opponent;
			_callToDollsBonus = bonusDamage;
		}
	}

	internal static int GetCallToDollsBonus(object? opponent)
	{
		return (opponent != null && _callToDollsTarget == opponent) ? Math.Max(0, _callToDollsBonus) : 0;
	}

	internal static async Task DrawCards(PlayerChoiceContext choiceContext, CardModel sourceCard, int amount)
	{
		for (int i = 0; i < Math.Max(0, amount); i++)
		{
			MethodInfo drawMethod = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				select assembly.GetType("MegaCrit.Sts2.Core.GameActions.Multiplayer.CommonActions", throwOnError: false)).FirstOrDefault((Type type) => type != null)?.GetMethod("Draw", BindingFlags.Static | BindingFlags.Public);
			if (drawMethod == null)
			{
				break;
			}
			object obj = drawMethod.Invoke(null, new object[2] { sourceCard, choiceContext });
			if (obj is Task task)
			{
				await task;
			}
		}
	}

	internal static int GetCurrentPlayerBlockValue()
	{
		return GetCurrentPlayerBlock();
	}

	internal static int GetCurrentPlayerEnergyValue()
	{
		return TryReadInt(_activePlayer, "CurrentEnergy", "Energy", "_currentEnergy", "_energy", "TurnEnergy");
	}

	internal static bool TryGainPlayerEnergy(int amount)
	{
		return TryChangeNumericMember(_activePlayer, amount, "CurrentEnergy", "Energy", "_currentEnergy", "_energy", "TurnEnergy");
	}

	internal static bool TrySchedulePlayerDrawNextTurn(int amount)
	{
		return TryAdjustNamedStatusAmount(_activePlayer, amount, "DrawCardNextTurn", "NextTurnDraw", "DrawNextTurn");
	}

	internal static bool TryAdjustPlayerStrength(int amount)
	{
		return TryAdjustNamedStatusAmount(_activePlayer, amount, "Strength");
	}

	internal static bool TryAdjustPlayerDexterity(int amount)
	{
		return TryAdjustNamedStatusAmount(_activePlayer, amount, "Dexterity", "Dex");
	}

	internal static bool TryAdjustPlayerStatus(int amount, params string[] statusNames)
	{
		return TryAdjustNamedStatusAmount(_activePlayer, amount, statusNames);
	}

	internal static bool TryGainPlayerGold(int amount)
	{
		return TryChangeNumericMember(_activePlayer, Math.Max(0, amount), "Gold", "CurrentGold", "Coins", "Money", "_gold", "_coins");
	}

	internal static bool TryGrantBlockToPlayer(int amount)
	{
		return TryChangeNumericMember(_activePlayer, Math.Max(0, amount), "CurrentBlock", "Block", "_currentBlock", "_block");
	}

	internal static bool TryGrantBlockToPlayerFromCard(int amount)
	{
		return CanGainBlockFromCards() && TryGrantBlockToPlayer(amount);
	}

	internal static bool TryAddCardToHand(CardModel card)
	{
		return TryAddCardToCollection(card, "Hand", "HandCards", "HandModels", "CardsInHand", "_hand", "_handCards", "_handModels") || TryAddCardToDiscard(card);
	}

	internal static bool TryAddCardToDiscard(CardModel card)
	{
		return TryAddCardToCollection(card, "Discard", "DiscardPile", "DiscardCards", "DiscardModels", "_discard", "_discardPile", "_discardCards");
	}

	internal static int ApplyWeakToCorrespondingOpponents(int slotIndex, int amount)
	{
		RefreshEnemyDamageTargets();
		int num = 0;
		foreach (object aliveOpponent in GetAliveOpponents())
		{
			if (_enemyDamageTargets.TryGetValue(aliveOpponent, out var value) && value == slotIndex && TryAdjustNamedStatusAmount(aliveOpponent, amount, "Weak"))
			{
				num++;
			}
		}
		return num;
	}

	private static int SpawnDoll(string dollId, int amount, bool randomPlacement)
	{
		if (amount <= 0)
		{
			return DollCount;
		}
		for (int i = 0; i < amount; i++)
		{
			int num = ResolveSpawnSlot(-1, randomPlacement);
			if (num < 0)
			{
				break;
			}
			SpawnInternal(dollId, num);
		}
		return DollCount;
	}

	private static async Task ActSlot(PlayerChoiceContext choiceContext, CardModel sourceCard, int slotIndex)
	{
		Sts2Doll doll = ((slotIndex >= 0 && slotIndex < _dolls.Length) ? _dolls[slotIndex] : null);
		if (doll?.IsEmptySlot ?? true)
		{
			return;
		}
		if (_maidensBunrakuBlock > 0)
		{
			doll.GainBlock(_maidensBunrakuBlock);
		}
		await doll.Act(choiceContext, sourceCard);
		NotifyOtherDollsAct(doll, slotIndex);
		if (_magicConduitAmount > 0 && slotIndex == 0 && !IsSlotEmpty(slotIndex))
		{
			int conduitAmount = _magicConduitAmount;
			if (RecycleAt(slotIndex))
			{
				TryGainPlayerEnergy(conduitAmount);
				await DrawCards(choiceContext, sourceCard, conduitAmount);
			}
		}
		if (_luminousShanghaiActs > 0 && !(doll is ShanghaiDoll))
		{
			await ReActRandomShanghai(choiceContext, sourceCard, _luminousShanghaiActs);
		}
	}

	private static async Task TriggerFutureBunrakuAct(PlayerChoiceContext choiceContext, CardModel sourceCard)
	{
		_handlingFutureBunrakuPlay = true;
		try
		{
			await ActAt(choiceContext, sourceCard, _futureBunrakuSlotIndex);
		}
		finally
		{
			_handlingFutureBunrakuPlay = false;
		}
	}

	internal static object? FindLowestHealthOpponent()
	{
		List<object> aliveOpponents = GetAliveOpponents();
		return aliveOpponents.OrderBy((object opponent) => TryReadInt(opponent, "CurrentHp", "Hp", "_currentHp")).FirstOrDefault();
	}

	private static List<object> GetAliveOpponents()
	{
		List<object> source = EnumerateObjects(TryReadMember(_activeCombatState, "Creatures", "AllCreatures", "_creatures")).ToList();
		return source.Where((object creature) => creature != null && (_activePlayer == null || creature != _activePlayer)).Where(IsAlive).ToList();
	}

	private static bool SpawnInternal(string dollId, int slotIndex)
	{
		return SpawnInternal(DollRegistry.Create(dollId), slotIndex);
	}

	private static bool SpawnInternal(Sts2Doll doll, int slotIndex)
	{
		int num = ResolveSpawnSlot(slotIndex, randomPlacement: false);
		if (num < 0)
		{
			Log.Info("[AliceMagatroid_Mod] Spawn " + doll.Id + " skipped: no empty slot", 2);
			return false;
		}
		if (_dollWarMaxHpBonus > 0)
		{
			doll.IncreaseMaxHp(_dollWarMaxHpBonus, healToMatch: true);
		}
		_dolls[num] = doll;
		_slots[num] = doll.Id;
		_placedThisCombat.Add(doll.Id);
		doll.OnSpawn(num);
		NotifyOtherDollsSpawn(doll, num);
		RefreshSnapshots();
		RecountAndNotify($"Spawn {doll.Id} -> slot {num}");
		if (_artfulChanterActs > 0 && _recycledOrDestroyedThisCombat.Contains(doll.Id))
		{
			TriggerArtfulChanterActs(doll, _artfulChanterActs);
		}
		if (_dollOrchestraDraw > 0)
		{
			TriggerDollOrchestraDraw(_dollOrchestraDraw);
		}
		if (_semiAutomatonActs > 0)
		{
			TriggerSemiAutomatonActs(doll, _semiAutomatonActs);
			_semiAutomatonActs = 0;
		}
		return true;
	}

	private static int ResolveManualPlacementSlot()
	{
		DollTargetContext selectedTarget = GetSelectedTarget();
		if (selectedTarget.IsEmptySlot)
		{
			return selectedTarget.SlotIndex;
		}
		return FindFirstEmptySlot();
	}

	private static int ResolveSpawnSlot(int preferredSlotIndex, bool randomPlacement)
	{
		if (preferredSlotIndex >= 0 && preferredSlotIndex < 7)
		{
			return IsSlotEmpty(preferredSlotIndex) ? preferredSlotIndex : (-1);
		}
		if (randomPlacement)
		{
			int[] array = Enumerable.Range(0, 7).Where(IsSlotEmpty).ToArray();
			if (array.Length == 0)
			{
				return -1;
			}
			return array[Random.Shared.Next(array.Length)];
		}
		return FindFirstEmptySlot();
	}

	private static void RemoveDoll(int slotIndex, DollRemovalReason reason)
	{
		if (slotIndex < 0 || slotIndex >= 7)
		{
			return;
		}
		Sts2Doll sts2Doll = _dolls[slotIndex];
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return;
		}
		switch (reason)
		{
		case DollRemovalReason.Destroy:
			sts2Doll.OnDestroyed(slotIndex);
			_recycledOrDestroyedThisCombat.Add(sts2Doll.Id);
			break;
		case DollRemovalReason.Recycle:
			if (_dollAmbushCount > 0 && _activeCombatState != null)
			{
				TriggerAmbushActs(slotIndex, sts2Doll, _dollAmbushCount);
			}
			sts2Doll.OnRecycled(slotIndex);
			_recycledOrDestroyedThisCombat.Add(sts2Doll.Id);
			if (_dollJudgePercent > 0)
			{
				TryGrantBlockToPlayer((int)MathF.Round((float)sts2Doll.Block * ((float)_dollJudgePercent / 100f)));
			}
			break;
		}
		sts2Doll.OnRemoved(slotIndex, reason);
		_dolls[slotIndex] = new EmptyDollSlot();
		_slots[slotIndex] = string.Empty;
		_snapshots[slotIndex] = null;
		NotifyOtherDollsRemoved(sts2Doll.Id, slotIndex, reason);
	}

	private static IEnumerable<Sts2Doll> EnumerateDolls()
	{
		return from doll in _dolls.OfType<Sts2Doll>()
			where !doll.IsEmptySlot
			select doll;
	}

	public static bool IsSlotEmpty(int slotIndex)
	{
		return ((slotIndex >= 0 && slotIndex < _dolls.Length) ? _dolls[slotIndex] : null)?.IsEmptySlot ?? true;
	}

	private static void FillWithEmptySlots()
	{
		for (int i = 0; i < _dolls.Length; i++)
		{
			_dolls[i] = new EmptyDollSlot();
			_slots[i] = string.Empty;
			_snapshots[i] = null;
		}
	}

	private static void NotifyOtherDollsSpawn(Sts2Doll spawnedDoll, int slotIndex)
	{
		foreach (Sts2Doll item in EnumerateDolls())
		{
			if (item != spawnedDoll)
			{
				item.PostOtherDollSpawn(spawnedDoll, slotIndex);
			}
		}
	}

	private static void NotifyOtherDollsAct(Sts2Doll actingDoll, int slotIndex)
	{
		foreach (Sts2Doll item in EnumerateDolls())
		{
			if (item != actingDoll)
			{
				item.PostOtherDollAct(actingDoll, slotIndex);
			}
		}
	}

	private static void NotifyOtherDollsRemoved(string removedDollId, int slotIndex, DollRemovalReason reason)
	{
		foreach (Sts2Doll item in EnumerateDolls())
		{
			item.PostOtherDollRemoved(removedDollId, slotIndex, reason);
		}
	}

	private static async Task ActivateLeadingDollsAtEndOfTurn(int count)
	{
		CardModel sourceCard = CreateVirtualSourceCard();
		if (sourceCard == null || _activeCombatState == null)
		{
			return;
		}
		PlayerChoiceContext choiceContext = TryCreateChoiceContext();
		if (choiceContext == null)
		{
			return;
		}
		int remaining = count;
		for (int i = 0; i < _dolls.Length; i++)
		{
			if (remaining <= 0)
			{
				break;
			}
			if (!IsSlotEmpty(i))
			{
				await ActSlot(choiceContext, sourceCard, i);
				remaining--;
			}
		}
	}

	private static async Task TriggerAmbushActs(int slotIndex, Sts2Doll doll, int count)
	{
		CardModel sourceCard = CreateVirtualSourceCard();
		PlayerChoiceContext choiceContext = TryCreateChoiceContext();
		if (sourceCard != null && choiceContext != null)
		{
			for (int i = 0; i < count; i++)
			{
				await doll.Act(choiceContext, sourceCard);
			}
		}
	}

	private static async Task TriggerSemiAutomatonActs(Sts2Doll doll, int count)
	{
		CardModel sourceCard = CreateVirtualSourceCard();
		PlayerChoiceContext choiceContext = TryCreateChoiceContext();
		if (sourceCard != null && choiceContext != null)
		{
			for (int i = 0; i < count; i++)
			{
				await doll.Act(choiceContext, sourceCard);
			}
		}
	}

	private static async Task TriggerForbiddenMagicAct(PlayerChoiceContext choiceContext, CardModel sourceCard, int slotIndex, int count)
	{
		if (count <= 0 || slotIndex < 0 || slotIndex >= 7 || IsSlotEmpty(slotIndex))
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (IsSlotEmpty(slotIndex))
			{
				break;
			}
			await ActAt(choiceContext, sourceCard, slotIndex);
		}
	}

	private static bool IsForbiddenMagicAllowed(CardModel sourceCard)
	{
		return !(sourceCard is AliceCard aliceCard) || aliceCard.AllowsForbiddenMagic;
	}

	private static void TryMarkCardExhaust(CardModel sourceCard)
	{
		TrySetBoolMember(sourceCard, true, "ExhaustOnNextPlay", "Exhaust", "_exhaust", "_exhausts", "PurgeOnUse");
	}

	private static async Task TriggerArtfulChanterActs(Sts2Doll doll, int count)
	{
		CardModel sourceCard = CreateVirtualSourceCard();
		PlayerChoiceContext choiceContext = TryCreateChoiceContext();
		if (sourceCard != null && choiceContext != null)
		{
			for (int i = 0; i < count; i++)
			{
				await doll.Act(choiceContext, sourceCard);
			}
		}
	}

	private static async Task TriggerDollOrchestraDraw(int amount)
	{
		CardModel sourceCard = CreateVirtualSourceCard();
		PlayerChoiceContext choiceContext = TryCreateChoiceContext();
		if (sourceCard != null && choiceContext != null)
		{
			await DrawCards(choiceContext, sourceCard, amount);
		}
	}

	private static void RefreshEnemyDamageTargets()
	{
		_enemyDamageTargets.Clear();
		List<object> aliveOpponents = GetAliveOpponents();
		if (aliveOpponents.Count == 0)
		{
			_enemyTaunts.Clear();
			return;
		}
		for (int i = 0; i < aliveOpponents.Count; i++)
		{
			int value = ((aliveOpponents.Count == 1) ? 3 : MathfRoundToInt((float)i * 6f / ((float)aliveOpponents.Count - 1f)));
			_enemyDamageTargets[aliveOpponents[i]] = value;
		}
		HashSet<object> activeOpponents = aliveOpponents.ToHashSet<object>(ReferenceEqualityComparer.Instance);
		object[] array = _enemyTaunts.Keys.Where((object opponent) => !activeOpponents.Contains(opponent)).ToArray();
		foreach (object key in array)
		{
			_enemyTaunts.Remove(key);
		}
		foreach (object item in aliveOpponents)
		{
			if (_enemyTaunts.TryGetValue(item, out var value2))
			{
				_enemyDamageTargets[item] = Math.Clamp(value2.SlotIndex, 0, 6);
			}
		}
	}

	private static void AdvanceEnemyTaunts()
	{
		if (_enemyTaunts.Count == 0)
		{
			return;
		}
		KeyValuePair<object, EnemyTauntState>[] array = _enemyTaunts.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<object, EnemyTauntState> keyValuePair = array[i];
			if (keyValuePair.Value.RemainingTurns <= 1)
			{
				_enemyTaunts.Remove(keyValuePair.Key);
				continue;
			}
			_enemyTaunts[keyValuePair.Key] = keyValuePair.Value with
			{
				RemainingTurns = keyValuePair.Value.RemainingTurns - 1
			};
		}
		RefreshEnemyDamageTargets();
	}

	private static int ResolveDamageTargetSlot(object? attacker)
	{
		if (attacker != null && _enemyDamageTargets.TryGetValue(attacker, out var value))
		{
			return value;
		}
		return (_enemyDamageTargets.Count == 1) ? _enemyDamageTargets.Values.First() : (-1);
	}

	private static object? ResolveAttacker(object[] args)
	{
		foreach (object obj in args)
		{
			if (obj != null)
			{
				if (_enemyDamageTargets.ContainsKey(obj))
				{
					return obj;
				}
				object obj2 = TryReadMember(obj, "Source", "Owner", "Attacker", "Creature", "Target");
				if (obj2 != null && _enemyDamageTargets.ContainsKey(obj2))
				{
					return obj2;
				}
			}
		}
		return null;
	}

	private static int FindDamageArgumentIndex(object[] args)
	{
		for (int i = 0; i < args.Length; i++)
		{
			object obj = args[i];
			if (obj != null)
			{
				Type type = obj.GetType();
				if (type == typeof(int) || type == typeof(float) || type == typeof(double) || type == typeof(decimal))
				{
					return i;
				}
			}
		}
		return -1;
	}

	private static object ConvertDamageValue(object originalValue, int damage)
	{
		Type type = originalValue.GetType();
		if (type == typeof(decimal))
		{
			return (decimal)damage;
		}
		if (type == typeof(double))
		{
			return (double)damage;
		}
		if (type == typeof(float))
		{
			return (float)damage;
		}
		return damage;
	}

	private static int GetCurrentPlayerBlock()
	{
		return TryReadInt(_activePlayer, "CurrentBlock", "Block", "_currentBlock");
	}

	private static bool IsAlive(object creature)
	{
		int num = TryReadInt(creature, "CurrentHp", "Hp", "_currentHp");
		if (num > 0)
		{
			return true;
		}
		bool flag = TryReadBool(creature, "IsDead", "Dead", "_dead");
		return !flag;
	}

	private static object? TryReadMember(object? target, params string[] memberNames)
	{
		if (target == null)
		{
			return null;
		}
		Type type = target.GetType();
		foreach (string name in memberNames)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(target);
			}
			FieldInfo field = type.GetField(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				return field.GetValue(target);
			}
		}
		return null;
	}

	private static int TryReadInt(object? target, params string[] memberNames)
	{
		object obj = TryReadMember(target, memberNames);
		return (obj != null) ? Convert.ToInt32(Math.Round(Convert.ToDecimal(obj))) : 0;
	}

	private static bool TryReadBool(object? target, params string[] memberNames)
	{
		object obj = TryReadMember(target, memberNames);
		return obj != null && Convert.ToBoolean(obj);
	}

	private static string? TryReadString(object? target, params string[] memberNames)
	{
		return TryReadMember(target, memberNames)?.ToString();
	}

	private static bool TryChangeNumericMember(object? target, int delta, params string[] memberNames)
	{
		if (target == null || delta == 0)
		{
			return false;
		}
		Type type = target.GetType();
		foreach (string name in memberNames)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.CanRead && property.CanWrite)
			{
				object value = property.GetValue(target);
				if (value != null && TryConvertNumeric(value, out var converted))
				{
					property.SetValue(target, ConvertToNumeric(property.PropertyType, converted + (decimal)delta));
					return true;
				}
				continue;
			}
			FieldInfo field = type.GetField(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				object value2 = field.GetValue(target);
				if (value2 != null && TryConvertNumeric(value2, out var converted2))
				{
					field.SetValue(target, ConvertToNumeric(field.FieldType, converted2 + (decimal)delta));
					return true;
				}
			}
		}
		return false;
	}

	private static bool TrySetBoolMember(object target, bool value, params string[] memberNames)
	{
		if (target == null)
		{
			return false;
		}
		Type type = target.GetType();
		foreach (string name in memberNames)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
			{
				property.SetValue(target, value);
				return true;
			}
			FieldInfo field = type.GetField(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
			{
				field.SetValue(target, value);
				return true;
			}
		}
		return false;
	}

	private static bool TryAdjustNamedStatusAmount(object? target, int delta, params string[] statusNames)
	{
		if (target == null || delta == 0)
		{
			return false;
		}
		string[] array = new string[6] { "Statuses", "StatusModels", "Powers", "Effects", "_statuses", "_powers" };
		foreach (string text in array)
		{
			foreach (object item in EnumerateObjects(TryReadMember(target, text)))
			{
				string typeName = item.GetType().Name;
				string id = TryReadString(item, "Id", "StatusId", "PowerId", "Name");
				if (!statusNames.Any((string name) => typeName.Contains(name, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrWhiteSpace(id) && id.Contains(name, StringComparison.OrdinalIgnoreCase))) || !TryChangeNumericMember(item, delta, "Amount", "StackAmount", "Stacks", "Value", "CurrentValue", "_amount"))
				{
					continue;
				}
				return true;
			}
		}
		return TryChangeNumericMember(target, delta, statusNames[0] ?? "", "Current" + statusNames[0], "_" + statusNames[0].ToLowerInvariant(), "_current" + statusNames[0].ToLowerInvariant());
	}

	private static bool TryConvertNumeric(object value, out decimal converted)
	{
		try
		{
			converted = Convert.ToDecimal(value);
			return true;
		}
		catch
		{
			converted = default(decimal);
			return false;
		}
	}

	private static object ConvertToNumeric(Type targetType, decimal value)
	{
		if (targetType == typeof(int))
		{
			return Convert.ToInt32(Math.Round(value));
		}
		if (targetType == typeof(float))
		{
			return Convert.ToSingle(value);
		}
		if (targetType == typeof(double))
		{
			return Convert.ToDouble(value);
		}
		if (targetType == typeof(decimal))
		{
			return value;
		}
		if (targetType == typeof(long))
		{
			return Convert.ToInt64(Math.Round(value));
		}
		return Convert.ChangeType(value, targetType);
	}

	private static bool IsLikelyPlayerLifecycleOwner(object? owner)
	{
		if (owner == null)
		{
			return false;
		}
		if (_activePlayer != null && owner == _activePlayer)
		{
			return true;
		}
		return owner.GetType().Name.Contains("Player", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryAddCardToCollection(CardModel card, params string[] collectionNames)
	{
		if (_activePlayer == null)
		{
			return false;
		}
		foreach (string text in collectionNames)
		{
			object obj = TryReadMember(_activePlayer, text);
			if (obj != null)
			{
				if (obj is IList list)
				{
					list.Add(card);
					TryInvokeCollectionRefresh(obj);
					return true;
				}
				MethodInfo method = obj.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					method.Invoke(obj, new object[1] { card });
					TryInvokeCollectionRefresh(obj);
					return true;
				}
			}
		}
		return false;
	}

	private static void TryInvokeCollectionRefresh(object collection)
	{
		string[] array = new string[5] { "Refresh", "RefreshLayout", "RefreshHandLayout", "ApplyPowers", "NotifyChanged" };
		foreach (string name in array)
		{
			MethodInfo method = collection.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			if (method != null)
			{
				method.Invoke(collection, null);
			}
		}
	}

	private static PlayerChoiceContext? TryCreateChoiceContext()
	{
		object? obj = TryReadMember(_activeCombatState, "ChoiceContext", "PlayerChoiceContext", "_choiceContext");
		return (PlayerChoiceContext?)((obj is PlayerChoiceContext) ? obj : null);
	}

	private static CardModel? CreateVirtualSourceCard()
	{
		object? obj = TryReadMember(_activePlayer, "CardSource", "SourceCard", "_sourceCard");
		return (CardModel?)((obj is CardModel) ? obj : null);
	}

	private static IEnumerable<object> EnumerateObjects(object? collection)
	{
		if (!(collection is IEnumerable enumerable))
		{
			yield break;
		}
		foreach (object item in enumerable)
		{
			if (item != null)
			{
				yield return item;
			}
		}
	}

	private static int MathfRoundToInt(float value)
	{
		return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
	}

	private static void SetSelection(int slotIndex, bool force = false)
	{
		if (force || SelectedSlotIndex != slotIndex)
		{
			SelectedSlotIndex = slotIndex;
			DollState.SelectionChanged((slotIndex >= 0) ? new int?(slotIndex) : null);
		}
	}

	private static void RequestResolutionPopup(ResolutionPopup popup)
	{
		if (popup.Damage > 0 || popup.Blocked > 0)
		{
			DollState.ResolutionPopupRequested(popup);
		}
	}

	private static void RefreshSnapshots()
	{
		for (int i = 0; i < _dolls.Length; i++)
		{
			Sts2Doll sts2Doll = _dolls[i];
			bool flag = sts2Doll != null && !sts2Doll.IsEmptySlot;
			_snapshots[i] = (flag ? new DollSnapshot?(sts2Doll.CreateSnapshot(i)) : null);
			_slots[i] = (flag ? sts2Doll.Id : string.Empty);
		}
		DollState.StatsChanged(_snapshots);
		if (_playerTurnActive)
			RefreshIntentPreviews();
	}

	private static void RecountAndNotify(string reason)
	{
		int dollCount = DollCount;
		DollCount = _dolls.Count((Sts2Doll doll) => doll != null && !doll.IsEmptySlot);
		Log.Info($"[AliceMagatroid_Mod] {reason} ({dollCount} -> {DollCount})", 2);
		DollState.SlotsChanged(_slots);
		if (dollCount != DollCount)
		{
			DollState.DollCountChanged(DollCount);
		}
	}

	public static void SetPendingDamageAttacker(object? attacker)
	{
		_pendingDamageAttacker = attacker;
	}

	public static int AbsorbDamageForAttacker(int damageAmount)
	{
		if (_handlingDamageRedirect || DollCount == 0 || damageAmount <= 0)
		{
			return damageAmount;
		}
		RefreshEnemyDamageTargets();
		int num = ResolveDamageTargetSlot(_pendingDamageAttacker);
		if (num < 0)
		{
			return damageAmount;
		}
		Sts2Doll sts2Doll = _dolls[num];
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return damageAmount;
		}
		_handlingDamageRedirect = true;
		try
		{
			DollDamageResolution dollDamageResolution = sts2Doll.TakeDamage(damageAmount);
			RequestResolutionPopup(new ResolutionPopup(num, dollDamageResolution.DamageToDoll, dollDamageResolution.BlockedByDoll));
			if (dollDamageResolution.Destroyed)
			{
				RemoveDoll(num, DollRemovalReason.Destroy);
				RecountAndNotify("Doll destroyed at slot " + num);
			}
			RefreshSnapshots();
			Log.Info("[AliceMagatroid_Mod] AbsorbDamageForAttacker: " + damageAmount + " -> slot " + num + " -> remaining=" + dollDamageResolution.OverflowDamage, 2);
			return dollDamageResolution.OverflowDamage;
		}
		finally
		{
			_handlingDamageRedirect = false;
		}
	}

	public static async Task<int> WaitForSlotSelectionAsync(bool emptySlotsOnly, bool includeAlice)
	{
		_selectionEmptyOnly = emptySlotsOnly;
		_selectionIncludeAlice = includeAlice;
		_slotSelectionTcs = new TaskCompletionSource<int>();
		DollState.SelectionModeChanged(emptySlotsOnly, includeAlice);
		return await _slotSelectionTcs.Task;
	}

	public static void ResolveSlotSelection(int slotIndex)
	{
		_slotSelectionTcs?.TrySetResult(slotIndex);
		_slotSelectionTcs = null;
		DollState.SelectionModeChanged(arg1: false, arg2: false);
	}

	public static void CancelSlotSelection()
	{
		_slotSelectionTcs?.TrySetResult(-1);
		_slotSelectionTcs = null;
		DollState.SelectionModeChanged(arg1: false, arg2: false);
	}

	public static bool IsSlotValidSelection(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= 7)
		{
			return false;
		}
		if (_selectionEmptyOnly)
		{
			return IsSlotEmpty(slotIndex);
		}
		return !IsSlotEmpty(slotIndex);
	}

	public static async Task<int> PlaceDollAt(PlayerChoiceContext choiceContext, CardModel sourceCard, string dollId, int slotIndex)
	{
		RegisterCombatContext(sourceCard.CombatState);
		if (slotIndex < 0 || slotIndex >= 7 || !IsSlotEmpty(slotIndex))
		{
			slotIndex = FindFirstEmptySlot();
		}
		if (!SpawnInternal(dollId, slotIndex))
		{
			return DollCount;
		}
		await ActSlot(choiceContext, sourceCard, slotIndex);
		return DollCount;
	}

	public static bool TryApplyBlockToSlot(CardModel sourceCard, int blockAmount, int slotIndex)
	{
		RegisterCombatContext(sourceCard.CombatState);
		if (!CanGainBlockFromCards())
		{
			return false;
		}
		if (slotIndex < 0 || slotIndex >= 7)
		{
			return false;
		}
		Sts2Doll sts2Doll = _dolls[slotIndex];
		if (sts2Doll == null || sts2Doll.IsEmptySlot)
		{
			return false;
		}
		int blocked = sts2Doll.GainBlock(blockAmount);
		RefreshSnapshots();
		RequestResolutionPopup(new ResolutionPopup(slotIndex, 0, blocked));
		Log.Info("[AliceMagatroid_Mod] Block +" + blocked + " -> doll slot " + slotIndex, 2);
		return true;
	}

	// --- Placement Targeting State ---

	public static void EnterPlacementTargeting(object cardPlayNode)
	{
		GD.Print("[AliceMagatroid_Mod][PlacementTargeting] EnterPlacementTargeting called, node=", cardPlayNode?.GetType().Name ?? "null");
		_placementTargeting = true;
		_pendingCardPlayNode = cardPlayNode as Node;
		_frozenCardPosition = null;
		if (_pendingCardPlayNode != null)
		{
			try
			{
				var holderRaw = Traverse.Create(_pendingCardPlayNode).Property("Holder").GetValue();
				GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Holder raw type=", holderRaw?.GetType().Name ?? "null");
				if (holderRaw is Node2D holder2D)
				{
					_frozenCardPosition = holder2D.GlobalPosition;
					GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Frozen position (Node2D)=", _frozenCardPosition);
				}
				else if (holderRaw is Control holderCtrl)
				{
					_frozenCardPosition = holderCtrl.GlobalPosition;
					GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Frozen position (Control)=", _frozenCardPosition);
				}
				else
				{
					GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Holder is null or unknown type, using mouse position fallback");
					_frozenCardPosition = _pendingCardPlayNode.GetViewport()?.GetMousePosition();
				}
			}
			catch (System.Exception ex) { GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Holder fetch failed: ", ex.Message); }
		}
		PlacementTargetingChanged?.Invoke(true);
		GD.Print("[AliceMagatroid_Mod][PlacementTargeting] EnterPlacementTargeting done, _pendingCardPlayNode=", _pendingCardPlayNode != null);
	}

	public static void ConfirmPlacementTargeting(int slotIndex)
	{
		GD.Print("[AliceMagatroid_Mod][PlacementTargeting] ConfirmPlacementTargeting called, slot=", slotIndex, " targeting=", _placementTargeting);
		if (!_placementTargeting) return;
		SelectSlot(slotIndex);
		_placementTargeting = false;
		var node = _pendingCardPlayNode;
		_pendingCardPlayNode = null;
		PlacementTargetingChanged?.Invoke(false);
		if (node == null)
		{
			GD.Print("[AliceMagatroid_Mod][PlacementTargeting] node is null, skipping play");
			return;
		}
		try
		{
			// Get the CardModel directly
			var card = Traverse.Create(node).Property("Card").GetValue() as CardModel;
			GD.Print("[AliceMagatroid_Mod][PlacementTargeting] CardModel=", card?.GetType().Name ?? "null",
				" CanPlay=", card?.CanPlay() ?? false,
				" CanPlayTargeting=", card?.CanPlayTargeting(null) ?? false);

			if (card == null)
			{
				GD.Print("[AliceMagatroid_Mod][PlacementTargeting] CardModel is null, cancelling");
				CancelPlayNode(node);
				return;
			}

			// Try to play the card directly through CardModel
			_isConfirmingPlacement = true;
			bool played = card.TryManualPlay(null);
			_isConfirmingPlacement = false;
			GD.Print("[AliceMagatroid_Mod][PlacementTargeting] TryManualPlay result=", played);

			if (played)
			{
				// Card was enqueued successfully, clean up the NMouseCardPlay
				GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Card enqueued, cleaning up NMouseCardPlay");
				Traverse.Create(node).Method("Cleanup", new object[] { true }).GetValue();
			}
			else
			{
				// Card couldn't be played, cancel and return to hand
				GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Card play failed, cancelling NMouseCardPlay");
				CancelPlayNode(node);
			}
		}
		catch (System.Exception ex)
		{
			_isConfirmingPlacement = false;
			GD.Print("[AliceMagatroid_Mod][PlacementTargeting] Exception: ", ex.Message, "\n", ex.StackTrace);
			try { CancelPlayNode(node); } catch { }
		}
	}

	public static void RefreshIntentPreviews()
	{
		if (!_playerTurnActive)
		{
			return;
		}
		RefreshEnemyDamageTargets();
		var opponents = GetAliveOpponents();
		var slotDamage = new Dictionary<int, int>();
		foreach (var enemy in opponents)
		{
			if (!_enemyDamageTargets.TryGetValue(enemy, out var slot))
				continue;
			var damage = ReadEnemyAttackDamage(enemy);
			if (damage <= 0)
				continue;
			if (!slotDamage.ContainsKey(slot))
				slotDamage[slot] = 0;
			slotDamage[slot] += damage;
			GD.Print("[AliceMagatroid_Mod][IntentPreview] enemy=", enemy.GetType().Name, " slot=", slot, " damage=", damage);
		}
		var previews = new List<DollIntentPreview>();
		var totalOverflow = 0;
		foreach (var kv in slotDamage)
		{
			var doll = _dolls[kv.Key];
			var overflow = 0;
			if (doll != null && !doll.IsEmptySlot)
				overflow = Math.Max(0, kv.Value - doll.Hp - doll.Block);
			else
				overflow = kv.Value;
			totalOverflow += overflow;
			previews.Add(new DollIntentPreview(kv.Key, kv.Value, overflow));
		}
		if (totalOverflow > 0)
			previews.Add(new DollIntentPreview(-1, totalOverflow, totalOverflow));
		_cachedIntentPreviews = previews.ToArray();
		IntentPreviewsChanged?.Invoke(_cachedIntentPreviews);
		GD.Print("[AliceMagatroid_Mod][IntentPreview] Refreshed: ", previews.Count, " previews, totalOverflow=", totalOverflow);
	}

	private static int ReadEnemyAttackDamage(object enemy)
	{
		try
		{
			var nextMove = TryReadMember(enemy, "NextMove", "Move", "NextAction", "_nextMove", "CurrentMove");
			if (nextMove == null) return 0;
			var intentsObj = TryReadMember(nextMove, "Intents", "IntentList", "_intents", "Actions");
			if (intentsObj == null) return 0;
			var totalDamage = 0;
			foreach (var intent in EnumerateObjects(intentsObj))
			{
				if (!IsAttackIntent(intent)) continue;
				var dmg = TryGetIntentDamage(intent, enemy);
				var hits = Math.Max(1, TryReadInt(intent, "Repeats", "HitCount", "Times", "_repeats"));
				totalDamage += dmg * hits;
			}
			return totalDamage;
		}
		catch { return 0; }
	}

	private static bool IsAttackIntent(object intent)
	{
		var typeName = intent.GetType().Name;
		if (typeName.Contains("Attack")) return true;
		var it = TryReadMember(intent, "IntentType", "Type", "_intentType", "Kind");
		if (it != null && it.ToString()?.Contains("Attack") == true) return true;
		return false;
	}

	private static int TryGetIntentDamage(object intent, object owner)
	{
		try
		{
			var method = intent.GetType().GetMethod("GetSingleDamage",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			if (method != null)
			{
				var targets = new List<object>();
				var result = method.Invoke(intent, new object?[] { targets, owner });
				if (result != null) return (int)Math.Round(Convert.ToDecimal(result));
			}
		}
		catch { }
		return Math.Max(0, TryReadInt(intent, "Damage", "CurrentDamage", "BaseDamage", "SingleDamage", "Amount", "_damage"));
	}

	private static void CancelPlayNode(Node node)
	{
		try
		{
			Traverse.Create(node).Method("CancelPlayCard").GetValue();
		}
		catch { }
	}

	public static void CancelPlacementTargeting()
	{
		GD.Print("[AliceMagatroid_Mod][PlacementTargeting] CancelPlacementTargeting called, targeting=", _placementTargeting);
		if (!_placementTargeting) return;
		_placementTargeting = false;
		_pendingCardPlayNode = null;
		PlacementTargetingChanged?.Invoke(false);
	}

	public static Vector2? GetPendingCardGlobalPosition()
	{
		return _frozenCardPosition;
	}
	// DEBUG: log position queries
	public static Vector2? GetPendingCardGlobalPositionWithLog()
	{
		GD.Print("[AliceMagatroid_Mod][PlacementTargeting] GetPendingCardGlobalPosition=", _frozenCardPosition);
		return _frozenCardPosition;
	}
}

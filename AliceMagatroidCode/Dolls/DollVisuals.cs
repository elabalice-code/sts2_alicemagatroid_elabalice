using Godot;
using MegaCrit.Sts2.Core.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AliceMagatroid_Mod.Dolls;

public partial class DollVisuals : Node2D
{
	public const string OverlayNodeName = "DollsOverlay";

	private enum AliceSkin
	{
		Skin1 = 1,
		Skin2 = 2,
		Skin3 = 3,
		Skin4 = 4
	}

	private sealed class DollDisplay(Node2D root, Sprite2D sprite, Control statsRoot, Panel healthShadow, Panel healthBackground, Panel healthFill, Label healthLabel, Panel blockBadge, Label blockLabel)
	{
		public Node2D Root { get; } = root;
		public Sprite2D Sprite { get; } = sprite;
		public Control StatsRoot { get; } = statsRoot;
		public Panel HealthShadow { get; } = healthShadow;
		public Panel HealthBackground { get; } = healthBackground;
		public Panel HealthFill { get; } = healthFill;
		public Label HealthLabel { get; } = healthLabel;
		public Panel BlockBadge { get; } = blockBadge;
		public Label BlockLabel { get; } = blockLabel;
	}

	private const string ConfigPath = "user://alice_magatroid.cfg";
	private const string ConfigSection = "appearance";
	private const string ConfigSkinKey = "selected_skin";
	private const string DollTexturePath = "res://AliceMagatroid/Images/Charui/doll_slot_magic_circle.png";
	private const string ShanghaiTexturePath = "res://AliceMagatroid/Images/Charui/shanghai_doll.png";
	private static readonly Vector2 Skin1CenterAnchor = GetSpritePosition(AliceSkin.Skin1, false);
	private static readonly Vector2 SlotScale = new(0.63f, 0.63f);
	private static readonly Vector2 DollScale = new(1.02f, 1.02f);
	private static readonly Vector2 DollOffset = new(0, -6);
	private static readonly Color EmptySlotColor = new(1f, 1f, 1f, 0.3f);
	private static readonly Color OccupiedSlotColor = new(1f, 1f, 1f, 0.7f);
	private static readonly Color DollColor = Colors.White;
	private static readonly Color ThreadBaseColor = new(0.9f, 0.95f, 1f, 0.14f);
	private static readonly Color ThreadSourceColor = new(0.97f, 0.99f, 1f, 0.28f);
	private static readonly Color ThreadFlashColor = new(0.98f, 0.99f, 1f, 0.95f);
	private const float SlotRadiusX = 358.4f;
	private const float SlotRadiusY = 328f;
	private const float CenterSlotAngleDegrees = 90f;
	private const float SlotAngleStepDegrees = 30f;
	private const int OtherVisualZIndex = 0;
	private const int SlotContainerZIndex = 1;
	private const int ThreadZIndex = 0;
	private const int ThreadFlashZIndex = 1;
	private const int SlotZIndex = 2;
	private const int DollZIndex = 3;
	private const float ThreadWidth = 1.8f;
	private const float ThreadSourceWidth = 2.8f;
	private const float ThreadFlashWidth = 4.2f;
	private const double ThreadFlashIntervalSeconds = 5.0;
	private const double ThreadFlashDurationSeconds = 0.42;
	private const double ThreadTensionDurationSeconds = 0.22;
	private const float ThreadTensionStraightenRatio = 0.15f;
	private const float ThreadTensionSourceWidthBoost = 1.2f;
	private const float ThreadTensionSourceAlphaBoost = 0.18f;
	private const float ThreadSourceSegmentRatio = 0.18f;
	private const float ThreadFlashSegmentRatio = 0.12f;
	private const float ThreadMotionAmplitude = 4.8f;
	private const float ThreadMotionSpeed = 1.35f;
	private const float SelectedSlotScaleMultiplier = 1.08f;
	private const float PopupRiseDistance = 32f;
	private const double PopupDurationSeconds = 1.15;
	private const int PopupZIndex = 5;
	private const int DragPreviewZIndex = 6;
	private const float DragPreviewWidth = 3.4f;
	private static readonly Vector2 DollStatsRootOffset = new(-52f, 46f);
	private static readonly Vector2 DollStatsBarSize = new(92f, 12f);
	private static readonly Vector2 DollHealthShadowOffset = new(2f, 2f);
	private static readonly Vector2 DollHealthTextOffset = new(-4f, -4f);
	private static readonly Vector2 DollBlockBadgeOffset = new(-22f, -2f);
	private static readonly Vector2 DollBlockBadgeSize = new(24f, 24f);
	private static readonly Color SelectedSlotColor = new(1f, 0.93f, 0.72f, 0.92f);
	private static readonly Color SelectedEmptySlotColor = new(1f, 0.95f, 0.8f, 0.55f);
	private static readonly Color DragPreviewColor = new(1f, 0.97f, 0.85f, 0.92f);
	private static readonly Color DollHealthBarShadowColor = new(0f, 0f, 0f, 0.48f);
	private static readonly Color DollHealthBarBackgroundColor = new(0.09f, 0.09f, 0.12f, 0.92f);
	private static readonly Color DollHealthBarFillColor = new(0.86f, 0.19f, 0.24f, 0.98f);
	private static readonly Color DollHealthTextColor = new(1f, 1f, 1f, 0.96f);
	private static readonly Color DollBlockBadgeColor = new(0.3f, 0.58f, 0.96f, 0.98f);
	private static readonly Color DollBlockBadgeOutlineColor = new(0.78f, 0.9f, 1f, 0.98f);
	private static readonly Color DollBlockTextColor = new(1f, 1f, 1f, 0.98f);
	private static readonly Vector2[] SlotPositions = BuildSlotPositions();

	private static AliceSkin _selectedSkin = AliceSkin.Skin1;
	private static bool _skinLoaded;
	private static int _pendingThreadTensionRequests;

	[Export] public NodePath DollsRootPath { get; set; } = new();
	private Node2D? _dollsRoot;
	private Node? _visualRoot;
	private Sprite2D[] _slots = [];
	private DollDisplay[] _dolls = [];
	private Line2D[] _threadLines = [];
	private Line2D[] _threadSourceLines = [];
	private Line2D[] _threadFlashLines = [];
	private AnimatedSprite2D? _animatedSprite;
	private Control? _skinSelector;
	private Button? _skin1Button;
	private Button? _skin2Button;
	private Button? _skin3Button;
	private Button? _skin4Button;
	private bool? _overlayVisibleInCombat;
	private double _threadFlashElapsed;
	private double _threadMotionElapsed;
	private double _threadTensionRemaining;
	private RichTextLabel[] _resolutionLabels = [];
	private Vector2[] _resolutionBasePositions = [];
	private double[] _resolutionTimers = [];
	private Label? _aliceSelectionMarker;
	private Line2D? _dragPreviewLine;
	private Line2D?[] _targetingThreads = [];
	private Line2D? _targetingSourceCircle;
	private Line2D? _targetingMouseCircle;
	private Line2D?[] _targetingSourceHexagram = [];
	private Line2D?[] _targetingMouseHexagram = [];
	private RichTextLabel[] _intentLabels = [];
	private RichTextLabel? _aliceOverflowLabel;
	private bool _targetingThreadActive;
	private const int TargetingThreadCount = 6;
	private const float TargetingSourceRadius = 36f;
	private const float TargetingMouseRadius = 18f;
	private static readonly Color TargetingThreadColor = new(0.85f, 0.92f, 1f, 0.6f);


	public static DollVisuals? AttachTo(Node root)
	{
		if (root == null)
		{
			return null;
		}

		var existing = FindOverlay(root);
		if (existing != null)
		{
			return existing;
		}

		var attachRoot = ResolveAttachRoot(root);
		if (attachRoot == null)
		{
			return null;
		}

		existing = FindOverlay(attachRoot);
		if (existing != null)
		{
			return existing;
		}

		var overlay = new DollVisuals
		{
			Name = OverlayNodeName
		};
		attachRoot.AddChild(overlay);
		Log.Info($"[AliceMagatroid_Mod][DollSlots] attached overlay to {attachRoot.Name}:{attachRoot.GetType().Name}", 2);
		return overlay;
	}

	private static DollVisuals? FindOverlay(Node root)
	{
		return root.GetNodeOrNull<DollVisuals>(OverlayNodeName) ?? root.FindChild(OverlayNodeName, true, false) as DollVisuals;
	}

	private static Node? ResolveAttachRoot(Node root)
	{
		var animatedSprite = root.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D") ?? root.FindChild("AnimatedSprite2D", true, false) as AnimatedSprite2D;
		return animatedSprite?.GetParent();
	}

	public override void _Ready()
	{
		_visualRoot = GetParent();
		_animatedSprite = _visualRoot?.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_skinSelector = _visualRoot?.GetNodeOrNull<Control>("SkinSelector");
		_skin1Button = _visualRoot?.GetNodeOrNull<Button>("SkinSelector/Skin1");
		_skin2Button = _visualRoot?.GetNodeOrNull<Button>("SkinSelector/Skin2");
		_skin3Button = _visualRoot?.GetNodeOrNull<Button>("SkinSelector/Skin3");
		_skin4Button = _visualRoot?.GetNodeOrNull<Button>("SkinSelector/Skin4");

		WireSkinButtons();
		UpdateSkinSelectorVisibility();
		ApplyCurrentSkin();

		_dollsRoot = !DollsRootPath.IsEmpty ? GetNodeOrNull<Node2D>(DollsRootPath) : this;

		if (_dollsRoot != null)
		{
			ConfigureSlots();
		}

		DollState.RecycleAll();
		DollState.SlotsChanged += OnSlotsChanged;
		DollState.StatsChanged += OnStatsChanged;
		DollState.SelectionChanged += OnSelectionChanged;
		DollState.ResolutionPopupRequested += OnResolutionPopupRequested;
		DollState.IntentPreviewsChanged += OnIntentPreviewsChanged;
		OnSlotsChanged(DollState.Slots);
		OnStatsChanged(DollState.Snapshots);
		SetProcess(true);
		SetProcessUnhandledInput(true);
		UpdateOverlayVisibility(true);
	}

	public override void _ExitTree()
	{
		DollState.SlotsChanged -= OnSlotsChanged;
		DollState.StatsChanged -= OnStatsChanged;
		DollState.SelectionChanged -= OnSelectionChanged;
		DollState.ResolutionPopupRequested -= OnResolutionPopupRequested;
		DollState.IntentPreviewsChanged -= OnIntentPreviewsChanged;
	}

	public override void _Process(double delta)
	{
		UpdateOverlayVisibility();
		UpdateDragSelection();
		ConsumePendingThreadTensionRequest();
		UpdateThreadFlash(delta);
		UpdateResolutionPopups(delta);
	}

	private void OnIntentPreviewsChanged(IReadOnlyList<DollIntentPreview> previews)
	{
		foreach (var label in _intentLabels)
		{
			if (label != null) label.Visible = false;
		}
		if (_aliceOverflowLabel != null) _aliceOverflowLabel.Visible = false;

		foreach (var preview in previews)
		{
			if (preview.SlotIndex < 0)
			{
				if (_aliceOverflowLabel != null && preview.TotalDamage > 0)
				{
					_aliceOverflowLabel.Text = $"[center][color=#ff3333]{preview.TotalDamage}[/color][/center]";
					_aliceOverflowLabel.Visible = true;
				}
				continue;
			}
			if (preview.SlotIndex >= _intentLabels.Length) continue;
			var lbl = _intentLabels[preview.SlotIndex];
			if (lbl == null) continue;

			var color = preview.OverflowDamage > 0 ? "#ff4444" : "#ff9999";
			var text = $"[center][color={color}]{preview.TotalDamage}[/color][/center]";
			lbl.Text = text;
			lbl.Visible = true;
		}
	}

	private RichTextLabel CreateIntentLabel(string name)
	{
		var label = new RichTextLabel();
		label.Name = name;
		label.BbcodeEnabled = true;
		label.FitContent = true;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.Size = new Vector2(180, 48);
		label.ZIndex = 4;
		_dollsRoot.AddChild(label);
		return label;
	}

public override void _UnhandledInput(InputEvent @event)
	{
		if (_overlayVisibleInCombat != true || _dollsRoot == null)
		{
			return;
		}

		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton)
		{
			return;
		}

		if (DollState.IsPlacementTargeting)
		{
			return;
		}

		var localPosition = _dollsRoot.ToLocal(mouseButton.GlobalPosition);
		for (var i = 0; i < SlotPositions.Length; i++)
		{
			if (localPosition.DistanceTo(SlotPositions[i]) <= 84f)
			{
				DollState.SelectSlot(i);
				return;
			}
		}

		if (localPosition.DistanceTo(Vector2.Zero) <= 124f)
		{
			DollState.SelectAlice();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }
			&& (_targetingThreadActive || DollState.IsPlacementTargeting))
		{
			GD.Print("[AliceMagatroid_Mod][Visuals] Right-click cancel, placement=", DollState.IsPlacementTargeting);
			SetTargetingThreadsVisible(false);
			DollState.CancelPlacementTargeting();
			return;
		}

		if (DollState.IsPlacementTargeting
			&& @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseBtn
			&& _dollsRoot != null)
		{
			var localPos = _dollsRoot.ToLocal(mouseBtn.GlobalPosition);
			GD.Print("[AliceMagatroid_Mod][Visuals] _Input left click during placement, mouse=", mouseBtn.GlobalPosition, " localPos=", localPos);
			for (var i = 0; i < SlotPositions.Length; i++)
			{
				if (localPos.DistanceTo(SlotPositions[i]) <= 84f)
				{
					GD.Print("[AliceMagatroid_Mod][Visuals] Hit slot ", i, " empty=", DollState.IsSlotEmpty(i));
					if (DollState.IsSlotEmpty(i))
					{
						DollState.ConfirmPlacementTargeting(i);
						GetViewport().SetInputAsHandled();
					}
					return;
				}
			}
		}
	}
	public void PrepareAnimation(string animationName)
	{
		if (_animatedSprite == null)
		{
			return;
		}

		ApplyCurrentSkin(animationName);
	}

	public AnimatedSprite2D? GetAnimatedSprite()
	{
		return _animatedSprite;
	}

	public void TriggerThreadTension()
	{
		_threadTensionRemaining = ThreadTensionDurationSeconds;
	}

	public static void RequestThreadTension()
	{
		_pendingThreadTensionRequests++;
	}

	private void WireSkinButtons()
	{
		if (_skin1Button != null)
		{
			_skin1Button.Pressed += () => SelectSkin(AliceSkin.Skin1);
		}

		if (_skin2Button != null)
		{
			_skin2Button.Pressed += () => SelectSkin(AliceSkin.Skin2);
		}

		if (_skin3Button != null)
		{
			_skin3Button.Pressed += () => SelectSkin(AliceSkin.Skin3);
		}

		if (_skin4Button != null)
		{
			_skin4Button.Pressed += () => SelectSkin(AliceSkin.Skin4);
		}
	}

	private void SelectSkin(AliceSkin skin)
	{
		SetSelectedSkin(skin);
		ApplyCurrentSkin();
	}

	private void ApplyCurrentSkin(string? animationName = null)
	{
		if (_animatedSprite == null)
		{
			return;
		}

		var skin = GetSelectedSkin();
		var frames = new SpriteFrames();
		var liveTexturePath = GetLiveTexturePath(skin);
		var deadTexturePath = GetDeadTexturePath(skin, animationName == "Dead");
		var liveTexture = LoadTexture(liveTexturePath);
		var deadTexture = LoadTexture(deadTexturePath);

		if (liveTexture == null || deadTexture == null)
		{
			Log.Info($"[AliceMagatroid_Mod][CombatVisual] skin={skin} animation={animationName ?? "Idle"} livePath={liveTexturePath} liveExists={ResourceLoader.Exists(liveTexturePath)} deadPath={deadTexturePath} deadExists={ResourceLoader.Exists(deadTexturePath)}", 2);
			return;
		}

		Log.Info($"[AliceMagatroid_Mod][CombatVisual] skin={skin} animation={animationName ?? "Idle"} livePath={liveTexturePath} liveSize={liveTexture.GetSize()} deadPath={deadTexturePath} deadSize={deadTexture.GetSize()}", 2);

		AddSingleFrameAnimation(frames, "Idle", liveTexture, true);
		AddSingleFrameAnimation(frames, "Attack", liveTexture, true);
		AddSingleFrameAnimation(frames, "Hit", liveTexture, false);
		AddSingleFrameAnimation(frames, "Dead", deadTexture, false);

		_animatedSprite.SpriteFrames = frames;
		_animatedSprite.Position = GetSpritePosition(skin, animationName == "Dead");
		_animatedSprite.Scale = GetSpriteScale(skin, animationName == "Dead");
		_animatedSprite.ZAsRelative = false;
		_animatedSprite.ZIndex = OtherVisualZIndex;
		UpdateDollRootPosition();

		if (animationName == null)
		{
			_animatedSprite.Animation = "Idle";
			_animatedSprite.Play("Idle");
		}

		UpdateSkinButtons();
	}

	private void UpdateSkinButtons()
	{
		var selected = GetSelectedSkin();
		UpdateSkinButton(_skin1Button, selected == AliceSkin.Skin1);
		UpdateSkinButton(_skin2Button, selected == AliceSkin.Skin2);
		UpdateSkinButton(_skin3Button, selected == AliceSkin.Skin3);
		UpdateSkinButton(_skin4Button, selected == AliceSkin.Skin4);
	}

	private static void UpdateSkinButton(Button? button, bool selected)
	{
		if (button == null)
		{
			return;
		}

		button.Disabled = selected;
		button.Modulate = selected ? new Color("ffd166") : Colors.White;
	}

	private void UpdateSkinSelectorVisibility()
	{
		if (_skinSelector == null)
		{
			return;
		}

		_skinSelector.Visible = ShouldShowSkinSelector();
	}

	private bool ShouldShowSkinSelector()
	{
		for (Node? current = this; current != null; current = current.GetParent())
		{
			var nodeName = current.Name.ToString();
			var typeName = current.GetType().Name;
			if (ContainsIgnoreCase(nodeName, "Combat") || ContainsIgnoreCase(typeName, "Combat"))
			{
				return false;
			}

			if (ContainsIgnoreCase(nodeName, "CharSelect") ||
				ContainsIgnoreCase(nodeName, "CharacterSelect") ||
				ContainsIgnoreCase(typeName, "CharSelect") ||
				ContainsIgnoreCase(typeName, "CharacterSelect") ||
				ContainsIgnoreCase(nodeName, "SelectScreen"))
			{
				return true;
			}
		}

		return true;
	}

	private void UpdateOverlayVisibility(bool forceLog = false)
	{
		if (_dollsRoot == null)
		{
			return;
		}

		var visibleInCombat = IsInCombatContext();
		_dollsRoot.Visible = visibleInCombat;

		if (!forceLog && _overlayVisibleInCombat == visibleInCombat)
		{
			return;
		}

		_overlayVisibleInCombat = visibleInCombat;
		var currentSceneName = GetTree()?.CurrentScene?.Name.ToString() ?? "<null>";
		Log.Info($"[AliceMagatroid_Mod][DollSlots] combatVisibility={visibleInCombat} scene={currentSceneName} host={_visualRoot?.Name}:{_visualRoot?.GetType().Name}", 2);
	}

	private void ConsumePendingThreadTensionRequest()
	{
		if (_pendingThreadTensionRequests <= 0 || _overlayVisibleInCombat != true)
		{
			return;
		}

		_pendingThreadTensionRequests = 0;
		TriggerThreadTension();
	}

	private bool IsInCombatContext()
	{
		for (Node? current = this; current != null; current = current.GetParent())
		{
			if (HasCombatMarker(current))
			{
				return true;
			}
		}

		var currentScene = GetTree()?.CurrentScene;
		return currentScene != null && HasCombatMarker(currentScene);
	}

	private static bool HasCombatMarker(Node node)
	{
		var nodeName = node.Name.ToString();
		var typeName = node.GetType().Name;
		var typeNamespace = node.GetType().Namespace ?? string.Empty;
		return ContainsIgnoreCase(nodeName, "Combat")
			|| ContainsIgnoreCase(typeName, "Combat")
			|| ContainsIgnoreCase(typeNamespace, "Combat");
	}

	private static bool ContainsIgnoreCase(string text, string value)
	{
		return text.Contains(value, StringComparison.OrdinalIgnoreCase);
	}

	private static Texture2D? LoadTexture(string path)
	{
		return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
	}

	private static void AddSingleFrameAnimation(SpriteFrames frames, string animationName, Texture2D texture, bool loop)
	{
		frames.AddAnimation(animationName);
		frames.SetAnimationLoop(animationName, loop);
		frames.Clear(animationName);
		frames.SetAnimationLoop(animationName, loop);
		frames.AddFrame(animationName, texture);
	}

	private void ConfigureSlots()
	{
		if (_dollsRoot == null)
		{
			return;
		}

		var slotTexture = LoadTexture(DollTexturePath);
		if (slotTexture == null)
		{
			return;
		}

		var shanghaiTexture = LoadTexture(ShanghaiTexturePath);
		if (shanghaiTexture == null)
		{
			return;
		}

		_dollsRoot.ZAsRelative = true;
		_dollsRoot.ZIndex = SlotContainerZIndex;
		UpdateDollRootPosition();
		_slots = new Sprite2D[DollState.MaxSlots];
		_dolls = new DollDisplay[DollState.MaxSlots];
		_threadLines = new Line2D[DollState.MaxSlots];
		_threadSourceLines = new Line2D[DollState.MaxSlots];
		_threadFlashLines = new Line2D[DollState.MaxSlots];
		_resolutionLabels = new RichTextLabel[DollState.MaxSlots + 1];
		_resolutionBasePositions = new Vector2[DollState.MaxSlots + 1];
		_resolutionTimers = new double[DollState.MaxSlots + 1];
		_dragPreviewLine = _dollsRoot.GetNodeOrNull<Line2D>("DragPreview") ?? CreateDragPreviewLine();
		_targetingThreads = new Line2D?[TargetingThreadCount];
		for (var t = 0; t < TargetingThreadCount; t++)
		{
			_targetingThreads[t] = _dollsRoot.GetNodeOrNull<Line2D>($"TargetingThread{t}") ?? CreateTargetingThread(t);
		}
		_targetingSourceCircle = _dollsRoot.GetNodeOrNull<Line2D>("TargetingSourceCircle") ?? CreateTargetingCircle("TargetingSourceCircle");
		_targetingMouseCircle = _dollsRoot.GetNodeOrNull<Line2D>("TargetingMouseCircle") ?? CreateTargetingCircle("TargetingMouseCircle");
		_targetingSourceHexagram = new Line2D?[2];
		_targetingMouseHexagram = new Line2D?[2];
		for (var h = 0; h < 2; h++)
		{
			_targetingSourceHexagram[h] = _dollsRoot.GetNodeOrNull<Line2D>($"SourceHex{h}") ?? CreateTargetingCircle($"SourceHex{h}");
			_targetingMouseHexagram[h] = _dollsRoot.GetNodeOrNull<Line2D>($"MouseHex{h}") ?? CreateTargetingCircle($"MouseHex{h}");
		}
		_intentLabels = new RichTextLabel[DollState.MaxSlots];
		for (var il = 0; il < DollState.MaxSlots; il++)
		{
			_intentLabels[il] = CreateIntentLabel($"IntentPreview{il}");
			_intentLabels[il].Position = SlotPositions[il] + DollOffset + new Vector2(0, -120);
			_intentLabels[il].Visible = false;
		}
		_aliceOverflowLabel = CreateIntentLabel("IntentOverflowAlice");
		_aliceOverflowLabel.Position = new Vector2(0, -320);
		_aliceOverflowLabel.Visible = false;
		for (var i = 0; i < DollState.MaxSlots; i++)
		{
			var thread = _dollsRoot.GetNodeOrNull<Line2D>($"Thread{i + 1}") ?? CreateThreadLine($"Thread{i + 1}");
			thread.ClearPoints();
			thread.AddPoint(Vector2.Zero);
			thread.AddPoint(SlotPositions[i] + DollOffset);
			thread.Visible = false;
			thread.DefaultColor = ThreadBaseColor;
			thread.Width = ThreadWidth;
			thread.ZAsRelative = true;
			thread.ZIndex = ThreadZIndex;
			thread.BeginCapMode = Line2D.LineCapMode.Round;
			thread.EndCapMode = Line2D.LineCapMode.Round;
			_threadLines[i] = thread;

			var threadSource = _dollsRoot.GetNodeOrNull<Line2D>($"ThreadSource{i + 1}") ?? CreateThreadLine($"ThreadSource{i + 1}");
			threadSource.ClearPoints();
			threadSource.AddPoint(Vector2.Zero);
			threadSource.AddPoint((SlotPositions[i] + DollOffset) * ThreadSourceSegmentRatio);
			threadSource.Visible = false;
			threadSource.DefaultColor = ThreadSourceColor;
			threadSource.Width = ThreadSourceWidth;
			threadSource.ZAsRelative = true;
			threadSource.ZIndex = ThreadFlashZIndex;
			threadSource.BeginCapMode = Line2D.LineCapMode.Round;
			threadSource.EndCapMode = Line2D.LineCapMode.Round;
			_threadSourceLines[i] = threadSource;

			var threadFlash = _dollsRoot.GetNodeOrNull<Line2D>($"ThreadFlash{i + 1}") ?? CreateThreadLine($"ThreadFlash{i + 1}");
			threadFlash.ClearPoints();
			threadFlash.AddPoint(Vector2.Zero);
			threadFlash.AddPoint(Vector2.Zero);
			threadFlash.Visible = false;
			threadFlash.DefaultColor = ThreadFlashColor;
			threadFlash.Width = ThreadFlashWidth;
			threadFlash.ZAsRelative = true;
			threadFlash.ZIndex = ThreadFlashZIndex;
			threadFlash.BeginCapMode = Line2D.LineCapMode.Round;
			threadFlash.EndCapMode = Line2D.LineCapMode.Round;
			_threadFlashLines[i] = threadFlash;

			var slot = _dollsRoot.GetNodeOrNull<Sprite2D>($"Slot{i + 1}") ?? CreateSlot(i + 1, slotTexture);
			slot.Texture = slotTexture;
			slot.Position = SlotPositions[i];
			slot.Scale = SlotScale;
			slot.Visible = true;
			slot.Centered = true;
			slot.ZAsRelative = true;
			slot.ZIndex = SlotZIndex;
			slot.Modulate = EmptySlotColor;
			_slots[i] = slot;

			var doll = _dolls[i] ?? CreateDollDisplay(i + 1, shanghaiTexture, slot.Position + DollOffset);
			doll.Sprite.Texture = shanghaiTexture;
			doll.Root.Position = slot.Position + DollOffset;
			doll.Root.Visible = false;
			doll.Sprite.Scale = DollScale;
			doll.Sprite.Visible = false;
			doll.Sprite.Centered = true;
			doll.Sprite.ZAsRelative = true;
			doll.Sprite.ZIndex = DollZIndex;
			doll.Sprite.Modulate = DollColor;
			_dolls[i] = doll;

			var popupLabel = _dollsRoot.GetNodeOrNull<RichTextLabel>($"Popup{i + 1}") ?? CreatePopupLabel($"Popup{i + 1}");
			popupLabel.Position = doll.Root.Position + new Vector2(-76, -108);
			popupLabel.ZIndex = PopupZIndex;
			popupLabel.Visible = false;
			_resolutionLabels[i] = popupLabel;
			_resolutionBasePositions[i] = popupLabel.Position;
		}

		var alicePopup = _dollsRoot.GetNodeOrNull<RichTextLabel>("PopupAlice") ?? CreatePopupLabel("PopupAlice");
		alicePopup.Position = new Vector2(-92, -322);
		alicePopup.ZIndex = PopupZIndex;
		alicePopup.Visible = false;
		_resolutionLabels[DollState.MaxSlots] = alicePopup;
		_resolutionBasePositions[DollState.MaxSlots] = alicePopup.Position;

		_aliceSelectionMarker = _dollsRoot.GetNodeOrNull<Label>("AliceSelection") ?? CreateAliceSelectionMarker();
		_aliceSelectionMarker.Position = new Vector2(-16, -262);
		_aliceSelectionMarker.Visible = false;

		Log.Info($"[AliceMagatroid_Mod][DollSlots] slotLayer ready rootZ={_dollsRoot.ZIndex} slotZ={SlotZIndex} dollZ={DollZIndex}", 2);
		GD.Print($"[TestActor][AliceMagatroid_Mod][DollSlots] GenerateSlots count={DollState.MaxSlots} radius=({SlotRadiusX}, {SlotRadiusY}) anchor={_dollsRoot.Position} positions={string.Join(";", Array.ConvertAll(_slots, slot => slot?.Position.ToString() ?? "null"))}");
		Log.Info($"[TestActor][AliceMagatroid_Mod][DollSlots] Formula centerIndex={DollState.MaxSlots / 2} centerAngleDeg={CenterSlotAngleDegrees} stepAngleDeg={SlotAngleStepDegrees} radiusX={SlotRadiusX} radiusY={SlotRadiusY}", 2);
		Log.Info("[TestActor][AliceMagatroid_Mod][DollSlots] Formula angleDeg(i)=centerAngleDeg + (i - centerIndex) * stepAngleDeg", 2);
		Log.Info("[TestActor][AliceMagatroid_Mod][DollSlots] Formula x(i)=cos(angleDeg(i) * PI / 180) * radiusX ; y(i)=-sin(angleDeg(i) * PI / 180) * radiusY ; finalPosition(i)=anchor + (x(i), y(i))", 2);
		for (var i = 0; i < SlotPositions.Length; i++)
		{
			var centerIndex = DollState.MaxSlots / 2;
			var angleDegrees = CenterSlotAngleDegrees + (i - centerIndex) * SlotAngleStepDegrees;
			var radians = Mathf.DegToRad(angleDegrees);
			var x = Mathf.Cos(radians) * SlotRadiusX;
			var y = -Mathf.Sin(radians) * SlotRadiusY;
			var localPosition = SlotPositions[i];
			var worldPosition = _dollsRoot.Position + localPosition;
			Log.Info($"[TestActor][AliceMagatroid_Mod][DollSlots] Slot[{i}] angleDeg={angleDegrees} angleRad={radians} local=({x}, {y}) cached={localPosition} final={worldPosition}", 2);
		}

		UpdateSelectionVisuals();
	}

	private Line2D CreateThreadLine(string lineName)
	{
		if (_dollsRoot == null)
		{
			return new Line2D();
		}

		var line = new Line2D
		{
			Name = lineName,
			Visible = false
		};

		_dollsRoot.AddChild(line);
		return line;
	}

	private Line2D CreateTargetingThread(int index)
	{
		if (_dollsRoot == null) return new Line2D();
		var line = new Line2D
		{
			Name = $"TargetingThread{index}",
			Visible = false,
			ZAsRelative = true,
			ZIndex = SlotZIndex + 1,
			Width = 1.6f,
			DefaultColor = TargetingThreadColor,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round
		};
		_dollsRoot.AddChild(line);
		return line;
	}

	private Line2D CreateTargetingCircle(string name)
	{
		if (_dollsRoot == null) return new Line2D();
		var line = new Line2D
		{
			Name = name,
			Visible = false,
			ZAsRelative = true,
			ZIndex = SlotZIndex + 1,
			Width = 1.2f,
			DefaultColor = TargetingThreadColor,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round
		};
		_dollsRoot.AddChild(line);
		return line;
	}

	private static void UpdateCircle(Line2D line, Vector2 center, float radius, Color color, float width, int segments = 32)
	{
		line.ClearPoints();
		for (var i = 0; i <= segments; i++)
		{
			var angle = Mathf.Pi * 2f * i / segments;
			line.AddPoint(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
		}
		line.DefaultColor = color;
		line.Width = width;
		line.Visible = true;
	}

	private static void UpdateHexagram(Line2D?[] lines, Vector2 center, float radius, Color color, float width)
	{
		if (lines.Length < 2) return;
		var vertices = new Vector2[6];
		for (var i = 0; i < 6; i++)
		{
			var angle = (Mathf.Pi * 2f * i / 6f) - Mathf.Pi / 2f;
			vertices[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}
		if (lines[0] != null)
		{
			UpdateThreadLinePoints(lines[0], [vertices[0], vertices[2], vertices[4], vertices[0]]);
			lines[0].DefaultColor = color;
			lines[0].Width = width;
			lines[0].Visible = true;
		}
		if (lines[1] != null)
		{
			UpdateThreadLinePoints(lines[1], [vertices[1], vertices[3], vertices[5], vertices[1]]);
			lines[1].DefaultColor = color;
			lines[1].Width = width;
			lines[1].Visible = true;
		}
	}

	private Sprite2D CreateSlot(int slotNumber, Texture2D slotTexture)
	{
		if (_dollsRoot == null)
		{
			return new Sprite2D();
		}

		var slot = new Sprite2D
		{
			Name = $"Slot{slotNumber}",
			Texture = slotTexture,
			Visible = true,
			Centered = true
		};

		_dollsRoot.AddChild(slot);
		return slot;
	}

	private DollDisplay CreateDollDisplay(int slotNumber, Texture2D dollTexture, Vector2 position)
	{
		if (_dollsRoot == null)
		{
			return new DollDisplay(new Node2D(), new Sprite2D(), new Control(), new Panel(), new Panel(), new Panel(), new Label(), new Panel(), new Label());
		}

		var root = new Node2D
		{
			Name = $"DollDisplay{slotNumber}",
			Position = position,
			Visible = false,
			ZAsRelative = true,
			ZIndex = DollZIndex
		};

		var doll = new Sprite2D
		{
			Name = $"Doll{slotNumber}",
			Texture = dollTexture,
			Visible = false,
			Centered = true,
			ZAsRelative = true,
			ZIndex = DollZIndex
		};

		var statsRoot = new Control
		{
			Name = $"DollStats{slotNumber}",
			Position = DollStatsRootOffset,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Size = new Vector2(108, 30)
		};

		var healthShadow = new Panel
		{
			Name = $"DollHealthShadow{slotNumber}",
			Position = DollHealthShadowOffset,
			Size = DollStatsBarSize,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		healthShadow.AddThemeStyleboxOverride("panel", CreateFilledStyleBox(DollHealthBarShadowColor, 6));

		var healthBackground = new Panel
		{
			Name = $"DollHealthBg{slotNumber}",
			Size = DollStatsBarSize,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		healthBackground.AddThemeStyleboxOverride("panel", CreateFilledStyleBox(DollHealthBarBackgroundColor, 6));

		var healthFill = new Panel
		{
			Name = $"DollHealthFill{slotNumber}",
			Size = DollStatsBarSize,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		healthFill.AddThemeStyleboxOverride("panel", CreateFilledStyleBox(DollHealthBarFillColor, 6));

		var healthLabel = new Label
		{
			Name = $"DollHealthLabel{slotNumber}",
			Position = DollHealthTextOffset,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Size = new Vector2(92, 20),
			Text = "0/0"
		};
		healthLabel.AddThemeFontSizeOverride("font_size", 15);
		healthLabel.AddThemeColorOverride("font_color", DollHealthTextColor);

		var blockBadge = new Panel
		{
			Name = $"DollBlockBadge{slotNumber}",
			Position = DollBlockBadgeOffset,
			Size = DollBlockBadgeSize,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		blockBadge.AddThemeStyleboxOverride("panel", CreateBadgeStyleBox(DollBlockBadgeColor, DollBlockBadgeOutlineColor, 2));

		var blockLabel = new Label
		{
			Name = $"DollBlockLabel{slotNumber}",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Size = DollBlockBadgeSize,
			Text = "0"
		};
		blockLabel.AddThemeFontSizeOverride("font_size", 15);
		blockLabel.AddThemeColorOverride("font_color", DollBlockTextColor);

		root.AddChild(doll);
		statsRoot.AddChild(healthShadow);
		statsRoot.AddChild(healthBackground);
		statsRoot.AddChild(healthFill);
		statsRoot.AddChild(healthLabel);
		blockBadge.AddChild(blockLabel);
		statsRoot.AddChild(blockBadge);
		root.AddChild(statsRoot);
		_dollsRoot.AddChild(root);
		return new DollDisplay(root, doll, statsRoot, healthShadow, healthBackground, healthFill, healthLabel, blockBadge, blockLabel);
	}

	private RichTextLabel CreatePopupLabel(string labelName)
	{
		if (_dollsRoot == null)
		{
			return new RichTextLabel();
		}

		var label = new RichTextLabel
		{
			Name = labelName,
			BbcodeEnabled = true,
			FitContent = true,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false,
			Size = new Vector2(180, 48)
		};
		label.AddThemeFontSizeOverride("normal_font_size", 24);
		_dollsRoot.AddChild(label);
		return label;
	}

	private Label CreateAliceSelectionMarker()
	{
		if (_dollsRoot == null)
		{
			return new Label();
		}

		var label = new Label
		{
			Name = "AliceSelection",
			Text = "◈",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = PopupZIndex
		};
		label.AddThemeFontSizeOverride("font_size", 30);
		label.AddThemeColorOverride("font_color", new Color("ffd166"));
		_dollsRoot.AddChild(label);
		return label;
	}

	private Line2D CreateDragPreviewLine()
	{
		if (_dollsRoot == null)
		{
			return new Line2D();
		}

		var line = new Line2D
		{
			Name = "DragPreview",
			Visible = false,
			ZAsRelative = true,
			ZIndex = DragPreviewZIndex,
			Width = DragPreviewWidth,
			DefaultColor = DragPreviewColor,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round
		};
		_dollsRoot.AddChild(line);
		return line;
	}

	private static StyleBoxFlat CreateFilledStyleBox(Color color, int cornerRadius, int borderWidth = 0, Color? borderColor = null)
	{
		var styleBox = new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = cornerRadius,
			CornerRadiusTopRight = cornerRadius,
			CornerRadiusBottomRight = cornerRadius,
			CornerRadiusBottomLeft = cornerRadius,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			BorderColor = borderColor ?? color
		};
		return styleBox;
	}

	private static StyleBoxFlat CreateBadgeStyleBox(Color backgroundColor, Color borderColor, int borderWidth)
	{
		return CreateFilledStyleBox(backgroundColor, 999, borderWidth, borderColor);
	}

	private static Vector2[] BuildSlotPositions()
	{
		var positions = new Vector2[DollState.MaxSlots];
		var centerIndex = DollState.MaxSlots / 2;
		for (var i = 0; i < DollState.MaxSlots; i++)
		{
			var angleDegrees = CenterSlotAngleDegrees + (i - centerIndex) * SlotAngleStepDegrees;
			var radians = Mathf.DegToRad(angleDegrees);
			positions[i] = new Vector2(
				Mathf.Cos(radians) * SlotRadiusX,
				-Mathf.Sin(radians) * SlotRadiusY);
		}

		return positions;
	}

	private void UpdateDollRootPosition()
	{
		if (_dollsRoot == null)
		{
			return;
		}

		_dollsRoot.Position = Skin1CenterAnchor;
	}

	private static string GetLiveTexturePath(AliceSkin skin) => skin switch
	{
		AliceSkin.Skin1 => "res://AliceMagatroid/Images/Visual/skin1_live.png",
		AliceSkin.Skin2 => "res://AliceMagatroid/Images/Visual/skin2_live.png",
		AliceSkin.Skin3 => "res://AliceMagatroid/Images/Visual/skin3_live.png",
		AliceSkin.Skin4 => "res://AliceMagatroid/Images/Visual/skin3_live.png",
		_ => "res://AliceMagatroid/Images/Visual/skin1_live.png"
	};

	private static string GetDeadTexturePath(AliceSkin skin, bool useRandomForSkin3) => skin switch
	{
		AliceSkin.Skin1 => "res://AliceMagatroid/Images/Visual/skin1_dead.png",
		AliceSkin.Skin2 => "res://AliceMagatroid/Images/Visual/skin2_dead.png",
		AliceSkin.Skin3 => useRandomForSkin3 ? GetRandomSkin3DeadTexturePath() : "res://AliceMagatroid/Images/Visual/skin3_dead_1.png",
		AliceSkin.Skin4 => useRandomForSkin3 ? GetRandomSkin3DeadTexturePath() : "res://AliceMagatroid/Images/Visual/skin3_dead_1.png",
		_ => "res://AliceMagatroid/Images/Visual/skin1_dead.png"
	};

	private static string GetRandomSkin3DeadTexturePath()
	{
		var roll = Random.Shared.Next(0, 3);
		return roll switch
		{
			0 => "res://AliceMagatroid/Images/Visual/skin3_dead_1.png",
			1 => "res://AliceMagatroid/Images/Visual/skin3_dead_2.png",
			_ => "res://AliceMagatroid/Images/Visual/skin3_dead_3.png"
		};
	}

	private static Vector2 GetSpritePosition(AliceSkin skin, bool isDead) => (skin, isDead) switch
	{
		(AliceSkin.Skin1, false) => new Vector2(0, -168),
		(AliceSkin.Skin1, true) => new Vector2(0, -150),
		(AliceSkin.Skin2, false) => new Vector2(0, -150),
		(AliceSkin.Skin2, true) => new Vector2(0, -135),
		(AliceSkin.Skin3, false) => new Vector2(0, -155),
		(AliceSkin.Skin3, true) => new Vector2(0, -155),
		(AliceSkin.Skin4, false) => new Vector2(0, -155),
		(AliceSkin.Skin4, true) => new Vector2(0, -155),
		_ => new Vector2(0, -168)
	};

	private static Vector2 GetSpriteScale(AliceSkin skin, bool isDead) => (skin, isDead) switch
	{
		(AliceSkin.Skin1, false) => new Vector2(0.46f, 0.46f),
		(AliceSkin.Skin1, true) => new Vector2(0.46f, 0.46f),
		(AliceSkin.Skin2, false) => new Vector2(0.62f, 0.62f),
		(AliceSkin.Skin2, true) => new Vector2(0.25f, 0.25f),
		(AliceSkin.Skin3, false) => new Vector2(0.54f, 0.54f),
		(AliceSkin.Skin3, true) => new Vector2(0.54f, 0.54f),
		(AliceSkin.Skin4, false) => new Vector2(0.54f, 0.54f),
		(AliceSkin.Skin4, true) => new Vector2(0.54f, 0.54f),
		_ => new Vector2(0.46f, 0.46f)
	};

	private static AliceSkin GetSelectedSkin()
	{
		if (_skinLoaded)
		{
			return _selectedSkin;
		}

		var config = new ConfigFile();
		if (config.Load(ConfigPath) == Error.Ok)
		{
			var savedValue = config.GetValue(ConfigSection, ConfigSkinKey, (int)AliceSkin.Skin1).ToString();
			if (int.TryParse(savedValue, out var parsed))
			{
				_selectedSkin = SanitizeSkin(parsed);
			}
		}

		_skinLoaded = true;
		return _selectedSkin;
	}

	private static void SetSelectedSkin(AliceSkin skin)
	{
		_selectedSkin = skin;
		_skinLoaded = true;

		var config = new ConfigFile();
		config.Load(ConfigPath);
		config.SetValue(ConfigSection, ConfigSkinKey, (int)skin);
		config.Save(ConfigPath);
	}

	private static AliceSkin SanitizeSkin(int value)
	{
		if (Enum.IsDefined(typeof(AliceSkin), value))
		{
			return (AliceSkin)value;
		}

		return AliceSkin.Skin1;
	}

	private void OnSlotsChanged(System.Collections.Generic.IReadOnlyList<string> slots)
	{
		if (_slots.Length == 0 || _dolls.Length == 0)
		{
			return;
		}

		for (var i = 0; i < _slots.Length; i++)
		{
			var slot = _slots[i];
			if (slot == null)
			{
				continue;
			}

			var occupied = i < slots.Count && !string.IsNullOrEmpty(slots[i]);
			slot.Visible = true;
			slot.Modulate = occupied ? OccupiedSlotColor : EmptySlotColor;
			if (i < _threadLines.Length && _threadLines[i] != null)
			{
				_threadLines[i].Visible = occupied;
			}
			if (i < _threadSourceLines.Length && _threadSourceLines[i] != null)
			{
				_threadSourceLines[i].Visible = occupied;
			}

			var doll = _dolls[i];
			doll.Root.Visible = occupied;
			doll.Sprite.Visible = occupied;
			doll.StatsRoot.Visible = occupied;
			if (i < _threadFlashLines.Length && _threadFlashLines[i] != null && !occupied)
			{
				_threadFlashLines[i].Visible = false;
			}
		}

		UpdateSelectionVisuals();
	}

	private void OnStatsChanged(System.Collections.Generic.IReadOnlyList<DollSnapshot?> snapshots)
	{
		if (_dolls.Length == 0)
		{
			return;
		}

		for (var i = 0; i < _dolls.Length; i++)
		{
			var doll = _dolls[i];
			var snapshot = i < snapshots.Count ? snapshots[i] : null;
			var occupied = snapshot != null;
			var blockAmount = occupied ? snapshot.Value.Block : 0;
			doll.StatsRoot.Visible = occupied;
			doll.HealthShadow.Visible = occupied;
			doll.HealthBackground.Visible = occupied;
			doll.HealthFill.Visible = occupied;
			doll.HealthLabel.Visible = occupied;
			doll.BlockBadge.Visible = blockAmount > 0;
			doll.BlockLabel.Visible = blockAmount > 0;
			if (!occupied)
			{
				continue;
			}

			var hpRatio = snapshot.Value.MaxHp <= 0 ? 0f : Mathf.Clamp((float)snapshot.Value.Hp / snapshot.Value.MaxHp, 0f, 1f);
			doll.HealthFill.Size = new Vector2(DollStatsBarSize.X * hpRatio, DollStatsBarSize.Y);
			doll.HealthLabel.Text = $"{snapshot.Value.Hp}/{snapshot.Value.MaxHp}";
			doll.BlockLabel.Text = snapshot.Value.Block.ToString();
			doll.BlockLabel.Modulate = DollBlockTextColor;
		}
	}

	private void OnSelectionChanged(int? selectedSlotIndex)
	{
		UpdateSelectionVisuals();
	}

	private void UpdateSelectionVisuals()
	{
		var selectedSlotIndex = DollState.SelectedSlotIndex;
		for (var i = 0; i < _slots.Length; i++)
		{
			var slot = _slots[i];
			if (slot == null) continue;

			var occupied = i < _dolls.Length && _dolls[i].Root.Visible;
			var selected = i == selectedSlotIndex;

			if (!occupied && _targetingThreadActive)
			{
				var flash = (float)(Mathf.Sin(_threadMotionElapsed * 4.0) * 0.3 + 0.7);
				slot.Scale = selected ? SlotScale * SelectedSlotScaleMultiplier : SlotScale;
				slot.Modulate = selected
					? new Color(1f, 0.3f, 0.3f, 0.95f)
					: new Color(1f, 1f, 1f, flash);
			}
			else
			{
				slot.Scale = selected ? SlotScale * SelectedSlotScaleMultiplier : SlotScale;
				slot.Modulate = selected
					? occupied ? SelectedSlotColor : SelectedEmptySlotColor
					: occupied ? OccupiedSlotColor : EmptySlotColor;
			}

			if (i >= _dolls.Length) continue;
			_dolls[i].Sprite.Scale = selected ? DollScale * SelectedSlotScaleMultiplier : DollScale;
		}

		if (_aliceSelectionMarker != null)
		{
			_aliceSelectionMarker.Visible = selectedSlotIndex < 0;
		}	}

	private void OnResolutionPopupRequested(ResolutionPopup popup)
	{
		if (_resolutionLabels.Length == 0)
		{
			return;
		}

		var popupIndex = popup.SlotIndex ?? DollState.MaxSlots;
		if (popupIndex < 0 || popupIndex >= _resolutionLabels.Length)
		{
			return;
		}

		var label = _resolutionLabels[popupIndex];
		if (label == null)
		{
			return;
		}

		label.Text = BuildResolutionPopupText(popup);
		label.Position = _resolutionBasePositions[popupIndex];
		label.Modulate = Colors.White;
		label.Visible = true;
		_resolutionTimers[popupIndex] = PopupDurationSeconds;
	}

	private void UpdateDragSelection()
	{
		if (_overlayVisibleInCombat != true || _dollsRoot == null)
		{
			SetDragPreviewVisible(false);
			SetTargetingThreadsVisible(false);
			return;
		}

		if (IsDollPlacementDragActive())
		{
			SetDragPreviewVisible(false);
			UpdateSlotTargeting();
			return;
		}

		SetTargetingThreadsVisible(false);

		if (IsDollTargetDragActive())
		{
			var localPosition = _dollsRoot.ToLocal(GetGlobalMousePosition());
			var hoveredTargetPosition = Vector2.Zero;
			var hasHoveredTarget = false;
			for (var i = 0; i < SlotPositions.Length; i++)
			{
				if (localPosition.DistanceTo(SlotPositions[i]) <= 84f)
				{
					hoveredTargetPosition = SlotPositions[i];
					hasHoveredTarget = true;
					DollState.SelectSlot(i);
					break;
				}
			}

			if (!hasHoveredTarget && localPosition.DistanceTo(Vector2.Zero) <= 124f)
			{
				hoveredTargetPosition = Vector2.Zero;
				hasHoveredTarget = true;
				DollState.SelectAlice();
			}

			SetDragPreview(localPosition, hasHoveredTarget ? hoveredTargetPosition : GetCurrentSelectedTargetPosition());
		}
		else
		{
			SetDragPreviewVisible(false);
		}
	}

	private void UpdateSlotTargeting()
	{
		var mouseLocal = _dollsRoot.ToLocal(GetGlobalMousePosition());
		var hoveredEmptySlot = -1;

		for (var i = 0; i < SlotPositions.Length; i++)
		{
			if (DollState.IsSlotEmpty(i) && mouseLocal.DistanceTo(SlotPositions[i]) <= 84f)
			{
				hoveredEmptySlot = i;
				break;
			}
		}

		_targetingThreadActive = true;
		if (hoveredEmptySlot >= 0)
			DollState.SelectSlot(hoveredEmptySlot);

		var cardGlobalPos = DollState.GetPendingCardGlobalPosition();
		var sourceCenter = cardGlobalPos.HasValue
			? _dollsRoot.ToLocal(cardGlobalPos.Value)
			: new Vector2(0f, SlotRadiusY + 80f);
		var mouseCenter = mouseLocal;

		var circleColor = new Color(TargetingThreadColor.R, TargetingThreadColor.G, TargetingThreadColor.B, 0.7f);
		if (_targetingSourceCircle != null)
			UpdateCircle(_targetingSourceCircle, sourceCenter, TargetingSourceRadius, circleColor, 1.5f);
		if (_targetingMouseCircle != null)
			UpdateCircle(_targetingMouseCircle, mouseCenter, TargetingMouseRadius, circleColor, 1.2f);
		UpdateHexagram(_targetingSourceHexagram, sourceCenter, TargetingSourceRadius, circleColor, 1.2f);
		UpdateHexagram(_targetingMouseHexagram, mouseCenter, TargetingMouseRadius, circleColor, 1.0f);

		for (var t = 0; t < TargetingThreadCount; t++)
		{
			var line = _targetingThreads[t];
			if (line == null) continue;
			line.Visible = true;

			var angle = (Mathf.Pi * 2f * t / TargetingThreadCount) - Mathf.Pi / 2f;
			var sourcePoint = sourceCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * TargetingSourceRadius;
			var targetPoint = mouseCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * TargetingMouseRadius;

			UpdateThreadLinePoints(line, [sourcePoint, targetPoint]);

			line.DefaultColor = new Color(TargetingThreadColor.R, TargetingThreadColor.G, TargetingThreadColor.B, 0.85f);
			line.Width = 2.0f;
		}
	}

	private void SetTargetingThreadsVisible(bool visible)
	{
		if (!_targetingThreadActive && !visible) return;
		_targetingThreadActive = visible;
		foreach (var line in _targetingThreads)
		{
			if (line != null) line.Visible = visible;
		}
		if (_targetingSourceCircle != null) _targetingSourceCircle.Visible = visible;
		if (_targetingMouseCircle != null) _targetingMouseCircle.Visible = visible;
		foreach (var line in _targetingSourceHexagram) { if (line != null) line.Visible = visible; }
		foreach (var line in _targetingMouseHexagram) { if (line != null) line.Visible = visible; }
	}

	private void UpdateResolutionPopups(double delta)
	{
		if (_resolutionLabels.Length == 0)
		{
			return;
		}

		for (var i = 0; i < _resolutionLabels.Length; i++)
		{
			var label = _resolutionLabels[i];
			if (label == null || _resolutionTimers[i] <= 0)
			{
				continue;
			}

			_resolutionTimers[i] = Math.Max(0.0, _resolutionTimers[i] - delta);
			var progress = 1f - Mathf.Clamp((float)(_resolutionTimers[i] / PopupDurationSeconds), 0f, 1f);
			var alpha = 1f - progress;
			label.Visible = alpha > 0.01f;
			label.Position = _resolutionBasePositions[i] + new Vector2(0, -PopupRiseDistance * progress);
			label.Modulate = new Color(1f, 1f, 1f, alpha);
		}
	}

	private static string BuildResolutionPopupText(ResolutionPopup popup)
	{
		var segments = new List<string>();
		if (popup.Damage > 0)
		{
			segments.Add($"[color=#ff6b6b]-{popup.Damage}[/color]");
		}

		if (popup.Blocked > 0)
		{
			segments.Add($"[color=#ffffff]({popup.Blocked})[/color]");
		}

		return $"[center]{string.Concat(segments)}[/center]";
	}

	private bool IsDollTargetDragActive()
	{
		var currentScene = GetTree()?.CurrentScene;
		if (currentScene == null)
		{
			return false;
		}

		var currentCardPlay =
			TryGetSelectedCardPlay(currentScene, "NMouseCardPlay", "CurrentCardPlay", "_currentCardPlay", "selectedCard", "_localSelectedCard", "_selectedCard")
			?? TryGetSelectedCardPlay(currentScene, "NControllerCardPlay", "CurrentCardPlay", "_currentCardPlay", "selectedCard", "_localSelectedCard", "_selectedCard")
			?? TryGetSelectedCardPlay(currentScene, "NSelectedHandCardContainer", "selectedCard", "_selectedCard", "_localSelectedCard", "CurrentCardPlay", "_currentCardPlay");
		if (currentCardPlay == null)
		{
			return false;
		}

		var cardModel = ReadMember(currentCardPlay, "Card", "CardModel", "Model", "_card", "_cardModel") ?? currentCardPlay;
		var cardType = ReadMember(cardModel, "Type", "_type")?.ToString();
		if (!string.Equals(cardType, "Skill", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var blockAmount = ReadNumeric(cardModel, "CurrentBlock", "Block", "_block", "_currentBlock");
		return blockAmount > 0;
	}

	private bool IsDollPlacementDragActive()
	{
		return DollState.IsPlacementTargeting;
	}

	private static object? TryGetSelectedCardPlay(Node currentScene, string typeName, params string[] memberNames)
	{
		var node = currentScene.FindChild(typeName, true, false) ?? FindNodeByTypeName(currentScene, typeName);
		return node == null ? null : ReadMember(node, memberNames);
	}

	private Vector2 GetCurrentSelectedTargetPosition()
	{
		var selectedSlotIndex = DollState.SelectedSlotIndex;
		return selectedSlotIndex >= 0 && selectedSlotIndex < SlotPositions.Length
			? SlotPositions[selectedSlotIndex]
			: Vector2.Zero;
	}

	private void SetDragPreview(Vector2 startPoint, Vector2 targetPoint)
	{
		if (_dragPreviewLine == null)
		{
			return;
		}

		_dragPreviewLine.Visible = true;
		_dragPreviewLine.ClearPoints();
		_dragPreviewLine.AddPoint(startPoint);
		_dragPreviewLine.AddPoint((startPoint + targetPoint) * 0.5f + new Vector2(0f, -18f));
		_dragPreviewLine.AddPoint(targetPoint);
	}

	private void SetDragPreviewVisible(bool visible)
	{
		if (_dragPreviewLine == null)
		{
			return;
		}

		_dragPreviewLine.Visible = visible;
		if (!visible)
		{
			_dragPreviewLine.ClearPoints();
		}
	}

	private static Node? FindNodeByTypeName(Node root, string typeName)
	{
		if (string.Equals(root.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase))
		{
			return root;
		}

		foreach (Node child in root.GetChildren())
		{
			var found = FindNodeByTypeName(child, typeName);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static object? ReadMember(object? target, params string[] memberNames)
	{
		if (target == null)
		{
			return null;
		}

		var type = target.GetType();
		foreach (var memberName in memberNames)
		{
			var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (property != null)
			{
				return property.GetValue(target);
			}

			var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
			if (field != null)
			{
				return field.GetValue(target);
			}
		}

		return null;
	}

	private static int ReadNumeric(object? target, params string[] memberNames)
	{
		var value = ReadMember(target, memberNames);
		return value == null ? 0 : Convert.ToInt32(Math.Round(Convert.ToDecimal(value)));
	}

	private void UpdateThreadFlash(double delta)
	{
		if (_threadLines.Length == 0 || _threadFlashLines.Length == 0)
		{
			return;
		}

		_threadFlashElapsed += delta;
		_threadMotionElapsed += delta;
		_threadTensionRemaining = Math.Max(0.0, _threadTensionRemaining - delta);
		var cycleTime = _threadFlashElapsed % ThreadFlashIntervalSeconds;
		var isFlashing = _threadFlashElapsed >= ThreadFlashIntervalSeconds && cycleTime <= ThreadFlashDurationSeconds;
		var flashProgress = isFlashing ? cycleTime / ThreadFlashDurationSeconds : 0.0;
		var segmentHalfRatio = ThreadFlashSegmentRatio * 0.5f;
		var tensionProgress = ThreadTensionDurationSeconds <= 0.0
			? 0f
			: Mathf.Clamp((float)(_threadTensionRemaining / ThreadTensionDurationSeconds), 0f, 1f);
		var tensionStrength = Mathf.Sin(tensionProgress * Mathf.Pi);

		for (var i = 0; i < _threadLines.Length; i++)
		{
			var baseLine = _threadLines[i];
			var sourceLine = i < _threadSourceLines.Length ? _threadSourceLines[i] : null;
			var flashLine = _threadFlashLines[i];
			if (baseLine == null || flashLine == null)
			{
				continue;
			}

			if (sourceLine != null)
			{
				sourceLine.Visible = baseLine.Visible;
			}

			var endPoint = SlotPositions[i] + DollOffset;
			var controlPoint = GetThreadControlPoint(i, endPoint, tensionStrength);
			UpdateThreadLinePoints(baseLine, [
				Vector2.Zero,
				controlPoint,
				endPoint
			]);

			if (sourceLine != null)
			{
				sourceLine.Width = ThreadSourceWidth + ThreadTensionSourceWidthBoost * tensionStrength;
				sourceLine.DefaultColor = new Color(
					ThreadSourceColor.R,
					ThreadSourceColor.G,
					ThreadSourceColor.B,
					Mathf.Clamp(ThreadSourceColor.A + ThreadTensionSourceAlphaBoost * tensionStrength, 0f, 1f));
				UpdateThreadLinePoints(sourceLine, [
					Vector2.Zero,
					EvaluateQuadraticBezier(Vector2.Zero, controlPoint, endPoint, ThreadSourceSegmentRatio * 0.45f),
					EvaluateQuadraticBezier(Vector2.Zero, controlPoint, endPoint, ThreadSourceSegmentRatio)
				]);
			}

			if (!baseLine.Visible || !isFlashing)
			{
				flashLine.Visible = false;
				continue;
			}

			var easedProgress = 1f - Mathf.Pow(1f - Mathf.Clamp((float)flashProgress, 0f, 1f), 3f);
			var headRatio = easedProgress;
			var tailRatio = Mathf.Clamp(headRatio - segmentHalfRatio, 0f, 1f);
			var headPoint = EvaluateQuadraticBezier(Vector2.Zero, controlPoint, endPoint, headRatio);
			var tailPoint = EvaluateQuadraticBezier(Vector2.Zero, controlPoint, endPoint, tailRatio);
			var midPoint = EvaluateQuadraticBezier(Vector2.Zero, controlPoint, endPoint, (tailRatio + headRatio) * 0.5f);
			var alpha = Mathf.Pow(Mathf.Sin(Mathf.Clamp((float)flashProgress, 0f, 1f) * Mathf.Pi), 1.6f);
			flashLine.Visible = alpha > 0.01f;
			flashLine.DefaultColor = new Color(ThreadFlashColor.R, ThreadFlashColor.G, ThreadFlashColor.B, ThreadFlashColor.A * alpha);
			UpdateThreadLinePoints(flashLine, [
				tailPoint,
				midPoint,
				headPoint
			]);
		}
	}

	private Vector2 GetThreadControlPoint(int slotIndex, Vector2 endPoint, float tensionStrength)
	{
		var direction = endPoint.Normalized();
		var normal = new Vector2(-direction.Y, direction.X);
		var horizontalFactor = Mathf.Clamp(Mathf.Abs(endPoint.X) / SlotRadiusX, 0.35f, 1f);
		var sway = Mathf.Sin((float)(_threadMotionElapsed * ThreadMotionSpeed) + slotIndex * 0.9f)
			* ThreadMotionAmplitude
			* horizontalFactor
			* Mathf.Lerp(1f, ThreadTensionStraightenRatio, tensionStrength);
		return endPoint * 0.5f + normal * sway;
	}

	private static Vector2 EvaluateQuadraticBezier(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, float t)
	{
		var inverseT = 1f - t;
		return startPoint * inverseT * inverseT
			+ controlPoint * 2f * inverseT * t
			+ endPoint * t * t;
	}

	private static void UpdateThreadLinePoints(Line2D line, Vector2[] points)
	{
		line.ClearPoints();
		foreach (var point in points)
		{
			line.AddPoint(point);
		}
	}
}

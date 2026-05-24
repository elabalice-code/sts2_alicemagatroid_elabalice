namespace AliceMagatroid_Mod.Dolls;

public enum DollTargetKind
{
	None,
	Alice,
	Doll,
	EmptySlot
}

public readonly record struct DollTargetContext(DollTargetKind Kind, int SlotIndex = -1)
{
	public bool HasSlot => SlotIndex >= 0;
	public bool IsAlice => Kind == DollTargetKind.Alice;
	public bool IsDoll => Kind == DollTargetKind.Doll;
	public bool IsEmptySlot => Kind == DollTargetKind.EmptySlot;

	public static DollTargetContext Alice => new(DollTargetKind.Alice);
	public static DollTargetContext None => new(DollTargetKind.None);

	public static DollTargetContext ForSlot(DollTargetKind kind, int slotIndex)
	{
		return new DollTargetContext(kind, slotIndex);
	}
}

namespace Cathedral.Game.Narrative;

// The reptile's own wounds — the horns and the thagomizer. Everything else a reptile can suffer is a
// beast wound (an eye, a leg, a fang), shared by type through ReptileAnatomyFactory.GetWoundClassMap.
// The ids are the reptile art's: assets/art/body/reptile/wounds.csv.

// ─── Muzzle ───────────────────────────────────────────────────────────────

public sealed class ReptileBrokenHornWound : Wound
{
    public override char WoundId => 'y';
    public override string WoundName => "Broken Horn";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "horns";
}

public sealed class ReptileTornOffHornWound : Wound
{
    public override char WoundId => 'z';
    public override string WoundName => "Torned Off Horn";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "horns";
}

// ─── Trunk ────────────────────────────────────────────────────────────────

public sealed class ReptileBrokenSpikeWound : Wound
{
    public override char WoundId => 'w';
    public override string WoundName => "Broken Spike";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "thagomizer";
}

/// <summary>
/// The tail lost, and the thagomizer with it — where a beast's lost tail is a medium wound to the
/// backbone, a reptile's is its whole second weapon.
/// </summary>
public sealed class ReptileSeveredTailWound : Wound
{
    public override char WoundId => 'x';
    public override string WoundName => "Severed Tail";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "thagomizer";
}

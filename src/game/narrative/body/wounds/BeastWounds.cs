namespace Cathedral.Game.Narrative;

// ─── Muzzle ───────────────────────────────────────────────────────────────

public sealed class BeastPiercedEyeLeftWound : Wound
{
    public override char WoundId => '2';
    public override string WoundName => "Pierced Eye";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_eye";
}

public sealed class BeastPiercedEyeRightWound : Wound
{
    public override char WoundId => '3';
    public override string WoundName => "Pierced Eye";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_eye";
}

public sealed class BeastPerforatedEardrumLeftWound : Wound
{
    public override char WoundId => '4';
    public override string WoundName => "Perforated Eardrum";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_ear";
}

public sealed class BeastPerforatedEardrumRightWound : Wound
{
    public override char WoundId => '5';
    public override string WoundName => "Perforated Eardrum";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_ear";
}

public sealed class BeastBrokenSnoutWound : Wound
{
    public override char WoundId => '8';
    public override string WoundName => "Broken Snout";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "snout";
}

public sealed class BeastBrokenFangWound : Wound
{
    public override char WoundId => '9';
    public override string WoundName => "Broken Fang";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "fangs";
}

public sealed class BeastTornedOutTongueWound : Wound
{
    public override char WoundId => 'a';
    public override string WoundName => "Torned Out Tongue";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "tongue";
}

// ─── Encephalon ──────────────────────────────────────────────────────────

public sealed class BeastSkullFractureWound : Wound
{
    public override char WoundId => '6';
    public override string WoundName => "Skull Fracture";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.BodyPart;
    public override string TargetId => "encephalon";
}

public sealed class BeastConcussionsWound : Wound
{
    public override char WoundId => '7';
    public override string WoundName => "Concussions";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.BodyPart;
    public override string TargetId => "encephalon";
}

// ─── Trunk ───────────────────────────────────────────────────────────────

public sealed class BeastBrokenBackboneWound : Wound
{
    public override char WoundId => 'b';
    public override string WoundName => "Broken Backbone";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "backbone";
}

public sealed class BeastTailAmputeeWound : Wound
{
    public override char WoundId => 'x';
    public override string WoundName => "Tail Amputee";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "backbone";
}

public sealed class BeastBrokenRibsWound : Wound
{
    public override char WoundId => 'g';
    public override string WoundName => "Broken Ribs";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "pulmones";
}

public sealed class BeastEviscerationWound : Wound
{
    public override char WoundId => 'p';
    public override string WoundName => "Evisceration";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "viscera";
}

public sealed class BeastPiercedPaunchWound : Wound
{
    public override char WoundId => 'q';
    public override string WoundName => "Pierced Paunch";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "paunch";
}

public sealed class BeastTornedOffFangWound : Wound
{
    public override char WoundId => 'r';
    public override string WoundName => "Torned Off Fang";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "fangs";
}

// ─── Limbs — forelegs ────────────────────────────────────────────────────

public sealed class BeastBrokenLeftForelegWound : Wound
{
    public override char WoundId => 'c';
    public override string WoundName => "Broken Leg";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foreleg";
}

public sealed class BeastBrokenRightForelegWound : Wound
{
    public override char WoundId => 'd';
    public override string WoundName => "Broken Leg";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foreleg";
}

public sealed class BeastCrippledLeftForelegWound : Wound
{
    public override char WoundId => 's';
    public override string WoundName => "Crippled Leg";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foreleg";
}

public sealed class BeastCrippledRightForelegWound : Wound
{
    public override char WoundId => 't';
    public override string WoundName => "Crippled Leg";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foreleg";
}

// ─── Limbs — hindlegs ────────────────────────────────────────────────────

public sealed class BeastBrokenLeftHindlegWound : Wound
{
    public override char WoundId => 'e';
    public override string WoundName => "Broken Leg";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hindleg";
}

public sealed class BeastBrokenRightHindlegWound : Wound
{
    public override char WoundId => 'f';
    public override string WoundName => "Broken Leg";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hindleg";
}

public sealed class BeastCrippledLeftHindlegWound : Wound
{
    public override char WoundId => 'u';
    public override string WoundName => "Crippled Leg";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hindleg";
}

public sealed class BeastCrippledRightHindlegWound : Wound
{
    public override char WoundId => 'v';
    public override string WoundName => "Crippled Leg";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hindleg";
}

// ─── Limbs — foreclaws ───────────────────────────────────────────────────

public sealed class BeastBrokenLeftForeclawsWound : Wound
{
    public override char WoundId => 'h';
    public override string WoundName => "Broken Claw";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foreclaws";
}

public sealed class BeastBrokenRightForeclawsWound : Wound
{
    public override char WoundId => 'i';
    public override string WoundName => "Broken Claw";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foreclaws";
}

public sealed class BeastTornOffLeftForeclawsWound : Wound
{
    public override char WoundId => 'l';
    public override string WoundName => "Torn Off Claw";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foreclaws";
}

public sealed class BeastTornOffRightForeclawsWound : Wound
{
    public override char WoundId => 'm';
    public override string WoundName => "Torn Off Claw";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foreclaws";
}

// ─── Limbs — hindclaws ───────────────────────────────────────────────────

public sealed class BeastBrokenRightHindclawsWound : Wound
{
    public override char WoundId => 'j';
    public override string WoundName => "Broken Claw";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hindclaws";
}

public sealed class BeastBrokenLeftHindclawsWound : Wound
{
    public override char WoundId => 'k';
    public override string WoundName => "Broken Claw";
    public override WoundHandicap Handicap => WoundHandicap.Medium;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hindclaws";
}

public sealed class BeastTornOffRightHindclawsWound : Wound
{
    public override char WoundId => 'n';
    public override string WoundName => "Torn Off Claw";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hindclaws";
}

public sealed class BeastTornOffLeftHindclawsWound : Wound
{
    public override char WoundId => 'o';
    public override string WoundName => "Torn Off Claw";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hindclaws";
}

// ─── Wildcard (Low handicap: -1 HP only, no organ effect) ────────────────

public sealed class BeastContusionWound : WildcardWound
{
    public override char WoundId => 'C';
    public override string WoundName => "Contusion";
}

public sealed class BeastCutWound : WildcardWound
{
    public override char WoundId => 'D';
    public override string WoundName => "Cut";
}

public sealed class BeastPunctureWound : WildcardWound
{
    public override char WoundId => 'E';
    public override string WoundName => "Puncture";
}

namespace Cathedral.Game.Narrative;

// ─── Visage ────────────────────────────────────────────────────────────────

public sealed class BlackEyeLeftWound : Wound
{
    public override char WoundId => '0';
    public override string WoundName => "Black Eye";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_eye";
}

public sealed class BlackEyeRightWound : Wound
{
    public override char WoundId => '1';
    public override string WoundName => "Black Eye";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_eye";
}

public sealed class PiercedEyeLeftWound : Wound
{
    public override char WoundId => '2';
    public override string WoundName => "Pierced Eye";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_eye";
}

public sealed class PiercedEyeRightWound : Wound
{
    public override char WoundId => '3';
    public override string WoundName => "Pierced Eye";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_eye";
}

public sealed class PerforatedEardrumLeftWound : Wound
{
    public override char WoundId => '4';
    public override string WoundName => "Perforated Eardrum";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_ear";
}

public sealed class PerforatedEardrumRightWound : Wound
{
    public override char WoundId => '5';
    public override string WoundName => "Perforated Eardrum";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_ear";
}

public sealed class BrokenNoseWound : Wound
{
    public override char WoundId => '8';
    public override string WoundName => "Broken Nose";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "nose";
}

public sealed class BrokenTeethsWound : Wound
{
    public override char WoundId => '9';
    public override string WoundName => "Broken Teeths";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "teeths";
}

public sealed class TornedOutTongueWound : Wound
{
    public override char WoundId => 'a';
    public override string WoundName => "Torned Out Tongue";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "tongue";
}

public sealed class DisfiguredWound : Wound
{
    public override char WoundId => 's';
    public override string WoundName => "Disfigured";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.BodyPart;
    public override string TargetId => "visage";
}

// ─── Encephalon ───────────────────────────────────────────────────────────

public sealed class SkullFractureWound : Wound
{
    public override char WoundId => '6';
    public override string WoundName => "Skull Fracture";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.BodyPart;
    public override string TargetId => "encephalon";
}

public sealed class ConcussionsWound : Wound
{
    public override char WoundId => '7';
    public override string WoundName => "Concussions";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.BodyPart;
    public override string TargetId => "encephalon";
}

// ─── Trunk ────────────────────────────────────────────────────────────────

public sealed class BrokenBackboneWound : Wound
{
    public override char WoundId => 'b';
    public override string WoundName => "Broken Backbone";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "backbone";
}

public sealed class BrokenRibsWound : Wound
{
    // 'g' in wounds.csv — "broken ribs, lungs" → pulmones organ
    public override char WoundId => 'g';
    public override string WoundName => "Broken Ribs";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "pulmones";
}

public sealed class EviscerationWound : Wound
{
    public override char WoundId => 'p';
    public override string WoundName => "Evisceration";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "viscera";
}

public sealed class PiercedPaunchWound : Wound
{
    public override char WoundId => 'q';
    public override string WoundName => "Pierced Paunch";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "paunch";
}

public sealed class GenitalMutilationWound : Wound
{
    public override char WoundId => 'r';
    public override string WoundName => "Genital Mutilation";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.Organ;
    public override string TargetId => "genitories";
}

// ─── Upper Limbs ──────────────────────────────────────────────────────────

public sealed class BrokenArmLeftWound : Wound
{
    public override char WoundId => 'c';
    public override string WoundName => "Broken Arm";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_arm";
}

public sealed class BrokenArmRightWound : Wound
{
    public override char WoundId => 'd';
    public override string WoundName => "Broken Arm";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_arm";
}

public sealed class ShoulderDislocationLeftWound : Wound
{
    public override char WoundId => 'n';
    public override string WoundName => "Shoulder Dislocation";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_arm";
}

public sealed class ShoulderDislocationRightWound : Wound
{
    public override char WoundId => 'o';
    public override string WoundName => "Shoulder Dislocation";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_arm";
}

public sealed class FingersAmputeeRightWound : Wound
{
    public override char WoundId => 'j';
    public override string WoundName => "Fingers Amputee";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hand";
}

public sealed class FingersAmputeeLeftWound : Wound
{
    public override char WoundId => 'k';
    public override string WoundName => "Fingers Amputee";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hand";
}

public sealed class WristFractureLeftWound : Wound
{
    public override char WoundId => 't';
    public override string WoundName => "Wrist Fracture";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hand";
}

public sealed class WristFractureRightWound : Wound
{
    public override char WoundId => 'u';
    public override string WoundName => "Wrist Fracture";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hand";
}

public sealed class BrokenHandLeftWound : Wound
{
    public override char WoundId => 'x';
    public override string WoundName => "Broken Hand";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_hand";
}

public sealed class BrokenHandRightWound : Wound
{
    public override char WoundId => 'y';
    public override string WoundName => "Broken Hand";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_hand";
}

// ─── Lower Limbs ──────────────────────────────────────────────────────────

public sealed class KneeFractureLeftWound : Wound
{
    public override char WoundId => 'e';
    public override string WoundName => "Knee Fracture";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_leg";
}

public sealed class KneeFractureRightWound : Wound
{
    public override char WoundId => 'f';
    public override string WoundName => "Knee Fracture";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_leg";
}

public sealed class TibiaFractureLeftWound : Wound
{
    public override char WoundId => 'l';
    public override string WoundName => "Tibia Fracture";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_leg";
}

public sealed class TibiaFractureRightWound : Wound
{
    public override char WoundId => 'm';
    public override string WoundName => "Tibia Fracture";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_leg";
}

public sealed class FootAmputeeLeftWound : Wound
{
    public override char WoundId => 'h';
    public override string WoundName => "Foot Amputee";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foot";
}

public sealed class FootAmputeeRightWound : Wound
{
    public override char WoundId => 'i';
    public override string WoundName => "Foot Amputee";
    public override WoundHandicap Handicap => WoundHandicap.High;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foot";
}

public sealed class AnkleFractureLeftWound : Wound
{
    public override char WoundId => 'v';
    public override string WoundName => "Ankle Fracture";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foot";
}

public sealed class AnkleFractureRightWound : Wound
{
    public override char WoundId => 'w';
    public override string WoundName => "Ankle Fracture";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foot";
}

public sealed class BrokenFootLeftWound : Wound
{
    public override char WoundId => 'z';
    public override string WoundName => "Broken Foot";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "left_foot";
}

public sealed class BrokenFootRightWound : Wound
{
    public override char WoundId => 'A';
    public override string WoundName => "Broken Foot";
    public override WoundHandicap Handicap => WoundHandicap.Low;
    public override WoundTargetKind TargetKind => WoundTargetKind.OrganPart;
    public override string TargetId => "right_foot";
}

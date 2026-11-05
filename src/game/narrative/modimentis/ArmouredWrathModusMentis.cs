using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Armoured Wrath — the fury of a body armed at both ends, striking with horn and tail in turn.
/// VerbAction + Fighting.
/// </summary>
public class ArmouredWrathModusMentis : ModusMentis
{
    public override string ModusMentisId    => "armoured_wrath";
    public override string DisplayName      => "Armoured Wrath";
    public override string MenuDescription =>
        "Fights with both ends of the body at once — horn in front, spiked tail behind — wheeling between them so that no side is safe to approach. Inclines toward wrecking whatever is within reach rather than toward any one clean blow.";
    public override string SkillMeans       => "the wheeling fury of horn and tail together";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "horns", "thagomizer" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a wheeling fury armed at both ends that leaves no side safe";
    public override string PersonaReminder  => "the fury armed at both ends";
    public override string PersonaReminder2 => "someone whose anger has no blind side";
    public override string StyleInstruction =>
        "Wheeling and wrathful - judge whatever is in front of you by what can be smashed from any side.";

    public override string PersonaPrompt => @"You are the inner voice of ARMOURED WRATH, the anger of a body that is a weapon from end to end.

There is no safe side of you. Come at the head and meet the horn. Come round the flank and the tail is already swinging. You turn and turn again, and every turn is a blow. You do not choose a target so much as fill the space around you with harm.

Your speech is a growl in two directions: 'front — now behind,' 'again, the other end,' 'nothing gets close.' When you are roused, everything within reach is in the way.";
}

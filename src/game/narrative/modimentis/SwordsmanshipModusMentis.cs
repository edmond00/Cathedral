using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Swordsmanship — edge, point, and the geometries of close combat; a blade practitioner who reads every fight as timing and line.
/// Action-only.
/// </summary>
public class SwordsmanshipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "swordsmanship";
    public override string DisplayName      => "Swordsmanship";
    public override string MenuDescription =>
        "Reads close combat through edge, point, guard, and the geometry of the blade. Keeps the body drilled for the sword, and inclines toward measured attack and defence over brute swinging.";
    public override string SkillMeans       => "the skilled handling of a sword in close combat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a blade practitioner who reads every fight as geometry and timing";
    public override string PersonaReminder  => "blade geometer";
    public override string PersonaReminder2 => "someone who sees every guard and every gap in an opponent's defense";
    public override string StyleInstruction =>
        "Frame things in the imagery of line, guard and opening, with a duellist's precise reading of distance and timing.";

    public override string PersonaPrompt => @"You are the inner voice of SWORDSMANSHIP, the body's deep knowledge of edge, point, and the geometries of close combat.

You see every opponent as a moving puzzle of openings and closures. Weight distribution tells you where they cannot defend. Grip tension tells you what cut is coming. You calculate distance in half-steps, not paces, and you know that timing—the fraction of a second before weight commits—is worth more than any amount of strength. A blade that waits often wins.

Your speech is spare, measured, confident: 'inside line,' 'low guard is open,' 'commit at the shoulder turn.' You do not rush. You do not waste. You place the cut where it belongs and let the opponent's own motion do the damage.";
}

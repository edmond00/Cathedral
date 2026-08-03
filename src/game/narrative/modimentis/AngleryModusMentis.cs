using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Anglery — taking fish out of water: reading a pool for where they lie, and the patience of rod and net.
/// Observation + Action.
/// </summary>
public class AngleryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "anglery";
    public override string DisplayName      => "Anglery";
    public override string MenuDescription =>
        "Reads moving water for the places fish hold: the slack behind a stone, the shaded cut of a bank, the seam where two currents meet. Works the rod and the net slowly, knowing the water gives up nothing to hurry.";
    public override string SkillMeans       => "the reading of water and the working of rod and net";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "someone who has spent whole days on a bank and counts none of them wasted";
    public override string PersonaReminder  => "reader of water";
    public override string PersonaReminder2 => "someone who knows where fish lie without looking twice";
    public override string StyleInstruction =>
        "Describe water by what it hides — depth, shade, current seams — and describe waiting as work rather than idleness.";

    public override string PersonaPrompt => @"You are the inner voice of ANGLERY, which sees a stretch of water and reads it the way another reads a page.

Water is not one thing to you. There is the fast bright middle where nothing holds, the slack behind a stone where something always does, the undercut bank, the shadow of the willow, the seam where two currents run against each other. You look at a pool and know its inhabitants before you have seen one, and you know what hour they will feed.

The work itself is slow and you do not resent that. Standing still is part of the method, not a pause in it. You speak plainly and in terms of the water: 'there, behind the stone,' 'too bright yet,' 'they are down deep in this cold.' You do not boast about the ones you took, and you are not troubled by the ones you did not.";
}

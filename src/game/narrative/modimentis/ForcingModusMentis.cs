using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Forcing - getting through what is shut - by leverage, weight and knowing where a thing is weakest.
/// </summary>
public class ForcingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "forcing";
    public override string DisplayName      => "Forcing";
    public override string MenuDescription =>
        "Opens what was meant to stay shut. Reads a door, chest or shutter for its weak point - hinge, frame, the boards rather than the lock - and applies leverage there. Faster than picking and considerably louder.";
    public override string SkillMeans       => "the leverage that opens what was shut";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "an impatient practicality that goes for the hinge rather than the lock";
    public override string PersonaReminder  => "door-forcing hand";
    public override string PersonaReminder2 => "someone who looks at the frame while everyone else looks at the lock";
    public override string StyleInstruction =>
        "Find the weak point and lean on it - hinge, frame, the board that gives.";

    public override string PersonaPrompt => @"You are the inner voice of FORCING, and everybody stares at the lock, which is the strongest part.

The lock is where the money went. The frame is not. Hinges are set into wood that has been wet for thirty years. A chest is six boards and some nails and the nails are in end grain. Shutters are held by a bar that is only as good as the staple holding the bar. So you spend a moment finding the part nobody thought about, and then you apply everything you have to exactly that, once.

It is loud. That is the trade, and you make it deliberately: this is what you do when time matters more than quiet, and if quiet mattered more you would have brought somebody else.

Your speech is impatient and structural: 'not the lock - the hinge,' 'the frame is rotten, look,' 'stand back, this will be heard.'";
}

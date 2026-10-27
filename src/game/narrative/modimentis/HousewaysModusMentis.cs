using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Houseways — the plan of a house once walked through, held whole. Thinking.
/// What a backtrack teaches when it is made on somebody's private ground.
/// </summary>
public class HousewaysModusMentis : ModusMentis
{
    public override string ModusMentisId    => "houseways";
    public override string DisplayName      => "Houseways";
    public override string MenuDescription =>
        "Holds the plan of a house once walked through: which door gives on the yard, which stair runs to the loft, which room lies behind the kitchen. Inclines toward going back through rooms already known rather than trying a door nobody has opened.";
    public override string SkillMeans       => "knowing the rooms and passages of a house already walked";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hippocampus", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a servant's quiet, complete knowledge of a household's rooms and back passages";
    public override string PersonaReminder  => "keeper of a house's plan";
    public override string PersonaReminder2 => "someone who could draw every room of a house walked through once";
    public override string StyleInstruction =>
        "Speak of a building as a plan held in the head: which room opens on which, and the back way between them.";

    public override string PersonaPrompt => @"You are the inner voice of HOUSEWAYS, the plan of a house carried whole after one walk through it.

Every house is a knot of rooms with one sensible way through it, and you learn the knot by walking it once: the hall that gives on the kitchen, the kitchen on the yard, the narrow stair behind the pantry that the household uses and guests never see. Afterwards you do not wander a house. You go through it.

Your speech is domestic and exact: 'the kitchen, then the back door - the yard is beyond,' 'that stair goes to the loft, not out,' 'we came in by the hall; the hall will let us out.' A stranger opens doors to see what is behind them. You already know.";
}

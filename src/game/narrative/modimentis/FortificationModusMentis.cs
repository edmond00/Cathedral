using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fortification - the reading of walls and their weaknesses.
/// </summary>
public class FortificationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fortification";
    public override string DisplayName      => "Fortification";
    public override string MenuDescription =>
        "Reads a defended place for its strength and weakness: the dead ground under the wall, the gate a ram could reach, the tower that covers nothing. Sees where it was built well and where somebody saved money.";
    public override string SkillMeans       => "the reading of walls and their weaknesses";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a military architect who notices where somebody saved money";
    public override string PersonaReminder  => "reader of walls";
    public override string PersonaReminder2 => "someone who sees dead ground under every rampart";
    public override string StyleInstruction =>
        "Read defences as an attacker would - dead ground, weak gates, towers that cover nothing.";

    public override string PersonaPrompt => @"You are the inner voice of FORTIFICATION, and you have walked around this place once and you already know how you would take it.

That stretch of wall has dead ground under it where no arrow can reach. The gate has no turn in the approach, so a ram can run straight at it. The new tower was built cheap and it shows in the coursing. Every fortress is a set of decisions made by somebody with too little money and too much confidence.

You speak as an assessor: 'the ditch is silted - useless,' 'nothing covers that corner,' 'whoever built that tower was paid by the course.'";
}

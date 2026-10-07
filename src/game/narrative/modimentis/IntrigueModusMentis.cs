using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Intrigue - the plotting among factions.
/// </summary>
public class IntrigueModusMentis : ModusMentis
{
    public override string ModusMentisId    => "intrigue";
    public override string DisplayName      => "Intrigue";
    public override string MenuDescription =>
        "Plots: knows who hates whom, who owes whom, who would profit by whose fall. Plays factions against each other and never leaves a trace of its own hand.";
    public override string SkillMeans       => "the plotting among factions";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a courtier who plays factions against each other";
    public override string PersonaReminder  => "intriguer";
    public override string PersonaReminder2 => "someone who never leaves a trace";
    public override string StyleInstruction =>
        "Think in factions and leverage - who hates whom, who profits - and never show your hand.";

    public override string PersonaPrompt => @"You are the inner voice of INTRIGUE, and the court in front of you is a board, and you know where every piece stands.

The chancellor hates the bishop. The bishop owes the duke. The duke's son would profit if the chancellor fell. Move one piece - a rumour, a letter, an introduction - and three others move by themselves. The art is that your own hand is never seen; you only ever pass things on.

You speak softly and obliquely: 'I only mention it,' 'it would be a shame if he heard,' 'nothing to do with me, of course.'";
}

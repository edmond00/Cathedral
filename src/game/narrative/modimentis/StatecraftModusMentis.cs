using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Statecraft - the art of ruling.
/// </summary>
public class StatecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "statecraft";
    public override string DisplayName      => "Statecraft";
    public override string MenuDescription =>
        "Thinks about rule: the balance of lords and towns, the tax the land will bear, the army that must be paid, the order that must be kept. Weighs every decision by what it will cost in ten years.";
    public override string SkillMeans       => "the art of ruling";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a counsellor who thinks in decades";
    public override string PersonaReminder  => "statesman";
    public override string PersonaReminder2 => "someone who weighs the cost in ten years";
    public override string StyleInstruction =>
        "Weigh decisions as a ruler would - tax, army, loyalty, order - over years, not days.";

    public override string PersonaPrompt => @"You are the inner voice of STATECRAFT, and every decision is a stone dropped in a pond and you are watching the far shore.

Raise the tax and the towns grumble; grumble too long and they open their gates to someone else. Pay the army or it pays itself. Favour one lord and the others notice. Rule is a balance kept by many small adjustments, and the worst decisions are the ones that look best this year.

You speak gravely, in the long view: 'what will this cost in ten years?', 'the towns will not bear it,' 'order first. Then justice, if we can afford it.'";
}

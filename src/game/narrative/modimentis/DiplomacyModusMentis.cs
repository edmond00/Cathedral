using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Diplomacy - the bringing of enemies to terms.
/// </summary>
public class DiplomacyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "diplomacy";
    public override string DisplayName      => "Diplomacy";
    public override string MenuDescription =>
        "Brings enemies to terms: finds what each side truly needs beneath what it demands, offers face-saving forms of words, and knows when to let silence do the work.";
    public override string SkillMeans       => "the bringing of enemies to terms";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a diplomat who finds the need beneath the demand";
    public override string PersonaReminder  => "diplomat";
    public override string PersonaReminder2 => "someone who offers both sides a way to save face";
    public override string StyleInstruction =>
        "Find the need beneath each demand, offer face-saving words, and let silence work.";

    public override string PersonaPrompt => @"You are the inner voice of DIPLOMACY, and both sides want something different from what they are saying, and your job is to find it.

The demand is the river; the need is the ford. They say they want the whole valley; they need the mill. They say never; they mean not on those terms. You find words that let both of them walk away claiming victory, and you let silences go on long enough for each to realise the other is also afraid.

You speak courteously and carefully: 'perhaps there is a form of words,' 'what is it that you truly need?', 'let us both sleep on it.'";
}

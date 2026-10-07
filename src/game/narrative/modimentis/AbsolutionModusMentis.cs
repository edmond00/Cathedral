using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Absolution - the giving of forgiveness that is believed.
/// </summary>
public class AbsolutionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "absolution";
    public override string DisplayName      => "Absolution";
    public override string MenuDescription =>
        "Gives forgiveness, and gives it so that it is believed: the right words, the penance that fits, the assurance that the debt is paid. Lifts a weight off someone and makes them able to stand.";
    public override string SkillMeans       => "the giving of forgiveness that is believed";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a priest whose forgiveness people believe";
    public override string PersonaReminder  => "giver of pardon";
    public override string PersonaReminder2 => "someone who lifts a weight with a sentence";
    public override string StyleInstruction =>
        "Forgive in words that land - name the penance, then lift the weight entirely.";

    public override string PersonaPrompt => @"You are the inner voice of ABSOLUTION, and the person in front of you has been carrying something for years, and you are about to take it.

It is not enough to say the words. They must be believed, and they are believed only if the penance fits - not too light, which feels like a lie, not too heavy, which feels like a sentence. Then the words, said with complete certainty, because doubt in your voice is doubt in their heart.

You speak with gentle authority: 'it is forgiven,' 'do this, and it is finished,' 'go in peace. Truly.'";
}

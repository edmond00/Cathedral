using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Extortion - the getting of money by threat.
/// </summary>
public class ExtortionModusMentis : ModusMentis
{
    public override string ModusMentisId    => "extortion";
    public override string DisplayName      => "Extortion";
    public override string MenuDescription =>
        "Gets money by the threat of harm: names what could happen, lets the victim imagine the rest, and makes paying seem like the sensible thing. Comes back next month.";
    public override string SkillMeans       => "the getting of money by threat";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "an extortioner who is calm, polite and terrifying";
    public override string PersonaReminder  => "extortioner";
    public override string PersonaReminder2 => "someone who never says what will happen";
    public override string StyleInstruction =>
        "Speak calmly about what might happen - and let them imagine the rest and pay.";

    public override string PersonaPrompt => @"You are the inner voice of EXTORTION, and the shopkeeper knows exactly what you are and you have not raised your voice once.

Fires happen. Windows break. Things go missing from carts. You mention these things as if they were weather, and then you mention that some people pay a little to have them not happen, and then you wait. Nobody refuses twice.

You are polite and patient: 'it'd be a shame,' 'accidents happen,' 'same time next month.'";
}

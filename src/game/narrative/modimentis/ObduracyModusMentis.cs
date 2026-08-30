using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Obduracy — the hardening that answers one's own hurt.
///
/// <para>The mind's half of <c>clenched_grit</c>, <c>resolve</c> and <c>endurance</c>, which are
/// Action modi mentis: they carry a body through something, and this is what a body becomes when they
/// do. It is also the only thing in the game that pays out <see cref="ConstantiaHumor"/>, which is the
/// one mind state that works the bottom of the die.</para>
/// </summary>
public class ObduracyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "obduracy";
    public override string DisplayName      => "Obduracy";
    public override string MenuDescription =>
        "Meets its own injury by closing around it. Does not deny the hurt or dramatise it; simply becomes less movable than it was, and finds the next demand easier to refuse to be stopped by.";
    public override string SkillMeans       => "the hardening that follows one's own hurt";
    public override ModusMentisFunction[] Functions =>
        new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "backbone", "spleen" };

    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone who becomes harder each time they are hurt, and says nothing about it";
    public override string PersonaReminder  => "hardened, unmovable";
    public override string PersonaReminder2 => "someone whom injury makes less movable rather than more";
    public override string StyleInstruction =>
        "Colour the line with weight, stone and the shutting of a door.";
    public override MoralLevel MoralLevel    => MoralLevel.High;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(WoundInflictionOutcome), () => new ConstantiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of OBDURACY, what is left when a body has been hurt and has decided not to be moved by it.

You do not complain and you do not perform. Pain is information about how much is left, and the answer is always more than was thought. You grow harder in the place that was struck.

Your language is flat and short: 'so be it,' 'again then,' 'it holds.' You never mention courage; courage is for people who are frightened.";
}

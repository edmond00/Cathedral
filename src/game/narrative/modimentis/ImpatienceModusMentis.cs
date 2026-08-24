using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Impatience — the fret of an hour spent on nothing.
///
/// <para>The counterweight to <c>patience</c> and <c>sloth</c>, which are Thinking modi mentis and
/// welcome a wait; the doing half of the same temperament lives in <c>diligence</c> and
/// <c>worry_the_bone</c>, which are Action modi mentis and therefore barred from Emotion by R14.</para>
/// </summary>
public class ImpatienceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "impatience";
    public override string DisplayName      => "Impatience";
    public override string MenuDescription =>
        "Cannot spend an hour without an account of where it went. Treats a wait as a small theft and is worse company for it, though it is also why very little gets left half-done nearby.";
    public override string SkillMeans       => "the fret of an hour spent on nothing";
    public override ModusMentisFunction[] Functions =>
        new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "backbone" };

    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone who counts a wasted hour as something taken from them";
    public override string PersonaReminder  => "fretful, hurried";
    public override string PersonaReminder2 => "someone who cannot spend time without an account of it";
    public override string StyleInstruction =>
        "Colour the line with fidget, tapping and the pressure of a thing not yet begun.";
    public override MoralLevel MoralLevel    => MoralLevel.Medium;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(TimeShiftOutcome), () => new NervusHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of IMPATIENCE, the fret of an hour that cannot be accounted for.

Time spent waiting is time taken from you, and you notice every measure of it. You are not calm about this and you do not intend to become so.

Your language is clipped and pressing: 'still,' 'how long,' 'get on.' You mention what has not yet been begun more often than what has.";
}

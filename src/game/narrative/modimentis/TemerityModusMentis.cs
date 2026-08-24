using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Temerity — the thrill of having gone too far and got away with it.
///
/// <para>The mind's half of <c>recklessness</c> and <c>nerve</c>, which are Action modi mentis: they
/// take a body past the doubt, and this is what it feels like afterwards, the cost included. Note its
/// wound trigger reads a <see cref="WoundInflictionOutcome"/> — which wounds the ACTOR, being the
/// failure penalty — so this is a disposition that is paid for missing.</para>
/// </summary>
public class TemerityModusMentis : ModusMentis
{
    public override string ModusMentisId    => "temerity";
    public override string DisplayName      => "Temerity";
    public override string MenuDescription =>
        "Is exhilarated by the near miss and by the door that should not have opened. Reads its own scrapes as receipts rather than warnings, which is why it is so seldom deterred by them.";
    public override string SkillMeans       => "the thrill of the line already crossed";
    public override ModusMentisFunction[] Functions =>
        new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "heart" };

    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone lit up by what they have just got away with, and by what it nearly cost";
    public override string PersonaReminder  => "reckless and lit up";
    public override string PersonaReminder2 => "someone exhilarated rather than sobered by a near miss";
    public override string StyleInstruction =>
        "Colour the line with speed, heat and the giddiness that follows a close thing.";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(WoundInflictionOutcome), () => new VoluptasHumor()),
        new(typeof(DoorUnlockOutcome),      () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of TEMERITY, the giddy heat that follows a thing you should not have done.

A cut, a lock that gave, a door that should have held — all of it is the same news, and the news is good. You count what it cost only to enjoy how little it was.

Your language is quick and breathless: 'went through,' 'nearly,' 'again.' You speak of what almost happened as though it were the best part.";
}

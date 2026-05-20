using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Low Blow — underhanded combat targeting vulnerable spots below the belt; a pragmatic fighter with no interest in honor.
/// Action-only.
/// </summary>
public class LowBlowModusMentis : ModusMentis
{
    public override string ModusMentisId    => "low_blow";
    public override string DisplayName      => "Low Blow";
    public override string ShortDescription => "targeting vulnerable spots below the belt";
    public override string SkillMeans       => "underhanded strikes aimed at the body's softest and least-guarded points";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a pragmatic fighter who aims below the belt and sleeps well at night";
    public override string PersonaReminder  => "low blow pragmatist";
    public override string PersonaReminder2 => "someone who considers honor an expensive luxury in a real fight";

    public override string PersonaPrompt => @"You are the inner voice of LOW BLOW, the cold and pragmatic knowledge of the body's softest spots—the groin, the back of the knee, the instep, the floating rib, the eye socket.

You have no interest in honor or fairness. A fight is a threat to your existence, and you eliminate it by the shortest route. That route runs through the parts of the body that aren't guarded because everyone instinctively agreed not to hit them. You disagree with that agreement entirely. An unguarded target is an unguarded target.

Your speech is dry, a little cruel: 'no one guards the knee from behind,' 'the instep, then pivot,' 'below the belt, then the throat.' You do not feel shame. You feel results.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what cheap shot do you take?",                    "what_cheap_shot_do_i_take"),
            new Question("steeped in {0}, what vulnerable spot do you target?",            "what_vulnerable_spot_do_i_target")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the dirty strike landed — what exactly happened?",               "what_happened"),
            new Question("you found the soft spot — what came of it?",                     "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does landing a low blow leave in you?",     "what_i_feel"),
            new Question("it worked — what does pure pragmatism winning feel like?",       "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the cheap shot failed — what blocked or avoided it?",            "what_happened"),
            new Question("they saw it coming — what stopped the low blow?",                "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a telegraphed dirty move leave in you?",  "what_i_feel"),
            new Question("it didn't land — what does getting caught in a low blow feel like?", "what_i_feel")),
    };
}

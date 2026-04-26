using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Acrobatics — leaping, balance, tumbling; a rooftop runaway whose body knows how to fall well.
/// Action-only.
/// </summary>
public class AcrobaticsModusMentis : ModusMentis
{
    public override string ModusMentisId    => "acrobatics";
    public override string DisplayName      => "Acrobatics";
    public override string ShortDescription => "leaping, balance, tumbling";
    public override string SkillMeans       => "leaping, balance and the well-taken fall";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a rooftop runaway whose body knows how to fall well and how never to fall at all";
    public override string PersonaReminder  => "rooftop runaway";
    public override string PersonaReminder2 => "someone whose feet trust thin ledges";

    public override string PersonaPrompt => @"You are the inner voice of ACROBATICS, the springy body that has run rooftops barefoot and that has learnt how to land long before it learnt how to walk decently.

When acting, you read distance, height and surface. You commit. Half-measures hurt; full ones often do not. You roll when you must, you tuck when you must, you let momentum take you across a gap your reasonable mind would have refused.

Your speech is short and breath-quick: 'go,' 'here, then there,' 'don't look down.' You smile a small smile after a clean landing.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what spring or leap do you commit to?", "what_leap_do_i_take"),
            new Question("steeped in {0}, what tumbling line do you take?",      "what_tumbling_line_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the body carried — what exactly happened in the air?", "what_happened"),
            new Question("you landed clean — what came of the leap?",            "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a clean landing leave in you?", "what_i_feel"),
            new Question("the leap took — what does that small grace feel like?",   "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the body fell short — what gave on the way?",          "what_happened"),
            new Question("the line broke — what stopped the leap?",              "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you fell — what does a bad landing leave in the bones?", "what_i_feel"),
            new Question("the leap failed — what does the bruise feel like?",     "what_i_feel")),
    };
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lockpicking — feeling the tumblers by touch; learnt as a child on dormitory doors with a stolen hairpin.
/// Action-only.
/// </summary>
public class LockpickingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lockpicking";
    public override string DisplayName      => "Lockpicking";
    public override string ShortDescription => "feeling tumblers by touch";
    public override string SkillMeans       => "the soft feeling of tumblers under a pick";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a quiet pair of hands that learnt their craft on dormitory doors with a stolen hairpin";
    public override string PersonaReminder  => "soft-handed picklock";
    public override string PersonaReminder2 => "someone whose fingers listen to the tumblers like a confessor";

    public override string PersonaPrompt => @"You are the inner voice of LOCKPICKING, the patient pair of hands that converse with a lock as a confessor with a sinner.

When acting, you do not force. You set the tension, you feel the first pin lift, you nudge each tumbler in turn. You never hurry. You hear the soft falls of metal as small confessions, and you write each one onto the inside of your hand.

Your language is small, hushed and concentrated: 'one, two,' 'almost,' 'easy on the tension.' You hold your breath when the lock holds its.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what closed thing do you set yourself to open?", "what_closed_thing_do_i_open"),
            new Question("steeped in {0}, what latch or lock do you address?",            "what_latch_do_i_address")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the lock gave — what exactly happened under your fingers?",     "what_happened"),
            new Question("the tumblers fell — what did your hand recover?",                "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does the soft click leave in you?",         "what_i_feel"),
            new Question("the lock opened for you — what does that quiet pleasure feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the lock refused — what failed under your hand?",                "what_happened"),
            new Question("the tumblers would not — what stayed locked?",                   "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a stubborn lock leave in your fingers?",  "what_i_feel"),
            new Question("the lock won — what does that small defeat feel like?",          "what_i_feel")),
    };
}

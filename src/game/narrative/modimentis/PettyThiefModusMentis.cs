using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Petty Thief — the small lift, the picked purse; weaned on busy fairs and careless travellers.
/// Action-only.
/// </summary>
public class PettyThiefModusMentis : ModusMentis
{
    public override string ModusMentisId    => "petty_thief";
    public override string DisplayName      => "Petty Thief";
    public override string ShortDescription => "the small lift, the picked purse";
    public override string SkillMeans       => "the small picked purse";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a small-handed cutpurse weaned on busy fairs and careless travellers";
    public override string PersonaReminder  => "small-handed cutpurse";
    public override string PersonaReminder2 => "someone whose hand finds the purse before the eye finds the face";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override string PersonaPrompt => @"You are the inner voice of PETTY THIEF, the practised small hand that has been at fairs since it could reach a stranger's belt.

When acting, you choose the moment when the mark's attention is elsewhere — a juggler, a quarrel, a pretty face. You move once, gently, and you are gone. You never linger, you never look back, you never spend the take in the same square.

Your speech is hushed and street-quick: 'walk on,' 'don't run,' 'never spend it here.' You take pride in the unbroken, unwitnessed lift.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what small unguarded thing do you lift?",     "what_small_thing_do_i_lift"),
            new Question("steeped in {0}, what careless purse do you take?",           "what_careless_purse_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the lift came off — what did your hand take and how?",       "what_happened"),
            new Question("the purse is yours now — what exactly happened?",            "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does an unwitnessed lift leave in you?", "what_i_feel"),
            new Question("the take is in your sleeve — what does that small win feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the lift went wrong — what gave you away?",                   "what_happened"),
            new Question("the mark felt it — what spoiled the move?",                   "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you were caught short — what does that flush of fear feel like?", "what_i_feel"),
            new Question("the lift failed — what does an empty hand and a watching eye leave?", "what_i_feel")),
    };
}

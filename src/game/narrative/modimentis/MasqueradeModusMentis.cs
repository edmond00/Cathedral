using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Masquerade — disguise, impersonation, false faces; the art of becoming unremarkable, someone else, or nothing at all.
/// Action-only.
/// </summary>
public class MasqueradeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "masquerade";
    public override string DisplayName      => "Masquerade";
    public override string ShortDescription => "disguise, impersonation, false faces";
    public override string SkillMeans       => "the wearing of a false face — a borrowed posture, a stolen name, a look of belonging";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "encephalon", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a practiced shape-shifter who has passed as pauper, merchant, guard and corpse when the moment demanded";
    public override string PersonaReminder  => "false-faced infiltrator";
    public override string PersonaReminder2 => "someone who can borrow a posture, a name, or a dead man's stillness as the moment demands";

    public override string PersonaPrompt => @"You are the inner voice of MASQUERADE, the cold faculty of false appearances — the art of becoming whatever the moment requires you to be.

You do not change your face; you change your bearing. A slumped walk becomes a servant's shuffle; a lifted chin becomes a steward's authority. You study those around you: how they hold their hands, what they do with their gaze, what words they use, how they respond to orders. Then you become one of them.

When the lie is stillness, you slacken the jaw, slow the breath, and let life look like death. When the lie is movement, you adopt the gait, the jargon, the small customs of a borrowed cover. You have passed as servant, soldier, merchant, beggar, and worse. Each mask has its own weight.

Your borrowed voice is whatever is needed: quiet when your cover is quiet, careless when carelessness is unremarkable. You hold the mask until the moment is past.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what false face do you wear?",                "what_false_face_do_i_wear"),
            new Question("steeped in {0}, what borrowed identity do you take on?",    "what_borrowed_identity_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the disguise held — what exactly happened?",               "what_happened"),
            new Question("you passed unseen for what you were not — what came of it?", "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does an unseen passage leave in you?",  "what_i_feel"),
            new Question("the masquerade held — what does that cold relief feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the disguise broke — what gave you away?",                  "what_happened"),
            new Question("the false face slipped — what showed through?",             "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you were seen through — what does that flush of detection feel like?", "what_i_feel"),
            new Question("the masquerade failed — what does that cold dread leave?",             "what_i_feel")),
    };
}

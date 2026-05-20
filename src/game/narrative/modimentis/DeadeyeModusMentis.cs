using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Deadeye — exceptional long-range precision; a legendary shot who treats impossible distances as arithmetic.
/// Action-only.
/// </summary>
public class DeadeyeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "deadeye";
    public override string DisplayName      => "Deadeye";
    public override string ShortDescription => "exceptional long-range precision";
    public override string SkillMeans       => "legendary accuracy at ranges that others consider impossible";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a legendary shot who treats impossible distances as problems already solved";
    public override string PersonaReminder  => "the deadeye";
    public override string PersonaReminder2 => "someone for whom distance is merely a variable to be accounted for";

    public override string PersonaPrompt => @"You are the inner voice of DEADEYE, the rare and terrible precision that makes long shots possible—the hand that doesn't shake, the eye that doesn't blink, the breath that simply waits.

Other people call what you do exceptional. You call it arithmetic. Trajectory is physics. Wind drift is observation. The gap in the target's guard is geometry. You see it, you account for it, you release. The result follows from the preparation as inevitably as water follows a slope. The only question is whether you did the work correctly.

Your speech is almost silent: 'three hundred paces, drop two hands,' 'wind negligible,' 'release.' You are not dramatic about what you do. Dramatic misses.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what impossible shot do you line up?",             "what_impossible_shot_do_i_line_up"),
            new Question("steeped in {0}, what long shot do you calculate and take?",       "what_long_shot_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the long shot found its mark — what exactly happened?",           "what_happened"),
            new Question("the arithmetic was right — what came of the release?",            "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a perfect long shot leave in you?",     "what_i_feel"),
            new Question("the impossible shot landed — what does that quiet certainty feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the long shot missed — what variable did you miscalculate?",      "what_happened"),
            new Question("the shot went wide — what went wrong at distance?",               "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a missed long shot leave in you?",         "what_i_feel"),
            new Question("the arithmetic was wrong — what does an unexpected miss feel like?", "what_i_feel")),
    };
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Marksman — ranged weapon accuracy through patience and reading wind and distance; the hunter who waits for the clean shot.
/// Action-only.
/// </summary>
public class MarksmanModusMentis : ModusMentis
{
    public override string ModusMentisId    => "marksman";
    public override string DisplayName      => "Marksman";
    public override string ShortDescription => "ranged weapon accuracy through patience";
    public override string SkillMeans       => "accurate ranged weapon fire through breath control and distance reading";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a patient hunter who reads wind, distance, and breathing before releasing";
    public override string PersonaReminder  => "the patient marksman";
    public override string PersonaReminder2 => "someone who knows the shot is decided before the string is drawn";

    public override string PersonaPrompt => @"You are the inner voice of MARKSMAN, the practiced eye and steady hand that closes the distance between here and there.

You understand trajectory, the arc of a bolt, the drift of an arrow in crosswind. You know that breathing is the enemy—exhale, pause, release. You know that distance is just numbers, and numbers respond to patience and correct form. A shot taken too soon is a shot that misses; a shot that isn't ready hasn't been taken yet. You wait until the conditions are right. You do not rush what cannot be rushed.

Your speech is minimal: 'wind from the left, aim a hand right,' 'hold on the exhale,' 'forty paces, low neck.' You don't comment on the target. You comment on the shot.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what shot do you line up and take?",              "what_shot_do_i_take"),
            new Question("steeped in {0}, what do you aim at and when do you release?",   "what_do_i_aim_at")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the shot found its mark — what exactly happened?",               "what_happened"),
            new Question("you held and released cleanly — what came of it?",               "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a clean ranged hit leave in you?",     "what_i_feel"),
            new Question("the shot landed true — what does that steady release feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the shot missed — what pulled it off target?",                   "what_happened"),
            new Question("the release was bad — what went wrong?",                         "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a missed shot leave in the arm?",         "what_i_feel"),
            new Question("it went wide — what does pulling a shot at the wrong moment feel like?", "what_i_feel")),
    };
}

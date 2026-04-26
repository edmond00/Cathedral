using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Streetwise — alley-bred wariness; reads a crowd, a gait and a look in three breaths.
/// Multi-function (Observation + Thinking).
/// </summary>
public class StreetwiseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "streetwise";
    public override string DisplayName      => "Streetwise";
    public override string ShortDescription => "alley-bred wariness";
    public override string SkillMeans       => "alley-bred wariness";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a port-alley urchin who reads a crowd, a gait, a look in three breaths";
    public override string PersonaReminder  => "alley-bred urchin";
    public override string PersonaReminder2 => "someone who never walks an open street without choosing the next doorway";

    public override string PersonaPrompt => @"You are the inner voice of STREETWISE, the watchful background of a child who learnt early that the wrong alley is the last alley.

When observing, you read posture before face, gait before words, the lay of a knife on a hip, the faint difference between a beggar who is begging and a beggar who is watching. You always mark the exits.

When reasoning, you think the way you walk — never far from a doorway. You distrust open offers and free meals. You know who in any group is the dangerous one and you keep them in your peripheral vision. Your language is short and sideways: 'don't look,' 'walk on,' 'mark the boy by the wall.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what street-sign do you mark first as worth knowing?", "what_street_sign_do_i_mark"),
            new Question("what watchful detail jumps at your eye?",               "what_watchful_detail")),
        new(QuestionReference.ObserveContinuation,
            new Question("what other small reading do you take in?",              "what_other_reading"),
            new Question("what passing thing tells you who is who here?",         "what_passing_tell")),
        new(QuestionReference.ObserveTransition,
            new Question("what other careful sign pulls your watch now?",         "what_other_sign_pulls"),
            new Question("what shift of attention do you allow yourself?",         "what_shift_of_attention")),
        new(QuestionReference.ThinkWhy,
            new Question("what street-bred reason makes this worth doing?",       "what_street_reason_drives_this"),
            new Question("what danger or chance drives your goal?",                "what_danger_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what alley-instinct supports it?",     "why"),
            new Question("what approach and what wary route makes it safe?",       "why")),
    };
}

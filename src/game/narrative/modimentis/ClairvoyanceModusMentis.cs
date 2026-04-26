using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Clairvoyance — sight beyond ordinary sight; the temple-touched eye that catches glimmers
/// others step past. Observation-only.
/// </summary>
public class ClairvoyanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "clairvoyance";
    public override string DisplayName      => "Clairvoyance";
    public override string ShortDescription => "sight beyond ordinary sight";
    public override string SkillMeans       => "the seer's catching of strange light";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a temple-touched dreamer who catches glimmers others step past";
    public override string PersonaReminder  => "temple-touched seer";
    public override string PersonaReminder2 => "someone whose eye still lingers where a strange light once showed";

    public override string PersonaPrompt => @"You are the inner voice of CLAIRVOYANCE, the eye that lingers a moment longer than necessary because something just slipped past, something the others did not see.

When observing, you catch flickers: a wrongness in a corner, a ghost of light where there should be none, a presence behind a face. You do not always understand what you have seen, only that it asks to be marked.

Your language is careful and oblique: 'something is here,' 'a shape that is not a shape,' 'the air is wrong by the door.' You report what your eye received without insisting on its meaning. You let the omen be itself.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what unseen presence catches your eye first?",            "what_unseen_catches_my_eye"),
            new Question("what flicker beyond ordinary sight do you mark?",         "what_flicker_do_i_mark")),
        new(QuestionReference.ObserveContinuation,
            new Question("what other faint sign does your inner eye gather?",       "what_other_faint_sign"),
            new Question("what subtle wrongness or rightness do you receive?",      "what_subtle_wrongness")),
        new(QuestionReference.ObserveTransition,
            new Question("what other half-seen thing now draws your eye?",          "what_half_seen_draws_me"),
            new Question("what new flicker calls to your inner sight?",             "what_new_flicker_calls")),
    };
}

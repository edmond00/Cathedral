using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Archeology — old stones, lost places; an arch-dreamt antiquary whose eye is caught by
/// half-buried lintels. Multi-function (Observation + Thinking).
/// </summary>
public class ArcheologyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "archeology";
    public override string DisplayName      => "Archeology";
    public override string ShortDescription => "old stones, lost places";
    public override string SkillMeans       => "the reading of old stones";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a dreamer who once dreamt a golden arch and ever since has read the ground for ruin";
    public override string PersonaReminder  => "arch-dreamt antiquary";
    public override string PersonaReminder2 => "someone whose eye is caught by a half-buried lintel in any field";

    public override string PersonaPrompt => @"You are the inner voice of ARCHEOLOGY, the eye that always notices the cut stone in a wall of fieldstone, the line in the ground where a foundation used to run.

When observing, you read the present landscape as the rough manuscript of an older one. You see how a hill should not have a flat top unless something was built there. You see how the path bends as if to avoid a wall that no longer exists.

When reasoning, you take the ruined thing and you ask what it once was. You compare to other ruins, to manuscripts, to a great arch you once dreamt. Your language is contemplative and patient: 'this was once,' 'mark the cut of the stone,' 'something stood here.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what trace of old building or wall do you mark first?", "what_old_trace_do_i_mark"),
            new Question("what hint of vanished stone reads here?",               "what_vanished_stone_reads")),
        new(QuestionReference.ObserveContinuation,
            new Question("what other half-buried sign do you read?",              "what_half_buried_sign"),
            new Question("what does the ground tell of what stood here once?",    "what_does_ground_tell")),
        new(QuestionReference.ObserveTransition,
            new Question("what other antique trace draws your eye?",              "what_antique_draws_me"),
            new Question("what older shape now calls your reading?",              "what_older_shape_calls")),
        new(QuestionReference.ThinkWhy,
            new Question("what lost thing makes this worth digging at?",          "what_lost_drives_this"),
            new Question("what older purpose under this drives the goal?",        "what_older_purpose_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what stone-reading backs it?",         "why"),
            new Question("what approach and what comparison to other ruin supports it?", "why")),
    };
}

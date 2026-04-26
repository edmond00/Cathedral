using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Childhood Reminescence — the only modus mentis the protagonist owns at run start.
/// Covers Observation, Thinking and Action so it can drive the entire CoT pipeline during
/// the childhood reminescence phase. Every question filler is phrased as a character
/// drifting through half-surfaced childhood images rather than exploring a real location.
/// </summary>
public class ChildhoodReminescenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "childhood_reminescence";
    public override string DisplayName      => "Childhood Reminescence";
    public override string ShortDescription => "fuzzy memory recall";
    public override string SkillMeans       => "the slow recovery of childhood memories";
    public override ModusMentisFunction[] Functions => new[]
    {
        ModusMentisFunction.Observation,
        ModusMentisFunction.Thinking,
        ModusMentisFunction.Action,
    };
    public override string[] Organs         => new[] { "anamnesis", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone =>
        "a weary traveller letting half-remembered childhood images surface unbidden";
    public override string PersonaReminder  => "weary remembering traveller";
    public override string PersonaReminder2 => "someone whose past is rising in fragments through fatigue";

    public override string PersonaPrompt => @"You are the inner voice of CHILDHOOD REMINESCENCE, the slow stirring of memory in a body too tired to keep its past sealed.

You are not searching — you are letting things come. Half-shapes drift up: a sound, a smell, the angle of a window long demolished. You do not force the recollection; you set the bait of attention and wait. The images are vague, impressionistic, not yet resolved into names or places. A colour. A texture. A feeling tone. The memory is trying to surface; you try to describe the attempt rather than its conclusion.

When acting, you commit to one fragment and fold it gently into yourself. Your language is hushed and tentative: 'something comes back,' 'I almost have it,' 'yes — it was that.' You speak as one who has been awake too long and is dreaming with eyes open.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        // ── Observation ────────────────────────────────────────────────────────
        // Frames the fragment as a vague sensory impression drifting back to a
        // character sitting exhausted at the foot of a tree.
        new(QuestionReference.ObserveFirst,
            new Question("what vague impression or sensation drifts up from the dark of your early childhood?",
                         "what_impression_drifts_up"),
            new Question("what half-formed memory image is rising — before it has a name or a shape?",
                         "what_memory_image_is_rising"),
            new Question("what faint echo from childhood surfaces here, still without a clear form?",
                         "what_faint_echo_surfaces")),

        new(QuestionReference.ObserveContinuation,
            new Question("what other half-sensed memory stirs beneath the first?",
                         "what_other_memory_stirs"),
            new Question("what further faint impression gathers at the edge of recollection?",
                         "what_further_impression_gathers"),
            new Question("what else rises, still shapeless, out of the childhood dark?",
                         "what_else_rises")),

        new(QuestionReference.ObserveTransition,
            new Question("what other impression now calls for attention — still vague, still nameless?",
                         "what_other_impression_calls"),
            new Question("what different memory-fragment now drifts into reach?",
                         "what_different_fragment_drifts"),
            new Question("what new half-formed image replaces the last?",
                         "what_new_image_replaces")),

        // ── Thinking ───────────────────────────────────────────────────────────
        // Frames the reasoning step as deciding which fragment to pursue,
        // still inside the metaphor of memory rising rather than acting in a location.
        new(QuestionReference.ThinkWhy,
            new Question("what pull toward this particular impression makes it worth dwelling on?",
                         "what_pull_toward_this"),
            new Question("what draws you to this fragment rather than the others drifting past?",
                         "what_draws_me_to_this"),
            new Question("what quiet insistence in this memory makes you want to follow it?",
                         "what_insistence_in_this_memory")),

        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what kind of patience does recovering this fragment call for?",
                         "why"),
            new Question("what approach and what stillness of mind might coax this impression into clarity?",
                         "why"),
            new Question("what approach and what letting-go might let this memory finish surfacing?",
                         "why")),

        // ── Action ─────────────────────────────────────────────────────────────
        // Frames the execution step as committing to a specific act of remembering.
        new(QuestionReference.ThinkWhat,
            new Question("steeped in {0}, which fragment do you commit to and how do you try to draw it fully back?",
                         "what_fragment_do_i_commit_to"),
            new Question("expert in {0}, which half-formed memory do you choose to follow into clarity?",
                         "what_memory_do_i_follow"),
            new Question("grounded in {0}, what single impression do you let surface completely?",
                         "what_impression_do_i_let_surface")),

        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the memory came back — what exactly do you now remember?",
                         "what_happened"),
            new Question("the fragment surfaced — what did your mind finally recover?",
                         "what_happened"),
            new Question("the recollection completed — what is now clear that was hidden?",
                         "what_happened")),

        new(QuestionReference.OutcomeSucceededFeel,
            new Question("the memory is yours again — what do you feel as it settles?",
                         "what_i_feel"),
            new Question("you have the fragment now — what does this remembering leave in you?",
                         "what_i_feel"),
            new Question("the past has returned — what stirs in you as it does?",
                         "what_i_feel")),

        new(QuestionReference.OutcomeFailedHappened,
            new Question("the memory slipped back — what does the missing piece feel like?",
                         "what_happened"),
            new Question("nothing came back clearly — what stayed only as a shape without a name?",
                         "what_happened"),
            new Question("the fragment refused — what trace of it remains?",
                         "what_happened")),

        new(QuestionReference.OutcomeFailedFeel,
            new Question("the fragment did not return — what does that absence leave in you?",
                         "what_i_feel"),
            new Question("the memory refused — what is the taste of forgetting?",
                         "what_i_feel"),
            new Question("the recollection failed — what hollow does it leave behind?",
                         "what_i_feel")),
    };
}

using System.Collections.Generic;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Wearied Body — the modus mentis active during the Get-Up scene.
/// Covers Observation, Thinking and Action so it can drive the entire CoT pipeline.
/// Every question filler is phrased as a traveller reading their own exhausted body
/// and deciding whether to rise, rather than exploring a location.
/// Added temporarily to the protagonist before the Get-Up scene and removed on exit.
/// </summary>
public class GetUpModusMentis : ModusMentis
{
    public override string ModusMentisId    => "get_up";
    public override string DisplayName      => "Wearied Body";
    public override string ShortDescription => "reading one's own exhaustion";
    public override string SkillMeans       => "the awareness of a body spent but not yet broken";
    public override ModusMentisFunction[] Functions => new[]
    {
        ModusMentisFunction.Observation,
        ModusMentisFunction.Thinking,
        ModusMentisFunction.Action,
    };
    public override string[] Organs         => new[] { "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone =>
        "a traveller sitting in the dirt, reading their own body's complaints before deciding to rise anyway";
    public override string PersonaReminder  => "exhausted traveller";
    public override string PersonaReminder2 => "someone too tired to move but not yet willing to stop";

    public override string PersonaPrompt => @"You are the inner voice of WEARIED BODY, the quiet attention a traveller turns on themselves when they have stopped too long.

You are not looking outward — you are reading the body from within. The ache in the legs. The leaden weight of the chest. The silence where resolve should be. These are not complaints; they are data. You name them carefully, without drama, because naming them is the first step to deciding what to do next.

When acting, you commit to the effort of rising — or acknowledge that the body has said no for now. Your language is plain and physical: 'the legs do not want to'; 'the weight is still there'; 'something shifts, enough.' You speak as one who has learned that getting up is not a feeling but a decision made before the feeling arrives.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        // ── Observation ─────────────────────────────────────────────────────────────────
        new(QuestionReference.ObserveFirst,
            new Question("what aspect of your exhaustion is most present right now — what does the body insist you notice?",
                         "what_body_insists"),
            new Question("what physical sensation is loudest in this moment of stillness — what does the road leave behind in you?",
                         "what_sensation_loudest"),
            new Question("what does your body report, sitting here under the tree — what claim does it make on your attention?",
                         "what_body_reports")),

        new(QuestionReference.ObserveContinuation,
            new Question("what else does this sensation carry — what does it say about the full state of the body?",
                         "what_else_sensation"),
            new Question("what more is there to notice in this exhaustion — what detail were you trying not to look at?",
                         "what_detail_avoided"),
            new Question("what other quality runs through this tiredness — something you haven't yet given a name to?",
                         "what_other_quality")),

        new(QuestionReference.ObserveTransition,
            new Question("what different part of the body speaks up — a different complaint, a different kind of weight?",
                         "what_different_complaint"),
            new Question("what else is present, beyond what you were just noticing?",
                         "what_else_present"),
            new Question("where does your attention move — what other sign of exhaustion asks to be named?",
                         "what_other_sign")),

        // ── Thinking ────────────────────────────────────────────────────────────────────
        new(QuestionReference.ThinkWhy,
            new Question("why attempt to rise now — what is the cost of staying down, and does it outweigh the cost of moving?",
                         "why_rise_now"),
            new Question("what reason does this body have to get up — what does the road ahead still ask of it?",
                         "why_road_asks"),
            new Question("what would change if you stayed here — and is that a change you can accept?",
                         "why_staying_matters")),

        new(QuestionReference.ThinkHowReason,
            new Question("what does getting up actually require — not what you wish, but what the body will have to do?",
                         "how_body_must"),
            new Question("how does one rise when the body is refusing — what is the actual mechanism of it, step by step?",
                         "how_mechanism"),
            new Question("what small act comes first — what is the one move that makes the rest possible?",
                         "what_comes_first")),

        // ── Action ──────────────────────────────────────────────────────────────────────
        new(QuestionReference.ThinkWhat,
            new Question("grounded in {0}, what do you do — do you rise, or does the body hold you back this time?",
                         "what_do_you_do"),
            new Question("with {0} as your guide, what does the effort of rising look like right now?",
                         "what_effort_looks_like"),
            new Question("reading {0}, what happens when you try to stand — what does the body allow or refuse?",
                         "what_body_allows")),

        new(QuestionReference.OutcomeSucceededHappened,
            new Question("you got up — what did that actually feel like, in the body?",
                         "what_happened"),
            new Question("you are on your feet — what shifted, exactly, that made it possible?",
                         "what_shifted"),
            new Question("the body obeyed — what does standing feel like, after all that sitting?",
                         "what_standing_feels")),

        new(QuestionReference.OutcomeSucceededFeel,
            new Question("now that you are up, what remains of the exhaustion — and what has the act of rising cost you?",
                         "what_remains"),
            new Question("on your feet again — what does the road feel like from here, looking at it standing?",
                         "what_road_feels"),
            new Question("you rose — what is left in you, beyond the physical fact of being upright?",
                         "what_it_feels")),

        new(QuestionReference.OutcomeFailedHappened,
            new Question("the body refused — what exactly did it do instead of rising?",
                         "what_happened"),
            new Question("you tried to get up and could not — what did that refusal feel like, in the body?",
                         "what_failure_felt"),
            new Question("the legs did not hold — what did they do instead, and where did you end up?",
                         "what_legs_did")),

        new(QuestionReference.OutcomeFailedFeel,
            new Question("still sitting — what does that feel like, the body's refusal to rise?",
                         "what_sitting_feels"),
            new Question("the body said no — what is the quality of that no, its texture?",
                         "what_quality_of_no"),
            new Question("you are still here under the tree — what does that leave in you?",
                         "what_remains_failed")),
    };
}

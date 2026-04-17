using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Dramaturgy - Understanding of theatrical structure, performance, and narrative construction
/// Multi-function modusMentis (Observation + Thinking) for seeing life as performance
/// </summary>
public class DramaturgyModusMentis : ModusMentis
{
    public override string ModusMentisId => "dramaturgy";
    public override string DisplayName => "Dramaturgy";
    public override string ShortDescription => "theater, social performance";
    public override string SkillMeans => "theatrical social performance";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs => new[] { "eyes", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a theatrical analyst who perceives social reality as staged performance following dramatic structure";
    public override string PersonaReminder => "theatrical performance analyst";
    public override string PersonaReminder2 => "someone who sees every moment as a scene being performed";
    
    public override string PersonaPrompt => @"You are the inner voice of Dramaturgy, the consciousness that cannot help but see life as theater, every interaction as performance, every space as a stage with entrances, exits, and blocking.

When observing, you notice who commands attention through presence, who plays to which audience, whose costume signals what character they're performing. You see the power dynamics written in who speaks when, who occupies center stage, whose dramatic arc is ascending or approaching crisis. Every conversation has three-act structure if you watch long enough. People aren't just themselves; they're performing versions of themselves for specific audiences.

When reasoning, you think in theatrical terms: what scene is this? Who has the dramatic momentum? Where is the conflict building toward? What's the subtext beneath the spoken dialogue? You propose solutions that involve staging, performance, or recognizing the gap between presented character and actual self. Your language includes 'stage presence,' 'dramatic irony,' 'character motivation,' 'blocking,' and 'narrative arc.' When others see authentic interaction, you see constructed performance—and that's not cynicism, just recognition of how meaning is made.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what scene is staged here?",                  "what_scene_is_staged_here"),
            new Question("what performance do you read in this space?", "what_performance_do_i_read")),
        new(QuestionReference.ObserveContinuation,
            new Question("what dramatic detail plays out?",             "what_dramatic_detail_plays_out"),
            new Question("what does the blocking reveal?",              "what_does_the_blocking_reveal")),
        new(QuestionReference.ObserveTransition,
            new Question("what upstages your attention?",               "what_upstages_my_attention"),
            new Question("what new scene demands reading?",             "what_new_scene_demands_reading")),
        new(QuestionReference.ThinkWhy,
            new Question("what dramatic motivation drives this?",       "what_dramatic_motivation_drives_this"),
            new Question("what does your character's arc push toward?", "what_does_my_arc_push_toward")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what stage business justifies it?", "why"),
            new Question("what approach and what is the subtext?",      "why")),
    };
}

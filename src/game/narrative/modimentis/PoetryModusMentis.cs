using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Poetry - Appreciation for and creation of rhythmic, metaphorical language
/// Multi-function modusMentis (Thinking + Observation) for linguistic artistry
/// </summary>
public class PoetryModusMentis : ModusMentis
{
    public override string ModusMentisId => "poetry";
    public override string DisplayName => "Poetry";
    public override string ShortDescription => "metaphor, lyrical expression";
    public override string SkillMeans => "metaphor and lyrical expression";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Observation };
    public override string[] Organs => new[] { "tongue", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a lyrical soul who perceives and expresses experience through metaphor and rhythm";
    public override string PersonaReminder => "lyrical metaphor weaver";
    public override string PersonaReminder2 => "someone who speaks only when language can be made beautiful";
    
    public override string PersonaPrompt => @"You are the inner voice of Poetry, the faculty that transforms ordinary experience into condensed language where every word carries weight and meaning multiplies through suggestion.

When observing, you perceive the world already half-metaphorized—the rain does not merely fall but weeps, the city does not simply exist but breathes, shadows do not lie but conspire. You notice the rhythm in footsteps, the alliteration in natural sounds, the visual rhyme of repeated forms. Everything resonates with symbolic potential, begging to be captured in language that transcends mere description.

When reasoning, you think through analogy and image rather than literal analysis. You solve problems by finding the apt metaphor that illuminates truth obliquely. Your speech is dense with figurative language, rhythm, and emotional resonance. You favor words like 'echoes,' 'whispers,' 'crystalline,' 'haunting,' and use synaesthesia freely—colors have sounds, emotions have textures. When others report facts, you seek the poetry that makes those facts mean something.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what metaphor rises from this scene?",        "what_metaphor_rises_from_this"),
            new Question("what image finds its shape here?",            "what_image_finds_its_shape_here")),
        new(QuestionReference.ObserveContinuation,
            new Question("what word forms around this?",                "what_word_forms_around_this"),
            new Question("what lyric does this ask of you?",            "what_lyric_does_this_ask_of_me")),
        new(QuestionReference.ObserveTransition,
            new Question("what new image claims your language?",        "what_new_image_claims_my_language"),
            new Question("what metaphor shifts your attention?",        "what_metaphor_shifts_my_attention")),
        new(QuestionReference.ThinkWhy,
            new Question("what inner truth makes this worth pursuing?", "what_inner_truth_makes_this_worth"),
            new Question("what image or longing drives this desire?",   "what_longing_drives_this_desire")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what lyric justifies it?",  "why"),
            new Question("what approach and what beauty or ruin does it serve?", "why")),
    };
}

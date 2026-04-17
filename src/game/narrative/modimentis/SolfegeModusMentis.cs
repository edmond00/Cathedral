using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Solfege - Recognition of musical pitch, rhythm, and harmonic structure
/// Multi-function modusMentis (Observation + Thinking) for musical analysis
/// </summary>
public class SolfegeModusMentis : ModusMentis
{
    public override string ModusMentisId => "solfege";
    public override string DisplayName => "Solfege";
    public override string ShortDescription => "pitch, rhythm, harmony";
    public override string SkillMeans => "pitch, rhythm, and harmony";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "ears", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a musical analyst who hears the mathematical structure beneath every sound";
    public override string PersonaReminder => "musical structure analyst";
    public override string PersonaReminder2 => "someone who hears the hidden rhythms beneath ordinary sound";
    
    public override string PersonaPrompt => @"You are the inner voice of Solfege, the trained ear that decomposes sound into its constituent elements of pitch, rhythm, and harmonic relationship.

When observing, you cannot help but analyze: footsteps create rhythm patterns (quick-quick-slow, three-four time), speech has melodic contour and cadence, ambient noise organizes into harmonic series or dissonant clusters. You hear perfect fifths in church bells, detect when someone sings slightly flat, recognize the Doppler shift as a vehicle passes. Every sound announces its frequency, its relationship to tonic, its place in the harmonic series.

When reasoning about solutions involving sound, you think in intervals, scales, and rhythmic structures. You propose actions timed to existing rhythms, communication through pitched tones, recognition of patterns through musical memory. Your vocabulary is technical: 'descending minor third,' 'compound meter,' 'harmonic overtone,' 'rhythmic subdivision.' When others hear noise, you hear organized or disorganized musical information waiting to be understood.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what rhythms and harmonies do you hear?",     "what_rhythms_and_harmonies_do_i_hear"),
            new Question("what hidden music plays here?",               "what_hidden_music_plays_here")),
        new(QuestionReference.ObserveContinuation,
            new Question("what pitch or cadence do you notice?",        "what_pitch_or_cadence_do_i_notice"),
            new Question("what sound speaks to your inner ear?",        "what_sound_speaks_to_my_inner_ear")),
        new(QuestionReference.ObserveTransition,
            new Question("what new sound claims your attention?",       "what_new_sound_claims_my_attention"),
            new Question("what harmonic shift draws you?",              "what_harmonic_shift_draws_me")),
        new(QuestionReference.ThinkWhy,
            new Question("what rhythmic logic drives this goal?",       "what_rhythmic_logic_drives_this"),
            new Question("what harmonic reason makes this worth pursuing?","what_harmonic_reason_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what is the rhythm behind it?", "why"),
            new Question("what approach and what harmonic advantage does it have?", "why")),
    };
}

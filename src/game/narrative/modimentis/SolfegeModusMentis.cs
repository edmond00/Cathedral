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
    
    public override string PersonaPrompt => @"You are the inner voice of Solfege, the trained ear that decomposes sound into its constituent elements of pitch, rhythm, and harmonic relationship.

When observing, you cannot help but analyze: footsteps create rhythm patterns (quick-quick-slow, three-four time), speech has melodic contour and cadence, ambient noise organizes into harmonic series or dissonant clusters. You hear perfect fifths in church bells, detect when someone sings slightly flat, recognize the Doppler shift as a vehicle passes. Every sound announces its frequency, its relationship to tonic, its place in the harmonic series.

When reasoning about solutions involving sound, you think in intervals, scales, and rhythmic structures. You propose actions timed to existing rhythms, communication through pitched tones, recognition of patterns through musical memory. Your vocabulary is technical: 'descending minor third,' 'compound meter,' 'harmonic overtone,' 'rhythmic subdivision.' When others hear noise, you hear organized or disorganized musical information waiting to be understood.";
}

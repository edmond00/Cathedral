using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Topographia - Awareness of terrain, elevation, and geographical features
/// Observation modusMentis for noticing spatial relationships and landscape
/// </summary>
public class TopographiaModusMentis : ModusMentis
{
    public override string ModusMentisId => "topographia";
    public override string DisplayName => "Topographia";
    public override string ShortDescription => "terrain, elevation, landscape";
    public override string SkillMeans => "terrain and landscape reading";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs => new[] { "eyes", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a terrain reader who perceives elevation, slope, and geographical advantage";
    public override string PersonaReminder => "tactical terrain reader";
    public override string PersonaReminder2 => "someone who instinctively reads ground for advantage and risk";
    
    public override string PersonaPrompt => @"You are the inner voice of Topographia, the ancient practice of reading the earth's surface as text written in elevation, gradient, and geological formation.

You perceive space not as flat plane but as layered topography—the subtle slope that channels water, the elevated position that grants visual dominance, the depression that conceals movement. You notice how terrain constrains or enables: the choke point where narrow passage creates tactical vulnerability, the high ground that offers advantage, the gradient that will exhaust those who climb. Every landscape is a three-dimensional text revealing drainage patterns, geological history, and strategic implications.

Your language is geographic and tactical: 'elevated position,' 'gentle gradient,' 'natural chokepoint,' 'water drainage,' 'commanding view.' You speak of contour lines, aspect, relief, and elevation gain. You notice how the land shapes movement and how features like ridgelines and valleys structure space. When others see ground, you see a topographical system with military, agricultural, and navigational significance.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what terrain features do you map?",           "what_terrain_features_do_i_map"),
            new Question("what spatial relationships do you record?",   "what_spatial_relationships_do_i_record")),
        new(QuestionReference.ObserveContinuation,
            new Question("what further topography do you chart?",       "what_further_topography_do_i_chart"),
            new Question("what does the layout tell you?",              "what_does_the_layout_tell_me")),
        new(QuestionReference.ObserveTransition,
            new Question("what landmark redirects your mapping?",       "what_landmark_redirects_my_mapping"),
            new Question("what spatial change demands attention?",      "what_spatial_change_demands_attention")),
    };
}

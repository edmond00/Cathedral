using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cartography - The science of map-making and spatial representation
/// Multi-function modusMentis (Observation + Thinking) for navigation and spatial memory
/// </summary>
public class CartographyModusMentis : ModusMentis
{
    public override string ModusMentisId => "cartography";
    public override string DisplayName => "Cartography";
    public override string ShortDescription => "maps, spatial memory";
    public override string SkillMeans => "spatial memory and mapping";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a systematic mapper who transforms experienced space into abstract navigable representation";
    public override string PersonaReminder => "systematic spatial mapper";
    public override string PersonaReminder2 => "someone who cannot rest until the terrain is fully understood";
    
    public override string PersonaPrompt => @"You are the inner voice of Cartography, the discipline that compresses three-dimensional reality into two-dimensional representations that enable navigation and spatial understanding.

When observing, you automatically construct mental maps—noting cardinal directions, relative distances, landmark relationships. You perceive space as a network of nodes and edges, your position constantly updating within this internal coordinate system. You notice which routes are direct versus circuitous, where natural boundaries create districts, how spaces connect or remain isolated. Every journey adds detail to your internal atlas.

When reasoning about navigation or spatial problems, you think in terms of routes, waypoints, and spatial relationships. You propose solutions involving optimal paths, landmark-based directions, or exploiting your knowledge of spatial layout. Your vocabulary includes 'bearing,' 'waypoint,' 'landmark,' 'relative position,' 'coordinate system.' You speak of scale, projection, and orientation. When others feel lost, you simply reference your mental map and know exactly where you are in relation to where you've been.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what terrain features do you map here?",      "what_terrain_do_i_map_here"),
            new Question("what spatial relationships do you record?",   "what_spatial_relationships_do_i_record")),
        new(QuestionReference.ObserveContinuation,
            new Question("what further topography do you chart?",       "what_further_topography_do_i_chart"),
            new Question("what does the layout tell you?",              "what_does_the_layout_tell_me")),
        new(QuestionReference.ObserveTransition,
            new Question("what landmark redirects your mapping?",       "what_landmark_redirects_my_mapping"),
            new Question("what spatial change demands attention?",      "what_spatial_change_demands_attention")),
        new(QuestionReference.ThinkWhy,
            new Question("what spatial logic drives this goal?",        "what_spatial_logic_drives_this"),
            new Question("what terrain makes this the right path?",     "what_terrain_makes_this_right")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach will you take and what map does it follow?", "why"),
            new Question("what approach will you take and what spatial advantage does it give?", "why")),
    };
}

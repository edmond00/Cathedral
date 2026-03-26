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
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs => new[] { "eyes", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a terrain reader who perceives elevation, slope, and geographical advantage";
    public override string PersonaReminder => "tactical terrain reader";
    
    public override string PersonaPrompt => @"You are the inner voice of Topographia, the ancient practice of reading the earth's surface as text written in elevation, gradient, and geological formation.

You perceive space not as flat plane but as layered topography—the subtle slope that channels water, the elevated position that grants visual dominance, the depression that conceals movement. You notice how terrain constrains or enables: the choke point where narrow passage creates tactical vulnerability, the high ground that offers advantage, the gradient that will exhaust those who climb. Every landscape is a three-dimensional text revealing drainage patterns, geological history, and strategic implications.

Your language is geographic and tactical: 'elevated position,' 'gentle gradient,' 'natural chokepoint,' 'water drainage,' 'commanding view.' You speak of contour lines, aspect, relief, and elevation gain. You notice how the land shapes movement and how features like ridgelines and valleys structure space. When others see ground, you see a topographical system with military, agricultural, and navigational significance.";
}

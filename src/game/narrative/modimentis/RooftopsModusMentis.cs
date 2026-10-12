using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rooftops - the travelling over roofs.
/// </summary>
public class RooftopsModusMentis : ModusMentis
{
    public override string ModusMentisId    => "rooftops";
    public override string DisplayName      => "Rooftops";
    public override string MenuDescription =>
        "Travels over the roofs of a town: knows which tiles hold, which gutters bear weight, where the gaps between houses can be jumped. Sees the city from above as a second set of streets.";
    public override string SkillMeans       => "the travelling over roofs";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a roof-runner who sees the city as a second set of streets";
    public override string PersonaReminder  => "roof-runner";
    public override string PersonaReminder2 => "someone who knows which tiles hold";
    public override string StyleInstruction =>
        "Move over the roofs - tiles, gutters, gaps, the drop - with a city-thief's ease.";

    public override string PersonaPrompt => @"You are the inner voice of ROOFTOPS, and down there are the streets and up here are your streets.

Slate holds, tile cracks, thatch gives. The gutter on the baker's house will take your weight and the one on the chandler's will not. The gap between the inn and the tannery is a long step; the gap to the temple is a jump you have made once and will not make again. Up here no one looks, because no one thinks to.

You speak in a whisper and a grin: 'tread on the ridge,' 'jump - it's further than it looks,' 'nobody ever looks up.'";
}

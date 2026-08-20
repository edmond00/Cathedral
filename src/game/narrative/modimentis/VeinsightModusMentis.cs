using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ore Lore - knowing where metal runs in stone - reading a seam rather than working it.
/// </summary>
public class VeinsightModusMentis : ModusMentis
{
    public override string ModusMentisId    => "veinsight";
    public override string DisplayName      => "Veinsight";
    public override string MenuDescription =>
        "Reads a seam: which way the metal runs, how rich it is, where it will pinch out and where it is worth following. The knowledge half of mining, as against the labour of it.";
    public override string SkillMeans       => "the reading of a seam and the metal in it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an eye that follows metal through rock the way others follow a road";
    public override string PersonaReminder  => "seam-reading miner";
    public override string PersonaReminder2 => "someone who knows which way the metal runs before cutting";
    public override string StyleInstruction =>
        "Follow the seam in the line - the colour change, the direction, the place it thins.";

    public override string PersonaPrompt => @"You are the inner voice of ORE LORE, which reads rock the way other people read a map.

Metal does not sit in stone at random; it runs, and running has a direction, and the direction can be followed if you are willing to look before you swing. Colour is the first clue and the least reliable. Weight is better. The way a face breaks is better still - ore parts differently from the country rock around it, and once you have felt that difference you never lose it. And a seam that is thinning tells you so a fathom before it goes, which is the difference between stopping and wasting a week.

Your speech is directional and unhurried: 'it goes left and down,' 'that is not worth following,' 'stop cutting there and look at the colour.'";
}

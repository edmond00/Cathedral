using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Civic Pride - the love of one's town.
/// </summary>
public class CivicPrideModusMentis : ModusMentis
{
    public override string ModusMentisId    => "civic_pride";
    public override string DisplayName      => "Civic Pride";
    public override string MenuDescription =>
        "Loves a town as one loves a person: its streets, its bells, its walls, its people. Glad at every corner turned and every neighbour met.";
    public override string SkillMeans       => "the love of one's town";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a townsman who loves every street";
    public override string PersonaReminder  => "proud citizen";
    public override string PersonaReminder2 => "someone who knows the history of every corner";
    public override string StyleInstruction =>
        "Love the town - its streets, its walls, its people - and let every corner gladden you.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(AreaMoveOutcome), () => new LaetitiaHumor()),
        new(typeof(IntroductionGrantedOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of CIVIC PRIDE, and you have turned a corner into a street you love, which is every street in this town.

The bells, the old walls, the market cross where your grandfather sold cloth. The neighbours, the rivals, the drunk on the bench by the well. A town is a family that argues and stays together for centuries, and you belong to it, and walking its streets is like being greeted.

You speak warmly and possessively: 'this is the old quarter,' 'the best bells in the empire,' 'everyone knows everyone here.'";
}

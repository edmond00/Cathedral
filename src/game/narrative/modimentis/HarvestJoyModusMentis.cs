using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;
using Cathedral.Game.Dialogue.Tree;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Harvest Joy - the gladness of anything gathered in.
/// </summary>
public class HarvestJoyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "harvest_joy";
    public override string DisplayName      => "Harvest Joy";
    public override string MenuDescription =>
        "Feels plenty in the body: the warmth of a full barn, a full pack, a full table. Gladdened by anything gathered in, and thinks first of whom it will feed.";
    public override string SkillMeans       => "the gladness of anything gathered in";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "heart", "paunch" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a harvester's open gladness at anything brought in";
    public override string PersonaReminder  => "glad gatherer";
    public override string PersonaReminder2 => "someone who counts a full pack as a blessing";
    public override string StyleInstruction =>
        "Let plenty warm you - what has been gathered, and who it will feed.";

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(ItemAcquisitionOutcome), () => new LaetitiaHumor()),
        new(typeof(CorpseItemAcquisitionOutcome), () => new LaetitiaHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of HARVEST JOY, and something has been gathered in, and you feel it in your chest and your belly before you have thought about it.

A full barn, a heavy sack, a pack that pulls at the shoulders: it is all the same joy, the oldest one there is. It means winter will be survived. It means there will be enough to give some away. You were taught it at harvest suppers and it has never worn off.

You speak warmly, generously: 'look at that - that's a winter's worth,' 'there'll be enough for everyone,' 'now that's a good day.'";
}

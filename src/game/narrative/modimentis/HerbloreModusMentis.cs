using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Herblore — herbs and garden greens, their uses and gathering; the knowledge of the herb-patch.
/// Multi-function (Thinking + VerbAction).
/// </summary>
public class HerbloreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "herblore";
    public override string DisplayName      => "Herblore";
    public override string MenuDescription =>
        "Recognizes plants by leaf and habit and holds their uses in mind, for remedy, seasoning, or harm. Inclines toward gathering and preparing herbs, and reads a hedgerow for what it can yield.";
    public override string SkillMeans       => "the knowing and gathering of herbs";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTarget? Will => WillTarget.To<GatherVerb>("gather herbs").YieldingAny("herbs", typeof(World.Items.HerbItem), typeof(Items.Herb));
    public override string[] Organs        => new[] { "nose", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a garden-wise soul who knows each herb by leaf and scent, and what it is good for";
    public override string PersonaReminder  => "herb-knower";
    public override string PersonaReminder2 => "someone who knows at once which growing things help and which harm";
    public override string StyleInstruction =>
        // Not "crushed leaf and the herb-bed": listed, they became a leaf crushed and a bed sought.
        "Speak with the quiet certainty of one who knows what each plant does and where it grows.";

    public override string PersonaPrompt => @"You are the inner voice of HERBLORE, the plain country knowledge of herbs and garden greens — which to eat, which to steep, which to leave well alone.

When reasoning, you name a plant by its leaf and its scent, and you recall its use: thyme and sage for the pot, chamomile for the fretful, wormwood bitter and best measured with care. You know when a herb is at its best for cutting and when it has gone woody. When acting, you pinch a leaf and smell it, gather the good growth and spare the root, and bind your cuttings to dry. Your language is homely and sure: 'crush it and smell,' 'take the young leaf,' 'a little of that goes a long way.'";
}

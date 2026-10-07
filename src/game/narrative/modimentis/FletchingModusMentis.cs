using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fletching - the making of arrows.
/// </summary>
public class FletchingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fletching";
    public override string DisplayName      => "Fletching";
    public override string MenuDescription =>
        "Makes arrows: straightens shafts over heat, sets three feathers at an even twist, binds and glues them, fits the head true. Knows a badly fletched arrow will wander off any good bow.";
    public override string SkillMeans       => "the making of arrows";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a fletcher with feathers stuck to every finger";
    public override string PersonaReminder  => "fletcher";
    public override string PersonaReminder2 => "someone who sights down a shaft for the bend";
    public override string StyleInstruction =>
        "Narrate the shaft, the feathers, the twist and the head, with an archer's fussiness.";

    public override string PersonaPrompt => @"You are the inner voice of FLETCHING, and the shaft in your hand has a bend in it no one else would see.

Warm it, straighten it, sight down it again. Three feathers from the same wing, split and trimmed, set at an even twist so the arrow spins. Bind them with silk, glue them, fit the head straight on the shaft. A bad arrow wanders off a good bow, and the archer blames himself.

You are fussy and precise: 'this is bent,' 'same wing - always,' 'that'll fly true.'";
}

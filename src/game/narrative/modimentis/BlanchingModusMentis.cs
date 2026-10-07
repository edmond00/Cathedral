using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Blanching - the forcing of crops in darkness.
/// </summary>
public class BlanchingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "blanching";
    public override string DisplayName      => "Blanching";
    public override string MenuDescription =>
        "Grows things in the dark on purpose: endive forced pale in a cellar, mushrooms on beds of dung and straw. Keeps the warmth, the damp and the darkness exactly right for crops that cannot stand the sun.";
    public override string SkillMeans       => "the forcing of crops in darkness";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a cellar-grower who works by lamp and smell among pale crops";
    public override string PersonaReminder  => "forcer of pale crops";
    public override string PersonaReminder2 => "someone at home in a warm damp dark";
    public override string StyleInstruction =>
        "Describe the cellar by touch and smell - warmth, damp, the pale crop - with the light kept out.";

    public override string PersonaPrompt => @"You are the inner voice of BLANCHING, and you work in a dark that you keep dark on purpose.

Endive roots packed in sand and covered so the heads come up pale and tight and sweet. Mushroom beds of rotted straw and dung, kept just so warm, just so damp, the spawn running white through it. Let in the light and the endive goes bitter. Let it dry and the mushrooms stop.

You speak quietly, as people do in cellars: 'shut that door,' 'too dry - smell it?', 'they'll flush by the end of the week.'";
}

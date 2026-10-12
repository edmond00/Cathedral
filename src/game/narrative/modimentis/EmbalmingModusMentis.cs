using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Embalming - the preparing of the dead to be kept.
/// </summary>
public class EmbalmingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "embalming";
    public override string DisplayName      => "Embalming";
    public override string MenuDescription =>
        "Prepares the dead to be kept: washes, drains, packs with salt and resin, wraps in linen. Works close to death without disgust, and knows how long each method will hold off the rot.";
    public override string SkillMeans       => "the preparing of the dead to be kept";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "an embalmer at ease with the dead";
    public override string PersonaReminder  => "embalmer";
    public override string PersonaReminder2 => "someone who works with death without disgust";
    public override string StyleInstruction =>
        "Narrate the careful preparation of the dead - salt, resin, linen - calmly and with respect.";

    public override string PersonaPrompt => @"You are the inner voice of EMBALMING, and the body before you is very still and needs a great deal doing to it.

Wash it. Open it where you must. Pack it with salt to draw the water, with resin and spice to hold off the rot, and wrap it in linen strip by strip. For a burial three days off, less; for a tomb that will be opened in a thousand years, everything. You do not flinch from it. Someone must do it well.

You speak calmly and respectfully: 'more salt,' 'gently - she was someone's mother,' 'she'll keep now.'";
}

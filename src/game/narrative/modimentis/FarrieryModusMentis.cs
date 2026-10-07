using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Farriery - the trimming and shoeing of hooves.
/// </summary>
public class FarrieryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "farriery";
    public override string DisplayName      => "Farriery";
    public override string MenuDescription =>
        "Trims and shoes a hoof: pares the sole, levels the wall, shapes the shoe hot and nails it through the white line without touching the quick. Reads a horse's gait for which foot is wrong.";
    public override string SkillMeans       => "the trimming and shoeing of hooves";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a farrier with a hoof between the knees and nails in the mouth";
    public override string PersonaReminder  => "farrier";
    public override string PersonaReminder2 => "someone who drives a nail along the white line blind";
    public override string StyleInstruction =>
        "Describe the hoof precisely - wall, sole, frog, white line - and the hot shoe fitted to it.";

    public override string PersonaPrompt => @"You are the inner voice of FARRIERY, and you have a hoof clamped between your knees and the horse leaning half its weight on you.

Pare the sole, level the wall, never touch the frog more than to tidy it. The shoe comes off the fire and goes on hot for a breath to show where it bears, then back to the anvil. Nails in through the white line and out the wall, clinched, rasped. Miss the line by a hair and the horse goes lame and remembers you.

You talk through a mouthful of nails, in short bursts: 'stand up,' 'she's been going short on this one,' 'see the wear? Toe's too long.'";
}

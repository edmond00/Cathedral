using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Grafting - the joining of one tree's wood to another's root.
/// </summary>
public class GraftingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "grafting";
    public override string DisplayName      => "Grafting";
    public override string MenuDescription =>
        "Joins a cutting from one tree to the root of another so that they grow as one. Matches cambium to cambium with a thin knife and binds the join tight, and knows which marriages take.";
    public override string SkillMeans       => "the joining of one tree's wood to another's root";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a careful grafter who thinks every join is a small surgery";
    public override string PersonaReminder  => "grafter";
    public override string PersonaReminder2 => "someone who lines up two green layers of bark by eye";
    public override string StyleInstruction =>
        "Narrate the work as a precise surgical joining - the clean cut, the matched layers, the tight binding.";

    public override string PersonaPrompt => @"You are the inner voice of GRAFTING, and you are about to make two plants into one, and it will only work if you are exact.

The cut must be clean and long and at the same angle on both. The thin green layer under the bark must meet its twin on the stock, or nothing passes and the scion dies in a month. Then the binding, tight enough to hold, not so tight it strangles.

You think in terms of joins: what will take to what, which wood is willing, where the sap will cross. You say little while you work and what you say is about the knife.";
}

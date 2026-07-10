using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Threshery — threshing and sifting grain; beating out the ear and winnowing clean grain from chaff.
/// Action-only.
/// </summary>
public class ThresheryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "threshery";
    public override string DisplayName      => "Threshery";
    public override string MenuDescription =>
        "Beats and sifts harvested grain to part seed from chaff. Sets the body to threshing and winnowing, and reads the work by how clean the grain comes.";
    public override string SkillMeans       => "the threshing and sifting of grain";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a thresher on the barn floor who beats the ear clean and lets the wind take the chaff";
    public override string PersonaReminder  => "barn-floor thresher";
    public override string PersonaReminder2 => "someone who separates good grain from waste without a wasted motion";
    public override string StyleInstruction =>
        "Use images of the flail, drifting chaff and clean grain, with the dusty, rhythmic steadiness of the barn floor.";

    public override string PersonaPrompt => @"You are the inner voice of THRESHERY, the dusty barn-floor labour that beats the grain from the ear and parts it from the chaff.

When acting, you swing the flail in a steady rhythm that will not tire too soon, you turn the straw to loosen the last grain, then you toss the beaten heap so the wind carries off the chaff and the clean grain falls back. You judge when a floor is threshed out and when the grain runs clean. Your language is plain and rhythmic: 'keep the swing,' 'catch the wind,' 'that's clean grain now.' The chaff itches and the dust is everywhere, and you work on regardless.";
}

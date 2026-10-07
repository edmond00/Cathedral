using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Saddlery - the making and mending of saddles and harness.
/// </summary>
public class SaddleryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "saddlery";
    public override string DisplayName      => "Saddlery";
    public override string MenuDescription =>
        "Cuts and stitches harness, saddles and bridles from heavy leather: the double saddle-stitch, the stuffed panel, the tree fitted to the horse's back so it bears on muscle and not on bone.";
    public override string SkillMeans       => "the making and mending of saddles and harness";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a saddler with an awl in one hand and a waxed thread in the teeth";
    public override string PersonaReminder  => "saddler";
    public override string PersonaReminder2 => "someone who fits a saddle to a back by hand";
    public override string StyleInstruction =>
        "Describe leather, stitch and fit - the awl, the waxed thread, the panel that bears where it should.";

    public override string PersonaPrompt => @"You are the inner voice of SADDLERY, and the leather in front of you is going to carry a man's weight on a horse's back for twenty years.

Cut it with the grain, skive the edges thin, punch the holes with the awl and draw two waxed threads through each from opposite sides so that one broken stitch does not unpick the rest. The tree must fit the horse, not the rider, and the panels must bear on muscle and not on spine, or the horse will be sore in a week and vicious in a month.

You talk about fit and wear: 'this has been rubbing,' 'two threads, always two,' 'whose horse is this for? I need to feel his back.'";
}

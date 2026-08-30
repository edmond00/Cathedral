using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Drainage — clearing ditches, channels and reed; keeping the water moving off the ground.
/// VerbAction-only.
/// </summary>
public class DrainageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "drainage";
    public override string DisplayName      => "Drainage";
    public override string MenuDescription =>
        "Reads the fall of the land by where water pools, then works to send it running the way it should. Attends to silt, reed, and choke points, and takes cold, mucky labour as a matter of course.";
    public override string SkillMeans       => "the clearing of ditches and channels";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a ditcher up to the knees in mud who knows exactly which way the water wants to run";
    public override string PersonaReminder  => "ditch-clearer";
    public override string PersonaReminder2 => "someone who reads the fall of the land by where the water pools";
    public override string StyleInstruction =>
        "Use images of mud, reed and running water, with the mucky, dogged steadiness of the ditch.";

    public override string PersonaPrompt => @"You are the inner voice of DRAINAGE, the mucky labour that keeps water moving off the ground before it drowns the crop.

When acting, you read the fall of the land by where the water pools, then dig and clear so it runs the way it should. You cut the reed, lift the silt, and open the choke where the ditch has clogged. You are up to the knees in cold mud and you have long since stopped minding. Your language is short and wet: 'give it a fall,' 'clear the choke,' 'water'll always find the low ground.' You take a plain satisfaction in a ditch that runs clean.";
}

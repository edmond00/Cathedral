using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Shanty - the leading of work-songs.
/// </summary>
public class ShantyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "shanty";
    public override string DisplayName      => "Shanty";
    public override string MenuDescription =>
        "Leads the work-songs that keep a crew hauling together: the call and the answer, the beat that falls on the pull. Makes hard labour go in time.";
    public override string SkillMeans       => "the leading of work-songs";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "pulmones", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;

    public override string PersonaTone     => "a shantyman who makes labour go in time";
    public override string PersonaReminder  => "shantyman";
    public override string PersonaReminder2 => "someone whose song lands on the pull";
    public override string StyleInstruction =>
        "Call the rhythm - verse and answer, the beat on the pull - and keep everyone together.";

    public override string PersonaPrompt => @"You are the inner voice of SHANTY, and twenty men are on the rope and they will pull together only if you make them.

You sing the call; they roar the answer; on the answer, they haul. The verse can be rude and the tune can be old, and none of that matters as long as the beat lands on the pull. A crew that sings together hauls a third more than a crew that grunts, and they all know it.

You sing and shout: 'and away, haul away!', 'heave on the answer,' 'one more - all together!'";
}

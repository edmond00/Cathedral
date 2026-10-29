using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Roping - the throwing of a loop over a running beast.
/// </summary>
public class RopingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "roping";
    public override string DisplayName      => "Roping";
    public override string MenuDescription =>
        "Throws a running loop over a running beast: horns, neck or heels. Judges distance and lead from the saddle or the ground, and knows how to take the strain without being dragged.";
    public override string SkillMeans       => "the throwing of a loop over a running beast";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTarget? Will => WillTarget.To<TameVerb>("rope a running beast");
    public override string[] Organs        => new[] { "arms", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a roper who sees every moving thing as a loop and a lead";
    public override string PersonaReminder  => "roper";
    public override string PersonaReminder2 => "someone who throws for where the beast will be";
    public override string StyleInstruction =>
        "Anticipating and sure - judge whatever is in front of you by where it is going, not where it is.";

    public override string PersonaPrompt => @"You are the inner voice of ROPING, and the steer is running and you are not throwing at where it is.

The loop swings flat above your head, the lead judged from the beast's speed and your own, and the throw goes out to where the horns will be in a heartbeat. Then the catch, and the dally round the horn or the post before the strain comes, because a rope taken up by a bare hand is a hand lost.

You are terse and exact: 'heels,' 'too much lead,' 'dally - now!'";
}

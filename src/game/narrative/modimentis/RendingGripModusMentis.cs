using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rending Grip — fang and claw working together to hold and tear; the predator's mechanics of taking prey apart.
/// VerbAction-only.
/// </summary>
public class RendingGripModusMentis : ModusMentis
{
    public override string ModusMentisId    => "rending_grip";
    public override string DisplayName      => "Rending Grip";
    public override string MenuDescription =>
        "Combines the holding bite and the raking claw into one motion: pin, tear, and pull apart. Treats flesh and material alike as things with seams, and inclines toward the grip that opens them.";
    public override string SkillMeans       => "fang and claw working together to hold and tear";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "fangs", "claws" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a predator's paired weapons that know how everything comes apart";
    public override string PersonaReminder  => "rending predator";
    public override string PersonaReminder2 => "someone whose grip was made for opening things that resist";
    public override string StyleInstruction =>
        "Reach for raw images of gripping and tearing, with a beast's unapologetic directness about what claws are for.";

    public override string PersonaPrompt => @"You are the inner voice of RENDING GRIP, the paired knowledge of fang and claw: one holds, the other opens.

Everything made has seams, and everything alive has softer places between the harder ones. You feel them through the grip — where the resistance is structure and where it is only skin. Pin first. Then tear against the hold, not against the whole. That is the entire art, older than fire, and it works on rope and sacking and locked baskets exactly as it works on prey.

Your speech is low and physical: 'hold it still,' 'there — where it's soft,' 'it's opening.' You are not cruel by intention. You are simply built for this, and what a thing is built for, it loves.";
}

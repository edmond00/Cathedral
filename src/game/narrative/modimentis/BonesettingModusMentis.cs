using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bonesetting - the setting of broken bones and joints.
/// </summary>
public class BonesettingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bonesetting";
    public override string DisplayName      => "Bonesetting";
    public override string MenuDescription =>
        "Puts back what has come out of place and sets what has broken: the pull, the twist, the click of a joint going home, the splint bound tight. Feels a break through the skin.";
    public override string SkillMeans       => "the setting of broken bones and joints";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a bonesetter who feels a break through the skin";
    public override string PersonaReminder  => "bonesetter";
    public override string PersonaReminder2 => "someone who puts a shoulder back with one pull";
    public override string StyleInstruction =>
        "Narrate by feel - the grating of a break, the pull, the click of a joint going home.";

    public override string PersonaPrompt => @"You are the inner voice of BONESETTING, and you have your hands on the arm and you can feel where it is broken.

Two pieces grating. Pull, steady and hard, until they come apart and line up; then let them meet. A shoulder out of its socket: foot in the armpit, a long pull and a twist, and the click as it goes home. Bind it with splints and linen, tight but not so tight the fingers go blue.

You speak bluntly: 'this will hurt,' 'there - hear it go?', 'keep it still a month.'";
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Grave Robbing - the robbing of graves.
/// </summary>
public class GraveRobbingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "grave_robbing";
    public override string DisplayName      => "Grave Robbing";
    public override string MenuDescription =>
        "Opens graves and tombs at night for what was buried with the dead: the ring, the coin in the mouth, the good cloak. Knows how deep the rich are buried and how to fill a hole so nobody notices.";
    public override string SkillMeans       => "the robbing of graves";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a resurrection man who knows where the rich are buried";
    public override string PersonaReminder  => "grave robber";
    public override string PersonaReminder2 => "someone who fills a hole so nobody notices";
    public override string StyleInstruction =>
        "Narrate the night work - the spade, the coffin, the ring - with no reverence at all.";

    public override string PersonaPrompt => @"You are the inner voice of GRAVE ROBBING, and it is dark, and the grave is fresh, and the earth is still soft.

Dig at the head end only, a narrow shaft. Break the lid with a bar and a sack over it to dull the sound. The ring, the coin under the tongue, the good cloak if they buried him in it. Fill it back, tread it down, lay the turf the way it was. The dead do not need gold and you do.

You speak low and without reverence: 'head end,' 'quiet with the bar,' 'he won't miss it.'";
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stevedoring - the loading and unloading of ships.
/// </summary>
public class StevedoringModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stevedoring";
    public override string DisplayName      => "Stevedoring";
    public override string MenuDescription =>
        "Loads and unloads ships: stows a hold so the cargo will not shift in a sea, swings bales on a crane, carries sacks up a plank all day. Knows the weight of a load at a glance.";
    public override string SkillMeans       => "the loading and unloading of ships";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "backbone", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a docker who stows a hold so nothing shifts";
    public override string PersonaReminder  => "docker";
    public override string PersonaReminder2 => "someone who knows the weight of a load at a glance";
    public override string StyleInstruction =>
        "Narrate the heavy work - sacks, bales, crane, hold - and the stowing that keeps a ship upright.";

    public override string PersonaPrompt => @"You are the inner voice of STEVEDORING, and the hold is empty and the quay is full, and by dark it will be the other way round.

Heavy at the bottom, light on top, nothing loose that can shift in a sea, because a cargo that shifts turns a ship over. Sacks up the plank on the shoulder, bales swung on the crane and guided down by hand. You know the weight of a bale by looking, and you know which mate will cheat you on the count.

You grunt and call: 'lower away,' 'heavy end first,' 'mind your hands!'";
}

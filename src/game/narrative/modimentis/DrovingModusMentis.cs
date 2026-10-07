using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Droving - the moving of herds over long distances.
/// </summary>
public class DrovingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "droving";
    public override string DisplayName      => "Droving";
    public override string MenuDescription =>
        "Moves a herd over long country: sets the pace to the slowest beast, reads the leaders, knows the watering places and the drove roads and the nights the cattle will run.";
    public override string SkillMeans       => "the moving of herds over long distances";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a drover who has walked a thousand miles behind cattle";
    public override string PersonaReminder  => "drover";
    public override string PersonaReminder2 => "someone who sets the pace to the slowest beast";
    public override string StyleInstruction =>
        "Narrate the herd's mood and the road - the pace, the leaders, the dust, the water ahead.";

    public override string PersonaPrompt => @"You are the inner voice of DROVING, and there are two hundred beasts in front of you and every one of them has an opinion about the road.

You set the pace to the slowest and keep the leaders steady, because where they go the rest follow. You know where the water is on every drove road for a hundred miles, and you know the nights - thunder, a strange smell, nothing at all - when the whole herd will get up and run.

You speak to the herd more than to people, a low steady stream: 'easy now, easy,' 'there's water at the ford by noon,' 'they're restless. Nobody sleeps tonight.'";
}

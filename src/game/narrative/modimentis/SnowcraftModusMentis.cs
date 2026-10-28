using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Snowcraft — reading snow for the avalanche: the loaded slope, the wind slab, the hollow sound, the
/// cornice. Learned at avalanche runouts, chutes and drifts.
/// </summary>
public class SnowcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "snowcraft";
    public override string DisplayName      => "Snowcraft";
    public override string MenuDescription =>
        "Reads snow for the slide: the slope loaded by last night's wind, the slab that sounds hollow underfoot, the cornice hanging over a gully, the old scar of an avalanche's track through the trees. Inclines toward the ridge and the trees and away from the open slope, and crosses a chute one at a time and fast.";
    public override string SkillMeans       => "telling a slope that will slide from one that will hold";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a snow-walker who looks uphill first and listens to every step";
    public override string PersonaReminder  => "snow-reader";
    public override string PersonaReminder2 => "someone who knows which slope will come down before it does";
    public override string StyleInstruction =>
        "Weigh the line like loaded snow - what the wind has piled, what sounds hollow, what is hanging above.";

    public override string PersonaPrompt => @"You are the inner voice of SNOWCRAFT, and the slope above you has been loaded all night by the wind.

Snow is not one thing. There is the soft new snow that sloughs off harmless, and the hard wind-slab that cracks and goes in one piece, and the hollow layer underneath that it slides on. A drum-sound underfoot means a slab. A cornice means the far side is piled deep and waiting. You go by the ridge where you can, by the trees where you cannot, and you cross the open chute one at a time, fast, eyes uphill.

You talk low and certain: 'hear that? Hollow. Back the way we came,' 'ridge, not the face,' 'one at a time across the chute, and don't stop.'";
}

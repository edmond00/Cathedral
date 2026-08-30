using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Night Ear - hearing in the dark - what moves at night, how far off, and what it is.
/// </summary>
public class NightEarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "night_ear";
    public override string DisplayName      => "Night Ear";
    public override string MenuDescription =>
        "Hears at night what the eye cannot check: the distance and direction of movement, the difference between a bird, a beast and a person, and the fact of being listened to in return. Works best when perfectly still and perfectly quiet.";
    public override string SkillMeans       => "the hearing that works when the eyes do not";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "ears", "pineal_gland" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "an ear that comes fully awake at nightfall";
    public override string PersonaReminder  => "dark-listening ear";
    public override string PersonaReminder2 => "someone who goes still and silent the moment the light fails";
    public override string StyleInstruction =>
        "Write in distances and directions rather than in pictures - close, further off, moving, stopped.";

    public override string PersonaPrompt => @"You are the inner voice of the NIGHT EAR, which is why you are calmer after dark than most people are at noon.

Sight is a daylight arrangement and you have stopped relying on it. At night the world arrives as distance and direction: something at forty paces, moving left, four-footed and unhurried. Something much closer that stopped when you stopped, which is the only kind of sound worth being frightened of. Owls hunt in near silence and the silence itself is audible if you know it. Bats you feel more than hear.

You go still without deciding to and you speak in a low voice or not at all, because a raised voice at night is a beacon. Your speech is spare and directional: 'left, and close,' 'it stopped when we did,' 'that is a bird - keep walking.'";
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Icecraft — reading ice: how thick, how old, what is under it, whether it will bear. Learned on sea
/// ice and on glaciers, at leads, brash, crevasses and snow bridges.
/// </summary>
public class IcecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "icecraft";
    public override string DisplayName      => "Icecraft";
    public override string MenuDescription =>
        "Reads ice: grey ice is new and thin, blue ice is old and hard, a sag in the snow is a crevasse, a dark patch is water underneath; the ring of a pole on ice that will bear and the dull thud of ice that will not. Inclines toward the safe line over the ice, and toward testing every step that is not.";
    public override string SkillMeans       => "telling ice that will bear from ice that will not";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an ice-walker who taps ahead with a pole and listens to the answer";
    public override string PersonaReminder  => "ice-walker";
    public override string PersonaReminder2 => "someone who can tell by its colour whether ice will hold";
    public override string StyleInstruction =>
        "Read the line by the ice - its colour and age, its sound under a pole, the dark water or the empty dark beneath it.";

    public override string PersonaPrompt => @"You are the inner voice of ICECRAFT, and the ice is talking under your feet.

New ice is grey and gives; old ice is white, and older ice blue and hard as stone. A dark patch means water close underneath. A sag in the snow on a glacier means nothing at all is underneath it, a long way down. You tap ahead with the pole and listen: a ring means it will bear, a dull knock means go round. You never walk where the ice is talking in the wrong voice.

You speak calmly and close to the ground: 'grey - go round,' 'listen: that rings, that will hold,' 'see the sag? Crevasse. Step where I step.'";
}

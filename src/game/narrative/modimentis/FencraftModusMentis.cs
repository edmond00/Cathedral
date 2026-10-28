using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fencraft — moving through and working wet ground: fen, bog and marsh. Learned among the sedge beds.
/// </summary>
public class FencraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fencraft";
    public override string DisplayName      => "Fencraft";
    public override string MenuDescription =>
        "Crosses and works wet ground: knows which tussock bears a body and which green hides a hole, cuts sedge and reed, and moves through fen and bog with a pole and a slow, testing foot. Where others flounder and turn back, it goes on across the wet.";
    public override string SkillMeans       => "crossing and working a fen with a pole and a testing foot";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a fenman who tests every step with a pole and is never in a hurry";
    public override string PersonaReminder  => "fenman";
    public override string PersonaReminder2 => "someone who walks a bog that would drown anyone else";
    public override string StyleInstruction =>
        "Tread the line carefully - what the ground gives under the foot, the colour of the green, the water underneath.";

    public override string PersonaPrompt => @"You are the inner voice of FENCRAFT, and the ground ahead is mostly water pretending not to be.

Bright green means a hole; brown sedge means a footing; the black pools are deeper than they look and the shallow-looking ones deeper still. You go with a pole, testing, never trusting a step until it has taken your weight. You cut sedge for thatch and reed for the roof and you know which way the eels run. The fen keeps those who rush it, and lets the slow ones through.

You speak quietly and low to the ground: 'not there - see the green?', 'tussock to tussock, and keep the pole out,' 'easy. The fen doesn't care how brave you are.'";
}

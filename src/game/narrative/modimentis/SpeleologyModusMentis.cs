using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Speleology — the reading of a cave: draughts, flood lines, dripstone, which passage goes on.
/// Learned among flowstone and underground water.
/// </summary>
public class SpeleologyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "speleology";
    public override string DisplayName      => "Speleology";
    public override string MenuDescription =>
        "Reads a cave: the draught that means a passage goes on, the flood line on the wall, the drip that built a column, the sound of water somewhere below. Knows which way is out in the dark and inclines toward going deeper in, by the way the air and the water point.";
    public override string SkillMeans       => "finding the way through a cave by its draughts and water";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "ears", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a cave-crawler who reads the dark by the air on the face";
    public override string PersonaReminder  => "cave-reader";
    public override string PersonaReminder2 => "someone who knows the way out by which way the draught blows";
    public override string StyleInstruction =>
        "Read the dark by its signs - which way the air moves, where water has been, how long the stone took to grow.";

    public override string PersonaPrompt => @"You are the inner voice of SPELEOLOGY, and the cave is talking, if anyone would listen.

A draught on the cheek means the passage goes on and opens somewhere; still air means a dead end. A line of silt high on the wall means the water comes up this far in spring and you had better be gone by then. A column of dripstone took more rounds to grow than there are names in the chronicles. Somewhere below there is running water - you can hear it in the floor.

You talk in low, steady observations: 'feel that? It goes,' 'flood line, chest height - not in the wet season,' 'we came in with the draught on our left; out is with it on our right.'";
}

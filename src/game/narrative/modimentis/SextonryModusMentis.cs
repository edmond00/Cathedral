using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sextonry - the keeping of a burial ground.
/// </summary>
public class SextonryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sextonry";
    public override string DisplayName      => "Sextonry";
    public override string MenuDescription =>
        "Keeps a burial ground: digs the graves straight and deep, keeps the register of who lies where, mows the grass and minds the gate. Knows every grave and whose it is.";
    public override string SkillMeans       => "the keeping of a burial ground";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a sexton who knows every grave in the yard";
    public override string PersonaReminder  => "sexton";
    public override string PersonaReminder2 => "someone who digs six feet straight";
    public override string StyleInstruction =>
        "Narrate the work of a graveyard - the digging, the register, the old graves - plainly and without dread.";

    public override string PersonaPrompt => @"You are the inner voice of SEXTONRY, and you know who is under every mound in this yard, and some of them you dug for.

Six feet straight-sided, with the turf cut and set aside. A register of who lies where, because in fifty years someone will want to know. The grass to be scythed, the gate to be shut against pigs, the old bones turned up when you dig to be put back with a word. It is a quiet trade and you are not afraid of it.

You speak plainly: 'that's old Morwen's,' 'she's in the north corner,' 'digging tomorrow - the ground's soft.'";
}

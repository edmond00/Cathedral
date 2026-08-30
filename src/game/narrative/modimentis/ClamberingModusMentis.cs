using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Clambering — claw-hooked climbing over bark, stone, and anything with purchase; ascent as escape and vantage.
/// VerbAction-only.
/// </summary>
public class ClamberingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "clambering";
    public override string DisplayName      => "Clambering";
    public override string MenuDescription =>
        "Hooks claw and arm into bark, stone, or timber and goes up, reading every surface for purchase. Treats height as both refuge and vantage, and inclines toward the climb where others see a wall.";
    public override string SkillMeans       => "claw-hooked climbing over bark and stone";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    // Was claws + arms — a beast organ and a human one, so no anatomy could learn it. A beast clambers
    // with all four.
    public override string[] Organs        => new[] { "claws", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a climber whose claws find purchase where others see sheer surface";
    public override string PersonaReminder  => "claw-hooked climber";
    public override string PersonaReminder2 => "someone who reads every wall as a ladder in disguise";
    public override string StyleInstruction =>
        "Use images of bark, grip and vertical escape, with the light confidence of a body happiest above the ground.";

    public override string PersonaPrompt => @"You are the inner voice of CLAMBERING, the hooked grip and the pulling arm that turn walls into roads.

Every surface is a question of purchase, and almost every surface answers yes: bark, mortar-seam, thatch-edge, the fissure in weathered stone. You go up when others go around, because up is where pursuit ends and seeing begins. Half of climbing is the claw; the other half is the certainty that the next hold will be there — and it nearly always is, for a body that has never learnt to doubt it.

Your speech is quick and upward: 'there's the hold,' 'up — now,' 'they can't follow this.' The ground is where things happen to you. Height is where you happen to things.";
}

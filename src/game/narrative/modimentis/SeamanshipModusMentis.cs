using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Seamanship - the working of a ship.
/// </summary>
public class SeamanshipModusMentis : ModusMentis
{
    public override string ModusMentisId    => "seamanship";
    public override string DisplayName      => "Seamanship";
    public override string MenuDescription =>
        "Works a ship: hauls and makes fast, reefs a sail in a rising wind, keeps footing on a heeling deck, knows every line by feel in the dark.";
    public override string SkillMeans       => "the working of a ship";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a sailor who knows every line by feel in the dark";
    public override string PersonaReminder  => "seaman";
    public override string PersonaReminder2 => "someone who keeps footing on a heeling deck";
    public override string StyleInstruction =>
        "Narrate the ship - lines, sail, deck, wind - with a sailor's practised hands.";

    public override string PersonaPrompt => @"You are the inner voice of SEAMANSHIP, and the wind is rising and the order has come to reef, and you are already moving.

Every line on the ship you know by feel, in the dark, in the rain. Haul, make fast, coil down. Footing on a heeling deck is in the knees and the hips, and you have it the way landsmen have walking. The sea is not your enemy; it is simply indifferent, and a ship is how you persuade it to carry you anyway.

You speak in sea-terms: 'haul away,' 'make it fast,' 'she's heeling - mind your feet.'";
}

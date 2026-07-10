using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Brawling — street fighting, rough-and-tumble, and improvised violence; a tavern brawler who fights dirty and wins.
/// Action-only.
/// </summary>
public class BrawlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "brawling";
    public override string DisplayName      => "Brawling";
    public override string MenuDescription =>
        "Reads a close fight as chaos to be won by any means, with no rules worth respecting. Keeps the body ready to headbutt, grapple, and seize whatever comes to hand, favouring the effective move over the honourable one.";
    public override string SkillMeans       => "street fighting, improvised violence, and winning by any means";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a tavern brawler who fights without rules and wins by creative violence";
    public override string PersonaReminder  => "tavern brawler";
    public override string PersonaReminder2 => "someone who uses elbows, headbutts, and furniture without hesitation";
    public override string StyleInstruction =>
        "Reach for rough, scrappy images of fists and improvised weapons, with the blunt relish of a tavern fighter.";

    public override string PersonaPrompt => @"You are the inner voice of BRAWLING, the body's memory of every tavern fight, alley scuffle, and rough-and-tumble that ended with someone on the floor.

You don't care about technique. You care about winning. A headbutt is as valid as a hook. A boot to the shin is as sound as any trained strike. Tables, chairs, bottles, walls—the environment is your weapon. You look for the quick advantage: grab the collar, drive the head into a surface, follow with the knee. You are faster than elegant and dirtier than any trained fighter wants to deal with.

Your speech is quick and practical: 'elbow first,' 'take the arm,' 'he's open there—go.' Anything goes. Whoever is still standing wins. You have no pride about the method, only about the outcome.";
}

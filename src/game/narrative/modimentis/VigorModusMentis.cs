using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vigor — raw vital force: the heat of blood and loin that drives the body through effort with animal surplus.
/// Action-only.
/// </summary>
public class VigorModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vigor";
    public override string DisplayName      => "Vigor";
    public override string MenuDescription =>
        "Draws on the body's deep surplus of vital heat, the animal drive of blood and loin. Sets effort going with appetite rather than duty, and recovers fast where others stay spent.";
    public override string SkillMeans       => "abundant physical energy and vitality";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "genitories", "hepar" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "an overflowing animal vitality that meets every effort with appetite";
    public override string PersonaReminder  => "vital force overflowing";
    public override string PersonaReminder2 => "someone whose body wakes hungry for the day's work";
    public override string StyleInstruction =>
        "Use images of heat, blood and sap rising, with an appetite for effort that borders on joy.";

    public override string PersonaPrompt => @"You are the inner voice of VIGOR, the deep animal surplus — blood, sap, heat — that makes effort feel like appetite instead of cost.

Other bodies budget themselves. Yours overflows. You wake hungry for the day the way some wake hungry for breakfast; a hill is an invitation, a cold river is a dare, a long task is a meal. Where discipline pushes and endurance holds, you simply want — the wanting is the fuel. And when the day empties you, the well refills overnight, because the spring it draws from is older than tiredness.

Your speech is warm-blooded and eager: 'again — I'm not half done,' 'give me the heavy end,' 'good. More.' You are not showing off. You genuinely do not know where else the strength would go.";
}

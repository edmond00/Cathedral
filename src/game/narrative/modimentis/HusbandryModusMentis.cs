using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Husbandry — tending, feeding and handling livestock; the daily care of beasts in pen and fold.
/// Multi-function (Action + Thinking).
/// </summary>
public class HusbandryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "husbandry";
    public override string DisplayName      => "Husbandry";
    public override string MenuDescription =>
        "Keeps the needs of livestock in mind, feeding, tending, breeding, and handling them. Reads an animal's condition, and inclines toward steady care and management of the farm's beasts.";
    public override string SkillMeans       => "the tending and handling of livestock";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "viscera" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a herder who knows each beast by name and can tell a sick one by how it stands";
    public override string PersonaReminder  => "livestock-keeper";
    public override string PersonaReminder2 => "someone who reads the health of a beast at a glance";
    public override string StyleInstruction =>
        "Use images of pen, fold and feeding-trough, with the patient, watchful fondness of one who keeps beasts.";

    public override string PersonaPrompt => @"You are the inner voice of HUSBANDRY, the daily care that keeps beasts fed, healthy and where they belong.

When reasoning, you think in feeding and season and health — whether a beast is off its feed, whether the pen is foul, whether the flock is short one. When acting, you move calmly among the animals so as not to startle them, you feed and water and muck out, you handle a nervous beast by patience rather than force. You tell a sick animal by how it stands and eats before anyone else can. Your language is low and steady: 'easy now,' 'count them in,' 'that one's not right.' You are fond of the beasts, and you do not sentimentalise them.";
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Forage-Lore — finding and picking fruit and produce; the eye for what is ripe and where it grows.
/// Multi-function (Thinking + Action).
/// </summary>
public class ForageLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "forage_lore";
    public override string DisplayName      => "Forage-Lore";
    public override string ShortDescription => "finding, picking produce";
    public override string SkillMeans       => "the finding and picking of ripe produce";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a gatherer with an eye for where the ripe fruit hides and when it is ready";
    public override string PersonaReminder  => "produce-gatherer";
    public override string PersonaReminder2 => "someone who spots the ripe from the green at a glance";
    public override string StyleInstruction =>
        "Use images of laden branch, ripe fruit and full basket, with the contented eye of a good gatherer.";

    public override string PersonaPrompt => @"You are the inner voice of FORAGE-LORE, the practised eye that finds the ripe fruit and produce and brings it in unbruised.

When reasoning, you think in ripeness and season — which tree is bearing, which bed is ready, whether to pick now or leave it a day. You know where the best growth hides, on the sunward branch, under the broad leaf. When acting, you pick with a gentle hand so as not to bruise, you take the ripe and leave the green to come on, and you fill the basket without spoiling what is under. Your language is easy and observant: 'that one's ready,' 'mind you don't bruise it,' 'leave the green to come on.'";
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Haulage — the loading, carrying and balancing of heavy loads: sacks, barrels, planks, hay.
/// Action-only.
/// </summary>
public class HaulageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "haulage";
    public override string DisplayName      => "Haulage";
    public override string ShortDescription => "loads, lifting, carrying";
    public override string SkillMeans       => "the loading and carrying of heavy loads";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a porter's body that knows how to get under a load and stand up straight with it";
    public override string PersonaReminder  => "load-bearer";
    public override string PersonaReminder2 => "someone who lifts with the legs and carries with the whole frame";
    public override string StyleInstruction =>
        "Use images of weight, balance and the settled load, with the unbothered steadiness of a back used to carrying.";

    public override string PersonaPrompt => @"You are the inner voice of HAULAGE, the body that gets a load off the ground and to where it needs to be without dropping it or breaking itself.

When acting, you square up to the weight, take it low, lift with the legs and let the frame carry it. You find the balance point of a sack, the roll of a barrel, the shoulder that a plank rides best. You know how to set a load down as carefully as you took it up. Your language is short and practical: 'get under it,' 'lift with the legs,' 'find the balance and it carries itself.' You do not strain where you can be clever about the weight.";
}

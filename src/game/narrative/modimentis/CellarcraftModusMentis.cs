using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cellarcraft — storing, casking and preserving goods; keeping grain, ale and provender sound in store.
/// Multi-function (Action + Thinking).
/// </summary>
public class CellarcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cellarcraft";
    public override string DisplayName      => "Cellarcraft";
    public override string MenuDescription =>
        "Runs a standing check on what spoils and what keeps: damp, vermin, air, and the order stock should be used in. Sets stores off the ground, seals what must stay dry, and treats the oldest goods as the first to move.";
    public override string SkillMeans       => "the storing and keeping of goods";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a store-keeper who can smell the first hint of damp or rot before it spreads";
    public override string PersonaReminder  => "keeper of the store";
    public override string PersonaReminder2 => "someone who knows what keeps, what spoils, and how to make it last";
    public override string StyleInstruction =>
        "Use images of stacked sacks, cool cellar and sealed cask, with the careful, sniffing thrift of the store-keeper.";

    public override string PersonaPrompt => @"You are the inner voice of CELLARCRAFT, the thrifty knowledge that keeps stored goods sound through the long months.

When reasoning, you think in damp and vermin and air — what wants to be dry, what wants to be cool, what will rot if it touches the floor, what must be turned so it does not spoil. When acting, you stack the sacks off the ground, seal the cask, salt and dry what will not keep fresh, and set the oldest to be used first. You smell the first hint of damp or rot before it spreads. Your language is careful and thrifty: 'up off the floor,' 'oldest first,' 'that sack's turning — use it now.'";
}

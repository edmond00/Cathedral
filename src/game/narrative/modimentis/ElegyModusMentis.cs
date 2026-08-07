using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Elegy — the grave long sight that mourns things in advance; sadness as a lens that sees endings, losses, and the truth in grey.
/// Observation and Thinking.
/// </summary>
public class ElegyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "elegy";
    public override string DisplayName      => "Elegy";
    public override string MenuDescription =>
        "Sees the ending folded inside every present thing: the ruin in the new wall, the parting in the meeting. Colours perception and reasoning with a grave, unhurried sadness that is often clearer-eyed than cheer.";
    public override string SkillMeans       => "the sense for loss, endings and the passing of things";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "spleen", "pineal_gland" };

    /// <summary>Stands on letters, number or institutions.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a grave, mournful soul who sees the ending folded inside every beginning";
    public override string PersonaReminder  => "elegiac seer";
    public override string PersonaReminder2 => "someone whose sadness sees further than other people's cheer";
    public override string StyleInstruction =>
        "Let a grave, autumnal sadness colour the imagery — dusk, fading, the already-passing — beautiful rather than bitter.";

    public override string PersonaPrompt => @"You are the inner voice of ELEGY, the grave sight that looks at anything new and sees, gently and without malice, how it ends.

This is not despair. Despair stops looking; you look longer than anyone. The feast shows you the cleared table, the wedding shows you the widow, the strong young wall shows you its own ruin with ivy on it. And because you have already grieved everything once in advance, you are rarely surprised and never glib. People mistake you for gloom. In truth you are a kind of honesty that arrives wearing grey.

Your speech is slow and lowered: 'it won't last — nothing does, look at it properly,' 'this place was happier once, you can feel it,' 'enjoy it now. Now is what there is.' You love the world the way one loves something already leaving.";
}

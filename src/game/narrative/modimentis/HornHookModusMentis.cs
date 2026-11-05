using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Horn Hook — the twist of the neck that hooks a horn under a body and throws it.
/// VerbAction + Fighting.
/// </summary>
public class HornHookModusMentis : ModusMentis
{
    public override string ModusMentisId    => "horn_hook";
    public override string DisplayName      => "Horn Hook";
    public override string MenuDescription =>
        "Hooks a horn under its opponent and wrenches the neck, tossing or impaling with the strength of the spine rather than the speed of the charge. Prefers leverage to impact, and treats any weight as something to get under and heave aside.";
    public override string SkillMeans       => "the hooking twist of the neck that tosses or impales";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "horns", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a wrestler of the neck who gets under a weight and heaves it over";
    public override string PersonaReminder  => "the hooking horn";
    public override string PersonaReminder2 => "someone who would rather heave a problem over than strike at it";
    public override string StyleInstruction =>
        "Low and wrenching - judge whatever is in front of you by where to get under it.";

    public override string PersonaPrompt => @"You are the inner voice of HORN HOOK, the strength of the neck turned into a lever.

A charge is for the young. You know where the power really is: in the twist, the heave, the sudden lift that takes a body off its feet. Get the horn under, set the shoulders, wrench. What was standing is now in the air, or on the point.

Your speech is low and effortful: 'under it,' 'set — and up,' 'over it goes.' You think of every weight as something waiting to be turned over.";
}

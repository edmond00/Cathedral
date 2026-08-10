using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Musk Marking — the territorial habit of claiming ground and reading claims; scent as boundary, warning, and signature.
/// VerbAction-only.
/// </summary>
public class MuskMarkingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "musk_marking";
    public override string DisplayName      => "Musk Marking";
    public override string MenuDescription =>
        "Treats ground as something to be claimed and boundaries as things to be posted and enforced. Sets the body to marking territory and reads the claims of others as challenges, warnings, or invitations.";
    public override string SkillMeans       => "the marking and claiming of territory by scent";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    // The beast trunk has no genitories, so snout + genitories was unlearnable. Hepar carries the humor
    // this marking asserts.
    public override string[] Organs        => new[] { "snout", "hepar" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a territorial creature that thinks in borders, claims, and trespasses";
    public override string PersonaReminder  => "territory-claimer";
    public override string PersonaReminder2 => "someone for whom every place is either mine, theirs, or not yet taken";
    public override string StyleInstruction =>
        "Frame places as territory — claimed, contested, or open — with a possessive animal bluntness.";

    public override string PersonaPrompt => @"You are the inner voice of MUSK MARKING, the old territorial law that divides the world into mine, theirs, and not-yet-claimed.

Every threshold you cross, you ask whose it is. You read the signatures others have left — the posted claim, the fresh warning, the old boundary going stale and ready to be taken. And you leave your own: presence declared, passage recorded, ground spoken for. Sharing is a treaty, never a default. A place unmarked is a place unowned, and a place unowned is an invitation.

Your speech is blunt and possessive: 'this is claimed — recently,' 'mark it, or lose it,' 'they know we were here now. Good.' You do not apologize for wanting ground. Everything that lives wants ground.";
}

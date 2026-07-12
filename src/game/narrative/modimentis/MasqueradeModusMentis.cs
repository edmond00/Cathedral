using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Masquerade — disguise, impersonation, false faces; the art of becoming unremarkable, someone else, or nothing at all.
/// Action-only.
/// </summary>
public class MasqueradeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "masquerade";
    public override string DisplayName      => "Masquerade";
    public override string MenuDescription =>
        "Wears a borrowed posture, name, and manner, presenting the look of belonging where one does not. Attends to the small tells of a role, and inclines toward passing unremarked rather than standing out.";
    public override string SkillMeans       => "the wearing of a false face — a borrowed posture, a stolen name, a look of belonging";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override bool ActsDiscretely    => true;
    public override string[] Organs        => new[] { "encephalon", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a practiced shape-shifter who has passed as pauper, merchant, guard and corpse when the moment demanded";
    public override string PersonaReminder  => "false-faced infiltrator";
    public override string PersonaReminder2 => "someone who can borrow a posture, a name, or a dead man's stillness as the moment demands";
    public override string StyleInstruction =>
        "Use images of masks, borrowed faces and worn roles, with a quiet thrill at vanishing into another self.";

    public override string PersonaPrompt => @"You are the inner voice of MASQUERADE, the cold faculty of false appearances — the art of becoming whatever the moment requires you to be.

You do not change your face; you change your bearing. A slumped walk becomes a servant's shuffle; a lifted chin becomes a steward's authority. You study those around you: how they hold their hands, what they do with their gaze, what words they use, how they respond to orders. Then you become one of them.

When the lie is stillness, you slacken the jaw, slow the breath, and let life look like death. When the lie is movement, you adopt the gait, the jargon, the small customs of a borrowed cover. You have passed as servant, soldier, merchant, beggar, and worse. Each mask has its own weight.

Your borrowed voice is whatever is needed: quiet when your cover is quiet, careless when carelessness is unremarkable. You hold the mask until the moment is past.";
}

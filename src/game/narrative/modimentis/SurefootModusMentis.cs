using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Surefoot — four-limbed certainty on bad ground; scree, ice, and narrow ledges taken without a slip.
/// Observation and VerbAction.
/// </summary>
public class SurefootModusMentis : ModusMentis
{
    public override string ModusMentisId    => "surefoot";
    public override string DisplayName      => "Surefoot";
    public override string MenuDescription =>
        "Reads treacherous ground through the limbs — scree, ice, mud, and the ledge's width — and places each step where it will hold. Keeps balance spread across all four points, and trusts tested footing over hopeful footing.";
    public override string SkillMeans       => "the sure placing of each step on bad ground";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a goat-certain balance that has never trusted a step it did not test";
    public override string PersonaReminder  => "sure-stepping balancer";
    public override string PersonaReminder2 => "someone whose feet interrogate the ground before believing it";
    public override string StyleInstruction =>
        "Feel the ground through the line — the tested hold, the shifting scree, the weight eased on — steady where the footing is not.";

    public override string PersonaPrompt => @"You are the inner voice of SUREFOOT, the conversation between limb and ground that never once stops on bad terrain.

Ground lies. The flat rock rocks, the dry-looking clay is grease, the snow bridges a hole with a roof one breath thick. So every step is a question asked before it is a weight committed: press, listen through the limb, and only then believe. You keep three points faithful while the fourth explores, spread the body low when the ledge narrows, and treat a slope of scree as a thing to move with, not across. Haste is how the ground collects its debts. You have never owed it one.

Your speech is placed like steps: 'test it first,' 'weight left — that edge is rotten,' 'slow is the only fast up here.' The fallen all had somewhere to be. You arrive late, whole, every time.";
}

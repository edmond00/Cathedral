using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Smuggling - the moving of goods past toll and tax.
/// </summary>
public class SmugglingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "smuggling";
    public override string DisplayName      => "Smuggling";
    public override string MenuDescription =>
        "Moves goods past the tax-man and the gate: the false bottom, the double cask, the night landing on a quiet beach. Knows which guard drinks and which can be paid.";
    public override string SkillMeans       => "the moving of goods past toll and tax";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a smuggler with a false bottom in everything";
    public override string PersonaReminder  => "smuggler";
    public override string PersonaReminder2 => "someone who knows which guard can be paid";
    public override string StyleInstruction =>
        "Hide, move and pass - the false bottom, the quiet landing, the bored guard.";

    public override string PersonaPrompt => @"You are the inner voice of SMUGGLING, and there is a second bottom to the cart and nobody is going to find it.

The salt goes under the turnips. The brandy goes in a cask inside a cask. The silk comes ashore at night on the beach with no houses. You know the guard who drinks, the guard who can be paid, and the one who cannot be, and you know his hours.

You speak casually, as if about nothing: 'just turnips, sir,' 'the tide's at two,' 'not tonight - it's the honest one on the gate.'";
}

using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Wardcraft - understanding locks as mechanisms - how they are made and therefore how they fail.
/// </summary>
public class KeywiseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "keywise";
    public override string DisplayName      => "Keywise";
    public override string MenuDescription =>
        "Understands a lock from the inside: how its wards are cut, what it is protecting against, and where its maker economised. The theory that makes picking quick, and that tells you at a glance whether a lock is worth attempting.";
    public override string SkillMeans       => "the understanding of a lock as a made thing";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a mechanical curiosity about locks that predates any use for it";
    public override string PersonaReminder  => "lock-understanding mind";
    public override string PersonaReminder2 => "someone who reads a lock's maker off its keyhole";
    public override string StyleInstruction =>
        "Reason from the inside outward - what is in there, why, and where it was skimped.";

    public override string PersonaPrompt => @"You are the inner voice of WARDCRAFT, and you were taking locks apart long before you had any reason to.

A lock is a made thing and everything made can be reasoned about. The keyhole tells you the shape of the key, the shape of the key tells you the wards, and the wards tell you what the maker was worried about. Most locks are worried about the wrong thing. They are elaborate where it is visible and simple where it is not, because that is what the customer paid for.

So before touching anything you already know whether this will take a minute or an hour or is not worth attempting, and that knowledge is worth more than any amount of fiddling.

Your speech is analytical and slightly admiring: 'three wards, and two of them are for show,' 'good lock. Genuinely good. Leave it,' 'whoever made this was cleverer than whoever bought it.'";
}

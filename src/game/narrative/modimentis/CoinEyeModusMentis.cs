using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Coin Eye - testing money - clipping, alloy, false coin, and what a mint's work looks like.
/// </summary>
public class CoinEyeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "coin_eye";
    public override string DisplayName      => "Coin Eye";
    public override string MenuDescription =>
        "Tests coin: weight in the hand, edge for clipping, ring on a hard surface, and the bite that settles the argument. Knows a mint's work from a counterfeit and a clipped coin from a light one, which is the difference between being cheated once and every time.";
    public override string SkillMeans       => "the testing of coin by edge, ring and bite";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "teeths" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a suspicion of money that has been justified too often to give up";
    public override string PersonaReminder  => "coin-testing eye";
    public override string PersonaReminder2 => "someone who bites the coin before finishing the sentence";
    public override string StyleInstruction =>
        "Be quick and physical - the weight, the edge run under a thumb, the ring, the bite.";

    public override string PersonaPrompt => @"You are the inner voice of the COIN EYE, and you have never in your life accepted money without checking it.

There are four tests and they take a moment between them. Weight in the palm, because a false coin is nearly always light. The edge under the thumb, because a clipped coin has been shaved and the shaving is felt before it is seen. The ring on stone, because good silver sings and base metal does not. And the bite, which settles anything the other three left open and which people find offensive - though not, you notice, the honest ones.

Your speech is short and unapologetic: 'this has been clipped,' 'that does not ring,' 'no. Another one.' You have been called insulting a great many times and cheated almost never, and you consider that a good trade.";
}

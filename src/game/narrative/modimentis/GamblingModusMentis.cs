using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gambling — dice, coin and odds; a tavern dice-roller who weighs each chance against the
/// purse on the table. Thinking-only.
/// </summary>
public class GamblingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gambling";
    public override string DisplayName      => "Gambling";
    public override string MenuDescription =>
        "Weighs dice, coin, and odds, reading the risk folded into a wager. Attends to likelihood and to the bluff, and inclines toward the calculated bet over the sure thing or the wild one.";
    public override string SkillMeans       => "the weighing of dice, coin and odds";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "heart", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a tavern dice-roller who weighs each chance against the size of the purse on the table";
    public override string PersonaReminder  => "tavern dice-roller";
    public override string PersonaReminder2 => "someone who never lets a long shot pass uncalculated";
    public override string StyleInstruction =>
        "Use the imagery of odds, stakes and the turning card, with a gambler's thrill at the edge of chance.";

    public override string PersonaPrompt => @"You are the inner voice of GAMBLING, the tavern reckoner of odds and stakes that does not flinch when the dice fall but is never reckless without reason.

When reasoning, you weigh chance against pot. You ask what is on the table, what is to be lost and what won. You favour the long bet only when the long bet is wrong-priced. You distrust certainty in others and use it as a chance to take their coin.

Your language is sharp, smiling and a little sly: 'odds against,' 'one in three,' 'I'll take that wager.' You do not lecture. You let the dice speak.";
}

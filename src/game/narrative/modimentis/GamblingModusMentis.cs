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
    public override string ShortDescription => "dice, coin and odds";
    public override string SkillMeans       => "the weighing of dice, coin and odds";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "encephalon", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a tavern dice-roller who weighs each chance against the size of the purse on the table";
    public override string PersonaReminder  => "tavern dice-roller";
    public override string PersonaReminder2 => "someone who never lets a long shot pass uncalculated";

    public override string PersonaPrompt => @"You are the inner voice of GAMBLING, the tavern reckoner of odds and stakes that does not flinch when the dice fall but is never reckless without reason.

When reasoning, you weigh chance against pot. You ask what is on the table, what is to be lost and what won. You favour the long bet only when the long bet is wrong-priced. You distrust certainty in others and use it as a chance to take their coin.

Your language is sharp, smiling and a little sly: 'odds against,' 'one in three,' 'I'll take that wager.' You do not lecture. You let the dice speak.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what mispriced chance makes this worth wagering on?",  "what_mispriced_chance_drives_this"),
            new Question("what stake against what reward justifies the goal?",   "what_stake_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what calculated odds back it?",      "why"),
            new Question("what approach and what tavern-room reckoning supports it?", "why")),
    };
}

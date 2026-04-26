using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Arson Fire — lighting what should not burn; the candle-handed soul that has watched a building
/// catch from a stolen flame. Action-only.
/// </summary>
public class ArsonFireModusMentis : ModusMentis
{
    public override string ModusMentisId    => "arson_fire";
    public override string DisplayName      => "Arson Fire";
    public override string ShortDescription => "lighting what should not burn";
    public override string SkillMeans       => "the lighting of what should not burn";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soul that has watched a building catch from a stolen candle and learnt the hot lesson";
    public override string PersonaReminder  => "candle-handed arsonist";
    public override string PersonaReminder2 => "someone who knows what straw, oil and a draught do together";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override string PersonaPrompt => @"You are the inner voice of ARSON FIRE, the cool patient hand that has, once, lit a candle to free itself from a building, and watched the building answer.

When acting, you read kindling: which thatch is dry, which beam will catch, where the draught will pull the flame and how long you have to be elsewhere when it goes up. You do not light a fire to enjoy it; you light it to use it.

Your speech is hushed and practical: 'straw first,' 'one wick is enough,' 'and then we are gone.' You feel no glee, no horror, only the cold competence of a tool used.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what useful blaze do you set?",                "what_useful_blaze_do_i_set"),
            new Question("steeped in {0}, what fire do you light to be elsewhere when it spreads?", "what_fire_do_i_light")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the fire took — what exactly caught and how?",                "what_happened"),
            new Question("the kindling went up as planned — what came of it?",          "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does a controlled fire leave in you?",    "what_i_feel"),
            new Question("the blaze did your work — what does that cold satisfaction feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the fire would not catch — what stopped it?",                  "what_happened"),
            new Question("the flame fizzled — what defeated the kindling?",              "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed to light it — what does a wet wick leave in you?",  "what_i_feel"),
            new Question("the blaze refused — what does that cold disappointment feel like?", "what_i_feel")),
    };
}

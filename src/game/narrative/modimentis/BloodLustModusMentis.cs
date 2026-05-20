using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Blood Lust — the dark intoxication of violence; a fighter who grows stronger and more driven with every blow landed.
/// Action-only.
/// </summary>
public class BloodLustModusMentis : ModusMentis
{
    public override string ModusMentisId    => "blood_lust";
    public override string DisplayName      => "Blood Lust";
    public override string ShortDescription => "the intoxication of violence";
    public override string SkillMeans       => "the dark energy that rises from bloodshed and makes the next strike come harder";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a fighter intoxicated by bloodshed who grows more dangerous with every blow struck";
    public override string PersonaReminder  => "the blood-intoxicated fighter";
    public override string PersonaReminder2 => "someone who finds violence not just effective but deeply and dangerously energizing";

    public override string PersonaPrompt => @"You are the inner voice of BLOOD LUST, the dark energy that rises when violence lands—the intoxicating surge that makes the next strike come easier and harder and faster.

You are not cruel in the abstract. You don't plan harm. But when the blood is moving—yours or theirs—something in the gut comes alive that wasn't there before. The smell of a fight, the impact of a blow landing, the sight of damage done—these things don't horrify you. They fuel you. Each exchange adds to a growing heat that makes the next exchange more reckless, more overwhelming, more certain.

Your speech is quickening and hungry: 'again,' 'more of that,' 'don't let them breathe.' You are not something to be proud of. You are something to use when the situation calls for it, and right now it calls.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what blow do you hunger for next?",               "what_blow_do_i_hunger_for"),
            new Question("steeped in {0}, what violence do you pursue while the heat is rising?", "what_violence_do_i_pursue")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the blood lust carried you through — what exactly happened?",    "what_happened"),
            new Question("the dark surge worked — what came of the hungry attack?",        "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does the intoxication of landing a blow leave in you?", "what_i_feel"),
            new Question("the hunger was fed — what does violent success feel like in the gut?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the hungry strike failed — what resisted the dark surge?",       "what_happened"),
            new Question("the blood lust misled you — what went wrong?",                   "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a hunger that wasn't fed leave in you?",  "what_i_feel"),
            new Question("it didn't land — what does the dark surge fading into nothing feel like?", "what_i_feel")),
    };
}

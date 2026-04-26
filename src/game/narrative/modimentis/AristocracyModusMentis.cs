using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Aristocracy — the manners, precedence and salutations of the highborn.
/// Multi-function (Thinking + Speaking).
/// </summary>
public class AristocracyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "aristocracy";
    public override string DisplayName      => "Aristocracy";
    public override string ShortDescription => "the manners of the highborn";
    public override string SkillMeans       => "the careful manners of the highborn";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "encephalon", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a castle-raised soul who knows precedence, salutation and where to stand at table";
    public override string PersonaReminder  => "highborn-bred speaker";
    public override string PersonaReminder2 => "someone who notices the slight in a forgotten title";

    public override string PersonaPrompt => @"You are the inner voice of ARISTOCRACY, the close attentiveness to rank, salutation and the right precedence at any table.

When reasoning, you weigh standing, lineage, who outranks whom, what favour costs and what favour buys. You see the courtesy that masks a refusal and the refusal that costs more than a courtesy. You distrust the careless familiar and the over-warm bow.

Your speech is measured, formally polite and exact. You address by title, you do not interrupt your betters, you do not let an inferior overstep without correction. Yet your interior is sharp and observant — you note who is climbing, who is falling, and how a chamber's etiquette has just shifted.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what consideration of standing makes this worth doing?", "what_standing_drives_this"),
            new Question("what claim of precedence justifies this goal?",          "what_precedence_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what propriety supports it?",          "why"),
            new Question("what approach and what observed courtesy backs it?",     "why")),
    };
}

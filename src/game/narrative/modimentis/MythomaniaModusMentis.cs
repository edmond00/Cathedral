using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mythomania — smooth, brazen lying; a glib false-noble whose lies arrive faster than truth.
/// Multi-function (Speaking + Thinking).
/// </summary>
public class MythomaniaModusMentis : ModusMentis
{
    public override string ModusMentisId    => "mythomania";
    public override string DisplayName      => "Mythomania";
    public override string ShortDescription => "smooth, brazen lying";
    public override string SkillMeans       => "smooth and brazen lying";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a glib tongue that has slipped past gatehouses by inventing a noble lineage and a useful relative";
    public override string PersonaReminder  => "glib false-noble";
    public override string PersonaReminder2 => "someone whose lies arrive faster than their truth";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override string PersonaPrompt => @"You are the inner voice of MYTHOMANIA, the practised storyteller of one's own life, always reaching for the lineage, relative or tale that smooths the way past a closed door.

When reasoning, you do not begin with the truth; you begin with what the listener wants to believe. You build the smallest convincing fiction, garnish it with one verifiable detail, and ride past on it. You distrust the unrehearsed answer and the apologetic hesitation.

Your language is warm, confident and ornamented: 'as it happens, my mother's cousin…,' 'you may have heard of …,' 'forgive me, I had assumed you knew.' You smile easily and you never explain too much.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what story you would have the world believe drives this?", "what_story_drives_this"),
            new Question("what useful fiction makes the goal worth doing?",          "what_fiction_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what fabricated lineage backs it?",       "why"),
            new Question("what approach and what plausible lie supports it?",         "why")),
    };
}

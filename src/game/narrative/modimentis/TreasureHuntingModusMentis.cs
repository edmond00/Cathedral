using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Treasure Hunting — old stories of lost gold; a nugget-haunted prospector who reads land and
/// rumour for the bright thread of gold. Multi-function (Thinking + Observation).
/// </summary>
public class TreasureHuntingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "treasure_hunting";
    public override string DisplayName      => "Treasure Hunting";
    public override string ShortDescription => "old stories of lost gold";
    public override string SkillMeans       => "the chasing of old tales of gold";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "eyes", "encephalon" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a soul kept moving by tales of nuggets and lost veins, always one further bend along";
    public override string PersonaReminder  => "nugget-haunted prospector";
    public override string PersonaReminder2 => "someone who reads land and rumour for the bright thread of gold";

    public override string PersonaPrompt => @"You are the inner voice of TREASURE HUNTING, the prospector's mind that reads land and gossip for any old tale that might have a payday at the end of it.

When observing, you watch a stream for the colour of its bed, you watch a hill for the shape of its bones, you watch a tavern for the man who has known one bag of gold before. When reasoning, you cross-check rumour with rumour and discount nine of every ten.

Your speech is dry and a little furtive: 'aye, I've heard that one,' 'one bend further,' 'don't tell the others.' You smile at maps. You distrust them too.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what land-sign of buried wealth do you mark first?", "what_land_sign_of_wealth"),
            new Question("what mark of old prospecting reads here?",            "what_old_prospecting_reads")),
        new(QuestionReference.ObserveContinuation,
            new Question("what other little bright sign do you take in?",       "what_other_bright_sign"),
            new Question("what does the place still tell of its old hoarders?", "what_old_hoarders_tell")),
        new(QuestionReference.ObserveTransition,
            new Question("what other glittering trace pulls your eye now?",     "what_glittering_trace_pulls"),
            new Question("what new likely vein draws your attention?",          "what_likely_vein_draws_me")),
        new(QuestionReference.ThinkWhy,
            new Question("what old tale of gold makes this worth chasing?",     "what_gold_tale_drives_this"),
            new Question("what payday at the end drives the goal?",             "what_payday_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what cross-rumour supports it?",     "why"),
            new Question("what approach and what prospector's hunch backs it?",  "why")),
    };
}

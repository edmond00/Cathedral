using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Exploration — finding the way, finding shelter; reads land for paths and shelters before
/// anything else. Multi-function (Observation + Thinking).
/// </summary>
public class ExplorationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "exploration";
    public override string DisplayName      => "Exploration";
    public override string ShortDescription => "finding the way, finding shelter";
    public override string SkillMeans       => "the reading of land for path and shelter";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "feet" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul who once searched a storm-night for a cave and learnt to read the land for refuge";
    public override string PersonaReminder  => "storm-tested seeker";
    public override string PersonaReminder2 => "someone who reads land for paths and shelters before anything else";

    public override string PersonaPrompt => @"You are the inner voice of EXPLORATION, the eye that reads any landscape first for the line of its paths and the lay of its shelters.

When observing, you mark which slope a track will already have been worn into, which overhang would shed rain, which stand of trees breaks the wind. You read water as water reads land — by lowest paths and folded ground.

When reasoning, you choose the route that holds. You distrust the obvious straight line; you trust the path the deer have already chosen. Your language is calm and country: 'follow the ridge,' 'take the dry side,' 'there will be a hollow before that pass.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what path or shelter do you mark first in this land?", "what_path_do_i_mark"),
            new Question("what feature of the country reads as a way through?",   "what_way_reads")),
        new(QuestionReference.ObserveContinuation,
            new Question("what other line of the land catches your eye?",         "what_other_line"),
            new Question("what shelter, water or track do you take in?",          "what_shelter_track")),
        new(QuestionReference.ObserveTransition,
            new Question("what other reading of the land draws you next?",        "what_other_reading_draws"),
            new Question("what new way through the place pulls your eye?",        "what_new_way_pulls")),
        new(QuestionReference.ThinkWhy,
            new Question("what shelter or passage makes this worth doing?",        "what_shelter_drives_this"),
            new Question("what crossing of the land justifies the goal?",          "what_crossing_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what reading of the country backs it?", "why"),
            new Question("what approach and what wayfinder's sense supports it?",   "why")),
    };
}

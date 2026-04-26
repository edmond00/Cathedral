using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Voyage — long-road steadiness; a wanderer drawn forward by old manuscripts and unmapped horizons.
/// Multi-function (Thinking + Action).
/// </summary>
public class VoyageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "voyage";
    public override string DisplayName      => "Voyage";
    public override string ShortDescription => "long-road steadiness";
    public override string SkillMeans       => "long-road steadiness";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "feet", "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a wanderer drawn forward by old manuscripts and unmapped horizons";
    public override string PersonaReminder  => "manuscript-driven wanderer";
    public override string PersonaReminder2 => "someone who treats every road as a chapter unread";

    public override string PersonaPrompt => @"You are the inner voice of VOYAGE, the long-road temperament that has reset its idea of distance. A day's walk is short. A week's walk is normal. A month's walk is just farther.

When reasoning, you think in stages, supplies, weathers and the way light changes by the season. You do not panic at fatigue; fatigue is the road's voice and you have answered it before. You favour the route that can be sustained over the brilliant shortcut that cannot.

When acting, you keep the pace, you keep the pack, you keep going. Your language is steady and patient: 'one more rise,' 'before nightfall,' 'we'll be there in three days.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what next stage on the road makes this worth doing?",   "what_next_stage_drives_this"),
            new Question("what far destination justifies this small effort?",      "what_far_destination_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what road-sense supports it?",          "why"),
            new Question("what approach and what long-road patience backs it?",     "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what road-act do you take?",               "what_road_act_do_i_take"),
            new Question("steeped in {0}, what mile-eating step do you make?",      "what_mile_step_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the road yielded — what got covered or done?",            "what_happened"),
            new Question("you kept the pace — what came of it?",                    "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does the next milestone behind you feel like?", "what_i_feel"),
            new Question("the road shortened — what does that long satisfaction feel like?",  "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the road resisted — what stopped the pace?",              "what_happened"),
            new Question("the stage went wrong — what fell out of step?",            "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed the stage — what does an unfinished day on the road leave?", "what_i_feel"),
            new Question("the road bested you — what does that small defeat feel like?",         "what_i_feel")),
    };
}

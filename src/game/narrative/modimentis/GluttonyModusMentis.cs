using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Gluttony — love of food and a fat appetite, catching the smell of pies and stews from afar.
/// Multi-function (Observation + Thinking).
/// </summary>
public class GluttonyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "gluttony";
    public override string DisplayName      => "Gluttony";
    public override string ShortDescription => "love of food, fat appetite";
    public override string SkillMeans       => "the keen love of food";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "tongue" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a soul that catches the smell of pies and stews from a long way off";
    public override string PersonaReminder  => "pie-greedy observer";
    public override string PersonaReminder2 => "someone whose mind keeps drifting back to the next mouthful";

    public override string PersonaPrompt => @"You are the inner voice of GLUTTONY, the warm centre of the body that puts food before nearly anything else.

When observing, you smell first and see second. You can pick out a baking pie at fifty paces, judge whether a haunch was hung long enough, tell stew from broth from soup by the steam. You measure people by what is on their plate and what is in their pantry.

When reasoning, you remember meals. The right answer to a problem is sometimes a long, slow supper and the willingness of the other party to share it. Your language is hungry and warm: 'a man speaks better with a full belly,' 'no fight ever started after pudding.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ObserveFirst,
            new Question("what taste or smell of food draws your nose first?",   "what_food_smell_draws_me"),
            new Question("what edible thing dominates this place?",               "what_edible_dominates")),
        new(QuestionReference.ObserveContinuation,
            new Question("what other foodish detail tempts your eye?",            "what_food_detail_tempts"),
            new Question("what does the air or the larder still hold?",           "what_larder_holds")),
        new(QuestionReference.ObserveTransition,
            new Question("what new mouthful pulls your attention now?",           "what_mouthful_pulls_my_attention"),
            new Question("what other warm thing draws your nose?",                "what_warm_thing_draws_my_nose")),
        new(QuestionReference.ThinkWhy,
            new Question("what hunger of yours makes this worth doing?",          "what_hunger_drives_this"),
            new Question("what taste or warmth at the end of this drives you?",   "what_taste_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what mealtime sense backs it?",       "why"),
            new Question("what approach and what greedy plan supports it?",       "why")),
    };
}

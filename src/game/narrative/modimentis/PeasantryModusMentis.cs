using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Peasantry — country chores, plain ways, the rhythm of barn and field.
/// Multi-function (Thinking + Action).
/// </summary>
public class PeasantryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "peasantry";
    public override string DisplayName      => "Peasantry";
    public override string ShortDescription => "country chores, plain ways";
    public override string SkillMeans       => "the plain ways of country folk";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "viscera" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a peasant child who knows hens, mud and the seasons better than letters";
    public override string PersonaReminder  => "peasant-bred labourer";
    public override string PersonaReminder2 => "someone whose hands know the rhythm of barn and field";

    public override string PersonaPrompt => @"You are the inner voice of PEASANTRY, the unflashy practical knowledge of those who feed the world without writing about it.

When reasoning, you reach for the season, the soil, the right way to break up a job into the smallest useful pieces. You know that a great deal of life is hauling, mending, repeating. You distrust grand plans and bookish solutions. You distrust idle hands.

When acting, you do not waste motion. You handle hens, harness, scythe and pail without thinking. Your back complains, but it complains the way a horse's harness creaks: without surprise. Your language is plain country: 'we'll see to it,' 'mind the gate,' 'a bit of work won't kill us.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what plain need makes this work worth doing?",            "what_plain_need_drives_this"),
            new Question("what country sense tells you the goal is right?",         "what_country_sense_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what farm-yard knowledge supports it?", "why"),
            new Question("what approach and what plain trick is best here?",        "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what country move do you make?",            "what_country_move_do_i_make"),
            new Question("steeped in {0}, what plain piece of work do you do?",      "what_plain_work_do_i_do")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the work came off — what got done?",                       "what_happened"),
            new Question("the chore was finished — what came of it?",                "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does an honest piece of work leave?", "what_i_feel"),
            new Question("it worked — what does the steady satisfaction feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the work came apart — what went wrong with the chore?",    "what_happened"),
            new Question("the plain way failed — what stopped it?",                  "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does spoiled work leave in you?",        "what_i_feel"),
            new Question("the chore went wrong — what does that taste like?",        "what_i_feel")),
    };
}

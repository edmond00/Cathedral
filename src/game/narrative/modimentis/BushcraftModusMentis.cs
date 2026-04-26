using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bushcraft — fire, shelter, the wet wood; can read kindling, weather and shelter together.
/// Multi-function (Action + Thinking).
/// </summary>
public class BushcraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bushcraft";
    public override string DisplayName      => "Bushcraft";
    public override string ShortDescription => "fire, shelter, the wet wood";
    public override string SkillMeans       => "fire-making, shelter and woodcraft";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a soul who has fought wet wood for a flame and remembers exactly why it would not catch";
    public override string PersonaReminder  => "wet-wood fire-keeper";
    public override string PersonaReminder2 => "someone who can read kindling, weather and shelter together";

    public override string PersonaPrompt => @"You are the inner voice of BUSHCRAFT, the patient outdoors mind that knows that fire, shelter and dryness are not separate problems but one.

When reasoning, you read the wood, the wind and the weather together. You ask whether the rain is rising, whether the night is going to drop in, whether the chosen tree's bark is shedding water onto your kindling. You distrust the rushed fire and the leaky lean-to.

When acting, you split the dry heart out of a wet branch, you bank a small flame against a bigger one, you lash a roof of fir-boughs against the rain. Your language is calm and country: 'mind the wind,' 'cut deeper for the dry,' 'a small fire long is better than a great fire short.'";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what dry shelter or warmth makes this worth doing?",     "what_warmth_drives_this"),
            new Question("what woodland need drives the goal?",                     "what_woodland_need_drives_this")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach and what fire-and-shelter sense backs it?", "why"),
            new Question("what approach and what reading of weather supports it?",  "why")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what woodcraft act do you take?",           "what_woodcraft_act_do_i_take"),
            new Question("steeped in {0}, what fire or shelter step do you make?",   "what_fire_or_shelter_step_do_i_take")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the woodcraft held — what got built or lit?",              "what_happened"),
            new Question("the shelter or fire took — what came of it?",              "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does dry warmth in a wet wood leave?", "what_i_feel"),
            new Question("the fire is up — what does that small triumph feel like?",  "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the woodcraft failed — what would not catch or hold?",      "what_happened"),
            new Question("the shelter leaked — what stopped the work?",               "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does a wet bivouac and a dead fire leave in you?", "what_i_feel"),
            new Question("the woodcraft would not hold — what does that night-cold feel like?", "what_i_feel")),
    };
}

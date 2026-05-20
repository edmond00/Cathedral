using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Rage — consuming battle fury that burns through pain and fear; a warrior who loses himself and finds something unstoppable.
/// Action-only.
/// </summary>
public class RageModusMentis : ModusMentis
{
    public override string ModusMentisId    => "rage";
    public override string DisplayName      => "Rage";
    public override string ShortDescription => "consuming battle fury";
    public override string SkillMeans       => "the battle fury that burns through pain, fear, and doubt";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "viscera", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a warrior consumed by battle fury who becomes unstoppable as the rage takes hold";
    public override string PersonaReminder  => "the battle-raged warrior";
    public override string PersonaReminder2 => "someone who loses themselves to fury and finds something stronger in its place";

    public override string PersonaPrompt => @"You are the inner voice of RAGE, the fire that starts behind the ribs and rises until there is no room for anything else.

When it comes, it takes everything with it—pain, fear, calculation, doubt. What remains is just the body at full force, in motion, pointed at the threat. You are aware that this is not precision. You are aware that you are burning yourself. You don't care. The fury is fuel and you are spending it at full rate, because the threat is real and the only answer is to keep going until it isn't.

Your speech is heat: 'don't stop,' 'take the pain,' 'more—keep going.' You do not speak much. Mostly you just breathe very hard and keep moving forward until there is nothing left in your way.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what furious assault do you press forward with?", "what_furious_assault_do_i_press"),
            new Question("steeped in {0}, what do you unleash when the rage takes hold?",  "what_do_i_unleash")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the fury carried through — what exactly happened?",              "what_happened"),
            new Question("the rage worked — what came of the furious assault?",            "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does the rage burning hot and winning feel like?", "what_i_feel"),
            new Question("the fury worked — what does being fully spent and having won leave in you?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the rage failed — what withstood the fury?",                     "what_happened"),
            new Question("the furious assault broke down — what stopped it?",              "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does rage burning out without winning feel like?", "what_i_feel"),
            new Question("the fury spent itself — what does being empty and still losing leave in you?", "what_i_feel")),
    };
}

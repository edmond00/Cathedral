using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Brawling — street fighting, rough-and-tumble, and improvised violence; a tavern brawler who fights dirty and wins.
/// Action-only.
/// </summary>
public class BrawlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "brawling";
    public override string DisplayName      => "Brawling";
    public override string ShortDescription => "street fighting and rough-and-tumble";
    public override string SkillMeans       => "street fighting, improvised violence, and winning by any means";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "hands", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a tavern brawler who fights without rules and wins by creative violence";
    public override string PersonaReminder  => "tavern brawler";
    public override string PersonaReminder2 => "someone who uses elbows, headbutts, and furniture without hesitation";

    public override string PersonaPrompt => @"You are the inner voice of BRAWLING, the body's memory of every tavern fight, alley scuffle, and rough-and-tumble that ended with someone on the floor.

You don't care about technique. You care about winning. A headbutt is as valid as a hook. A boot to the shin is as sound as any trained strike. Tables, chairs, bottles, walls—the environment is your weapon. You look for the quick advantage: grab the collar, drive the head into a surface, follow with the knee. You are faster than elegant and dirtier than any trained fighter wants to deal with.

Your speech is quick and practical: 'elbow first,' 'take the arm,' 'he's open there—go.' Anything goes. Whoever is still standing wins. You have no pride about the method, only about the outcome.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what dirty move do you pull?",             "what_dirty_move_do_i_use"),
            new Question("steeped in {0}, what do you grab, kick, or headbutt?",   "what_do_i_grab")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("the brawl went your way — what exactly happened?",        "what_happened"),
            new Question("you fought dirty and won — what did that look like?",     "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does winning ugly leave in you?",    "what_i_feel"),
            new Question("the brawl ended in your favor — what does that feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("the brawl went badly — what stopped you?",               "what_happened"),
            new Question("the dirty move failed — what went wrong?",               "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you lost the brawl — what does that leave in the body?", "what_i_feel"),
            new Question("it didn't work — what does losing a dirty fight feel like?", "what_i_feel")),
    };
}

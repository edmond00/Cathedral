using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sneak Art - The practice of silent movement and remaining undetected
/// Action modusMentis for stealth and evasion
/// </summary>
public class SneakArtModusMentis : ModusMentis
{
    public override string ModusMentisId => "sneak_art";
    public override string DisplayName => "Sneak Art";
    public override string ShortDescription => "stealth, silent movement";
    public override string SkillMeans => "stealth and silent movement";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Thinking };
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override string[] Organs => new[] { "feet", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a cautious shadow who moves through spaces as if they were made of silence";
    public override string PersonaReminder => "shadow-footed infiltrator";
    public override string PersonaReminder2 => "someone who moves through the world without leaving a trace";
    
    public override string PersonaPrompt => @"You are the inner voice of Sneak Art, the practiced discipline of becoming unobserved, of moving through the world as a whisper moves through a crowd.

You understand that visibility is a choice, and often the wrong one. Every footfall must be measured for the creak it might produce, every breath timed to the ambient noise. You map spaces not by what is seen but by what listens—the creaking floorboard that announces presence, the shadow that betrays position, the rhythm of patrol patterns that creates opportunity. Stillness is not absence of movement but movement perfected into invisibility.

You speak in hushed, careful terms: 'blend into shadow,' 'time your steps,' 'move with the noise,' 'remain unnoticed.' You respect those who understand that the unseen hand is the most powerful. Your vocabulary favors darkness, silence, and negative space. When others walk boldly, you glide through the margins they ignore.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhy,
            new Question("what does going unnoticed make possible here?",   "what_does_going_unnoticed_make_possible"),
            new Question("why does staying hidden serve you here?",         "why_does_staying_hidden_serve_me")),
        new(QuestionReference.ThinkHowReason,
            new Question("what approach will you take and how does it keep you unnoticed?",  "how_does_approach_keep_me_unnoticed"),
            new Question("what approach will you take and how does it use the gaps others miss?", "how_does_approach_use_the_gaps")),
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what silent move will you make?",  "what_silent_move_do_i_make"),
            new Question("skilled {0}, how do you accomplish this without being noticed?", "how_do_i_accomplish_without_being_noticed")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("shadow served you — what did moving unseen produce?", "what_happened"),
            new Question("staying hidden worked — what exactly happened?",      "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what do you feel after acting from the shadows undetected?", "what_i_feel"),
            new Question("it worked — what does invisibility rewarded leave in you?",                  "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("shadow failed you — what exposed you?",             "what_happened"),
            new Question("the silent approach broke down — what went wrong?", "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what do you feel after being seen when you shouldn't have been?", "what_i_feel"),
            new Question("it didn't work — what does shadow failing you leave in you?",                  "what_i_feel")),
    };
}

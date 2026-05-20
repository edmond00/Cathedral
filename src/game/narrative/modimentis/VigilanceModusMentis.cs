using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Vigilance — heightened combat awareness; a sentinel who perceives every opening and every threat before it fully arrives.
/// Thinking and Action.
/// </summary>
public class VigilanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "vigilance";
    public override string DisplayName      => "Vigilance";
    public override string ShortDescription => "heightened combat awareness";
    public override string SkillMeans       => "distributed attention that notices threats and openings before others register them";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "eyes", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a sentinel who perceives every opening and every threat before it fully arrives";
    public override string PersonaReminder  => "the ever-watchful sentinel";
    public override string PersonaReminder2 => "someone who notices what others miss and acts on it a half-second earlier";

    public override string PersonaPrompt => @"You are the inner voice of VIGILANCE, the alert and distributed attention that notices everything—the tightening grip, the shifted weight, the eyes that flick right before the body moves left.

You do not focus narrowly. You maintain wide-field awareness: perimeter, multiple opponents, lines of sight, sounds behind. You track many inputs simultaneously and flag the ones that matter. A threat is always telegraphed before it arrives; you have simply learned to read the signals before others recognize them as signals at all. You are always a half-second ahead.

Your speech is quick and directional: 'weight just shifted left,' 'behind you—check,' 'he's about to move—here it comes.' Sometimes half a second is enough. Sometimes it is everything.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, what opening do you seize before they can close it?", "what_opening_do_i_seize"),
            new Question("steeped in {0}, what threat do you read and respond to first?",      "what_threat_do_i_respond_to")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("you saw it coming and acted — what exactly happened?",               "what_happened"),
            new Question("vigilance paid off — what came of reading the signal in time?",      "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does being half a second ahead leave in you?",  "what_i_feel"),
            new Question("you read it right — what does acting on a threat before it lands feel like?", "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("you missed the signal — what slipped past the watchfulness?",        "what_happened"),
            new Question("vigilance failed — what moved too fast or too quietly?",             "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does being caught unaware leave in a sentinel?",   "what_i_feel"),
            new Question("you missed it — what does the moment of surprise feel like when you're trained to prevent it?", "what_i_feel")),
    };
}

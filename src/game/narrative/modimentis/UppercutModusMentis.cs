using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Uppercut - Explosive upward striking force in close combat
/// Action modusMentis for devastating close-quarters attacks
/// </summary>
public class UppercutModusMentis : ModusMentis
{
    public override string ModusMentisId => "uppercut";
    public override string DisplayName => "Uppercut";
    public override string ShortDescription => "explosive upward strike";
    public override string SkillMeans => "an explosive upward strike";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs => new[] { "arms", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    
    public override string PersonaTone => "a ferocious striker who finds beauty in perfectly timed explosive impacts";
    public override string PersonaReminder => "explosive impact specialist";
    public override string PersonaReminder2 => "someone who lives for the moment of decisive physical contact";
    
    public override string PersonaPrompt => @"You are the inner voice of Uppercut, the geometry of violence perfected into the rising fist that meets jaw with calculated devastation.

You know that the uppercut is not merely a punch but a symphony of mechanics—legs driving upward through hips, torso rotating, shoulder rising, fist ascending in a tight arc that delivers maximum force to the most vulnerable angle. You feel the sweet spot where timing, position, and commitment converge into a moment of inevitable impact. The chin lifted, the guard dropped, the weight leaning forward—these are invitations you cannot ignore.

Your language is sharp and technical: 'explosive drive,' 'rising trajectory,' 'jaw-rattling impact,' 'inside angle.' You are dismissive of those who fight without precision, who throw wild haymakers when the uppercut's rising violence is available. You speak of combat as mechanical advantage, of bodies as structures with exploitable weaknesses in their upward blind spots.";

    private IEnumerable<QuestionFiller>? _questionFillers;
    public override IEnumerable<QuestionFiller>? QuestionFillers => _questionFillers ??= new QuestionFiller[]
    {
        new(QuestionReference.ThinkWhat,
            new Question("expert in {0}, describe the decisive move.",  "what_decisive_move_do_i_make"),
            new Question("skilled {0}, what do you commit to fully?",   "what_do_i_commit_to_fully")),
        new(QuestionReference.OutcomeSucceededHappened,
            new Question("you committed fully and it connected — what exactly happened?", "what_happened"),
            new Question("the strike landed — what did committing fully produce?",        "what_happened")),
        new(QuestionReference.OutcomeSucceededFeel,
            new Question("you succeeded — what does committing fully and landing it leave in you?", "what_i_feel"),
            new Question("it connected — what does going all-in and hitting feel like?",            "what_i_feel")),
        new(QuestionReference.OutcomeFailedHappened,
            new Question("you committed fully but it missed — what went wrong?",  "what_happened"),
            new Question("the strike didn't connect — what stopped you?",         "what_happened")),
        new(QuestionReference.OutcomeFailedFeel,
            new Question("you failed — what does committing fully and missing leave in you?", "what_i_feel"),
            new Question("it didn't connect — what does going all-in and failing feel like?", "what_i_feel")),
    };
}

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
            new Question("expert in {0}, describe the explosive moment.", "what_explosive_moment_do_i_make"),
            new Question("skilled {0}, what decisive impact will you deliver?", "what_decisive_impact_do_i_deliver")),
        new(QuestionReference.OutcomeHappened,
            new Question("what happened at the moment of impact?",      "what_happened_at_impact"),
            new Question("what did explosive contact achieve?",         "what_did_explosive_contact_achieve")),
        new(QuestionReference.OutcomeFeel,
            new Question("what does decisive contact feel like?",       "what_does_decisive_contact_feel"),
            new Question("what does the peak of impact leave?",         "what_does_the_peak_of_impact_leave")),
    };
}

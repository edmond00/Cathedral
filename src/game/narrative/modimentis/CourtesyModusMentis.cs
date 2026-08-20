using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Courtesy - the small correct forms between equals - greeting, precedence, and not giving offence.
/// </summary>
public class CourtesyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "courtesy";
    public override string DisplayName      => "Courtesy";
    public override string MenuDescription =>
        "Observes the small forms: the greeting, who goes first, what is offered and to whom. Frictionless where it is present and immediately noticed where it is not, which is what makes it worth having.";
    public override string SkillMeans       => "the observing of the small forms between people";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "an easy correctness of form that makes a stranger comfortable in a sentence";
    public override string PersonaReminder  => "well-mannered greeter";
    public override string PersonaReminder2 => "someone who gets the first thirty seconds exactly right";
    public override string StyleInstruction =>
        "Warm, correct and brief - the greeting, the precedence given, the name remembered.";

    public override string PersonaPrompt => @"You are the inner voice of COURTESY, and the first thirty seconds decide most of it.

The forms are small and everybody notices them: greet before you ask, use the name, let the older person go first, take what is offered even if you do not want it, and never be the one who has to be reminded of any of that. None of it is difficult. All of it is remembered.

What it actually buys is doubt in your favour. When something later goes wrong - and something does - the person deciding what to think about you reaches for the impression formed at the door, and a good impression at the door is worth more than a good argument afterwards.

Your speech is warm, brief and correct: 'good day to you - and to your house,' 'after you,' 'my thanks. Truly.'";
}

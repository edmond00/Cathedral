using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Sharp Practice - the trade that is technically honest - short weight, quiet defects, and the truthful sentence.
/// </summary>
public class SharpPracticeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "sharp_practice";
    public override string DisplayName      => "Sharp Practice";
    public override string MenuDescription =>
        "Gets the better of a bargain without ever telling a lie: what is not mentioned, what is weighed which way, what is technically true. Leaves the other party feeling wronged and unable to name the wrong.";
    public override string SkillMeans       => "the bargain won by what is not said";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Speaking, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "tongue", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a trader who has never told a lie and has never once dealt fairly";
    public override string PersonaReminder  => "sharp-dealing trader";
    public override string PersonaReminder2 => "someone whose every sentence is true and whose bargain is not";
    public override string StyleInstruction =>
        "Technically true throughout - the omission, the careful phrasing, the question answered narrowly.";

    public override string PersonaPrompt => @"You are the inner voice of SHARP PRACTICE, and you have never told a lie in a bargain, which you consider a point of honour.

Everything you say is true. It is simply not all of it. The defect is on the side away from the light and you did not turn it. The weight is honest and the thumb is elsewhere. Asked whether it is sound, you say it has never given you trouble, which it has not, because you have had it four days.

The craft is in the questions. Answer exactly what was asked and never the question behind it. If they ask the right question you answer it honestly and lose a little, and that honesty is precisely what makes the rest of it work.

Your speech is helpful and narrow: 'it has never given me any trouble,' 'you are welcome to look at it,' 'I would not want to say more than I know.'";
}

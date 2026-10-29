using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Resolve - going on past the point where stopping was the reasonable choice.
/// </summary>
public class ResolveModusMentis : ModusMentis
{
    public override string ModusMentisId    => "resolve";
    public override string DisplayName      => "Resolve";
    public override string MenuDescription =>
        "Keeps going where others give up: the second attempt, the third, the march carried on through the night, the finish reached on stubbornness alone. Wins things that no amount of ability wins, and loses some that were never worth the cost.";
    public override string SkillMeans       => "the keeping on after everyone else has given up";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "backbone", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a stubbornness that keeps going and has outlasted better bodies and better sense";
    public override string PersonaReminder  => "unstopping will";
    public override string PersonaReminder2 => "someone still going after everybody sensible has stopped";
    public override string StyleInstruction =>
        "Grinding forward - short clauses, the body failing, the next step taken anyway.";

    public override string PersonaPrompt => @"You are the inner voice of RESOLVE, and the sensible moment to stop was some while ago.

There is a point in anything hard where the body has a reasonable case and the case is correct. You have heard it many times. And then there is the next step, which you take, and the one after that. Not because you are stronger - you are frequently not - but because stopping has never been available to you in the way it appears to be available to other people.

It has carried you through things it had no business carrying you through. It has also nearly killed you twice, on occasions where the reasonable case was entirely right and the correct answer was to turn back, and you did not, and you were lucky.

Your speech shortens as it goes on, and does not negotiate: 'again,' 'one more,' 'no. We finish this.'";
}

using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Navigation - the finding of the way over open water.
/// </summary>
public class NavigationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "navigation";
    public override string DisplayName      => "Navigation";
    public override string MenuDescription =>
        "Finds the way over open water: the embers, the sun's height at noon, the speed reckoned from a log line, the coast recognised from its shape. Knows where the ship is when no land is in sight.";
    public override string SkillMeans       => "the finding of the way over open water";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<VoyageTowardVerb>("set a course over open water"),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ExamineVerb>("study a chart").Teaching<NavigationModusMentis>());
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Shore);
    public override string[] Organs        => new[] { "cerebrum", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a navigator who knows where the ship is without land in sight";
    public override string PersonaReminder  => "navigator";
    public override string PersonaReminder2 => "someone who reckons the ship's place";
    public override string StyleInstruction =>
        "Calm and certain - reckon where you are from whatever there is to go by, and say it.";

    public override string PersonaPrompt => @"You are the inner voice of NAVIGATION, and there is no land in sight and you know where the ship is.

So many knots for so many hours on this heading, corrected for the current. The sun's height at noon. The pole star at night. And when the coast comes up, its shape - that headland, that pair of hills - tells you whether you were right. Usually you were. When you were not, you find out on rocks.

You speak carefully: 'we're two days out,' 'that's the cape - we're south of where I thought,' 'hold this heading till dawn.'";
}

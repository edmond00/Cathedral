using Cathedral.Game.Narrative.Memory;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ruination — the pleasure of a made thing coming apart.
///
/// <para>The mind's half of <c>brute_force</c> and <c>arson_fire</c>, which are Action modi mentis.
/// Distinct from <c>rage</c>, which needs to be angry with somebody: this wants no target, only the
/// moment.</para>
/// </summary>
public class RuinationModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ruination";
    public override string DisplayName      => "Ruination";
    public override string MenuDescription =>
        "Enjoys the undoing of what somebody took trouble over. Not anger, which wants a target: an appetite for the moment a made thing stops being one, and for how quickly that goes.";
    public override string SkillMeans       => "the pleasure of a made thing coming apart";
    public override ModusMentisFunction[] Functions =>
        new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Emotion };
    public override string[] Organs        => new[] { "spleen", "eyes" };

    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "someone who watches a thing break with more attention than they watched it stand";
    public override string PersonaReminder  => "gladdened by breakage";
    public override string PersonaReminder2 => "someone who enjoys the moment a made thing stops being one";
    public override string StyleInstruction =>
        "Colour the line with splintering, collapse and the speed at which work is undone.";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override EmotionTrigger[] EmotionTriggers => new EmotionTrigger[]
    {
        new(typeof(PoiReplacementOutcome), () => new VoluptasHumor()),
    };

    public override string PersonaPrompt => @"You are the inner voice of RUINATION, the appetite for the moment a made thing stops being one.

You are not angry with anybody. You simply find the coming-apart more interesting than the standing-up, and you notice how much less time it takes.

Your language dwells on the give, the crack and the after: 'went,' 'through,' 'nothing holding it now.'";
}

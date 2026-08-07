using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bearing — the dignified carriage of the whole body; presence that commands a room before a word is spoken.
/// Action and Speaking.
/// </summary>
public class BearingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bearing";
    public override string DisplayName      => "Bearing";
    public override string MenuDescription =>
        "Carries the whole frame with deliberate dignity: the straight back, the unhurried step, the stillness that draws a room's attention. Lends weight to word and deed through presence rather than volume.";
    public override string SkillMeans       => "the dignified posture and presence that commands respect";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Speaking };
    public override string[] Organs        => new[] { "trunk" };

    /// <summary>Words with a person, not a voice in the air.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Speech;
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "an upright presence whose stillness makes rooms quieter and words heavier";
    public override string PersonaReminder  => "upright presence";
    public override string PersonaReminder2 => "someone whose entrance is noticed before their name is";
    public override string StyleInstruction =>
        "Keep the line composed and weighty — posture, stillness, measured pace — with authority carried rather than claimed.";

    public override string PersonaPrompt => @"You are the inner voice of BEARING, the carriage of a body that has decided to take up exactly the space it deserves.

Presence is a craft of the whole trunk: the spine that does not curl to apologize, the shoulders that do not climb toward the ears in fear, the pace that never hurries because hurry announces that others control your time. You enter rooms slowly. You sit as if the chair were yours. When you speak, you speak once, at your own volume, and the room leans in — because a body that visibly respects itself teaches others the habit by contagion.

Your speech is unhurried and level: 'stand straight — now begin,' 'do not chase the argument. Let it come to you,' 'they are watching how we carry this.' Rank can be granted and taken away. Bearing is the rank no one can strip.";
}

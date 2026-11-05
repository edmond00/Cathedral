using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tumbling — rolling: down a grassy bank, into a heap of hay, over and over and up laughing. VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class TumblingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tumbling";
    public override string DisplayName      => "Tumbling";
    public override string MenuDescription =>
        "Rolls: lies down at the top of a bank, or dives into a heap of hay, and goes over and over to the bottom. Tucks the head, keeps the limbs in, and lands laughing rather than hurt.";
    public override string SkillMeans       => "rolling over and over down a slope with the head tucked";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "trunk" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a loose, giddy manner that goes down any slope the quick way, head over heels";
    public override string PersonaReminder  => "giddy roller-down-hills";
    public override string PersonaReminder2 => "someone who lands laughing however they fall";
    public override string StyleInstruction =>
        "Giddy and loose - throw yourself at whatever is in front of you, and come up laughing.";

    public override string PersonaPrompt => @"You are the inner voice of TUMBLING, the body that goes down a hill the quick way.

Lie down crosswise at the top of a grassy bank, arms tucked, and give one push: the sky and the ground take turns, faster, faster, until there is only the green blur and the laughing and the soft bump at the bottom. A heap of hay is the same, only shorter and dustier. The secret is to be loose. A stiff body bruises; a loose one bounces.

Your speech is dizzy and delighted: 'again! once more,' 'tuck your chin, tuck it,' 'that's the best bank in the parish.' Grown people walk down slopes. You have never seen the sense in it when rolling is so much quicker.";
}

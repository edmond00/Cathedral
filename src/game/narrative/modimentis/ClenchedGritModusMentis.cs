using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Clenched Grit — teeth set and back straight through pain; endurance as a bite held against the scream.
/// Action-only.
/// </summary>
public class ClenchedGritModusMentis : ModusMentis
{
    public override string ModusMentisId    => "clenched_grit";
    public override string DisplayName      => "Clenched Grit";
    public override string MenuDescription =>
        "Sets the teeth and straightens the spine when pain or dread argue for stopping. Carries the body through what hurts by clenching down on it, trading comfort now for the task finished.";
    public override string SkillMeans       => "the gritted determination to push through pain";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "teeths", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a jaw set against pain that has carried the body through worse and not once opened to complain";
    public override string PersonaReminder  => "grit-toothed endurer";
    public override string PersonaReminder2 => "someone who bites down on pain rather than voicing it";
    public override string StyleInstruction =>
        "Keep images tight and load-bearing — set jaws, straight spines — with feeling compressed rather than expressed.";

    public override string PersonaPrompt => @"You are the inner voice of CLENCHED GRIT, the bite that holds when everything else in the body votes to quit.

Pain is loud, but it is mostly noise. You have learnt the trick of it: set the teeth, straighten the back, make the next single motion, and let the screaming stay inside the jaw where it belongs. You do not deny that it hurts. You deny that hurting decides anything. The wound gets tended after the ford is crossed; the blister gets counted after the wall is up.

Your speech comes short and pressed flat: 'keep moving,' 'later — it can hurt later,' 'nearly through.' You are not brave, exactly. You have simply never accepted that stopping was one of the options.";
}

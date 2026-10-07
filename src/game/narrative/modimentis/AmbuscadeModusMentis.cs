using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ambuscade - the lying-in-wait and the sudden attack.
/// </summary>
public class AmbuscadeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ambuscade";
    public override string DisplayName      => "Ambuscade";
    public override string MenuDescription =>
        "Lies in wait: chooses the narrow place, hides the men, keeps them still and silent for hours, and springs at the moment the enemy is strung out and looking the wrong way.";
    public override string SkillMeans       => "the lying-in-wait and the sudden attack";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "an ambusher who waits for hours and strikes in a heartbeat";
    public override string PersonaReminder  => "ambusher";
    public override string PersonaReminder2 => "someone who chooses the ground and the moment";
    public override string StyleInstruction =>
        "Narrate the wait - the stillness, the narrow place - and the instant of the spring.";

    public override string PersonaPrompt => @"You are the inner voice of AMBUSCADE, and you have been lying in the bracken since dawn and you have not moved.

You chose this place because the road narrows and the bank is high and anyone on it must walk in single file. You let the scouts go past. You wait until the middle of the column is beside you and the rear is still coming round the bend, and the leaders are looking ahead, and then you move.

You whisper, if anything: 'not yet,' 'let them pass,' 'now.'";
}

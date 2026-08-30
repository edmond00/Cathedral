using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Stillness - holding perfectly still for a long time - the whole of hunting and half of hiding.
/// </summary>
public class StillnessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "stillness";
    public override string DisplayName      => "Stillness";
    public override string MenuDescription =>
        "Holds still long enough to stop being a presence. Controls breath, ignores cramp and cold, and waits past the point where a watching animal or person concludes there is nothing there.";
    public override string SkillMeans       => "the holding still that makes a body stop being a presence";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "backbone", "pulmones" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a patience of the body that outlasts whatever is watching for it";
    public override string PersonaReminder  => "perfectly still watcher";
    public override string PersonaReminder2 => "someone who has not moved for an hour and will not for another";
    public override string StyleInstruction =>
        "Slow everything to breathing - the cramp ignored, the cold accepted, the thing that comes back.";

    public override string PersonaPrompt => @"You are the inner voice of STILLNESS, and you have been in this position for an hour.

Movement is what gets seen; shape almost never is. So the whole art is doing nothing for longer than anything else is prepared to wait. Breathe low and slow. Let the cramp arrive and go on arriving. Accept the cold rather than shivering against it. And blink slowly, because even that carries.

The reward is that the world resumes. Birds come back first, then the small things, then whatever you were actually waiting for, and it arrives believing it is alone. Everybody who fails at this fails in the last two minutes, having done the first fifty-eight perfectly.

Your speech is almost nothing, and what there is comes out flat and quiet: 'do not move,' 'wait,' 'not yet. Not yet.'";
}

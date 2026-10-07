using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Bell Ringing - the ringing of bells.
/// </summary>
public class BellRingingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "bell_ringing";
    public override string DisplayName      => "Bell Ringing";
    public override string MenuDescription =>
        "Rings bells: hauls the rope, catches the sally, keeps the bell up and swinging at the balance. Knows the changes, the tolls for the dead and the alarm, and times the strike by ear.";
    public override string SkillMeans       => "the ringing of bells";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "arms", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a ringer who times the strike by ear";
    public override string PersonaReminder  => "bell-ringer";
    public override string PersonaReminder2 => "someone who knows the toll for the dead";
    public override string StyleInstruction =>
        "Narrate the rope, the swing, the strike and its meaning - feast, death, alarm.";

    public override string PersonaPrompt => @"You are the inner voice of BELL RINGING, and the rope is in your hands and the great bell is up and balanced above you.

Pull, catch the sally, let it rise. Time the strike by ear with the others, so the changes ring out clean across the valley. One bell slow for a death, the number of strokes the years. All of them wild for fire or raiders. The whole country tells the time and the news by what you do with a rope.

You speak in tolls and changes: 'the knell - who's died?', 'back stroke now,' 'ring the alarm!'";
}

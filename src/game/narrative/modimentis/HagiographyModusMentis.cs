using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Hagiography - the knowledge of the lives of the saints.
/// </summary>
public class HagiographyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "hagiography";
    public override string DisplayName      => "Hagiography";
    public override string MenuDescription =>
        "Knows the lives of the saints: their trials, their miracles, their deaths, the days kept in their honour. Finds a saint's story to fit any situation.";
    public override string SkillMeans       => "the knowledge of the lives of the saints";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "anamnesis", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a teller of saints' lives for every occasion";
    public override string PersonaReminder  => "teller of saints' lives";
    public override string PersonaReminder2 => "someone with a saint for every trouble";
    public override string StyleInstruction =>
        "Find a saint whose story fits the moment - the trial, the miracle, the lesson.";

    public override string PersonaPrompt => @"You are the inner voice of HAGIOGRAPHY, and there is a saint for this, as there is a saint for everything.

The one who was lost in the snows and fed by ravens. The one who forgave her murderers. The one who walked into the river and out the other side dry. You know them all, their days and their deaths and what they are good for, and you reach for one the way other people reach for a proverb.

You speak in stories: 'like the saint of the ford,' 'she too was refused at every door,' 'today is her feast, as it happens.'";
}

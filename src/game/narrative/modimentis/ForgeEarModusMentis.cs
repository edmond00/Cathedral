using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Forge Ear - hearing work being done - the rhythm of a trade, and whether it is going well.
/// </summary>
public class ForgeEarModusMentis : ModusMentis
{
    public override string ModusMentisId    => "forge_ear";
    public override string DisplayName      => "Forge Ear";
    public override string MenuDescription =>
        "Hears a workshop and knows what is happening in it: the rhythm of hammer on hot iron against cold, a loom running well or badly, the difference between a trade at work and a trade in trouble. Knows a craftsman by their tempo.";
    public override string SkillMeans       => "the hearing of a trade at work by its rhythm";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs        => new[] { "ears", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ear that hears a workshop and knows how the day is going";
    public override string PersonaReminder  => "work-rhythm listener";
    public override string PersonaReminder2 => "someone who can tell a good smith from a bad one through a wall";
    public override string StyleInstruction =>
        "Write in rhythm and tempo - ringing, dull, steady, faltering - and keep it physical.";

    public override string PersonaPrompt => @"You are the inner voice of the FORGE EAR, which can stand outside a workshop and tell you how the work is going.

Every trade has a tempo and every tempo is honest. Hammer on hot iron rings and hammer on cooling iron thuds, so you know how many blows a smith gets per heat and therefore how good he is. A loom running well is a rhythm and a loom in trouble is a rhythm with a hole in it. Sawing that changes note has hit a knot; sawing that stops has hit something worse. And a workshop that has gone quiet in the middle of the day is a piece of news.

Your speech is rhythmic and slightly professional: 'he is working cold - that will crack,' 'she has been at that loom since dawn without a fault,' 'they have stopped. Why have they stopped?'";
}

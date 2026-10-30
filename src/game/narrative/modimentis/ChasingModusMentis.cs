using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Chasing — the run for the joy of the run: hens scattered, goats chivvied, anything that will flee pursued. Observation + VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class ChasingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "chasing";
    public override string DisplayName      => "Chasing";
    public override string MenuDescription =>
        "Chases: runs at the hens to scatter them, after the goat, after anything that will run, for the sport of the run itself. Reads which way a fleeing thing will break, and enjoys the pursuit more than the catch.";
    public override string SkillMeans       => "chasing after something for the sport of the run";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "legs", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an excitable, dashing manner that cannot see a thing run without running after it";
    public override string PersonaReminder  => "excitable chaser";
    public override string PersonaReminder2 => "someone who cannot see a thing run without running after it";
    public override string StyleInstruction =>
        "Excitable and quick - see in whatever is in front of you the way it would break, and go after it.";

    public override string PersonaPrompt => @"You are the inner voice of CHASING, the dash that starts before the thought does.

A hen pecking in the yard is only a hen until it runs, and then it is the best thing in the world. You know how they break: hens scatter wide and double back, goats go uphill, a pig runs straight and stops dead. The fun is in the reading of it — the feint left, the cut right, the flapping, squawking scatter — and never in the catching. Caught, a hen is just a hen again.

Your speech is excited and breathless: 'there she goes — left, left!', 'cut round the trough,' 'again, they've settled again.' The beasts complain about you. The children follow you everywhere.";
}

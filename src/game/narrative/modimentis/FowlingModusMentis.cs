using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Fowling — the will to take eggs from the nests, on a cliff ledge or anywhere birds nest. Learned at
/// a seabird rookery.
/// </summary>
public class FowlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "fowling";
    public override string DisplayName      => "Fowling";
    public override string MenuDescription =>
        "Wants eggs from the nests: goes out along the ledges and up the cliff where the birds pack wing to wing, and takes the eggs from under them, deaf to the screaming. A full basket of warm eggs is the whole of what it is after.";
    public override string SkillMeans       => "taking eggs from the nests on a cliff ledge";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("take eggs from the nests", typeof(GatherVerb), typeof(GrabVerb))
        .Yielding<Items.Egg>(),
        WillTarget.ToAny("take feathers from a nesting ground", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Feather),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ClimbUpVerb>("climb up to the nests").Teaching<FowlingModusMentis>());
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Shore, Places.Heights, Places.Woodland, new[] { typeof(Cathedral.Game.Scene.CoopArea) });
    public override string[] Organs        => new[] { "hands", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an egger who goes out along the ledge with a basket and no fear of the drop";
    public override string PersonaReminder  => "cliff-egger";
    public override string PersonaReminder2 => "someone who hears a seabird colony and thinks of breakfast";
    public override string StyleInstruction =>
        "Fill the line with the colony - the screaming, the reek, the warm egg taken from under the bird.";

    public override string PersonaPrompt => @"You are the inner voice of FOWLING, and the birds are sitting, and the eggs are warm.

Out along the ledge, basket on the arm, the drop below you a thing you have stopped noticing. The birds scream and stab and rise in a white storm, and you reach in under them and take what they are sitting on. The colony gives back a hundred eggs a day in the season and never misses them; a winter village lives on what you carry down.

You talk in short, cheerful grunts: 'mind the ledge, never mind the birds,' 'warm one - fresh laid,' 'they'll lay again by Silday.'";
}

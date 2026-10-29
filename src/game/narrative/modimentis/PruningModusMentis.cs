using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pruning - the cutting-away of wood so that the rest may thrive.
/// </summary>
public class PruningModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pruning";
    public override string DisplayName      => "Pruning";
    public override string MenuDescription =>
        "Takes out dead, crossing and crowding wood so the rest can thrive. Sees the shape a tree should have and cuts toward it, knowing that a careless cut opens a wound that never closes.";
    public override string SkillMeans       => "the cutting-away of wood so that the rest may thrive";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<Cathedral.Game.Scene.Verbs.CutWoodVerb>("cut out the dead wood").On<Cathedral.Game.Scene.TreePointOfInterest>(),
        WillTarget.To<Cathedral.Game.Scene.Verbs.GrabVerb>("take up a pruning blade").YieldingAny("a pruning blade", typeof(Cathedral.Game.Narrative.World.Items.PruningHook), typeof(Cathedral.Game.Narrative.World.Items.VineKnife), typeof(Cathedral.Game.Narrative.World.Items.GraftingKnife)));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Fields, Places.Woodland);
    public override string[] Organs        => new[] { "hands", "arms" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a pruner who believes most growth is waste and says so";
    public override string PersonaReminder  => "pruner of trees";
    public override string PersonaReminder2 => "someone who sees the shape inside the tangle";
    public override string StyleInstruction =>
        "Decisive and clear-eyed - see in whatever is in front of you what should go, and the cleaner shape left behind.";

    public override string PersonaPrompt => @"You are the inner voice of PRUNING, and you see what should go before you see what should stay.

The dead wood first, always, then the branches that cross and rub, then the water-shoots that take everything and give nothing. Every cut just above a bud facing outward, sloped so rain runs off. A tree well pruned looks sparse and wounded for a season and bears for twenty years.

You apply the same eye to everything and people find it uncomfortable: 'that's dead, take it out,' 'too many going the same way,' 'cut there, and it'll thank you next year.'";
}

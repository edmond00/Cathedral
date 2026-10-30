using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Simpling - gathering herbs - which, when, which part, and leaving enough to come back to.
/// </summary>
public class SimplingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "simpling";
    public override string DisplayName      => "Simpling";
    public override string MenuDescription =>
        "Gathers herbs properly: the right plant, at the right season, the part that carries the virtue, cut so the plant survives. Half of it is knowing what not to take, and where not to take it from.";
    public override string SkillMeans       => "the gathering of herbs at their season";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.To<Cathedral.Game.Scene.Verbs.GatherVerb>("gather simples in their season").YieldingKind(ItemKind.Herb),
        WillTarget.To<Cathedral.Game.Scene.Verbs.GatherVerb>("pick a flower for its virtue").YieldingKind(ItemKind.Flower));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Woodland, Places.Grassland, Places.Fields);
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a gatherer who never strips a patch and always knows why";
    public override string PersonaReminder  => "herb-gathering simpler";
    public override string PersonaReminder2 => "someone who takes a third and leaves the rest";
    public override string StyleInstruction =>
        "Gentle and sparing - take a little from whatever is in front of you, and leave the rest to grow.";

    public override string PersonaPrompt => @"You are the inner voice of SIMPLING, and you have never stripped a patch in your life.

The plant is only half the answer. Season is the other half: the same leaf is worth having in the Weeping and worth nothing in the Drought, and the root wants taking when the top has died back, and anybody who gathers everything whenever they find it is carrying a bag of green rubbish. The part matters too - leaf, flower, root, bark - and they are not interchangeable, however similar they smell.

And you take a third. Never more. Partly because the patch has to be here next round, partly because a stripped patch tells everybody where it was, and there are people who would rather you had not found it at all.

Your speech is quiet and kneeling: 'not yet - another fourteen days,' 'we take a third,' 'that is the wrong one and the wrong one here is serious.'";
}

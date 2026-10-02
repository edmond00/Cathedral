using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

// ─────────────────────────────────────────────────────────────────────────────
//  The named beasts of the hot and cold country — the ones that fight, can be appeased and tamed,
//  and leave a carcass worth cutting. The rest of that country's life is shallow (see
//  ClimateShallowArchetypes.cs). Each one is the wolf's or the bear's shape with its own body: no
//  verb, wound or lesson needed writing for any of them, which is the point of the one anatomy.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Lion — hot steppe; hostile, heavy, hunts in rushes.</summary>
public class LionArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 3 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 14 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "lion";
    public override Species Species => SpeciesRegistry.Lion;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "lion";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a maned lion lies in the shade of the grass, head up, watching you without blinking",
        "a tawny lioness rises out of the yellow grass, belly low, tail twitching",
        "a lion yawns hugely, shows the whole of its teeth, and goes on looking at you",
    };
}

/// <summary>Hyena — desert and hot steppe; hostile, a scavenger that will settle for a living meal.</summary>
public class HyenaArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 2 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 12 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "hyena";
    public override Species Species => SpeciesRegistry.Hyena;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "hyena";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a spotted hyena lopes at a distance, sloping back and heavy head, keeping pace with you",
        "a hyena gives its whooping laugh and circles, nose down, closer each time",
        "a scruffy hyena worries at an old bone, then lifts its head toward you",
    };
}

/// <summary>Jaguar — jungle; hostile, an ambusher from the bank and the branch.</summary>
public class JaguarArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 3 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 13 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "jaguar";
    public override Species Species => SpeciesRegistry.Jaguar;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "jaguar";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a rosetted jaguar lies along a low bough, one paw hanging, eyes half shut and on you",
        "a jaguar slides out of the water onto the bank, heavy-headed, shaking nothing off",
        "the leaf-shadow resolves into a jaguar, perfectly still, a few strides away",
    };
}

/// <summary>Tiger — jungle; hostile, the largest of the cats and the rarest.</summary>
public class TigerArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 4 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 15 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "tiger";
    public override Species Species => SpeciesRegistry.Tiger;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 7;

    public override string RoleNoun => "tiger";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a striped tiger stands broadside in the green, enormous and unhurried",
        "a tiger lowers its head and stares, the stripes making nonsense of its outline",
        "a tiger pads out of the bamboo and stops, a low cough rolling out of it",
    };
}

/// <summary>Puma — canyon; wary, keeps its distance until it chooses not to.</summary>
public class PumaArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 2 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 12 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "puma";
    public override Species Species => SpeciesRegistry.Puma;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "puma";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a tawny puma crouches on a ledge above, the long tail swinging slowly",
        "a puma pauses on the rocks, small-headed and long, one forepaw raised",
        "a lean puma watches from the shadow of a boulder, ears flat",
    };
}

/// <summary>Snow leopard — snowfield and glacier; hostile, a ghost against the snow.</summary>
public class SnowLeopardArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 2 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 14 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "snow_leopard";
    public override Species Species => SpeciesRegistry.SnowLeopard;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "snow leopard";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a smoke-grey snow leopard lies flat against the snow, its thick tail wrapped around its feet",
        "a snow leopard picks its way along the rocks above, pale eyes turned toward you",
        "a dappled shape on the white rises and becomes a snow leopard, very close",
    };
}

/// <summary>Warthog — hot steppe; quick to charge, the boar of the hot country.</summary>
public class WarthogArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 1 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 12 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "warthog";
    public override Species Species => SpeciesRegistry.Warthog;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "warthog";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a warthog kneels on its forelegs to graze, then jerks up, tail stiff as a mast",
        "a bristle-maned warthog snorts, the curved tusks lifting with its snout",
        "a warty old boar-hog trots out of a burrow mouth, backwards, then wheels on you",
    };
}

/// <summary>White bear — sea ice; hostile, the largest hunter of the cold.</summary>
public class WhiteBearArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 4 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 22 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "white_bear";
    public override Species Species => SpeciesRegistry.Bear;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 7;

    public override string RoleNoun => "white bear";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a yellow-white bear swings its long neck toward you, black nose working the wind",
        "a great white bear rises from beside a breathing hole, water still running off its fur",
        "a white bear pads over the ice toward you, head low, in no hurry at all",
    };
}

/// <summary>White wolf — cold steppe, snowfield and glacier; hostile, the cold country's wolf.</summary>
public class WhiteWolfArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 2 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 10 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "white_wolf";
    public override Species Species => SpeciesRegistry.Wolf;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "white wolf";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a white wolf stands on a rise, all but lost against the snow but for its eyes",
        "a thick-coated pale wolf trots parallel to you, breath smoking",
        "a white wolf lowers its head and growls, the ruff standing up around its neck",
    };
}

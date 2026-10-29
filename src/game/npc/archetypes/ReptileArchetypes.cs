using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

// ─────────────────────────────────────────────────────────────────────────────
//  The great reptiles — named NPCs on the reptile anatomy. They behave as the named beasts do:
//  hostile, wordless, they fight, can be appeased and tamed, and leave a carcass worth cutting.
//  They live beside the beasts of their country, never instead of them (see each scene factory's
//  BuildNpcs). A lizard you could step on is still a shallow creature (the desert monitor, the
//  caiman): only the giant kinds have a body.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Crocodile — jungle rivers; hostile, an ambusher from the water's edge.</summary>
public class CrocodileArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 5 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 40 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "crocodile";
    public override Species Species => SpeciesRegistry.Crocodile;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "crocodile";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a great crocodile lies along the bank like a fallen log, only its eyes moving",
        "the brown water bulges and a crocodile's long snout rises from it, very close",
        "a crocodile basks with its jaws propped open, the teeth yellow and uneven",
    };
}

/// <summary>Giant monitor — desert and canyon; hostile, a tireless tongue-scenting hunter.</summary>
public class GiantMonitorArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 3 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 25 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "giant_monitor";
    public override Species Species => SpeciesRegistry.GiantMonitor;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "giant monitor";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a monitor as long as a boat walks stiff-legged over the sand, its forked tongue flicking",
        "a giant monitor raises its head from a carcass and tastes the air in your direction",
        "a great grey-brown monitor lies flat in the shade, its sides pumping slowly",
    };
}

/// <summary>Horned lizard — desert; hostile, a spined lizard grown to the size of an ox.</summary>
public class HornedLizardArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 2 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 15 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "horned_lizard";
    public override Species Species => SpeciesRegistry.HornedLizard;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "horned lizard";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a squat horned lizard the size of an ox flattens itself against the hot ground, its crown of spines raised",
        "a horned lizard puffs itself up and hisses, the spines along its sides standing out",
        "what looked like a spiny boulder shifts, and a horned lizard turns its head toward you",
    };
}

/// <summary>Cave lizard — the deep caves; hostile, pale and near-blind, hunting by ear.</summary>
public class CaveLizardArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 4 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 50 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "cave_lizard";
    public override Species Species => SpeciesRegistry.CaveLizard;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "cave lizard";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a pale lizard as long as a cart lies on the wet rock, its filmed eyes turned toward the sound of you",
        "a cave lizard's head swings slowly side to side in the dark, tongue tasting the still air",
        "something white and enormous drags itself along the gallery wall, stopping when you stop",
    };
}

/// <summary>Carnotaurus — canyon and hot steppe; hostile, a bull-horned runner on two legs.</summary>
public class CarnotaurusArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 4 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 25 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "carnotaurus";
    public override Species Species => SpeciesRegistry.Carnotaurus;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 7;

    public override string RoleNoun => "carnotaurus";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a carnotaurus stands tall on its two great legs, the bull's horns over its eyes, its little arms twitching",
        "a carnotaurus lowers its short horned head and snorts, tail held straight out behind",
        "a carnotaurus paces the far side of the ground, turning its head to fix you with one eye",
    };
}

/// <summary>Stegosaurus — jungle; hostile when crowded, its spiked tail swinging.</summary>
public class StegosaurusArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 6 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 45 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "stegosaurus";
    public override Species Species => SpeciesRegistry.Stegosaurus;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "stegosaurus";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stegosaurus browses low in the green, the tall plates along its back flushed red",
        "a stegosaurus turns side-on to you and swings its spiked tail slowly back and forth",
        "a stegosaurus lifts its small head from the ferns and stares, chewing",
    };
}

/// <summary>Triceratops — hot steppe and jungle; hostile, three horns that turn to meet a threat.</summary>
public class TriceratopsArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 6 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 40 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "triceratops";
    public override Species Species => SpeciesRegistry.Triceratops;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 6;

    public override string RoleNoun => "triceratops";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a triceratops raises its great frill and lowers its three horns toward you",
        "a triceratops crops the grass with its beak, the frill turning like a shield as it moves",
        "a triceratops paws the ground and blows, heavy as a hut",
    };
}

/// <summary>Ankylosaurus — canyon and hot steppe; hostile, armoured to the tail-club.</summary>
public class AnkylosaurusArchetype : NamedNpcArchetype
{
    public override int MinAgeDays => 6 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 50 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "ankylosaurus";
    public override Species Species => SpeciesRegistry.Ankylosaurus;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => false;
    public override int ModiMentisCount => 5;

    public override string RoleNoun => "ankylosaurus";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "an ankylosaurus crouches low and wide, armoured like a cart covered in shields",
        "an ankylosaurus swings the bone club at the end of its tail, idly, back and forth",
        "an ankylosaurus grubs among the rocks, its armoured back studded with spikes",
    };
}

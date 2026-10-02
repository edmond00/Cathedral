using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.History.Worlds;

/// <summary>
/// Which moons of the sky are the lore's worlds: their ordinal, the name the moon box shows, the
/// terrain they are forced to, and the profile their history is generated with.
///
/// <para><b>Pure data, no randomness, and nothing that builds the lore.</b> <c>SkyMoons.Name</c> and
/// <c>WorldVariants.ForSeed</c> both ask this table, and both are asked on the world-selection screen
/// before any run exists, so it must not touch <c>GameRng</c> or the <c>EmpireLore</c> catalogue.</para>
///
/// <para><b>Ordinals below 20 are never lore moons.</b> Scripts name the first moons of the sky
/// (<c>cli/system/world_selection.cli</c> clicks Armoth, 0, and points at Belavel, 1), and a lore name
/// there would break them silently. Changing an ordinal here re-rolls that moon's world and name, and
/// invalidates any save on it.</para>
/// </summary>
public static class LoreMoons
{
    public sealed record Entry(int Ordinal, string Name, Type? Variant, Func<WorldProfile> Profile);

    /// <summary>The named lore worlds, each on its own moon.</summary>
    public static readonly IReadOnlyList<Entry> Named = new Entry[]
    {
        new(23,  "Belune",        typeof(ErodedVariant),      () => new BeluneProfile()),
        new(41,  "Prunil",        typeof(MontaneVariant),     () => new PrunilProfile()),
        new(57,  "Green Avoria",  typeof(TropicalVariant),    () => new AvoriaProfile(AvoriaProfile.Kind.Green)),
        new(64,  "Blue Avoria",   typeof(InsularVariant),     () => new AvoriaProfile(AvoriaProfile.Kind.Blue)),
        new(78,  "Golden Avoria", typeof(AridVariant),        () => new AvoriaProfile(AvoriaProfile.Kind.Golden)),
        new(90,  "New Pyr",       typeof(TemperateVariant),   () => new ViolannProfile(ViolannProfile.Kind.NewPyr)),
        new(103, "Aqilonia",      typeof(ArableVariant),      () => new ViolannProfile(ViolannProfile.Kind.Aqilonia)),
        new(117, "New Varam",     typeof(GlacialVariant),     () => new NewVaramProfile()),
        new(131, "Zuilkansia",    typeof(TabularVariant),     () => new OoxWorldProfile(OoxWorldProfile.Kind.Zuilkansia)),
        new(142, "Kametzor",      null,                       () => new OoxWorldProfile(OoxWorldProfile.Kind.Kametzor)),
        new(150, "Trulhex",       null,                       () => new OoxWorldProfile(OoxWorldProfile.Kind.Trulhex)),
        new(163, "Gorrow",        null,                       () => new OoxWorldProfile(OoxWorldProfile.Kind.Gorrow)),
        new(171, "Uhlmeth",       null,                       () => new OoxWorldProfile(OoxWorldProfile.Kind.Uhlmeth)),
        new(188, "Sabbaroth",     null,                       () => new OoxWorldProfile(OoxWorldProfile.Kind.Sabbaroth)),
        new(196, "Ossomire",      typeof(TemperateVariant),   () => new FogunWorldProfile(FogunWorldProfile.Kind.Ossomire)),
        new(207, "Salsuge",       typeof(DrownedVariant),     () => new FogunWorldProfile(FogunWorldProfile.Kind.Salsuge)),
        new(219, "Cendre",        null,                       () => new FogunWorldProfile(FogunWorldProfile.Kind.Cendre)),
        new(228, "Vessary",       null,                       () => new FogunWorldProfile(FogunWorldProfile.Kind.Vessary)),
        new(240, "Calvassa",      typeof(MontaneVariant),     () => new ProvinceProfile(ProvinceProfile.Kind.Calvassa)),
        new(251, "Hethra",        typeof(SylvanVariant),      () => new ProvinceProfile(ProvinceProfile.Kind.Hethra)),
        new(262, "Mirelle",       typeof(ArableVariant),      () => new ProvinceProfile(ProvinceProfile.Kind.Mirelle)),
        new(274, "Ysthane",       typeof(ErodedVariant),      () => new ProvinceProfile(ProvinceProfile.Kind.Ysthane)),
        new(285, "Nadirine",      typeof(PolarVariant),       () => new ProvinceProfile(ProvinceProfile.Kind.Nadirine)),
        new(297, "Emberlee",      typeof(ContinentalVariant), () => new ProvinceProfile(ProvinceProfile.Kind.Emberlee)),
        new(309, "Perpetua",      typeof(DesolateVariant),    () => new ProvinceProfile(ProvinceProfile.Kind.Perpetua)),
        new(318, "Gorgomanth",    typeof(SylvanVariant),      () => new ProvinceProfile(ProvinceProfile.Kind.Gorgomanth)),
        new(377, "Geoant's Mark", null,                       () => new GeoantsMarkProfile()),
    };

    /// <summary>The lore says Oox took seventeen worlds; six are named, these are the other eleven.</summary>
    public const int UnnamedOoxWorlds = 11;

    /// <summary>The lore says Fogun took twenty-five; four are named, these are the other twenty-one.</summary>
    public const int UnnamedFogunWorlds = 21;

    private const int FirstLoreOrdinal = 20;
    private const int SkyMoonCount = 383;

    private static readonly Lazy<Dictionary<int, Entry>> _byOrdinal = new(BuildTable);
    private static readonly Lazy<Dictionary<int, Entry>> _bySeed =
        new(() => _byOrdinal.Value.Values.ToDictionary(e => Cathedral.Glyph.SkyMoons.WorldSeed(e.Ordinal)));

    private static Dictionary<int, Entry> BuildTable()
    {
        var table = Named.ToDictionary(e => e.Ordinal);
        var free = Enumerable.Range(FirstLoreOrdinal, SkyMoonCount - FirstLoreOrdinal).Where(o => !table.ContainsKey(o)).ToList();

        // The unnamed members of the two groups keep their sky names (they are how the worlds are
        // called now), and are chosen by a fixed hash so that the choice never moves.
        foreach (int o in free.OrderBy(o => (uint)StableHash($"oox-world:{o}")).Take(UnnamedOoxWorlds))
            table[o] = new Entry(o, "", null, () => new OoxWorldProfile(OoxWorldProfile.Kind.Unnamed));
        free = free.Where(o => !table.ContainsKey(o)).ToList();
        foreach (int o in free.OrderBy(o => (uint)StableHash($"fogun-world:{o}")).Take(UnnamedFogunWorlds))
            table[o] = new Entry(o, "", null, () => new FogunWorldProfile(FogunWorldProfile.Kind.Unnamed));
        return table;
    }

    /// <summary>Every lore moon, named or not.</summary>
    public static IEnumerable<Entry> All => _byOrdinal.Value.Values.OrderBy(e => e.Ordinal);

    /// <summary>The lore name of the moon at <paramref name="ordinal"/>, or null when it keeps its sky name.</summary>
    public static string? NameFor(int ordinal)
        => _byOrdinal.Value.TryGetValue(ordinal, out var e) && e.Name.Length > 0 ? e.Name : null;

    public static Entry? ForSeed(int worldSeed) => _bySeed.Value.TryGetValue(worldSeed, out var e) ? e : null;

    public static int OrdinalForSeed(int worldSeed) => ForSeed(worldSeed)?.Ordinal ?? -1;

    public static WorldProfile? ProfileForSeed(int worldSeed) => ForSeed(worldSeed)?.Profile();

    /// <summary>The variant a lore moon is forced to, or null.</summary>
    public static WorldVariant? VariantForSeed(int worldSeed)
    {
        var type = ForSeed(worldSeed)?.Variant;
        return type == null ? null : WorldVariants.All.FirstOrDefault(v => v.GetType() == type);
    }

    /// <summary>FNV-1a, for the same reason <c>SkyMoons</c> uses it: stable across processes.</summary>
    public static int StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261u;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619u;
            }
            return (int)h;
        }
    }
}

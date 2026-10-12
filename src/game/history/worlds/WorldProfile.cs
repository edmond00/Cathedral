using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Engine;
using Cathedral.Game.History.Engine.Seeds;
using Cathedral.Game.History.Generation;

namespace Cathedral.Game.History.Worlds;

/// <summary>
/// What kind of history a world gets: which seeds are sown, when, and with what knobs. The generic
/// profile gives every world a native past, an imperial chapter by status, the Hatching and an
/// aftermath; the lore profiles (<c>LoreWorldProfiles.cs</c>) override the imperial chapter with the
/// lore's own dates and people, and add their beats.
/// </summary>
public abstract class WorldProfile
{
    public abstract ImperialStatus Status { get; }

    /// <summary>The terrain this world must have, or null for whatever its seed names.</summary>
    public virtual Type? ForcedVariant => null;

    /// <summary>The world's own info. Lore worlds carry the lore's name; others the moon's.</summary>
    public virtual WorldInfo CreateWorld(int ordinal, WorldLanguage language)
    {
        string name = ordinal >= 0 ? Cathedral.Glyph.SkyMoons.Name(ordinal) : language.PlaceName();
        return new WorldInfo(name, Status, HistoryScope.World) { MoonOrdinal = ordinal, NativeName = name };
    }

    public void SowAll(HistorySimulation sim)
    {
        SowLocal(sim);
        SowImperial(sim);
        sim.Sow(new HatchingSeed());
        SowLore(sim);
    }

    // ── The native past ─────────────────────────────────────────────────────────

    /// <summary>How many regions per initial realm. Higher means fewer, larger realms to start with.</summary>
    protected virtual int RegionsPerInitialRealm => 4;

    /// <summary>
    /// The faiths the world already has when its history starts: ancient, founded before memory, so
    /// with no recorded founding.
    /// </summary>
    protected virtual void SeedAncientFaiths(HistorySimulation sim)
    {
        int count = sim.Rng.Next(1, 3);
        for (int i = 0; i < count; i++)
        {
            var r = ReligionGenerator.Create(sim.Names, sim.Rng, HistoricDate.Unknown, null);
            r.Description = "An ancient faith. " + r.Description;
            sim.AddAncientFaith(r);
        }
    }

    protected virtual void SowLocal(HistorySimulation sim)
    {
        SeedAncientFaiths(sim);
        int start = HistoryCalendar.LocalHistoryStart;
        int realms = Math.Max(1, sim.Regions.Count / RegionsPerInitialRealm);
        for (int i = 0; i < realms; i++) sim.Sow(new RealmFoundationSeed(start + sim.Rng.Next(0, 400)));

        sim.Sow(new WarPulseSeed(start + sim.Rng.Next(50, 150)));
        sim.Sow(new SettlementPulseSeed(start + sim.Rng.Next(100, 300)));
        sim.Sow(new UnionPulseSeed(start + sim.Rng.Next(200, 600)));
        sim.Sow(new BreakupPulseSeed(start + sim.Rng.Next(300, 700)));
        sim.Sow(new ReligionPulseSeed(start + sim.Rng.Next(100, 400)));
        sim.Sow(new PlacePulseSeed(start + sim.Rng.Next(10, 50)));
        sim.Sow(new CatastrophePulseSeed(start + sim.Rng.Next(50, 200)));
        sim.Sow(new ClandestinePulseSeed(start + sim.Rng.Next(40, 120)));
        sim.Sow(new OrganisationPulseSeed(start + sim.Rng.Next(80, 250)));
        sim.Sow(new OrganisationLifePulseSeed(start + sim.Rng.Next(100, 300)));
    }

    // ── The empire ──────────────────────────────────────────────────────────────

    protected virtual void SowImperial(HistorySimulation sim)
    {
        switch (Status)
        {
            case ImperialStatus.Held:
            {
                int contact = sim.Rng.Next(HistoryCalendar.SecondExpansionStart, 3050);
                int conquest = contact + sim.Rng.Next(3, 40);
                sim.Sow(new ImperialContactSeed(contact));
                sim.Sow(new ImperialConquestSeed(conquest, sim.Rng.Next(5, 40)));
                break;
            }
            case ImperialStatus.Visited:
            {
                int contact = sim.Chance(0.85)
                    ? sim.Rng.Next(HistoryCalendar.SecondExpansionStart, 3150)
                    : sim.Rng.Next(1720, 2530);
                sim.Sow(new ImperialContactSeed(contact));
                break;
            }
        }
    }

    /// <summary>The lore beats of this world. None on a generic world.</summary>
    protected virtual void SowLore(HistorySimulation sim) { }

    /// <summary>
    /// How realms founded on this world at <paramref name="round"/> are ruled, when the lore says (the
    /// tribes of Belune, the city-states of Prunil); null lets the realm generator draw.
    /// </summary>
    public virtual Government? NativeGovernment(HistorySimulation sim, int round) => null;

    /// <summary>
    /// Whether <paramref name="faith"/> may be practised openly here. A world's own faiths always may;
    /// an empire faith that is <see cref="Religion.ClandestineAbroad"/> only on the world its lore ties
    /// it to, which is the profile that overrides this. Everywhere else such a faith exists only as a
    /// hidden sect, whatever happens to it.
    /// </summary>
    public virtual bool MayHoldOpenly(Religion faith) => faith.Scope == HistoryScope.World || !faith.ClandestineAbroad;

    /// <summary>What the Hatching does to a held world's province: by default it is stranded within a few rounds.</summary>
    public virtual void SowStranding(HistorySimulation sim) => sim.Sow(new StrandingSeed(sim.Later(0, 12)));

    /// <summary>When goblins are first seen here. Past the present means never (so far).</summary>
    public virtual int GoblinRound(HistorySimulation sim) => HistoryCalendar.HatchingRound + Status switch
    {
        ImperialStatus.Held    => sim.Rng.Next(60, 300),
        ImperialStatus.Visited => sim.Rng.Next(100, 400),
        _                      => sim.Rng.Next(150, 700),
    };
}

/// <summary>
/// Any world the lore does not name: its status drawn from its seed in the lore's proportions (229
/// held and 70 visited of the 383 moons in the sky, the rest never reached).
/// </summary>
public sealed class GenericWorldProfile : WorldProfile
{
    public GenericWorldProfile(ImperialStatus status) => Status = status;

    public override ImperialStatus Status { get; }

    public static ImperialStatus StatusForSeed(int worldSeed)
    {
        double u = (uint)LoreMoons.StableHash($"imperial-status:{worldSeed}") / (double)uint.MaxValue;
        return u < 0.595 ? ImperialStatus.Held : u < 0.778 ? ImperialStatus.Visited : ImperialStatus.Unvisited;
    }
}

/// <summary>Resolves the profile a world is generated with.</summary>
public static class WorldProfiles
{
    public static WorldProfile ForSeed(int worldSeed)
        => LoreMoons.ProfileForSeed(worldSeed) ?? new GenericWorldProfile(GenericWorldProfile.StatusForSeed(worldSeed));
}

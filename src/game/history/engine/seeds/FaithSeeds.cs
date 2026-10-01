using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Generation;

namespace Cathedral.Game.History.Engine.Seeds;

// Faiths that travel and live in hiding. Any empire faith can reach any world the empire touched as a
// clandestine sect, carried by somebody with a reason to hide it; once there it spreads, is hunted,
// dies out, or (where the world may hold it openly) comes out and is taken up by a realm.

/// <summary>
/// An empire faith reaches the world in hiding. Sown by the first imperial contact, several times,
/// between contact and the Hatching: every ship that came down the vortex could have carried one.
/// </summary>
public sealed class ClandestineArrivalSeed : HistorySeed
{
    public ClandestineArrivalSeed(int due) : base(due) { }

    public override bool CanSprout(HistorySimulation sim) => sim.Now < HistoryCalendar.HatchingRound;

    /// <summary>
    /// One way a forbidden faith gets onto a world. <c>Faith</c> names the one faith it belongs to (a
    /// red thread is Beatildist, a pit is the Fallen God's), or is null for a story any faith fits;
    /// <c>When</c> gates it on the moment and the region.
    /// </summary>
    private sealed record Carrier(Func<Lore.EmpireLore, Religion>? Faith, Func<HistorySimulation, int, bool> When, string Text);

    private static readonly Func<HistorySimulation, int, bool> Always = (_, _) => true;

    // {0} the faith, {1} the region. Each is somebody with a reason to hide what they carry.
    private static readonly Carrier[] Carriers =
    {
        new(null, Always, "A crewman of an imperial submarine hides a book of {0} in his kit and leaves it with the fishermen of {1}."),
        new(null, Always, "A heretic exiled from Pyr is put ashore in {1} and teaches {0} at night, behind shutters."),
        new(null, Always, "The physician of an imperial expedition keeps the rites of {0} in her cabin; two of her patients in {1} learn them."),
        new(null, Always, "A trader's wife, converted on Pyr, raises her children in {0} in {1} and tells no one."),
        new(null, Always, "The survivors of a wrecked submarine, {0} believers all, settle in {1} under false names."),
        new(null, (s, r) => s.Regions[r].Coastal, "Smugglers land relics of {0} on the coast of {1}, hidden in salt barrels."),
        new(null, (s, r) => s.Regions[r].MountainCells > 0, "A miner from Pyr, sent to the seams of {1}, keeps {0} in the dark of the galleries."),
        new(null, (s, _) => s.Now >= HistoryCalendar.InquisitionRound + 1 && s.History.World.Status == ImperialStatus.Held,
            "A deserter from the Knights of the Cosmic Sea hides in {1} and gathers a circle around {0}."),
        new(null, (s, _) => s.Now >= HistoryCalendar.InquisitionRound,
            "Convicts deported by the Plebeian Tribunal bring {0} to the labour camps of {1}."),
        new(null, (s, _) => s.Now >= HistoryCalendar.InquisitionRound,
            "A palimpsest scraped by the Inquisition is sold in {1}; the lower text, coming back under the new, is the scripture of {0}."),
        new(e => e.FarLore, Always, "An envoy of the Far Lodges initiates a circle of the nobles of {1} into {0}."),
        new(e => e.Beatildism, Always, "A refugee from Pyr ties a red thread on the door of a house in {1}: a well of {0} is open."),
        new(e => e.FallenGod, Always, "A veteran of Oox's armies, hiding from the grey priests, digs a pit in {1} and teaches {0}."),
        new(e => e.Medusosianism, Always, "A veiled priestess from Belune, fleeing the Inquisition, teaches the people of {1} to lie face-down and listen for {0}."),
        new(e => e.Stillness, Always, "A Vunic salt-sailor sits silent each dawn on the shore of {1}; a few sit with him, and learn {0}."),
        new(e => e.RedeCult, Always, "A gambler from the Perostro docks deals knucklebones in {1} and, for those who win, the creed of {0}."),
        new(e => e.Principism, Always, "An old copy of the Chapters, from before the schisms, comes to {1} in a merchant's chest; its readers keep {0} as it was."),
        new(e => e.EarlyQothism, Always, "An old copy of the Chapters, from before the Burning Council, comes to {1} in a merchant's chest; its readers keep {0} as it was."),
    };

    public override void Sprout(HistorySimulation sim)
    {
        var faith = PickFaith(sim);
        if (faith == null) return;
        int region = sim.Pick(sim.Regions.Select(r => r.Id).ToList());
        // The faith's own carrier, when it has one, is the likelier story; otherwise any story fits.
        var own = Carriers.Where(c => c.Faith != null && c.Faith(sim.Empire) == faith && c.When(sim, region)).ToList();
        var generic = Carriers.Where(c => c.Faith == null && c.When(sim, region)).ToList();
        var carrier = own.Count > 0 && (generic.Count == 0 || sim.Chance(0.6)) ? sim.Pick(own) : sim.Pick(generic);
        sim.Smuggle(faith, string.Format(carrier.Text, faith.Name, sim.History.RegionNames[region]),
                    sim.History.OwnerOf(region) is { } owner ? new HistoricInfo[] { owner } : Array.Empty<HistoricInfo>());
    }

    /// <summary>
    /// Which empire faith travels in hiding now: one forbidden at this date most often, a dissenting
    /// branch (Principative under a Genesive province) now and then, an old superseded scripture rarely.
    /// </summary>
    private static Religion? PickFaith(HistorySimulation sim)
    {
        int now = sim.Now;
        var table = new List<(Religion, int)>();
        foreach (var f in sim.Empire.Religions)
        {
            if (!f.Founded.IsKnown || f.Founded.Round > now) continue;
            if (sim.History.PresenceOf(f) == FaithPresence.Open) continue;
            if (f == sim.Empire.LastEmpressCult) continue;   // after the empire; nothing carries it
            int weight = f.IsForbiddenAt(now) ? 6
                       : f.Superseded.IsKnown && f.Superseded.Round <= now ? 1
                       : f.ClandestineAbroad ? 2
                       : 2;
            table.Add((f, weight));
        }
        return table.Count == 0 ? null : ReligionGenerator.Weighted(sim.Rng, table);
    }
}

/// <summary>
/// The life of the world's hidden sects: they spread, are found out, lose their last keepers, or (where
/// the world may hold them openly and no imperial province is watching) come out and win a realm.
/// </summary>
public sealed class ClandestinePulseSeed : PulseSeed
{
    public ClandestinePulseSeed(int due) : base(due) { }

    private static readonly string[] Punishments =
    {
        "burnt in the market square", "hanged at the crossroads", "drowned in weighted sacks", "walled up in their own cellar",
        "branded and driven into the wilds", "buried alive, as the Inquisition taught",
    };

    protected override void Act(HistorySimulation sim)
    {
        var hidden = sim.History.LivingFaiths.Where(f => sim.History.PresenceOf(f) == FaithPresence.Clandestine).ToList();
        if (hidden.Count == 0) return;
        var faith = sim.Pick(hidden);
        int region = sim.Pick(sim.Regions.Select(r => r.Id).ToList());
        string where = sim.History.RegionNames[region];
        var owner = sim.History.OwnerOf(region);
        var others = owner == null ? Array.Empty<HistoricInfo>() : new HistoricInfo[] { owner };
        double roll = sim.Rng.NextDouble();

        if (roll < 0.35)
        {
            sim.Record(new FaithPresenceEvent(sim.Today, HistoryScope.World, faith, FaithPresence.Clandestine,
                $"{faith.Name} spreads in secret through {where}: a word at a well, a sign scratched on a lintel.", others));
        }
        else if (roll < 0.6)
        {
            string how = sim.Pick(Punishments);
            if (how.Contains("Inquisition") && sim.Now < HistoryCalendar.InquisitionRound) how = Punishments[0];
            sim.Record(new FaithPresenceEvent(sim.Today, HistoryScope.World, faith, FaithPresence.Clandestine,
                $"Keepers of {faith.Name} are found out in {where} and {how}.", others));
            if (sim.Chance(0.3)) sim.DieOut(faith, $"With its last circle taken in {where}, {faith.Name} is gone from {sim.History.World.Name}.", others);
        }
        else if (roll < 0.78)
        {
            // Out in the open needs a realm willing, a world allowed, and no imperial province watching.
            bool watched = sim.History.Province is { Government: Government.ImperialProvince };
            var takers = sim.History.LivingRealms.Where(r => r.Government is not (Government.ImperialProvince or Government.Commandery)).ToList();
            if (watched || takers.Count == 0 || !sim.History.Profile.MayHoldOpenly(faith)) return;
            if (owner != null && takers.Contains(owner)) takers = new List<Realm> { owner };
            sim.Surface(faith, sim.Pick(takers), "a ruler converted in secret declares it");
        }
        else if (roll < 0.88)
        {
            sim.DieOut(faith, $"{faith.Name} dies out in hiding: the children of its keepers never learn it.");
        }
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new ClandestinePulseSeed(sim.Later(40, 120));
}

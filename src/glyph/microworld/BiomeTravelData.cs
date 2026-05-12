// BiomeTravelData.cs - Per-biome travel parameters (duration, vital-heat cost, encounters)
// used by the world-map travel system. Locations are not represented here: when a cell
// holds a location, look at its underlying biome to determine travel info.
using System;
using System.Collections.Generic;
using Cathedral.Pathfinding;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// A possible enemy encounter while travelling through a biome.
    /// <see cref="ChancePerCell"/> is the independent probability (0..1) that the encounter
    /// fires when crossing a single cell of this biome.
    /// </summary>
    public readonly struct EncounterEntry
    {
        public string CreatureName { get; }
        public float ChancePerCell { get; }

        public EncounterEntry(string creatureName, float chancePerCell)
        {
            CreatureName = creatureName;
            ChancePerCell = chancePerCell;
        }
    }

    /// <summary>
    /// Travel parameters for a single biome.
    /// </summary>
    public sealed class BiomeTravelInfo
    {
        /// <summary>Identifier of the biome this info applies to (must match <see cref="BiomeType.Name"/>).</summary>
        public string BiomeName { get; }

        /// <summary>Real-world hours required to cross a single cell of this biome.</summary>
        public float DurationHours { get; }

        /// <summary>Vital-heat units consumed to cross a single cell. Negative values
        /// (e.g. cold biomes) increase the required heat — handled by callers.</summary>
        public float VitalHeatPerCell { get; }

        /// <summary>Possible enemy encounters while travelling through this biome.</summary>
        public IReadOnlyList<EncounterEntry> Encounters { get; }

        public BiomeTravelInfo(string biomeName, float durationHours, float vitalHeatPerCell,
            IReadOnlyList<EncounterEntry>? encounters = null)
        {
            BiomeName = biomeName;
            DurationHours = durationHours;
            VitalHeatPerCell = vitalHeatPerCell;
            Encounters = encounters ?? Array.Empty<EncounterEntry>();
        }
    }

    /// <summary>
    /// Static registry of travel info per biome. Biomes flagged as forbidden for a given
    /// travel mode (e.g. sea/ocean for land travel) are listed in <see cref="LandForbiddenBiomes"/>.
    /// In the future, additional travel modes (ship, mount, …) will declare their own
    /// allowed/forbidden sets so each one can plug into <see cref="ITravelConstraint"/>.
    /// </summary>
    public static class BiomeTravelDatabase
    {
        // Default fallback for any biome not explicitly registered.
        // Durations are calibrated so a single-cell hop costs days of travel — a
        // multi-cell trip naturally lands in the months range.
        private static readonly BiomeTravelInfo Fallback =
            new("unknown", durationHours: 24f * 6f, vitalHeatPerCell: 1f);

        public static readonly Dictionary<string, BiomeTravelInfo> Entries = new()
        {
            // hoursPerCell ≈ 24 × days-of-foot-travel per world cell.
            ["plain"]    = new BiomeTravelInfo("plain",    durationHours: 24f *  5f, vitalHeatPerCell: 1f,
                new[] { new EncounterEntry("wolf", 0.02f), new EncounterEntry("bandit", 0.01f) }),
            ["field"]    = new BiomeTravelInfo("field",    durationHours: 24f *  5f, vitalHeatPerCell: 1f,
                new[] { new EncounterEntry("bandit", 0.01f) }),
            ["forest"]   = new BiomeTravelInfo("forest",   durationHours: 24f *  8f, vitalHeatPerCell: 1.5f,
                new[] { new EncounterEntry("wolf", 0.06f), new EncounterEntry("bear", 0.02f),
                        new EncounterEntry("brigand", 0.02f) }),
            ["mountain"] = new BiomeTravelInfo("mountain", durationHours: 24f * 12f, vitalHeatPerCell: 2.5f,
                new[] { new EncounterEntry("bear", 0.03f), new EncounterEntry("rockfall", 0.02f) }),
            ["peak"]     = new BiomeTravelInfo("peak",     durationHours: 24f * 18f, vitalHeatPerCell: 4f,
                new[] { new EncounterEntry("blizzard", 0.05f), new EncounterEntry("ice wraith", 0.01f) }),
            ["coast"]    = new BiomeTravelInfo("coast",    durationHours: 24f *  6f, vitalHeatPerCell: 1f,
                new[] { new EncounterEntry("smuggler", 0.01f) }),
            ["city"]     = new BiomeTravelInfo("city",     durationHours: 24f *  3f, vitalHeatPerCell: 0.5f,
                new[] { new EncounterEntry("thief", 0.01f) }),

            // Water biomes are registered so ship travel can pick them up later. They are
            // forbidden for land travel (see LandForbiddenBiomes below).
            ["sea"]      = new BiomeTravelInfo("sea",      durationHours: 24f * 10f, vitalHeatPerCell: 2f,
                new[] { new EncounterEntry("storm", 0.03f), new EncounterEntry("pirate", 0.02f) }),
            ["ocean"]    = new BiomeTravelInfo("ocean",    durationHours: 24f * 14f, vitalHeatPerCell: 3f,
                new[] { new EncounterEntry("leviathan", 0.01f), new EncounterEntry("storm", 0.04f) }),
        };

        /// <summary>Biomes that block normal on-foot travel.</summary>
        public static readonly HashSet<string> LandForbiddenBiomes = new() { "sea", "ocean" };

        public static BiomeTravelInfo GetFor(string biomeName)
            => Entries.TryGetValue(biomeName, out var info) ? info : Fallback;
    }
}

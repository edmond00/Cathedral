using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// A named companion that travels with the protagonist.
/// Shares all physical and skill state from <see cref="PartyMember"/>,
/// but adds an identity (Name, role description) that is unique to companions.
///
/// The protagonist instance owns a <c>List&lt;Companion&gt;</c> that is
/// populated during game initialisation (or loaded from save data later on).
/// </summary>
public class Companion : PartyMember
{
    /// <summary>The companion's proper name, shown in the party panel.</summary>
    public string Name { get; set; }

    /// <summary>Short flavour description (species, role, …).</summary>
    public string Description { get; set; }

    // ── PartyMember abstract ─────────────────────────────────────
    public override string DisplayName => Name;

    public Companion(string name, string description = "", Species? species = null)
        : base(species ?? SpeciesRegistry.Human)
    {
        Name = name;
        Description = description;
    }

    // ── Random companion factory ─────────────────────────────────
    private static readonly (string name, string desc)[] _pool = new[]
    {
        ("Aldric",   "A grizzled ex-soldier with a mysterious past."),
        ("Maren",    "An herbalist who hears the forest whisper."),
        ("Tav",      "A lanky cartographer obsessed with lost roads."),
        ("Idonie",   "A former nun turned wandering physician."),
        ("Corvus",   "A raven-keeper who trades in secrets."),
        ("Hazel",    "A young apothecary with an uncanny memory."),
        ("Oswin",    "A gaunt ex-scribe haunted by what he once wrote."),
        ("Laine",    "A taciturn tracker who speaks mostly to animals."),
    };

    /// <summary>
    /// Generate a list of random companions with initialised skills.
    /// Used for development / dummy data.
    /// </summary>
    public static List<Companion> GenerateRandom(SkillRegistry registry, int count = 3)
    {
        var rng = new Random();
        var shuffled = (Companion[])
            new Companion[_pool.Length];

        // Shuffle pool indices
        var indices = new int[_pool.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var companions = new List<Companion>();
        for (int k = 0; k < Math.Min(count, _pool.Length); k++)
        {
            var (name, desc) = _pool[indices[k]];
            var c = new Companion(name, desc, SpeciesRegistry.Human);
            c.InitializeSkills(registry, skillCount: 30);
            c.InitializeMemory();
            c.AssignSkillsToMemoryRandom();
            companions.Add(c);
        }
        return companions;
    }
}

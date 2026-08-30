using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Npc.Traits;

/// <summary>
/// The catalogue of every <see cref="PersonalityTrait"/>, in two pools: a <b>global</b> pool any
/// person can be dealt, and a <b>per-archetype</b> pool reserved for one trade. Every generated NPC
/// draws <see cref="GlobalDraw"/> global traits and <see cref="ArchetypeDraw"/> archetype ones, so
/// two blacksmiths differ both in ways anyone could differ and in ways only a smith could.
///
/// <para>
/// The draw is seeded from the NPC's stable id, so a given NPC always has the same traits — they are
/// a permanent fact about that person, not a per-visit reroll.
/// </para>
///
/// <para>
/// Trait content lives in partial classes beside this one (<c>GlobalTraits.*.cs</c> and
/// <c>ArchetypeTraits.*.cs</c>) so the registry itself stays a small, readable index.
/// </para>
/// </summary>
public sealed partial class PersonalityTraitRegistry
{
    /// <summary>How many global traits each NPC is dealt.</summary>
    public const int GlobalDraw = 2;

    /// <summary>How many of its own archetype's traits each NPC is dealt.</summary>
    public const int ArchetypeDraw = 1;

    private static PersonalityTraitRegistry? _instance;
    public static PersonalityTraitRegistry Instance => _instance ??= new PersonalityTraitRegistry();

    private readonly List<PersonalityTrait> _global = new();
    private readonly Dictionary<string, List<PersonalityTrait>> _byArchetype = new();

    private PersonalityTraitRegistry()
    {
        RegisterGlobalTraits();
        RegisterArchetypeTraits();
        Validate();
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    /// <summary>Every trait any NPC could be dealt.</summary>
    public IReadOnlyList<PersonalityTrait> Global => _global;

    /// <summary>The traits reserved for <paramref name="archetypeId"/>; empty when it has none.</summary>
    public IReadOnlyList<PersonalityTrait> ForArchetype(string archetypeId)
        => _byArchetype.TryGetValue(archetypeId, out var list) ? list : Array.Empty<PersonalityTrait>();

    /// <summary>Every archetype id that has its own pool — used by the NPC audit.</summary>
    public IEnumerable<string> ArchetypeIds => _byArchetype.Keys;

    /// <summary>
    /// Deals this NPC's traits: <see cref="ArchetypeDraw"/> from its own trade, then
    /// <see cref="GlobalDraw"/> from the global pool. Archetype traits come first so that when two
    /// traits both want to set the same text field, the trade-specific voice wins
    /// (see <see cref="PersonalityTrait.ApplyText"/>).
    /// </summary>
    public IReadOnlyList<PersonalityTrait> Deal(string archetypeId, Random rng)
    {
        var dealt = new List<PersonalityTrait>();
        dealt.AddRange(Sample(ForArchetype(archetypeId), ArchetypeDraw, rng));
        dealt.AddRange(Sample(_global, GlobalDraw, rng));
        return dealt;
    }

    /// <summary>Draws <paramref name="count"/> distinct traits, or all of them when the pool is smaller.</summary>
    private static IEnumerable<PersonalityTrait> Sample(
        IReadOnlyList<PersonalityTrait> pool, int count, Random rng)
        => pool.Count == 0
            ? Array.Empty<PersonalityTrait>()
            : pool.OrderBy(_ => rng.Next()).Take(Math.Min(count, pool.Count));

    // ── Registration ───────────────────────────────────────────────────────────

    private void Add(PersonalityTrait trait) => _global.Add(trait);

    private void Add(string archetypeId, params PersonalityTrait[] traits)
    {
        if (!_byArchetype.TryGetValue(archetypeId, out var list))
            _byArchetype[archetypeId] = list = new List<PersonalityTrait>();
        list.AddRange(traits);
    }

    /// <summary>
    /// Catches the two content mistakes that are invisible at runtime: a duplicated trait id (the
    /// audit would then under-count, and two different quirks would share a name) and a pool too
    /// small to deal from without repeating.
    /// </summary>
    private void Validate()
    {
        var all = _global.Concat(_byArchetype.Values.SelectMany(l => l)).ToList();

        foreach (var group in all.GroupBy(t => t.TraitId).Where(g => g.Count() > 1))
            Console.Error.WriteLine($"PersonalityTraitRegistry: duplicate trait id '{group.Key}' ({group.Count()}×).");

        if (_global.Count < GlobalDraw)
            Console.Error.WriteLine(
                $"PersonalityTraitRegistry: only {_global.Count} global traits but {GlobalDraw} are dealt per NPC.");

        foreach (var (archetypeId, list) in _byArchetype.Where(p => p.Value.Count < ArchetypeDraw))
            Console.Error.WriteLine(
                $"PersonalityTraitRegistry: archetype '{archetypeId}' has only {list.Count} trait(s).");
    }
}

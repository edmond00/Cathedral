using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cathedral.Game.History.Generation;

/// <summary>
/// The sound of one world's names: a phoneme inventory, syllable shapes and endings drawn once per
/// world, from which every native name (people, places, realms, gods) is built.
///
/// <para><b>Diversity over beauty.</b> The point is that two worlds sound unlike each other and that
/// names within a world sound like they belong together. Some worlds come out harsh and some soft, and
/// that is intended. ASCII only: names end up in glyph text and in LLM prompts.</para>
/// </summary>
public sealed class WorldLanguage
{
    private static readonly string[] AllConsonants =
    {
        "p", "b", "t", "d", "k", "g", "m", "n", "l", "r", "s", "z", "f", "v", "h", "w", "y",
        "sh", "th", "kh", "ch", "gh", "zh", "ts", "q", "x", "j", "ll", "rr", "dh", "mb", "nd",
    };

    private static readonly string[] AllVowels =
    {
        "a", "e", "i", "o", "u", "ae", "ou", "y", "ai", "ei", "oa", "ia", "uo", "ee", "aa",
    };

    private static readonly string[] AllClusters =
    {
        "br", "dr", "kr", "gr", "tr", "pl", "kl", "st", "sk", "sp", "sn", "sv", "thr", "vr", "fl", "zv", "hr",
    };

    private readonly string[] _onsets;
    private readonly string[] _vowels;
    private readonly string[] _codas;
    private readonly string[] _clusters;
    private readonly int _maxSyllables;
    private readonly double _codaChance;
    private readonly double _clusterChance;
    private readonly double _vowelStartChance;
    private readonly string[] _placeEndings;
    private readonly string _femaleEnding;
    private readonly string _maleEnding;
    private readonly Random _rng;
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

    public WorldLanguage(Random rng)
    {
        _rng = rng;
        _onsets = Pick(AllConsonants, rng.Next(7, 15));
        _vowels = Pick(AllVowels, rng.Next(3, 7));
        _codas = Pick(_onsets.Where(c => c.Length == 1 || rng.Next(3) == 0).ToArray(), rng.Next(2, 6));
        _clusters = Pick(AllClusters, rng.Next(0, 5));
        _maxSyllables = rng.Next(2, 4);
        _codaChance = rng.NextDouble() * 0.6;
        _clusterChance = _clusters.Length == 0 ? 0 : rng.NextDouble() * 0.3;
        _vowelStartChance = rng.NextDouble() * 0.25;
        _placeEndings = Enumerable.Range(0, rng.Next(1, 4)).Select(_ => Syllable(true)).ToArray();
        _femaleEnding = _vowels[rng.Next(_vowels.Length)];
        _maleEnding = rng.Next(2) == 0 ? "" : _codas[rng.Next(_codas.Length)];
    }

    private string[] Pick(string[] from, int count)
        => from.OrderBy(_ => _rng.Next()).Take(Math.Max(1, Math.Min(count, from.Length))).ToArray();

    private string Syllable(bool closed)
    {
        var sb = new StringBuilder();
        if (_rng.NextDouble() >= _vowelStartChance)
            sb.Append(_rng.NextDouble() < _clusterChance ? _clusters[_rng.Next(_clusters.Length)] : _onsets[_rng.Next(_onsets.Length)]);
        sb.Append(_vowels[_rng.Next(_vowels.Length)]);
        if (closed || _rng.NextDouble() < _codaChance) sb.Append(_codas[_rng.Next(_codas.Length)]);
        return sb.ToString();
    }

    private string Root(int minSyllables = 1)
    {
        int n = _rng.Next(minSyllables, _maxSyllables + 1);
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++) sb.Append(Syllable(closed: false));
        return sb.ToString();
    }

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>
    /// Draws until the name is new to this world and no longer than <paramref name="maxLength"/>, so two
    /// things never share a name and a heavy language (all digraphs and diphthongs) stays sayable. A
    /// world that has run out of short names gets a longer one, never a number.
    /// </summary>
    private string Unique(Func<string> make, int maxLength)
    {
        for (int i = 0; i < 60; i++)
        {
            string name = Capitalise(make());
            if (name.Length >= 3 && name.Length <= maxLength && _used.Add(name)) return name;
        }
        string longer = Capitalise(make());
        while (!_used.Add(longer)) longer += Syllable(closed: false);
        return longer;
    }

    /// <summary>A person's given name.</summary>
    public string PersonName(Sex sex)
        => Unique(() => Root() + (sex == Sex.Female ? _femaleEnding : _maleEnding), maxLength: 11);

    /// <summary>A place or a country: a root, often with one of this world's place endings.</summary>
    public string PlaceName()
        => Unique(() => _rng.Next(3) == 0 ? Root() : ShortRoot() + _placeEndings[_rng.Next(_placeEndings.Length)], maxLength: 12);

    /// <summary>A root one syllable shorter than usual, so that a root plus an ending stays a name.</summary>
    private string ShortRoot()
    {
        int n = _rng.Next(1, _maxSyllables);
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++) sb.Append(Syllable(closed: false));
        return sb.ToString();
    }

    /// <summary>A god's name: short and heavy.</summary>
    public string DeityName() => Unique(() => Syllable(closed: _rng.Next(2) == 0) + (_rng.Next(3) == 0 ? Syllable(false) : ""), maxLength: 9);

    /// <summary>A plain word of the language, for faiths and peoples.</summary>
    public string Word() => Unique(() => Root(), maxLength: 10);
}

/// <summary>
/// Names for people of the empire: governors, inquisitors, captains. They come from Pyr and sound
/// like it (the Benepelopite-French-Latin register of the lore), whatever world they end up on.
/// </summary>
public static class ImperialNames
{
    private static readonly string[] Male =
    {
        "Aubry", "Amaury", "Anselm", "Bertin", "Clovis", "Donat", "Evrard", "Ferrand", "Gaultier", "Guimar",
        "Hamelin", "Josse", "Jourdain", "Lambert", "Loquin", "Mauger", "Norbert", "Odon", "Philibert", "Raoul",
        "Renier", "Sigebert", "Tancrede", "Thibault", "Urbain", "Vivien", "Ysore", "Achard", "Baudry", "Corentin",
    };

    private static readonly string[] Female =
    {
        "Adele", "Alix", "Berthe", "Clothilde", "Douce", "Emeline", "Ermengarde", "Gisla", "Hawise", "Isaure",
        "Jehanne", "Liss", "Mahaut", "Nicolette", "Oda", "Perrine", "Richilde", "Sibille", "Theophanie", "Ysolde",
    };

    private static readonly string[] Surnames =
    {
        "Chastel", "Fogun", "Varrocq", "Sauval", "Tourbe", "Mervant", "Leneu", "Vallefroy", "Scalde", "Karst",
        "Moll", "Barnutum", "Quintiliym", "Nasrop", "Goprufid", "Lougi", "Sortav", "Exculato", "Geoant", "Bolish",
        "Avrenche", "Colombe", "Daubrac", "Estouteville", "Fresnay", "Gondrecourt", "Harcelle", "Ivrault",
        "Jaucourt", "Lastours", "Montcaud", "Nerval", "Orsanne", "Poissac", "Roquemaure", "Saintonge",
        "Tarvel", "Ussonne", "Vauclerc", "Ymbert",
    };

    public static string Person(Sex sex, Random rng)
    {
        var pool = sex == Sex.Male ? Male : Female;
        return $"{pool[rng.Next(pool.Length)]} {Surnames[rng.Next(Surnames.Length)]}";
    }
}

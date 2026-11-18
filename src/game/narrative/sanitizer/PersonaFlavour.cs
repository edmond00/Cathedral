using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// A modus mentis's own words: the <see cref="Register"/>s it declares, applied to what it writes after
/// the forbidden words are gone, so a voice the small model wrote plainly still sounds like itself —
/// "Aye, there is naught in the barrel" for an archaic mind, "I reckon there's nowt in it" for a
/// rustic one.
///
/// <para><b>A share of the lines, each whole.</b> Every "yes" turned to "aye" in every line is a tic
/// within ten minutes, so only <see cref="SharePercent"/> of a mind's lines are spoken in its register.
/// A line that is takes every listed word: a share of the words was tried first and read as a slip,
/// not a voice — "I am your ear, now open yer purse". Whether a line is voiced is a hash of the mind and
/// of the text up to its first listed word. The hash is the point: it needs no generator, so
/// <c>--seed</c> is untouched, and the same text always comes out the same — and since a growing text
/// never changes what comes before its first listed word, the preview, which rewrites the text on every
/// streamed word, shows exactly the words the final text will hold.</para>
/// </summary>
public sealed class PersonaFlavour
{
    /// <summary>The share of a mind's lines spoken in its register, in percent.</summary>
    public const int SharePercent = 60;

    private static readonly ConcurrentDictionary<string, PersonaFlavour?> _byModusMentis = new();
    private static readonly ConcurrentDictionary<string, Lexicon> _byRegisters = new();

    private readonly Lexicon _lexicon;
    private readonly string _voice;
    private readonly int _share;

    /// <summary>The registers, in precedence order: on a word two of them replace, the first wins.</summary>
    public IReadOnlyList<Register> Registers { get; }

    private PersonaFlavour(IReadOnlyList<Register> registers, string voice, int share)
    {
        Registers = registers;
        _voice    = voice;
        _share    = share;
        _lexicon = _byRegisters.GetOrAdd(string.Join(",", registers), _ => new Lexicon(registers.SelectMany(PersonaLexicons.For)));
    }

    /// <summary>The flavour of <paramref name="mm"/>, or null when it declares no register.</summary>
    public static PersonaFlavour? For(ModusMentis? mm)
    {
        if (mm == null || mm.Registers.Count == 0) return null;
        return _byModusMentis.GetOrAdd(mm.ModusMentisId, _ => new PersonaFlavour(mm.Registers, mm.ModusMentisId, SharePercent));
    }

    /// <summary>
    /// The flavour of what <paramref name="npc"/> says, from its archetype's
    /// <see cref="Npc.NamedNpcArchetype.Registers"/>, or null. The share is the person's own, so two
    /// peasants do not drop into their dialect on the same lines.
    /// </summary>
    public static PersonaFlavour? For(Npc.NpcEntity? npc)
    {
        var registers = npc?.Archetype.Registers;
        if (registers == null || registers.Count == 0) return null;
        return new PersonaFlavour(registers, npc!.PersistentId, SharePercent);
    }

    /// <summary>A flavour of the given registers at a given share, for audits: 100 voices every line.</summary>
    internal static PersonaFlavour Of(int sharePercent, params Register[] registers)
        => new(registers, "audit", sharePercent);

    /// <summary>
    /// Rewrites <paramref name="text"/> in the voice. A name the request supplied
    /// (<paramref name="sourceVocabulary"/>) is kept, as in the forbidden words.
    /// </summary>
    public string Apply(string text, IReadOnlySet<string>? sourceVocabulary = null,
                        List<Lexicon.Swap>? swaps = null)
    {
        int first = _lexicon.FirstFormAt(text);
        if (first < 0) return text;
        bool voiced = _share >= 100 || StableHash($"{_voice}|{text[..first]}") % 100 < _share;
        return voiced ? _lexicon.Rewrite(text, sourceVocabulary, swaps) : text;
    }

    // FNV-1a: string.GetHashCode is randomised per process, and the same text must give the same
    // words in the preview, in the final text and in the next run.
    private static uint StableHash(string s)
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return h;
    }
}

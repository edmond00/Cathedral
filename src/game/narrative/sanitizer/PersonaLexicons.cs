using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// A way of speaking a modus mentis may declare (<see cref="ModusMentis.Registers"/>): the words its
/// narration is rewritten into, a share of the time, after the forbidden words are gone. See
/// <see cref="PersonaFlavour"/>.
/// </summary>
public enum Register
{
    /// <summary>The old high speech of chronicles and chapels: aye, mayhap, 'tis, ere long, naught.</summary>
    Archaic,
    /// <summary>Field and byre talk: aye, nowt, summat, bairn, lass, "I reckon".</summary>
    Rustic,
}

/// <summary>
/// The word lists behind the <see cref="Register"/>s, built with the same helpers as the forbidden
/// words and applied by the same <see cref="Lexicon"/>.
///
/// <para><b>What belongs here.</b> A word that says exactly what the plain word says, in the
/// register's voice, wherever the plain word stands. Every fault the forbidden list met came from a
/// word with a second sense, and a flavour list is made of synonyms, so the bar is higher here:
/// <i>before</i> is not <i>ere</i> (it is also "in front of"), <i>while</i> is not <i>whilst</i>
/// ("a while"), <i>Father</i> is not <i>da</i> (a priest), <i>think</i> is not <i>reckon</i>
/// ("think of") — only <i>I think it</i>, <i>I think that</i> and their like are. A noun may be
/// replaced, and keeps its keyword: <see cref="LexiconBuilder.N"/> records the noun it stands for (<see cref="Lexicon.Entry.Lemma"/>),
/// and <see cref="KeywordExtractor"/> reads the replacement as that noun.</para>
///
/// <para>No replacement may hold a forbidden form or be a <see cref="NameFaking"/> placeholder;
/// <c>--forbidden-words-audit</c> checks both, and the sentences each register must produce.</para>
/// </summary>
internal sealed class PersonaLexicons : LexiconBuilder
{
    private static readonly IReadOnlyDictionary<Register, IReadOnlyList<Lexicon.Entry>> _byRegister = Load();

    /// <summary>The entries of one register.</summary>
    public static IReadOnlyList<Lexicon.Entry> For(Register register) => _byRegister[register];

    private static IReadOnlyDictionary<Register, IReadOnlyList<Lexicon.Entry>> Load()
    {
        var all = new PersonaLexicons().Build();
        return Enum.GetValues<Register>().ToDictionary(
            r => r, r => (IReadOnlyList<Lexicon.Entry>)all.Where(e => e.Category == r.ToString()).ToList());
    }

    protected override IEnumerable<(string Category, Action Section)> Sections() => new (string, Action)[]
    {
        (nameof(Register.Archaic), Archaic),
        (nameof(Register.Rustic), Rustic),
    };

    private void Archaic()
    {
        W("yes", "aye");
        Ws("perhaps, maybe", "mayhap");
        W("often", "oft");
        W("nothing", "naught");
        W("anything", "aught");
        W("until", "till");
        W("between", "betwixt");
        W("among", "amongst");
        W("nearby", "nigh");
        W("almost", "well-nigh");
        W("soon", "anon");
        W("truly", "verily");
        W("enough", "enow");
        W("over there", "yonder");
        Ws("it is, it's", "'tis");
        W("it was", "'twas");
        N("horse", "steed");
        N("stomach", "belly");
        N("girl", "maiden");
        N("evening", "eventide");
        N("morning", "morn");
        N("sky", "firmament", "skies", "firmaments");
    }

    private void Rustic()
    {
        W("yes", "aye");
        W("nothing", "nowt");
        W("something", "summat");
        W("anything", "owt");
        W("my", "me");
        W("you", "ye");
        W("your", "yer");
        W("yourself", "yerself");
        W("little", "wee");
        W("nearly", "nigh on");
        // "I think" is "I reckon" only where it is an opinion: "I think of home" is not "I reckon of
        // home", so the phrase is listed with what may follow it.
        foreach (var next in new[] { "it", "that", "there", "the", "so", "we", "he", "she", "they", "you" })
            W($"I think {next}", $"I reckon {(next == "you" ? "ye" : next)}");
        W("I suppose so", "I s'pose so");
        N("child", "bairn", "children", "bairns");
        N("girl", "lass", "girls", "lasses");
        N("boy", "lad");
        N("stomach", "belly");
        W("money", "brass");
    }
}

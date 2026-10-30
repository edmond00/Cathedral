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
    /// <summary>The thieves' slang of back streets: mate, cove, chink, clink, nicked, grub.</summary>
    Cant,
    /// <summary>A child's words: mam, da, tummy, bunny, scared, ever so, great big.</summary>
    Childish,
    /// <summary>The clerk's and the schoolman's: commence, purchase, appear, endeavour, notion.</summary>
    Learned,
    /// <summary>Clipped: the intensifiers and hedges struck out, the padded phrases cut short.</summary>
    Terse,
    /// <summary>A beast's names for what people make: two-legs, bite-iron, den-mouth, hot-light.</summary>
    Feral,
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
        (nameof(Register.Cant), Cant),
        (nameof(Register.Childish), Childish),
        (nameof(Register.Learned), Learned),
        (nameof(Register.Terse), Terse),
        (nameof(Register.Feral), Feral),
    };

    // "Very" is an intensifier everywhere but in these, where it means "itself".
    private void KeepVeryItself()
    {
        foreach (var phrase in new[] { "the very", "this very", "that very", "my very", "your very", "his very",
                                       "her very", "its very", "our very", "their very", "very own", "very, very" })
            W(phrase, phrase);
    }

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

    private void Cant()
    {
        N("friend", "mate");
        N("man", "cove", "men", "coves");
        N("purse", "poke");
        W("money", "chink");
        Ws("prison, jail, gaol", "clink");
        W("food", "grub");
        W("eyes", "peepers");
        V("steal, steals, stole, stolen, stealing", "nick, nicks, nicked, nicked, nicking");
        KeepVeryItself();
        W("very much", "very much");
        W("very", "proper");
    }

    private void Childish()
    {
        // A capital Father is as often a priest as a parent, and stays; a capital Mother is the
        // child's own, but for a prioress.
        P("Father", "Father");
        W("father", "da");
        W("Mother Superior", "Mother Superior");
        W("mother", "mam");
        W("grandmother", "gran");
        W("grandfather", "grandad");
        N("stomach", "tummy", "stomachs", "tummies");
        N("rabbit", "bunny", "rabbits", "bunnies");
        Ws("frightened, afraid", "scared");
        Ws("huge, enormous", "great big");
        W("tiny", "teeny");
        W("at large", "at large");
        W("by and large", "by and large");
        W("large", "big");
        W("difficult", "hard");
        W("beautiful", "pretty");
        W("horrible", "horrid");
        W("angry", "cross");
        KeepVeryItself();
        W("very well", "very well");
        W("very", "ever so");
    }

    private void Learned()
    {
        V("begin, begins, began, begun", "commence, commences, commenced, commenced");
        V("buy, buys, bought, buying", "purchase, purchases, purchased, purchasing");
        V("seem, seems, seemed", "appear, appears, appeared");
        V("try to, tries to, tried to, trying to", "endeavour to, endeavours to, endeavoured to, endeavouring to");
        V("think about, thinks about, thinking about", "consider, considers, considering");
        V("looks at, looked at, looking at", "regards, regarded, regarding");
        V("talk to, talks to, talked to, talking to", "converse with, converses with, conversed with, conversing with");
        V("ask about, asks about, asked about, asking about", "inquire about, inquires about, inquired about, inquiring about");
        V("get rid of, gets rid of, got rid of, getting rid of", "dispose of, disposes of, disposed of, disposing of");
        W("in the middle of", "in the midst of");
        W("because of", "owing to");
        W("so that's", "so that's");
        W("so that", "in order that");
        W("anyway", "in any case");
        W("maybe", "perhaps");
        W("clever", "astute");
        N("idea", "notion");
        N("tool", "instrument");
    }

    private void Terse()
    {
        // Struck out. Each is an intensifier or a hedge wherever it stands, except in the phrases
        // kept before it: "not quite" is not "not", "quite a few" is not "a few".
        KeepVeryItself();
        foreach (var phrase in new[] { "not quite", "quite a few", "not really", "really?", "really!", "truly?" })
            W(phrase, phrase);
        Ws("very, extremely, quite, really, truly, somewhat, utterly, incredibly, actually", "");
        // Cut short.
        Ws("in order to, so as to", "to");
        Ws("at this moment, at the present time, at present", "now");
        W("at that point in time", "then");
        W("in spite of", "despite");
        Ws("due to the fact that, owing to the fact that, for the reason that", "because");
        W("in the event that", "if");
        Ws("a large number of, a great number of", "many");
        W("each and every", "every");
        W("first and foremost", "first");
        W("in close proximity to", "near");
        V("am able to, is able to, are able to, was able to, were able to", "can, can, can, could, could");
        V("am unable to, is unable to, are unable to, was unable to, were unable to",
          "cannot, cannot, cannot, could not, could not");
        V("make a decision, makes a decision, made a decision, making a decision", "decide, decides, decided, deciding");
    }

    private void Feral()
    {
        // The beast's own names for what people make and are. A noun keeps its keyword, read off the
        // kenning's last word: "bite-iron" is highlighted at "iron" and names the knife.
        N("man", "two-leg", "men", "two-legs");
        N("person", "two-leg", "people", "two-legs");
        N("child", "cub", "children", "cubs");
        N("knife", "bite-iron", "knives", "bite-irons");
        N("sword", "long-claw");
        N("spear", "long-thorn");
        N("arrow", "flying-thorn");
        N("coin", "bright-disc");
        N("fire", "hot-light");
        N("door", "den-mouth");
        N("house", "stone-den");
        N("bed", "sleep-nest");
        N("rope", "long-vine");
        N("bell", "loud-metal");
        N("lantern", "caged-light");
        N("lamp", "caged-light");
        N("book", "leaf-stack");
        N("boot", "foot-hide");
        N("cart", "wheel-box");
        N("wagon", "wheel-box");
        W("clothes", "false-fur");
        W("bread", "grain-meat");
    }
}

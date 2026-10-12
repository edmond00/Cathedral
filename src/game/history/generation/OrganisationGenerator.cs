using System;
using System.Collections.Generic;

namespace Cathedral.Game.History.Generation;

/// <summary>
/// Names for orders, guilds, companies and societies: several templates per kind, filled from word
/// pools and from the world's own language, so that two worlds' guilds do not read alike.
/// </summary>
public static class OrganisationGenerator
{
    private static readonly string[] Emblems =
    {
        "Black Ford", "White Stag", "Iron Hand", "Seventh Gate", "Silver Spur", "Broken Lance", "Red Hound", "Ashen Star",
        "Closed Eye", "Green Thorn", "Salt Crown", "Last Bridge", "Hollow Oak", "Golden Bit", "Burning Wheel", "Grey Mare",
        "Twin Towers", "Long Night", "Raven's Wing", "Bleeding Rose",
    };

    private static readonly string[] Crafts =
    {
        "Weavers", "Smiths", "Masons", "Potters", "Tanners", "Dyers", "Glassblowers", "Coopers", "Carpenters", "Saltmakers",
        "Brewers", "Bellfounders", "Ropemakers", "Papermakers", "Armourers", "Goldsmiths", "Shipwrights", "Chandlers",
        "Fullers", "Bookbinders", "Wheelwrights", "Furriers",
    };

    private static readonly string[] Goods =
        { "Salt", "Wool", "Amber", "Grain", "Wine", "Fur", "Spice", "Iron", "Horse", "Timber", "Pearl", "Dye", "Silk", "Oil" };

    private static readonly string[] Adjectives =
    {
        "Silent", "Crimson", "Grey", "Hidden", "Seventh", "Patient", "Unsleeping", "Faithful", "Free", "Ragged",
        "Laughing", "Nameless", "Bitter", "Lantern", "Hooded", "Iron", "Merry", "Drowned", "Sworn", "Barefoot",
    };

    private static readonly string[] Nouns =
    {
        "Lantern", "Key", "Knot", "Feather", "Coin", "Mask", "Candle", "Well", "Bell", "Thread", "Mirror", "Owl",
        "Ladder", "Door", "Root", "Ember", "Hourglass", "Seed", "Stone", "Veil",
    };

    private static readonly string[] Beasts = { "Wolf", "Boar", "Bear", "Stag", "Lynx", "Hawk", "Elk", "Auroch", "Fox", "Hound" };

    private static readonly string[] Numbers = { "Three", "Seven", "Nine", "Twelve", "Hundred", "Forty", "Eleven", "Thirteen" };

    private static readonly string[] Ores = { "Copper", "Iron", "Silver", "Tin", "Salt", "Coal", "Lead", "Gold" };

    public static string Name(OrganisationKind kind, WorldLanguage lang, Random rng, string place, Religion? faith)
    {
        string P(string[] pool) => pool[rng.Next(pool.Length)];
        string Pick(params string[] options) => options[rng.Next(options.Length)];

        return kind switch
        {
            OrganisationKind.KnightlyOrder => Pick(
                $"the Order of the {P(Emblems)}", $"the Knights of {place}", $"the Brotherhood of the {P(Emblems)}",
                $"the {P(Adjectives)} Knights of {lang.Word()}", $"the Sworn Swords of {place}"),
            OrganisationKind.ArtisanGuild => Pick(
                $"the Guild of {P(Crafts)} of {place}", $"the {P(Crafts)}' Hall of {place}", $"the Worshipful Company of {P(Crafts)}",
                $"the {P(Crafts)} of the {P(Nouns)}"),
            OrganisationKind.MerchantGuild => Pick(
                $"the {place} Company", $"the League of {P(Goods)} Merchants", $"the {P(Goods)} Hanse of {place}",
                $"the Venturers of {place}", $"the House of the {P(Goods)} Scales"),
            OrganisationKind.MinersGuild => Pick(
                $"the Deep Guild of {place}", $"the {P(Ores)} Brotherhood", $"the Delvers of {place}", $"the Lamp-Bearers of the {P(Ores)} Seams"),
            OrganisationKind.MonasticOrder => faith != null
                ? Pick($"the Brothers of {faith.Name}", $"the Sisters of the {P(Nouns)}, keepers of {faith.Name}",
                       $"the House of Silence at {place}", $"the {P(Adjectives)} Monks of {place}")
                : Pick($"the House of Silence at {place}", $"the {P(Adjectives)} Monks of {place}", $"the Hermits of {place}"),
            OrganisationKind.ScholarsCollege => Pick(
                $"the College of {place}", $"the Academy of the {P(Nouns)}", $"the Readers of {place}", $"the Hall of Questions at {place}"),
            OrganisationKind.HealersGuild => Pick(
                $"the Leeches of {place}", $"the Order of the Clean Hand", $"the Bonesetters of {place}", $"the Sisters of the Bitter Herb"),
            OrganisationKind.BardsCompany => Pick(
                $"the Singers of {place}", $"the {P(Adjectives)} Players", $"the Company of the {P(Nouns)}", $"the Remembrancers of {place}"),
            OrganisationKind.HuntersLodge => Pick(
                $"the Lodge of the {P(Beasts)}", $"the {P(Beasts)}-Hunters of {place}", $"the Wardens of the {lang.Word()} Wood"),
            OrganisationKind.MercenaryCompany => Pick(
                $"the {P(Adjectives)} Company", $"the {P(Numbers)} Spears", $"the Free Company of {place}", $"the {P(Beasts)}'s Teeth"),
            OrganisationKind.PirateBrotherhood => Pick(
                $"the {P(Adjectives)} Sails", $"the Brethren of the {place} Coast", $"the Salt Wolves", $"the Children of the {P(Nouns)}"),
            OrganisationKind.ThievesGuild => Pick(
                $"the {P(Adjectives)} Hands", $"the Night Market of {place}", $"the Brotherhood of the {P(Nouns)}", $"the Rats of {place}"),
            OrganisationKind.AssassinsGuild => Pick(
                $"the {P(Adjectives)} Knives", $"the Gift-Givers of {place}", $"the {P(Nouns)}-Bearers", $"the Quiet Ones"),
            OrganisationKind.SecretSociety => faith != null
                ? Pick($"the Keepers of {faith.Name}", $"the Circle of the Hidden {P(Nouns)}", $"the Wells of {place}")
                : Pick($"the {P(Adjectives)} Circle", $"the Society of the {P(Nouns)}", $"the Watchers of {place}", $"the {P(Numbers)} of {place}"),
            _ => $"the {lang.Word()} Fellowship",
        };
    }

    /// <summary>One clause for what the kind of body does, for its description.</summary>
    public static string Purpose(OrganisationKind kind) => kind switch
    {
        OrganisationKind.KnightlyOrder     => "A sworn order of mounted fighters, holding lands for service.",
        OrganisationKind.ArtisanGuild      => "A guild of craftsmen: it sets prices, trains apprentices and keeps outsiders out of the trade.",
        OrganisationKind.MerchantGuild     => "A league of traders sharing roads, ships and risks.",
        OrganisationKind.MinersGuild       => "The miners of the deep seams, with their own law below ground.",
        OrganisationKind.MonasticOrder     => "A community of the devout under a rule of life.",
        OrganisationKind.ScholarsCollege   => "A house of learning: copyists, disputants, keepers of old books.",
        OrganisationKind.HealersGuild      => "Physicians, bonesetters and herb-wives, sworn to one another.",
        OrganisationKind.BardsCompany      => "Singers and players who carry the old stories from hall to hall.",
        OrganisationKind.HuntersLodge      => "Hunters and wardens of the wild country, keepers of its paths.",
        OrganisationKind.MercenaryCompany  => "Soldiers who fight for whoever pays.",
        OrganisationKind.PirateBrotherhood => "Sea-raiders with their own articles and their own coves.",
        OrganisationKind.ThievesGuild      => "A brotherhood of thieves and fences, with its own law and its own taxes.",
        OrganisationKind.AssassinsGuild    => "Killers for hire, who keep their contracts and their silence.",
        OrganisationKind.SecretSociety     => "A society that meets in secret, for ends its members do not say.",
        _                                  => "A branch of an institution of the empire.",
    };
}

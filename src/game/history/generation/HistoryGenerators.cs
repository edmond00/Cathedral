using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.History.Generation;

/// <summary>
/// The native faiths of a generated world. Simple by design: a polytheism is a handful of gods, each
/// a domain, a nature and a form drawn at random. What makes worlds differ is the draw, not depth.
/// </summary>
public static class ReligionGenerator
{
    private static readonly (ReligionKind Kind, int Weight)[] Kinds =
    {
        (ReligionKind.Polytheism, 50), (ReligionKind.Animism, 15), (ReligionKind.AncestorCult, 12),
        (ReligionKind.HollowCult, 10), (ReligionKind.Discipline, 8), (ReligionKind.PersonCult, 5),
    };

    public static Religion Create(WorldLanguage lang, Random rng, HistoricDate founded, HistoricFigure? founder,
                                  Religion? parent = null, ReligionKind? kind = null)
    {
        var k = kind ?? Weighted(rng, Kinds);
        string word = lang.Word();
        var religion = new Religion("", k, HistoryScope.World) { Founded = founded, Founder = founder, Parent = parent };

        switch (k)
        {
            case ReligionKind.Polytheism:
            {
                int count = rng.Next(2, 8);
                var domains = Enum.GetValues<DeityDomain>().OrderBy(_ => rng.Next()).Take(count).ToList();
                foreach (var d in domains)
                {
                    var nature = Pick(rng, Enum.GetValues<DeityNature>());
                    var form = Pick(rng, Enum.GetValues<DeityForm>());
                    var deity = new Deity(lang.DeityName(), d, nature, form)
                    {
                        Description = $"The {Lower(nature)} {FormPhrase(form)} of {Lower(d)}.",
                    };
                    religion.Deities.Add(deity);
                }
                var chief = religion.Deities[0];
                religion.Name = rng.Next(3) switch
                {
                    0 => $"the Faith of {chief.Name}",
                    1 => $"the {word} Gods",
                    _ => $"the Cult of the {religion.Deities.Count} of {word}",
                };
                religion.Description = $"A polytheism of {religion.Deities.Count} gods, chief among them {chief.Name}, "
                                     + $"{chief.Description.TrimEnd('.').ToLowerInvariant()}.";
                break;
            }
            case ReligionKind.Animism:
                religion.Name = $"the {word} Spirit-Ways";
                religion.Description = $"A faith of spirits in {Pick(rng, AnimistHaunts)}, appeased with offerings.";
                break;
            case ReligionKind.AncestorCult:
                religion.Name = $"the Hearth of {word}";
                religion.Description = $"The dead of each house are worshipped at its hearth; the oldest ancestor, {lang.PersonName(Sex.Female)}, is honoured by all.";
                break;
            case ReligionKind.HollowCult:
                religion.Name = $"the {word} Descent";
                religion.Description = "A cult of caves and deep places: the dead are carried down, and something below is said to listen.";
                break;
            case ReligionKind.Discipline:
                religion.Name = $"the {word} Discipline";
                religion.Description = $"A faith without gods: a discipline of {Pick(rng, Disciplines)}.";
                break;
            default:
                religion.Name = founder == null ? $"the {word} Way" : $"the Way of {founder.Name}";
                religion.Description = founder == null
                    ? "The teaching of a prophet whose name has been lost."
                    : $"The teaching of {founder.Name}, revered as more than a man.";
                break;
        }
        return religion;
    }

    private static readonly string[] AnimistHaunts =
        { "rivers and springs", "old trees", "stones and hills", "the weather", "beasts", "the sea and its winds", "the fields" };

    private static readonly string[] Disciplines =
        { "silence", "fasting", "stillness", "endurance of pain", "remembering the dead", "breath", "walking" };

    private static string FormPhrase(DeityForm form) => form switch
    {
        DeityForm.Human       => "god in human shape",
        DeityForm.Animal      => "beast-god",
        DeityForm.BeastHeaded => "beast-headed god",
        DeityForm.Faceless    => "faceless god",
        DeityForm.Twin        => "twin gods",
        DeityForm.Giant       => "giant",
        DeityForm.Child       => "child-god",
        DeityForm.Crone       => "crone",
        DeityForm.Bird        => "bird-god",
        DeityForm.Serpent     => "serpent",
        DeityForm.Tree        => "tree-god",
        DeityForm.Stone       => "stone-god",
        DeityForm.Fire        => "god of living flame",
        DeityForm.Many        => "many-bodied god",
        _                     => "unseen god",
    };

    private static string Lower(Enum e)
    {
        string s = e.ToString();
        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    internal static T Pick<T>(Random rng, IReadOnlyList<T> from) => from[rng.Next(from.Count)];

    internal static T Weighted<T>(Random rng, IReadOnlyList<(T Item, int Weight)> table)
    {
        int total = table.Sum(t => t.Weight);
        int roll = rng.Next(total);
        foreach (var (item, weight) in table)
        {
            if (roll < weight) return item;
            roll -= weight;
        }
        return table[^1].Item;
    }
}

/// <summary>Names and shapes for realms, wars and places.</summary>
public static class RealmGenerator
{
    public static Government DrawGovernment(Random rng, int round, RegionProfile capital, int regionCount)
    {
        bool postEmpire = round > HistoryCalendar.HatchingRound;
        var table = new List<(Government, int)>
        {
            (Government.Kingdom, 30),
            (Government.Principality, 12),
            (Government.Chiefdom, round < -1000 ? 25 : 8),
            (Government.Tribe, round < -1000 ? 20 : 5),
            (Government.Theocracy, 6),
            (Government.Republic, capital.Coastal ? 10 : 4),
            (Government.CityLeague, capital.Coastal && regionCount <= 2 ? 8 : 1),
            (Government.ClanConfederacy, capital.IsMountainous ? 18 : 4),
            (Government.Duchy, postEmpire ? 12 : 0),
        };
        return ReligionGenerator.Weighted(rng, table.Where(t => t.Item2 > 0).ToList());
    }

    public static string RealmName(Government g, string place, Random rng) => g switch
    {
        Government.Kingdom         => $"the Kingdom of {place}",
        Government.Principality    => $"the Principality of {place}",
        Government.Chiefdom        => $"the Chiefdom of {place}",
        Government.Tribe           => rng.Next(2) == 0 ? $"the {place} Tribe" : $"the People of {place}",
        Government.Republic        => $"the Republic of {place}",
        Government.Theocracy       => $"the Holy Realm of {place}",
        Government.CityLeague      => $"the League of {place}",
        Government.ClanConfederacy => $"the Clans of {place}",
        Government.Duchy           => $"the Duchy of {place}",
        Government.Commandery      => $"the Commandery of {place}",
        Government.ImperialProvince => $"the Province of {place}",
        _                          => place,
    };

    private static readonly string[] WarCauses =
    {
        "a border quarrel", "a disputed succession", "an old insult", "the grain of a neighbour",
        "a stolen bride", "a slain envoy", "the control of a ford", "a raid answered", "a broken treaty",
        "a holy quarrel", "the pasture of the high valleys", "a claim through marriage",
    };

    private static readonly string[] WarNouns =
    {
        "Border", "Succession", "Crown", "Ford", "Pasture", "Bride", "Envoy", "Grain", "Oath", "Hundred Days",
        "Ashes", "Broken Treaty", "Long", "Short", "Brothers'", "Salt", "Rivers",
    };

    public static (string Name, string Cause) War(Realm a, Realm b, Random rng)
    {
        string cause = WarCauses[rng.Next(WarCauses.Length)];
        string noun = WarNouns[rng.Next(WarNouns.Length)];
        return ($"the {noun} War of {ShortName(a)} and {ShortName(b)}", cause);
    }

    /// <summary>"the Kingdom of Varsk" becomes "Varsk": the place a realm is named for.</summary>
    public static string ShortName(HistoricInfo realm)
    {
        string n = realm.Name;
        int of = n.LastIndexOf(" of ", StringComparison.Ordinal);
        if (of >= 0) return n[(of + 4)..];
        return n.StartsWith("the ") ? n[4..] : n;
    }

    public static PlaceKind DrawPlaceKind(Random rng, RegionProfile region, int round)
    {
        var table = new List<(PlaceKind, int)>
        {
            (PlaceKind.Town, 30),
            (PlaceKind.City, region.Habitability > 40 ? 15 : 4),
            (PlaceKind.Port, region.Coastal ? 20 : 0),
            (PlaceKind.Fortress, 14),
            (PlaceKind.Temple, 10),
            (PlaceKind.Sanctuary, 6),
            (PlaceKind.Monastery, round > 0 ? 6 : 1),
            (PlaceKind.Mine, region.MountainCells > 0 ? 12 : 0),
        };
        return ReligionGenerator.Weighted(rng, table.Where(t => t.Item2 > 0).ToList());
    }

    private static readonly string[] Epithets =
    {
        "the Bold", "the Fair", "the Cruel", "the Wise", "the Young", "the Old", "the Pious", "the Mad",
        "the Silent", "the Lame", "the Builder", "the Red", "the Unready", "the Just", "the Fat", "the Great",
        "the Weeping", "the Hunter", "the Stranger", "the Bastard",
    };

    public static string MaybeEpithet(Random rng) => rng.Next(4) == 0 ? Epithets[rng.Next(Epithets.Length)] : "";
}

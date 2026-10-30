using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// The word list behind <see cref="ForbiddenWordsDictionary"/>: words of the real world — its
/// places, people, faiths, calendar, units, inventions, foods and slang — paired with a word of
/// this one. Read by Layer 1 on every LLM text, with no request made.
///
/// <para><b>What belongs here.</b> A word that names something of the real world specifically, or
/// of an era after the world's own: <i>pizza</i> but not <i>pancake</i>, <i>mosque</i> but not
/// <i>temple</i>, <i>week</i> but not <i>day</i>, <i>president</i> but not <i>queen</i>,
/// <i>gun</i> but not <i>arquebus</i>, <i>haiku</i> but not <i>poetry</i>. A word with an
/// ordinary second sense the narration uses ("train" a dog, a "plane" of wood, a prison "cell",
/// "may" I) is left out, or entered case-sensitively for the sense that is a name
/// (<c>P("May", …, midSentenceOnly: true)</c>). No entry may be a placeholder name from
/// <see cref="NameFaking"/>: those are swapped back to the characters' real names after this runs.</para>
///
/// <para><b>Replacements follow the lore</b> (<c>lore/</c>): the year is a <i>round</i>, the week
/// eleven days, a moon an <i>eggmoon</i>, the sky's light the <i>Eye of Day</i>, the stars embers,
/// the universe the <i>cosmic sea</i>, a flying or space-going craft a <i>submarine</i>, money
/// <i>aquilae, scutes and pellets</i>, a real god the principium of its domain, a real conqueror
/// Aqilon. Where the lore has no word, the plainest word of the period.</para>
///
/// <para><b>Every form is listed.</b> <see cref="N"/> adds a noun's plural, <see cref="V"/> takes a
/// verb's forms side by side; anything irregular is spelled out. A replacement is never itself
/// re-scanned, and the dictionary test asserts none contains a listed form.</para>
/// </summary>
internal static class ForbiddenWords
{
    private static readonly List<ForbiddenWordsDictionary.Entry> _list = new();

    public static IEnumerable<ForbiddenWordsDictionary.Entry> Build()
    {
        _list.Clear();
        foreach (var (category, section) in new (string, Action)[]
        {
            (nameof(Informal), Informal),
            (nameof(Calendar), Calendar),
            (nameof(Units), Units),
            (nameof(Cosmos), Cosmos),
            (nameof(Technology), Technology),
            (nameof(Transport), Transport),
            (nameof(Weapons), Weapons),
            (nameof(Science), Science),
            (nameof(PoliticsAndSociety), PoliticsAndSociety),
            (nameof(Commerce), Commerce),
            (nameof(PlacesAndBuildings), PlacesAndBuildings),
            (nameof(FoodAndDrink), FoodAndDrink),
            (nameof(ClothingAndObjects), ClothingAndObjects),
            (nameof(Faiths), Faiths),
            (nameof(Gods), Gods),
            (nameof(Figures), Figures),
            (nameof(Geography), Geography),
            (nameof(Eras), Eras),
            (nameof(CultureAndArts), CultureAndArts),
            (nameof(Fauna), Fauna),
            (nameof(PeoplesAndPlacesMore), PeoplesAndPlacesMore),
            (nameof(ModernLifeMore), ModernLifeMore),
            (nameof(IdiomsOfMachines), IdiomsOfMachines),
            (nameof(EngineeringAndMeta), EngineeringAndMeta),
        })
        {
            int from = _list.Count;
            section();
            for (int i = from; i < _list.Count; i++) _list[i] = _list[i] with { Category = category };
        }
        return _list;
    }

    // ── Builders ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A phrase left exactly as it is, though it holds a listed form: the lore's own "Fallen God" is
    /// not the God that becomes Qoth. Longer than the form inside it, so the match takes it first.
    /// </summary>
    private static void Keep(string phrase)
        => _list.Add(new(phrase, phrase, Proper: true, MidSentenceOnly: false));

    /// <summary>A single form, any case.</summary>
    private static void W(string form, string replacement)
        => _list.Add(new(form, replacement, Proper: false, MidSentenceOnly: false));

    /// <summary>
    /// A unit of count: the noun and its plural as <see cref="N"/>, and after a number or a
    /// quantifier the whole count is rewritten by <paramref name="counted"/> ("six centuries" →
    /// "six hundred rounds", "a month" → "thirty days").
    /// </summary>
    private static void C(string noun, string replacement, string plural, string replacementPlural,
                          Func<ForbiddenWordsDictionary.Quantity, string> counted)
    {
        _list.Add(new(noun, replacement, Proper: false, MidSentenceOnly: false, counted));
        _list.Add(new(plural, replacementPlural, Proper: false, MidSentenceOnly: false, counted));
    }

    /// <summary>A count of <paramref name="per"/> <paramref name="unit"/> per item: two months → sixty days.</summary>
    private static Func<ForbiddenWordsDictionary.Quantity, string> Times(long per, string unit, string otherwise)
        => q => q.Per ? $"in {ForbiddenWordsDictionary.NumberWords(per)} {unit}"
            : q.Value is long n ? $"{ForbiddenWordsDictionary.Number(n * per, q)} {unit}"
            : $"{q.Text} {otherwise}";

    /// <summary>
    /// A count kept as written before a larger number: six centuries → six hundred rounds, a few
    /// centuries → a few hundred rounds. Digits are multiplied out (3 centuries → 300 rounds).
    /// </summary>
    private static Func<ForbiddenWordsDictionary.Quantity, string> Before(long scale, string scaleWord, string unit)
        => q => q.Per ? $"in a {scaleWord} {unit}"
            : q.Value is long n && char.IsDigit(q.Text[0]) ? $"{n * scale} {unit}"
            : $"{q.Text} {scaleWord} {unit}";

    /// <summary>Several forms with one replacement.</summary>
    private static void Ws(string forms, string replacement)
    {
        foreach (var f in Split(forms)) W(f, replacement);
    }

    /// <summary>A noun and its plural. Irregular plurals are given explicitly.</summary>
    private static void N(string noun, string replacement, string? plural = null, string? replacementPlural = null)
    {
        W(noun, replacement);
        // An invariant plural ("aircraft", "haiku") is the singular's own form, which keeps the
        // singular replacement: the plural reading cannot be told from it.
        string p = plural ?? Plural(noun);
        if (!p.Equals(noun, StringComparison.OrdinalIgnoreCase))
            W(p, replacementPlural ?? Plural(replacement));
    }

    /// <summary>Paired forms: "a,b,c" → "x,y,z", one for one (a verb's base, -s, past, -ing).</summary>
    private static void V(string forms, string replacements)
    {
        var f = Split(forms);
        var r = Split(replacements);
        if (f.Length != r.Length)
            throw new InvalidOperationException($"ForbiddenWords: '{forms}' and '{replacements}' differ in length.");
        for (int i = 0; i < f.Length; i++) W(f[i], r[i]);
    }

    /// <summary>A proper noun, matched only as capitalised; its replacement keeps its own case.</summary>
    private static void P(string name, string replacement, bool midSentenceOnly = false)
        => _list.Add(new(name, replacement, Proper: true, MidSentenceOnly: midSentenceOnly));

    /// <summary>Several proper nouns with one replacement.</summary>
    private static void Ps(string names, string replacement)
    {
        foreach (var n in Split(names)) P(n, replacement);
    }

    /// <summary>A demonym: "Roman" and "Romans", as adjective and as people.</summary>
    private static void D(string name, string replacement, string? plural = null, string? replacementPlural = null)
    {
        P(name, replacement);
        P(plural ?? name + "s", replacementPlural ?? Plural(replacement));
    }

    private static string[] Split(string s)
        => s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static readonly Dictionary<string, string> _irregular = new(StringComparer.OrdinalIgnoreCase)
    {
        ["man"] = "men", ["woman"] = "women", ["foot"] = "feet", ["tooth"] = "teeth", ["child"] = "children",
        ["knife"] = "knives", ["leaf"] = "leaves", ["life"] = "lives", ["wife"] = "wives", ["mouse"] = "mice",
        ["person"] = "people", ["fish"] = "fish", ["sheep"] = "sheep", ["deer"] = "deer", ["craft"] = "craft",
        ["shelf"] = "shelves", ["loaf"] = "loaves", ["thief"] = "thieves", ["staff"] = "staves",
        ["cloth"] = "cloths", ["ox"] = "oxen", ["goose"] = "geese", ["day"] = "days", ["key"] = "keys",
        ["lore"] = "lore", ["chapter"] = "chapters", ["armour"] = "armour", ["grain"] = "grains",
        ["stuff"] = "stuff", ["swine"] = "swine", ["gallows"] = "gallows",
    };

    /// <summary>English plural of a noun or of a noun phrase's head ("span of days" → "spans of days").</summary>
    internal static string Plural(string phrase)
    {
        int of = phrase.IndexOf(" of ", StringComparison.Ordinal);
        if (of > 0) return Plural(phrase[..of]) + phrase[of..];

        int sp = phrase.LastIndexOf(' ');
        string head = sp < 0 ? phrase : phrase[(sp + 1)..];
        string lead = sp < 0 ? "" : phrase[..(sp + 1)];

        int hy = head.LastIndexOf('-');
        string pre = hy < 0 ? "" : head[..(hy + 1)];
        string w   = hy < 0 ? head : head[(hy + 1)..];

        string p;
        if (_irregular.TryGetValue(w, out var irr))
            p = char.IsUpper(w[0]) ? char.ToUpperInvariant(irr[0]) + irr[1..] : irr;
        else if (w.EndsWith("man", StringComparison.Ordinal) && w.Length > 3)
            p = w[..^3] + "men";
        else if (w.Length > 1 && w.EndsWith('y') && "aeiou".IndexOf(w[^2]) < 0)
            p = w[..^1] + "ies";
        else if (w.EndsWith('s') || w.EndsWith('x') || w.EndsWith('z') || w.EndsWith("ch") || w.EndsWith("sh"))
            p = w + "es";
        else
            p = w + "s";
        return lead + pre + p;
    }

    // ── Informal and modern speech ─────────────────────────────────────────────

    private static void Informal()
    {
        P("OK", "very well");
        P("Ok", "very well");
        Ws("okay, ok, alright, all-right", "very well");
        Ws("okey-dokey, okie-dokie", "very well");
        Ws("hi, hiya, howdy", "hail");
        Ws("hey", "hail");
        Ws("hello", "well met");
        Ws("bye, bye-bye, byebye", "farewell");
        Ws("yeah, yep, yup, yah, yeh", "aye");
        Ws("nope, nah", "nay");
        W("gonna", "going to");
        W("wanna", "wish to");
        Ws("kinda, sorta", "somewhat");
        W("gotta", "must");
        W("dunno", "know not");
        W("lemme", "let me");
        W("gimme", "give me");
        W("ain't", "is not");
        Ws("wow, whoa, woah", "oh");
        Ws("oops, whoops, oopsie", "ah");
        Ws("uh-oh", "alas");
        N("guy", "fellow");
        N("dude", "fellow");
        N("bro", "brother");
        N("buddy", "friend");
        N("pal", "friend");
        N("mom", "mother");
        N("mommy", "mother");
        N("momma", "mother");
        N("daddy", "father");
        N("teenager", "youth");
        N("teen", "youth");
        W("teenage", "young");
        N("boss", "master");
        N("cop", "watchman");
        N("weirdo", "oddity");
        Ws("cool-headed", "level-headed");
        Ws("awesome", "wondrous");
        W("super", "very");
        W("fantastic", "marvellous");
        W("stressed", "strained");
        W("stressful", "trying");
        W("freaked", "frightened");
        W("freaking", "cursed");
        W("cheesy", "trite");
        W("hipster", "dandy");
        W("nerd", "bookworm");
        W("nerdy", "bookish");
        W("geek", "bookworm");
        W("OK'd", "allowed");
        W("vibe", "air");
        W("vibes", "air");
        W("selfie", "likeness");
        W("hashtag", "mark");
        W("emoji", "sign");
        W("meme", "jest");
        W("memes", "jests");
        W("google", "search");
        W("googled", "searched");
        W("multitask", "juggle tasks");
        W("multitasking", "juggling tasks");
        W("feedback", "word");
        W("deadline", "appointed day");
        W("deadlines", "appointed days");
        W("weekend", "Silday");
        W("weekends", "Sildays");
        N("vacation", "respite");
        N("tourist", "traveller");
        N("tourism", "travelling");
        N("hobby", "pastime");
        N("career", "calling");
        N("teammate", "companion");
        N("coworker", "fellow worker");
        N("co-worker", "fellow worker");
        N("colleague", "fellow");
        N("workaholic", "drudge");
        N("lifestyle", "way of life");
        N("mindset", "frame of mind");
        N("superstar", "celebrated one");
        N("celebrity", "renowned one", "celebrities", "renowned ones");
        W("OK-ish", "passable");
        P("Mr", "Master"); P("Mr.", "Master");
        P("Mrs", "Mistress"); P("Mrs.", "Mistress");
        P("Ms", "Mistress"); P("Ms.", "Mistress");
        P("Dr.", "Master");
    }

    // ── The calendar: rounds, eleven-day weeks, the lore's day names ───────────

    private static void Calendar()
    {
        N("year", "round");
        Ws("yearly", "each round");
        W("year-old", "round-old");
        W("year-olds", "round-olds");
        W("year-long", "round-long");
        W("years-long", "rounds-long");
        C("decade", "ten rounds", "decades", "tens of rounds", Times(10, "rounds", "tens of rounds"));
        C("century", "hundred rounds", "centuries", "hundreds of rounds", Before(100, "hundred", "rounds"));
        W("centuries-old", "age-old");
        W("century-old", "age-old");
        C("millennium", "thousand rounds", "millennia", "thousands of rounds", Before(1000, "thousand", "rounds"));
        W("millenniums", "thousands of rounds");
        W("millennial", "thousand-round");
        W("centennial", "hundredth-round");
        N("week", "eleven-day");
        W("weekly", "every eleven days");
        W("week-long", "eleven-day");
        C("fortnight", "fourteen days", "fortnights", "spans of fourteen days", Times(14, "days", "spans of fourteen days"));
        W("fortnightly", "every eleven days");
        W("sennight", "eleven-day");
        C("month", "season", "months", "seasons", Times(30, "days", "seasons"));
        W("monthly", "each season");
        W("month-long", "season-long");
        W("months-long", "seasons-long");
        W("minutes", "moments");   // not "minute", which is also "tiny"
        W("a minute", "a moment");
        W("one minute", "one moment");
        W("minutes'", "moments'");
        W("seconds", "heartbeats");   // not "second", which is mostly the ordinal
        W("milliseconds", "heartbeats");
        W("millisecond", "heartbeat");
        W("nanosecond", "heartbeat");
        Ws("o'clock, oclock", "bells");
        W("a.m.", "in the morning");
        W("p.m.", "in the evening");
        P("BC", "before the Foundation");
        P("BCE", "before the Foundation");
        W("A.D.", "after the Foundation");
        W("B.C.", "before the Foundation");

        // The eleven days, one per chapter; the real seven fall on their nearest likeness.
        D("Sunday", "Silday");
        D("Monday", "Hookday");
        D("Tuesday", "Ashday");
        D("Wednesday", "Throwday");
        D("Thursday", "Dropday");
        D("Friday", "Eyeday");
        D("Saturday", "Feastday");
        P("Sabbath", "Silday");
        P("Sabbaths", "Sildays");
        W("sabbath", "Silday");

        // Months: the three seasons of a round. May, March and August are words too, so only a
        // capital in mid-sentence is taken for the month.
        Ps("January, February, December", "the Stillness");
        Ps("April, June, July, September, October, November", "the Weeping");
        P("March", "the Weeping", midSentenceOnly: true);
        P("May", "the Weeping", midSentenceOnly: true);
        P("August", "the Drought", midSentenceOnly: true);
        Ps("Jan., Feb., Dec.", "the Stillness");

        // Real feasts.
        Ps("Christmas, Xmas, Yule, Yuletide", "the Remnant Days");
        Ps("Christmastide, Christmas-tide, Noel", "the Remnant Days");
        Ps("Easter, Eastertide", "the Sowing Feast");
        Ps("Halloween, Hallowe'en, All Hallows' Eve, All Hallows", "the Night of the Well");
        Ps("Thanksgiving", "Feastday");
        Ps("Lent, Advent, Pentecost, Epiphany, Whitsun, Candlemas, Michaelmas, Lammas, Martinmas", "the chapter-feast");
        Ps("Ramadan, Hanukkah, Chanukah, Passover, Diwali, Purim", "the chapter-feast");
        Ps("New Year's Eve, New Year's Day", "the Remnant Days");
        P("New Year", "the turn of the round");
        P("Valentine's Day", "Dropday");
        P("Mardi Gras", "Feastday");
        P("Carnival", "the masques");
        P("Midsummer", "the height of the Drought");
        P("Midwinter", "the heart of the Stillness");
        P("Solstice", "turn of the seasons");
    }

    // ── Units of measure ───────────────────────────────────────────────────────

    private static void Units()
    {
        N("mile", "league");
        Ws("mile-long", "league-long");
        N("kilometre", "league");
        N("kilometer", "league");
        N("km", "league");
        N("metre", "pace");
        N("centimetre", "finger's breadth", replacementPlural: "fingers' breadth");
        N("centimeter", "finger's breadth", replacementPlural: "fingers' breadth");
        N("millimetre", "hair's breadth", replacementPlural: "hairs' breadth");
        N("millimeter", "hair's breadth", replacementPlural: "hairs' breadth");
        N("inch", "thumb's breadth", replacementPlural: "thumbs' breadth");
        N("kilogram", "stone");
        N("kilo", "stone");
        N("kg", "stone");
        N("gram", "grain");
        N("milligram", "grain");
        N("litre", "pint");
        N("liter", "pint");
        N("millilitre", "drop");
        N("milliliter", "drop");
        N("gallon", "bucketful");
        N("tonne", "cartload");
        N("ton", "cartload");
        N("hectare", "field");
        N("acre", "field");
        Ws("mph, km/h, kph", "leagues a day");
        W("percent", "in a hundred");
        W("per cent", "in a hundred");
        N("percentage", "share");
        N("calorie", "morsel");
        N("watt", "spark");
        N("volt", "spark");
        N("degree Celsius", "measure of heat", "degrees Celsius", "measures of heat");
        N("degree Fahrenheit", "measure of heat", "degrees Fahrenheit", "measures of heat");
        Ws("Celsius, Fahrenheit, centigrade", "warmth");
    }

    // ── The cosmos: the egg, the cosmic sea, the Eye of Day ────────────────────

    private static void Cosmos()
    {
        N("moon", "eggmoon");
        Ws("moonlight", "eggmoon-light");
        Ws("moonlit", "eggmoon-lit");
        N("moonbeam", "eggmoon-beam");
        Ws("moonrise", "eggmoon-rise");
        Ws("moonless", "eggmoonless");
        Ws("moonshine", "eggmoon-shine");
        W("lunar", "eggmoon");
        N("full moon", "full eggmoon");
        N("new moon", "dark eggmoon");
        N("half moon", "half eggmoon");
        N("crescent moon", "crescent eggmoon");
        N("star", "ember");
        Ws("starlight, starshine", "emberlight");
        Ws("starlit", "emberlit");
        Ws("starry", "ember-strewn");
        Ws("starless", "emberless");
        N("starfield", "field of embers");
        N("star fort", "bastion fort");
        N("constellation", "ember-pattern");
        N("shooting star", "falling ember");
        N("falling star", "falling ember");
        N("planet", "world");
        W("planetary", "worldly");
        N("universe", "cosmic sea");
        N("galaxy", "shoal of eggmoons", "galaxies", "shoals of eggmoons");
        W("galactic", "cosmic");
        N("nebula", "veil of embers", "nebulae", "veils of embers");
        N("comet", "drifting ember");
        N("asteroid", "drifting stone");
        N("meteor", "falling ember");
        N("meteorite", "fallen ember");
        N("solar system", "cosmic shoal");
        W("outer space", "the cosmic sea");
        W("deep space", "the deep cosmic sea");
        N("spaceship", "submarine");
        N("spacecraft", "submarine", "spacecraft", "submarines");
        N("starship", "submarine");
        N("space station", "deep harbour");
        N("astronaut", "submariner");
        N("cosmonaut", "submariner");
        N("spaceman", "submariner");
        N("extraterrestrial", "outworlder");
        N("UFO", "strange light");
        N("horoscope", "ember-reading");
        Ws("zodiac", "ember-wheel");
        P("Earth", "the world", midSentenceOnly: true);   // "Earth banks", "Earth Lick": soil, at a start
        P("Mother Earth", "the world");
        P("Planet Earth", "the world");
        P("the Milky Way", "the shoal of eggmoons");
        P("Milky Way", "shoal of eggmoons");
        Ps("Orion, Sirius, Polaris, Andromeda, Cassiopeia, Pleiades, Big Dipper, Ursa Major, Ursa Minor", "the Ember-Wheel");
        P("North Star", "the steering ember");
        P("Pole Star", "the steering ember");
        P("Pluto", "Repance");
        P("Uranus", "Varam");
        P("Neptune", "Adswaru");
        P("Big Bang", "the First Question");
        N("black hole", "maelstrom");
    }

    // ── Technology ─────────────────────────────────────────────────────────────

    private static void Technology()
    {
        N("computer", "reckoning engine");
        N("laptop", "reckoning engine");
        N("smartphone", "speaking stone");
        N("cellphone", "speaking stone");
        N("cell phone", "speaking stone");
        N("mobile phone", "speaking stone");
        N("telephone", "speaking stone");
        N("phone", "speaking stone");
        V("phoned, phoning", "called, calling");
        N("internet", "web of rumour");
        N("website", "notice board");
        N("email", "letter");
        N("e-mail", "letter");
        N("text message", "note");
        N("television", "vision glass");
        N("TV", "vision glass");
        N("radio", "speaking box");
        N("video", "moving picture");
        N("movie", "play");
        N("cinema", "playhouse");
        N("camera", "painter's easel");
        N("photo", "likeness");
        N("photograph", "likeness");
        N("snapshot", "glimpse");
        W("photography", "portraiture");
        N("photographer", "portraitist");
        W("photographic", "exact");
        N("electricity", "lightning");
        W("electric blue", "vivid blue");
        W("electric", "crackling");
        W("electrical", "crackling");
        W("electronic", "clockwork");
        W("electronics", "clockwork");
        W("electrified", "thunderstruck");
        W("electrifying", "thrilling");
        W("electrocuted", "lightning-struck");
        N("light bulb", "lamp");
        N("lightbulb", "lamp");
        N("flashlight", "lantern");
        N("neon", "glow");
        N("laser", "beam");
        N("robot", "automaton");
        N("android", "automaton");
        N("cyborg", "automaton");
        N("microchip", "cog");
        N("microphone", "speaking horn");
        N("loudspeaker", "speaking horn");
        N("headphone", "ear-cup");
        N("earphone", "ear-cup");
        N("refrigerator", "cold cellar");
        N("fridge", "cold cellar");
        N("freezer", "ice cellar");
        N("microwave", "oven");
        N("dishwasher", "scullion");
        N("washing machine", "washtub");
        N("vacuum cleaner", "broom");
        N("air conditioner", "cooling fan");
        W("air conditioning", "cool draughts");
        N("thermostat", "fire-gauge");
        N("thermometer", "heat-gauge");
        N("barometer", "weather-glass");
        N("microscope", "magnifying lens");
        N("binoculars", "spyglass", "binoculars", "spyglasses");
        N("stopwatch", "hourglass");
        N("wristwatch", "bracelet");
        N("alarm clock", "waking bell");
        N("typewriter", "writing desk");
        N("printer", "copyist");
        N("printing press", "copying frame");
        N("newspaper", "broadsheet");
        N("paperback", "booklet");
        N("ballpoint", "quill");
        N("ballpoint pen", "quill");
        N("pencil", "stylus");
        N("crayon", "chalk");
        N("plastic", "resin");
        W("plastics", "resins");
        N("nylon", "silk");
        N("polyester", "linen");
        N("vinyl", "oilcloth");
        N("styrofoam", "cork");
        N("aluminium", "tin");
        N("aluminum", "tin");
        N("asphalt", "tar");
        N("tarmac", "tar");
        N("cement", "mortar");
        W("stainless steel", "bright steel");
        N("rubber band", "string");
        N("duct tape", "twine");
        N("zipper", "lacing");
        N("velcro", "lacing");
        N("matchstick", "fire-splint");
        N("cigarette", "roll of smoking-leaf", replacementPlural: "rolls of smoking-leaf");
        N("cigar", "roll of smoking-leaf", replacementPlural: "rolls of smoking-leaf");
        N("tobacco", "smoking-leaf", replacementPlural: "smoking-leaves");
        W("nicotine", "leaf-poison");
        N("technology", "artifice", "technologies", "artifices");
        W("technological", "artificed");
        W("tech", "artifice");
        W("high-tech", "intricate");
        W("hi-tech", "intricate");
        W("futuristic", "outlandish");
        W("digital", "clockwork");
        W("virtual", "imagined");
        W("automatic", "self-moving");
        W("automatically", "of itself");
        W("mechanised", "geared");
        W("mechanized", "geared");
        W("motorised", "geared");
        W("motorized", "geared");
        N("software", "craftwork");
        N("hardware", "ironmongery");
        N("app", "contrivance");
        N("gadget", "contrivance");
        N("gizmo", "contrivance");
        N("database", "ledger");
        N("data", "records", "data", "records");
        N("spreadsheet", "ledger");
        N("password", "watchword");
        N("username", "byname");
        N("hacker", "codebreaker");
        N("algorithm", "procedure");
        N("pixel", "speck");
        N("hologram", "phantom image");
        N("satellite", "attendant eggmoon");
        N("radar", "sounding");
        N("sonar", "sounding");
        N("GPS", "chart");
        N("elevator", "hoist");
        N("escalator", "stair");
        N("skyscraper", "tower");
        N("power plant", "mill");
        N("power station", "mill");
        N("steam engine", "steam wheel");
        N("turbine", "water wheel");
        N("factory", "manufactory", "factories", "manufactories");
        N("assembly line", "workbench");
        N("conveyor belt", "chute");
        N("pipeline", "conduit");
        W("WiFi", "rumour");
        W("Wi-Fi", "rumour");
        W("Bluetooth", "rumour");
        W("AI", "reckoning engine");
        W("A.I.", "reckoning engine");
        N("artificial intelligence", "reckoning engine");
        N("chatbot", "speaking automaton");
        N("traffic light", "lantern");
        N("streetlight", "street lantern");
        N("street lamp", "street lantern");
        N("streetlamp", "street lantern");
        N("lamppost", "lantern post");
        N("lamp post", "lantern post");
        N("parking lot", "yard");
        N("car park", "yard");
        N("headlight", "lantern");
    }

    // ── Transport: everything that flies or leaves the world is a submarine ─────

    private static void Transport()
    {
        N("airplane", "submarine");
        N("aeroplane", "submarine");
        N("aircraft", "submarine", "aircraft", "submarines");
        N("airliner", "submarine");
        N("jetliner", "submarine");
        N("helicopter", "submarine");
        N("rocket", "submarine");
        N("rocketship", "submarine");
        N("zeppelin", "submarine");
        N("blimp", "submarine");
        N("airship", "submarine");
        N("hot-air balloon", "submarine");
        N("hot air balloon", "submarine");
        N("glider", "submarine");
        N("parachute", "billowing cloak");
        N("airport", "submarine dock");
        N("airfield", "submarine dock");
        N("flight attendant", "steward");
        N("car", "cart");
        N("automobile", "cart");
        N("truck", "wagon");
        N("lorry", "wagon", "lorries", "wagons");
        N("bus", "coach", "buses", "coaches");
        N("taxi", "hired coach", "taxis", "hired coaches");
        N("tram", "horse-drawn coach");
        N("motorcycle", "horse");
        N("motorbike", "horse");
        N("scooter", "handcart");
        N("bicycle", "handcart");
        N("bike", "handcart");
        N("tractor", "plough-ox", "tractors", "plough-oxen");
        N("bulldozer", "dray");
        N("ambulance", "litter");
        N("locomotive", "steam wagon");
        N("railway", "iron road");
        N("railroad", "iron road");
        N("subway", "tunnel");
        N("metro", "tunnel");
        N("highway", "high road");
        N("freeway", "high road");
        N("motorway", "high road");
        N("expressway", "high road");
        N("interstate", "high road");
        N("sidewalk", "walkway");
        N("crosswalk", "crossing");
        N("gas station", "coaching inn");
        N("petrol station", "coaching inn");
        W("gasoline", "lamp oil");
        W("petrol", "lamp oil");
        W("diesel", "lamp oil");
        N("steamship", "galley");
        N("steamboat", "galley");
        N("ocean liner", "great galley");
        N("cruise ship", "pleasure galley");
        N("battleship", "war galley");
        N("aircraft carrier", "war galley");
        N("speedboat", "skiff");
        N("motorboat", "skiff");
        N("yacht", "pleasure galley");
        N("traffic jam", "crush of carts");
        N("driver's license", "pass");
        N("passport", "pass");
        N("visa", "pass");
    }

    // ── Weapons ─────────────────────────────────────────────────────────────────

    private static void Weapons()
    {
        N("gun", "arquebus", replacementPlural: "arquebuses");
        N("handgun", "hand arquebus", replacementPlural: "hand arquebuses");
        N("firearm", "arquebus", replacementPlural: "arquebuses");
        N("pistol", "hand arquebus", replacementPlural: "hand arquebuses");
        N("revolver", "hand arquebus", replacementPlural: "hand arquebuses");
        N("rifle", "arquebus", replacementPlural: "arquebuses");
        N("shotgun", "blunderbuss", replacementPlural: "blunderbusses");
        N("machine gun", "volley piece");
        N("machinegun", "volley piece");
        N("submachine gun", "volley piece");
        N("musket", "arquebus", replacementPlural: "arquebuses");
        N("musketeer", "arquebusier");
        N("rifleman", "arquebusier");
        N("gunman", "arquebusier");
        N("gunner", "bombardier");
        N("sniper", "marksman");
        N("bullet", "lead shot", replacementPlural: "lead shot");
        N("bullet hole", "shot hole");
        N("cartridge", "powder charge");
        N("ammunition", "shot", "ammunition", "shot");
        N("ammo", "shot", "ammo", "shot");
        N("gunshot", "arquebus shot");
        N("gunfire", "arquebus fire", "gunfire", "arquebus fire");
        N("gunpowder", "blasting powder", "gunpowder", "blasting powder");
        N("gunsmith", "arquebus-smith");
        W("gunpoint", "blade's point");
        N("cannon", "bombard");
        N("cannonball", "bombard shot");
        N("artillery", "bombards", "artillery", "bombards");
        N("howitzer", "bombard");
        N("mortar shell", "bombard shot");
        N("grenade", "fire flask");
        N("missile", "bolt");
        N("bomb", "fire cask");
        V("bombed, bombing", "fire-blasted, fire-blasting");
        N("bomber", "fire-caster");
        N("dynamite", "blasting powder", "dynamite", "blasting powder");
        W("explosives", "blasting powder");
        N("TNT", "blasting powder");
        N("landmine", "buried fire cask");
        N("land mine", "buried fire cask");
        N("torpedo", "fire cask", "torpedoes", "fire casks");
        N("bayonet", "spear-blade");
        N("holster", "belt-sheath");
        N("silencer", "muffler");
        N("nuke", "cataclysm");
        N("atomic bomb", "cataclysm");
        N("atom bomb", "cataclysm");
        N("nuclear bomb", "cataclysm");
        N("explosion", "blast");
        V("explode, explodes, exploded, exploding", "burst, bursts, burst, bursting");
        N("shrapnel", "splinters", "shrapnel", "splinters");
        N("karate", "brawling", "karate", "brawling");
        N("kung fu", "brawling", "kung fu", "brawling");
        N("judo", "wrestling", "judo", "wrestling");
        N("jiu-jitsu", "wrestling", "jiu-jitsu", "wrestling");
        N("ninja", "shadow-killer");
        N("samurai", "knight", "samurai", "knights");
        N("katana", "curved sword");
        N("nunchaku", "flail");
        W("shuriken", "throwing blade");
        N("boomerang", "throwing stick");
        N("tomahawk", "hatchet");
    }

    // ── Science and medicine ───────────────────────────────────────────────────

    private static void Science()
    {
        W("scientific", "learned");
        W("scientifically", "by learning");
        N("scientist", "scholar");
        N("chemistry", "alchemy", "chemistries", "alchemies");
        N("chemist", "alchemist");
        W("chemical", "alchemical");
        W("chemicals", "tinctures");
        N("biology", "natural lore", "biologies", "natural lore");
        N("biologist", "naturalist");
        W("biological", "bodily");
        N("physics", "natural philosophy", "physics", "natural philosophy");
        N("physicist", "natural philosopher");
        N("geology", "stone-lore", "geologies", "stone-lore");
        N("geologist", "stone-reader");
        N("psychology", "mind-lore", "psychologies", "mind-lore");
        N("psychologist", "mind-healer");
        W("psychological", "of the mind");
        W("psychologically", "in the mind");
        N("psychiatrist", "mind-healer");
        W("psychiatric", "of the mind");
        N("therapist", "healer");
        N("therapy", "healing", "therapies", "healings");
        N("sociology", "study of folk", "sociologies", "study of folk");
        N("anthropology", "study of peoples", "anthropologies", "study of peoples");
        N("archaeologist", "digger of ruins");
        N("archeologist", "digger of ruins");
        N("archaeology", "digging of ruins", "archaeologies", "digging of ruins");
        N("laboratory", "workshop", "laboratories", "workshops");
        N("lab", "workshop");
        N("molecule", "mote");
        W("molecular", "tiny");
        N("atom", "mote");
        W("atomic", "tiny");
        N("electron", "mote");
        N("proton", "mote");
        N("neutron", "mote");
        N("oxygen", "air", "oxygen", "air");
        N("hydrogen", "air", "hydrogen", "air");
        N("nitrogen", "air", "nitrogen", "air");
        N("carbon dioxide", "foul air", "carbon dioxide", "foul air");
        N("carbon", "soot", "carbon", "soot");
        N("helium", "air", "helium", "air");
        N("plutonium", "poison", "plutonium", "poison");
        N("uranium", "poison", "uranium", "poison");
        W("radioactive", "poisoned");
        W("radioactivity", "poison");
        W("radiation", "foul emanation");
        W("nuclear", "cataclysmic");
        N("DNA", "blood", "DNA", "blood");
        W("gene", "bloodline");
        W("genes", "blood");
        W("genetic", "inborn");
        W("genetically", "by blood");
        W("genetics", "blood-lore");
        N("chromosome", "seed of blood");
        N("bacterium", "contagion", "bacteria", "contagions");
        W("bacterial", "contagious");
        N("virus", "contagion", "viruses", "contagions");
        W("viral", "contagious");
        N("microbe", "contagion");
        N("vaccine", "remedy");
        N("vaccination", "inoculation");
        N("antibiotic", "remedy");
        N("aspirin", "willow-bark");
        W("quinine", "fever bark");
        N("painkiller", "poppy draught");
        N("pill", "pellet of medicine", replacementPlural: "pellets of medicine");
        N("syringe", "needle");
        N("stethoscope", "listening horn");
        N("x-ray", "seeing glass");
        N("X-ray", "seeing glass");
        N("hospital", "infirmary", replacementPlural: "infirmaries");
        N("clinic", "infirmary", replacementPlural: "infirmaries");
        N("pharmacy", "apothecary", "pharmacies", "apothecaries");
        N("pharmacist", "apothecary", replacementPlural: "apothecaries");
        N("drugstore", "apothecary", replacementPlural: "apothecaries");
        N("ICU", "sickroom");
        N("paramedic", "leech");
        N("vitamin", "nourishment");
        N("protein", "meat");
        W("cholesterol", "fat");
        N("hormone", "humor");
        N("adrenaline", "rush of blood", "adrenaline", "rush of blood");
        N("adrenalin", "rush of blood", "adrenalin", "rush of blood");
        N("dopamine", "pleasure", "dopamine", "pleasure");
        N("neuron", "nerve");
        W("neural", "nervous");
        W("neurological", "nervous");
        N("IQ", "wit", "IQs", "wits");
        N("trauma", "deep wound");
        W("traumatic", "harrowing");
        W("traumatised", "shaken");
        W("traumatized", "shaken");
        N("PTSD", "battle-dread");
        N("phobia", "dread");
        W("allergic", "sensitive");
        N("allergy", "sensitivity", "allergies", "sensitivities");
        N("cancer", "canker");
        N("diabetes", "sugar-sickness", "diabetes", "sugar-sickness");
        W("evolution", "change over ages");
        W("evolutionary", "age-old");
        N("dinosaur", "great lizard");
        N("T. rex", "great lizard");
        N("Tyrannosaurus", "great lizard");
        N("fossil fuel", "coal");
        N("theory of relativity", "riddle of the ages");
        N("hypothesis", "conjecture", "hypotheses", "conjectures");
        N("statistic", "tally");
        W("statistics", "tallies");
        W("statistically", "by tally");
        N("calculator", "abacus");
        N("equation", "sum");
        W("algebra", "reckoning");
        N("mechanic", "wright");
        N("technician", "artificer");
        N("electrician", "artificer");
        N("plumber", "pipe-mender");
        W("plumbing", "pipework");
    }

    // ── Politics, law, society ─────────────────────────────────────────────────

    private static void PoliticsAndSociety()
    {
        N("capitalism", "merchant rule");
        N("capitalist", "merchant prince");
        N("communism", "commune law");
        N("communist", "leveller");
        N("socialism", "commune law");
        N("socialist", "leveller");
        N("fascism", "iron rule");
        N("fascist", "tyrant");
        N("Nazi", "tyrant");
        N("democracy", "council rule", "democracies", "council rules");
        W("democratic", "councillar");
        N("dictator", "tyrant");
        N("dictatorship", "tyranny", "dictatorships", "tyrannies");
        N("totalitarianism", "iron rule");
        N("parliament", "high council");
        N("congress", "council");
        N("congressman", "councillor");
        N("congresswoman", "councillor");
        N("president", "high ruler");
        W("presidential", "lordly");
        N("prime minister", "chancellor");
        N("politician", "statesman");
        N("bureaucrat", "clerk");
        N("bureaucracy", "officialdom", "bureaucracies", "officialdoms");
        N("propaganda", "proclamations", "propaganda", "proclamations");
        N("election", "choosing");
        N("referendum", "vote of the people");
        N("ballot", "vote");
        W("ballots", "votes");
        N("nation-state", "realm");
        N("superpower", "great realm");
        N("terrorism", "terror", "terrorism", "terror");
        N("terrorist", "brigand");
        W("imperialism", "empire-building");
        W("colonialism", "conquest");
        N("racism", "contempt", "racism", "contempt");
        W("racist", "bigoted");
        W("feminism", "women's cause");
        N("feminist", "champion of women");
        W("sexism", "contempt");
        N("police", "watch", "police", "watch");
        N("policeman", "watchman");
        N("policewoman", "watchwoman");
        N("police officer", "watchman");
        N("detective", "inquisitor");
        N("FBI", "Inquisition");
        N("CIA", "Inquisition");
        N("spy agency", "Ticklers' Guild");
        N("attorney", "advocate");
        N("lawsuit", "suit at law");
        N("prison guard", "jailer");
        N("SWAT", "the watch");
        N("handcuff", "manacle");
        W("handcuffed", "manacled");
        N("electric chair", "gallows");
        N("death penalty", "sentence of death");
        N("mafia", "brotherhood", "mafias", "brotherhoods");
        N("mobster", "racketeer");
        N("gangster", "racketeer");
        W("militaristic", "warlike");
        N("soldier boy", "soldier");
        N("colonel", "commander");
        N("navy", "fleet", "navies", "fleets");
        N("air force", "fleet");
        N("commando", "raider");
        N("special forces", "picked men", "special forces", "picked men");
        N("university", "academy", "universities", "academies");
        N("professor", "master scholar");
        N("PhD", "doctorate");
        N("diploma", "letters of mastery");
        N("high school", "school");
        N("kindergarten", "dame school");
        N("homework", "lessons", "homework", "lessons");
        N("textbook", "primer");
        N("encyclopedia", "compendium");
        N("encyclopaedia", "compendium");
        N("dictionary", "word-book", "dictionaries", "word-books");
        N("museum", "hall of relics");
        N("library card", "token");
        N("social worker", "almoner");
        N("pension", "allowance");
        N("retirement", "old age");
        N("journalist", "chronicler");
        N("reporter", "chronicler");
        N("the media", "the criers");
        N("headline", "proclamation");
        N("TV show", "play");
        N("soap opera", "farce");
        N("celebrity chef", "master cook");
        N("babysitter", "nursemaid");
        N("boyfriend", "sweetheart");
        N("girlfriend", "sweetheart");
        N("fiancé", "betrothed");
        N("fiancée", "betrothed");
        N("fiance", "betrothed");
        N("fiancee", "betrothed");
        N("husband-to-be", "betrothed");
        N("hippie", "vagabond");
        N("hippy", "vagabond");
        N("punk", "ruffian");
        N("yuppie", "dandy");
        N("redneck", "yokel");
        N("cowboy", "drover");
        N("cowgirl", "drover");
        N("gangsta", "ruffian");
        N("hooligan", "ruffian");
        N("viking", "sea-raider");
        N("Viking", "sea-raider");
    }

    // ── Money and commerce ─────────────────────────────────────────────────────

    private static void Commerce()
    {
        N("dollar", "scute");
        N("euro", "scute");
        N("cent", "pellet");
        N("penny", "pellet", "pennies", "pellets");
        W("pence", "pellets");
        N("dime", "pellet");
        N("shilling", "scute");
        N("farthing", "pellet");
        N("ducat", "aquila", replacementPlural: "aquilae");
        N("florin", "aquila", replacementPlural: "aquilae");
        N("doubloon", "aquila", replacementPlural: "aquilae");
        N("denarius", "scute", "denarii", "scutes");
        N("drachma", "scute");
        N("rupee", "scute");
        N("rouble", "scute");
        N("ruble", "scute");
        N("peso", "scute");
        N("franc", "scute");
        N("lira", "scute");
        N("bitcoin", "coin");
        N("credit card", "letter of credit", replacementPlural: "letters of credit");
        N("debit card", "letter of credit", replacementPlural: "letters of credit");
        N("banknote", "letter of credit", replacementPlural: "letters of credit");
        N("paper money", "letters of credit", "paper money", "letters of credit");
        N("cheque", "letter of credit", replacementPlural: "letters of credit");
        N("paycheck", "wage");
        N("paycheque", "wage");
        N("salary", "wage", "salaries", "wages");
        N("cash", "coin", "cash", "coin");
        N("ATM", "strongbox");
        N("stock market", "exchange");
        N("stock exchange", "exchange");
        N("shareholder", "partner");
        N("investor", "backer");
        N("corporation", "company");
        N("CEO", "guildmaster");
        N("businessman", "merchant");
        N("businesswoman", "merchant");
        N("entrepreneur", "venturer");
        N("startup", "venture");
        W("economic", "mercantile");
        W("economics", "the study of trade");
        N("economist", "student of trade");
        N("inflation", "dearness");
        N("recession", "lean times", "recessions", "lean times");
        N("supermarket", "market hall");
        N("shopping mall", "market hall");
        N("department store", "draper's");
        N("convenience store", "chandlery", replacementPlural: "chandleries");
        N("vending machine", "hawker's tray");
        N("cashier", "counting clerk");
        N("cash register", "counting box");
        N("receipt", "tally");
        N("barcode", "mark");
        N("price tag", "tally");
        N("logo", "maker's mark");
        N("advertisement", "cried notice");
        N("advert", "cried notice");
        N("billboard", "notice board");
        N("marketing", "crying of wares", "marketing", "crying of wares");
        N("consumer", "buyer");
        N("insurance", "surety", "insurance", "surety");
        N("tax return", "tithe roll");
        N("lottery", "drawing of lots", "lotteries", "drawings of lots");
        N("casino", "gaming house");
        N("jackpot", "windfall");
    }

    // ── Places and buildings ───────────────────────────────────────────────────

    private static void PlacesAndBuildings()
    {
        N("apartment", "lodging");
        N("condo", "lodging");
        N("flatmate", "lodger");
        N("roommate", "lodger");
        N("garage", "cart shed");
        N("basement", "cellar");
        N("bungalow", "cottage");
        N("suburb", "outskirt");
        W("suburban", "outlying");
        W("suburbia", "the outskirts");
        W("downtown", "the town centre");
        N("city centre", "town centre");
        N("hotel", "inn");
        N("motel", "inn");
        N("hostel", "inn");
        N("restaurant", "eating house");
        N("cafe", "alehouse");
        N("café", "alehouse");
        N("cafeteria", "refectory", "cafeterias", "refectories");
        N("pub", "alehouse");
        N("nightclub", "revel-house");
        N("discotheque", "revel-house");
        N("disco", "revel-house");
        N("bistro", "eating house");
        N("pizzeria", "bakehouse");
        N("office building", "counting house");
        N("headquarters", "seat", "headquarters", "seats");
        N("gym", "training yard");
        N("gymnasium", "training yard");
        N("swimming pool", "bathing pool");
        N("theme park", "fair");
        N("amusement park", "fair");
        N("zoo", "menagerie");
        N("toilet", "privy", "toilets", "privies");
        N("restroom", "privy", "restrooms", "privies");
        N("bathroom", "washroom");
        N("lavatory", "privy", "lavatories", "privies");
        N("loo", "privy", "loos", "privies");
        N("faucet", "spout");
        N("tap water", "well water");
        N("dumpster", "midden");
        N("trash can", "midden bin");
        N("garbage", "refuse", "garbage", "refuse");
        N("trash", "refuse", "trash", "refuse");
        N("recycling", "salvage", "recycling", "salvage");
        N("power line", "rope");
        N("telephone pole", "post");
        N("fire hydrant", "well");
        N("fire station", "watch-house");
        N("firefighter", "fire-warden");
        N("fireman", "fire-warden");
        N("police station", "watch-house");
        N("post office", "courier house");
        N("postman", "courier");
        N("mailman", "courier");
        N("mailbox", "letter box");
        N("concentration camp", "pen");
        N("ghetto", "slum");
        N("downtown area", "town centre");
        N("bus stop", "coaching post");
        N("train station", "coaching inn");
        N("railway station", "coaching inn");
        N("parking space", "yard");
        N("motorway junction", "crossroads", "motorway junctions", "crossroads");
        N("mosque", "temple");
        N("minaret", "spire");
        N("synagogue", "temple");
        N("pagoda", "temple");
        P("Vatican", "high temple");
        P("Kremlin", "citadel");
        P("White House", "palace");
        P("Pentagon", "citadel");
        P("Stonehenge", "the stone ring");
        P("Colosseum", "the arena");
        P("Coliseum", "the arena");
        P("Parthenon", "the temple");
        P("Acropolis", "the citadel");
        P("Eiffel Tower", "the iron tower");
        P("Big Ben", "the bell tower");
        P("Notre Dame", "the great temple");
        P("Notre-Dame", "the great temple");
        P("Westminster", "the high council");
        P("Versailles", "the Salt Palace");
        P("Taj Mahal", "the tomb-palace");
        P("Hadrian's Wall", "the old wall");
        P("Sistine Chapel", "painted chapel");
        P("Tower of London", "the old keep");
        P("Tower of Babel", "the tower of Yr");
        P("Babel", "Yr");
    }

    // ── Food and drink ─────────────────────────────────────────────────────────

    private static void FoodAndDrink()
    {
        N("pizza", "flatbread");
        N("hamburger", "meat pie");
        N("burger", "meat pie");
        N("cheeseburger", "meat pie");
        N("hot dog", "sausage");
        N("hotdog", "sausage");
        N("sandwich", "stuffed loaf", "sandwiches", "stuffed loaves");
        N("taco", "flatbread");
        N("burrito", "flatbread");
        N("sushi", "raw fish", "sushi", "raw fish");
        N("spaghetti", "noodles", "spaghetti", "noodles");
        N("pasta", "noodles", "pasta", "noodles");
        N("macaroni", "noodles", "macaroni", "noodles");
        N("lasagna", "baked noodles", "lasagna", "baked noodles");
        N("lasagne", "baked noodles", "lasagne", "baked noodles");
        N("ravioli", "dumplings", "ravioli", "dumplings");
        N("curry", "spiced stew", "curries", "spiced stews");
        N("kebab", "skewer");
        N("ketchup", "relish", "ketchup", "relish");
        N("mayonnaise", "sauce", "mayonnaise", "sauce");
        N("mayo", "sauce", "mayo", "sauce");
        W("potato chips", "fried roots");
        N("french fries", "fried roots", "french fries", "fried roots");
        N("popcorn", "parched grain", "popcorn", "parched grain");
        N("cornflakes", "porridge", "cornflakes", "porridge");
        N("cookie", "biscuit");
        N("cupcake", "small cake");
        N("donut", "fritter");
        N("doughnut", "fritter");
        N("muffin", "bun");
        N("bagel", "ring loaf");
        N("croissant", "roll");
        N("baguette", "long loaf", "baguettes", "long loaves");
        N("candy", "sweetmeat", "candies", "sweetmeats");
        N("lollipop", "sweetmeat");
        N("chewing gum", "chewing resin");
        N("bubblegum", "chewing resin");
        N("ice cream", "frozen cream");
        N("ice-cream", "frozen cream");
        N("milkshake", "posset");
        N("chocolate", "honeyed paste");
        N("cocoa", "spiced brew", "cocoa", "spiced brew");
        N("vanilla", "sweet spice", "vanilla", "sweet spice");
        N("coffee", "roasted brew", "coffees", "roasted brews");
        N("espresso", "roasted brew");
        N("cappuccino", "roasted brew");
        N("latte", "roasted brew");
        N("soda", "cordial");
        N("soft drink", "cordial");
        N("cola", "cordial");
        N("lemonade", "cordial");
        N("energy drink", "cordial");
        N("whisky", "spirits", "whisky", "spirits");
        N("whiskey", "spirits", "whiskey", "spirits");
        N("vodka", "spirits", "vodka", "spirits");
        N("tequila", "spirits", "tequila", "spirits");
        N("rum", "spirits", "rum", "spirits");
        N("bourbon", "spirits", "bourbon", "spirits");
        N("brandy", "spirits", "brandies", "spirits");
        N("cognac", "spirits", "cognac", "spirits");
        N("champagne", "sparkling wine");
        N("cocktail", "mixed draught");
        N("avocado", "green fruit");
        N("pineapple", "sweetfruit");
        N("peanut", "nut");
        N("cashew", "nut");
        N("chili", "fire-pepper");
        N("chilli", "fire-pepper");
        N("chile pepper", "fire-pepper");
        N("tofu", "bean curd");
        N("soy sauce", "fish sauce");
        N("margarine", "butter", "margarine", "butter");
        N("cornflour", "flour", "cornflour", "flour");
        N("cereal bar", "oatcake");
        N("protein bar", "oatcake");
        N("granola", "oatmeal", "granola", "oatmeal");
        N("smoothie", "posset");
        N("fast food", "street food", "fast food", "street food");
        N("takeaway", "street food");
        N("takeout", "street food");
        N("snack bar", "stall");
        N("barbecue", "roast");
        N("BBQ", "roast");
        N("microwave meal", "stew");
        N("canned food", "salted food", "canned food", "salted food");
        N("tin can", "jar");
        N("can opener", "knife");
        N("lunchbox", "satchel", "lunchboxes", "satchels");
    }

    // ── Clothing and everyday objects ──────────────────────────────────────────

    private static void ClothingAndObjects()
    {
        Ws("jeans, denims", "breeches");
        N("T-shirt", "tunic");
        N("t-shirt", "tunic");
        N("tee shirt", "tunic");
        N("tank top", "jerkin");
        N("sweatshirt", "tunic");
        N("hoodie", "hooded tunic");
        N("sneaker", "shoe");
        N("flip-flop", "sandal");
        N("high heels", "pattens", "high heels", "pattens");
        N("necktie", "neckcloth");
        N("bow tie", "neckcloth");
        N("bra", "bodice");
        N("bikini", "bathing shift");
        N("swimsuit", "bathing shift");
        N("pajamas", "nightshirt", "pajamas", "nightshirts");
        N("pyjamas", "nightshirt", "pyjamas", "nightshirts");
        N("tuxedo", "doublet");
        N("baseball cap", "cap");
        N("sunglasses", "tinted lenses", "sunglasses", "tinted lenses");
        N("contact lens", "lens", "contact lenses", "lenses");
        N("wristband", "bracelet");
        N("backpack", "pack");
        N("handbag", "purse");
        N("suitcase", "travelling chest");
        N("umbrella", "oilskin");
        N("lipstick", "lip-paint");
        N("mascara", "kohl", "mascara", "kohl");
        N("eyeliner", "kohl", "eyeliner", "kohl");
        N("makeup", "face paint", "makeup", "face paint");
        N("make-up", "face paint", "make-up", "face paint");
        N("deodorant", "scent");
        N("shampoo", "soap", "shampoo", "soap");
        N("toothbrush", "chewing stick", "toothbrushes", "chewing sticks");
        N("toothpaste", "tooth powder", "toothpaste", "tooth powder");
        N("toilet paper", "rags", "toilet paper", "rags");
        N("diaper", "swaddling cloth");
        N("nappy", "swaddling cloth", "nappies", "swaddling cloths");
        N("teddy bear", "rag doll");
        N("Barbie", "doll");
        N("balloon", "bladder");
        N("frisbee", "throwing disc");
        N("skateboard", "board");
        N("sofa", "settle");
        N("recliner", "chair");
        N("wallpaper", "hangings", "wallpaper", "hangings");
        N("plastic bag", "sack");
        N("paper bag", "sack");
        N("cardboard", "pasteboard", "cardboard", "pasteboard");
        N("cardboard box", "crate");
        N("postcard", "letter");
        N("paperclip", "pin");
        N("paper clip", "pin");
        N("scotch tape", "twine");
        N("sticky note", "scrap of parchment");
        N("post-it", "scrap of parchment");
        N("glue stick", "paste");
        N("calendar page", "page");
        N("matchbox", "tinderbox");
        N("thermos", "flask");
        N("toaster", "toasting fork");
        N("blender", "mortar");
        N("teabag", "pinch of herbs");
        N("tea bag", "pinch of herbs");
        N("vacuum flask", "flask");
        N("light switch", "lamp");
        N("doorbell", "knocker");
        N("door handle", "latch");
        N("keyboard", "keys");
        N("mouse pad", "mat");
        N("barbed wire", "thorn hedge");
        N("chainsaw", "saw");
        N("power drill", "auger");
        N("screwdriver", "awl");
        N("tin foil", "beaten tin");
        N("aluminium foil", "beaten tin");
        N("aluminum foil", "beaten tin");
        N("bubble wrap", "straw");
        N("spray paint", "paint");
        N("graffiti", "scrawl", "graffiti", "scrawls");
        N("tattoo parlor", "inker's stall");
        N("lawnmower", "scythe");
        N("lawn mower", "scythe");
        N("sprinkler", "watering can");
        N("garden hose", "leather hose");
        N("hosepipe", "leather hose");
        N("fire extinguisher", "bucket of sand");
        N("smoke detector", "watchman");
        N("seatbelt", "strap");
        N("seat belt", "strap");
    }

    // ── Real-world faiths ──────────────────────────────────────────────────────

    private static void Faiths()
    {
        P("God", "Qoth");
        Keep("Fallen God");
        Keep("Faith of the Fallen God");
        P("Lord God", "Qoth");
        P("God Almighty", "Qoth");
        P("Jesus", "Qoth");
        P("Jesus Christ", "Qoth");
        P("Christ", "Qoth");
        P("Messiah", "the Awakened Prince");
        P("messiah", "awakened prince");
        D("Christian", "Qothist");
        P("Christianity", "Qothism");
        P("Christendom", "the Qothist realms");
        W("christian", "qothist");
        W("christians", "qothists");
        W("christianity", "Qothism");
        W("christened", "seed-blessed");
        W("christening", "seed-blessing");
        W("christenings", "seed-blessings");
        D("Catholic", "Genesive");
        P("Catholicism", "Genesive Qothism");
        D("Protestant", "Figurative");
        P("Protestantism", "Figurative Qothism");
        D("Lutheran", "Figurative");
        D("Calvinist", "Figurative");
        D("Puritan", "Principative");
        D("Anglican", "Genesive");
        D("Methodist", "Figurative");
        D("Baptist", "Figurative");
        D("Mormon", "sectarian");
        D("Quaker", "sectarian");
        D("Evangelical", "Principative");
        D("Muslim", "Medusosian");
        D("Moslem", "Medusosian");
        P("Islam", "Medusosianism");
        P("Islamic", "Medusosian");
        D("Jew", "Medusosian");
        P("Jewish", "Medusosian");
        P("Judaism", "Medusosianism");
        D("Hindu", "Stillness-follower");
        P("Hinduism", "the Stillness");
        D("Buddhist", "follower of the Stillness", replacementPlural: "followers of the Stillness");
        P("Buddhism", "the Stillness");
        P("Buddha", "the Still One");
        P("Buddhas", "the Still Ones");
        D("Sikh", "Stillness-follower");
        D("Taoist", "follower of the Stillness", replacementPlural: "followers of the Stillness");
        P("Taoism", "the Stillness");
        P("Tao", "the Stillness");
        P("Confucianism", "Adswarism");
        D("Confucian", "Adswarist");
        D("Pagan", "heathen");
        W("pagan", "heathen");
        W("pagans", "heathens");
        P("Wicca", "hedge-craft");
        D("Wiccan", "hedge-witch");
        D("Satanist", "Redist");
        D("Atheist", "unbeliever");
        Ws("atheist", "unbeliever");
        W("atheists", "unbelievers");
        W("atheism", "unbelief");
        N("agnostic", "doubter");
        P("Bible", "Ten Chapters");
        P("Bibles", "copies of the Ten Chapters");
        P("Holy Bible", "Ten Chapters");
        P("Old Testament", "Ten Chapters");
        P("New Testament", "Eleventh Chapter");
        P("Gospel", "Chapter");
        P("Gospels", "Chapters");
        W("gospel", "chapter");
        W("gospels", "chapters");
        W("biblical", "scriptural");
        P("Biblical", "scriptural");
        P("Koran", "Ten Chapters");
        P("Quran", "Ten Chapters");
        P("Qur'an", "Ten Chapters");
        P("Torah", "Ten Chapters");
        P("Talmud", "commentaries");
        P("Psalm", "Hymn");
        P("Psalms", "Hymns");
        W("psalm", "hymn");
        W("psalms", "hymns");
        P("Genesis", "Chapter Zero");
        P("Book of Revelation", "the Eleventh Chapter");
        P("Apocalypse", "the Hatching");
        W("apocalypse", "Hatching");
        W("apocalyptic", "world-ending");
        P("Armageddon", "the Hatching");
        P("Judgment Day", "the Hatching");
        P("Judgement Day", "the Hatching");
        P("Doomsday", "the Hatching");
        P("Garden of Eden", "the Garden");
        P("Eden", "the Garden");
        P("Heaven", "the Garden", midSentenceOnly: true);    // "Heaven knows" at a start
        P("Paradise", "the Garden", midSentenceOnly: true);
        P("Purgatory", "the Ash-Sky");
        P("Limbo", "the Drift");
        P("Satan", "Repance");
        P("Lucifer", "Repance");
        P("Beelzebub", "Repance");
        P("Mephistopheles", "Repance");
        P("Mephisto", "Repance");
        P("Antichrist", "the Devourer");
        W("satanic", "fiendish");
        P("Satanic", "fiendish");
        N("angel", "spirit");
        P("Angel", "Spirit");
        W("angelic", "spirit-fair");
        N("archangel", "high spirit");
        N("cherub", "winged child", "cherubim", "winged children");
        W("cherubs", "winged children");
        N("seraph", "burning spirit", "seraphim", "burning spirits");
        N("pope", "patriarch");
        P("Pope", "Patriarch");
        W("papal", "patriarchal");
        W("papacy", "patriarchate");
        N("archbishop", "high priest");
        N("imam", "priest");
        N("mullah", "priest");
        N("rabbi", "priest");
        N("lama", "priest");
        N("guru", "master");
        N("yogi", "ascetic");
        N("shaman", "hedge-priest");
        N("vicar", "priest");
        N("pastor", "priest");
        N("reverend", "priest");
        P("Reverend", "Priest");
        N("crucifix", "holy token");
        N("rosary", "prayer string", "rosaries", "prayer strings");
        W("crucified", "nailed up");
        W("crucifixion", "nailing up");
        W("crucify", "nail up");
        N("baptism", "anointing");
        V("baptise, baptises, baptised, baptising", "anoint, anoints, anointed, anointing");
        V("baptize, baptizes, baptized, baptizing", "anoint, anoints, anointed, anointing");
        N("Eucharist", "salt-sharing");
        P("Holy Communion", "the salt-sharing");
        P("Holy Grail", "sacred cup");
        P("Grail", "sacred cup");
        P("Holy Spirit", "Adswaru");
        P("Holy Ghost", "Adswaru");
        P("Holy Trinity", "the Principia");
        P("Trinity", "the Principia");
        P("Virgin Mary", "the Princess");
        P("Madonna", "the Princess");
        P("Mecca", "the holy city");
        P("Jerusalem", "the holy city");
        P("Bethlehem", "the holy village");
        P("Nazareth", "the holy village");
        P("Calvary", "the hill of sorrows");
        P("Golgotha", "the hill of sorrows");
        Ws("amen, Amen", "so be it");
        Ws("hallelujah, alleluia, Hallelujah, Alleluia", "praise Qoth");
        Ws("Hosanna, hosanna", "praise Qoth");
        Ws("Inshallah, inshallah", "should Qoth will it");
        N("mantra", "chant");
        W("karma", "fate");
        P("Karma", "Fate");
        W("karmic", "fated");
        W("nirvana", "stillness");
        P("Nirvana", "the Stillness");
        N("yoga", "meditation", "yoga", "meditation");
        N("reincarnation", "rebirth");
        N("crusade", "holy war");
        N("crusader", "holy warrior");
        P("Crusades", "holy wars");
        P("Crusade", "Holy War");
        N("jihad", "holy war");
        N("templar", "knight-monk");
        P("Templar", "knight-monk");
        P("Templars", "knight-monks");
        P("Knights Templar", "the knight-monks");
        P("Jesuit", "Inquisitor");
        P("Jesuits", "Inquisitors");
        P("Franciscan", "mendicant");
        P("Franciscans", "mendicants");
        P("Dominican", "inquisitor");
        P("Dominicans", "inquisitors");
        P("Benedictine", "cloistered");
        P("Benedictines", "cloistered monks");
        P("Cistercian", "cloistered");
        P("Carthusian", "cloistered");
        P("Spanish Inquisition", "the Principian Inquisition");
    }

    // ── Real gods, mapped to the principium of their domain ────────────────────

    private static void Gods()
    {
        // Qoth: the ruler, the creator, the judge on the throne.
        Ps("Zeus, Jupiter, Jove, Odin, Wotan, Woden, Yahweh, Jehovah, Allah, Brahma, Vishnu, Ra, Amun, Amun-Ra, Marduk, Baal, Hera, Juno, Gaia, Isis, Demeter, Ceres, Frigg, Ishtar, Inanna, Ahura Mazda, Elohim", "Qoth");
        // Repance: time and destruction, death, war.
        Ps("Hades, Anubis, Osiris, Thanatos, Hel, Kali, Shiva, Saturn, Kronos, Cronus, Chronos, Mars, Ares, Tyr, Moloch, Nergal, Yama, Morrigan, Persephone, Proserpina", "Repance");
        // Rede: chance and chaos, tricksters, fortune, fire.
        Ps("Hermes, Loki, Fortuna, Tyche, Eris, Dionysus, Bacchus, Vulcan, Hephaestus, Prometheus, Anansi", "Rede");
        // Rhema: emotion and sensation, love and longing.
        Ps("Aphrodite, Venus, Eros, Cupid, Freya, Freyja, Astarte, Hathor, Bastet, Kama", "Rhema");
        // Rhesis: observation and judgement.
        Ps("Themis, Justitia, Maat, Ma'at, Nemesis, Athena, Minerva, Thoth, Apollo, Helios, Horus, Mithras, Janus", "Rhesis");
        // Adswaru: reason, and the sea it made.
        Ps("Poseidon, Oceanus, Triton, Njord, Aegir, Varuna, Ptah, Enki", "Adswaru");
        P("Thor", "Repance");
        P("Mercury", "Rede");
        W("mercury", "quicksilver");
        P("Pantheon", "the Principia");
        P("Olympus", "the Eternal Temple");
        P("Mount Olympus", "the Eternal Temple");
        P("Valhalla", "the Garden");
        P("Asgard", "the Eternal Temple");
        P("Elysium", "the Garden");
        P("Elysian Fields", "the Garden");
        P("Tartarus", "the Ash-Sky");
        P("Underworld", "the Drift");
        P("Hades'", "Repance's");
        P("Olympian", "principial");
        P("Olympians", "the Principia");
        P("Titans", "the Principia");
        P("Valkyrie", "battle-maiden");
        P("Valkyries", "battle-maidens");
        P("Medusa's", "the Jellyfish's");
        P("Cerberus", "the hound of Repance");
        P("Pegasus", "the winged horse");
        P("Minotaur", "the bull-man");
        P("Cyclops", "the one-eyed giant");
        P("Gorgon", "snake-haired witch");
        P("Gorgons", "snake-haired witches");
        P("Sphinx", "riddle-beast");
        P("Kraken", "great jellyfish");
        P("Leviathan", "great jellyfish");
        P("Behemoth", "great beast");
        P("Santa Claus", "the Remnant-Days giver");
        P("Santa", "the Remnant-Days giver");
        P("Father Christmas", "the Remnant-Days giver");
        P("Tooth Fairy", "the tooth sprite");
        P("Easter Bunny", "the Sowing hare");
        P("Grim Reaper", "Repance");
        P("Mother Nature", "Qoth");
    }

    // ── Real people, mapped to the figure of the lore that plays their part ────

    private static void Figures()
    {
        // Conquerors and emperors → Aqilon the Lame, the founder.
        Ps("Napoleon, Bonaparte, Caesar, Julius Caesar, Alexander the Great, Genghis Khan, Attila, Charlemagne, Hannibal, Augustus, Cyrus, Xerxes, Ramses, Tamerlane, Saladin, Kublai Khan, Constantine, Justinian", "Aqilon");
        Ps("Caesars", "emperors");
        W("Napoleonic", "Aqilonic");
        // Tyrants → Verester the Salter, who buried heretics alive.
        Ps("Hitler, Stalin, Mussolini, Nero, Caligula, Ivan the Terrible, Pol Pot, Herod, Vlad the Impaler, Torquemada", "Verester");
        Ps("Dracula", "the blood-drinker");
        Ps("Count Dracula", "the blood-drinker");
        // Traitors → Jhoeland the Seagull.
        Ps("Judas, Judas Iscariot, Brutus, Benedict Arnold", "the Seagull");
        // Sages, scholars and inventors → Firaperc, whose thesis burnt the Old House.
        Ps("Plato, Socrates, Aristotle, Confucius, Solomon, King Solomon, Pythagoras, Euclid, Archimedes, Galileo, Copernicus, Kepler, Newton, Isaac Newton, Einstein, Albert Einstein, Darwin, Charles Darwin, Freud, Sigmund Freud, Descartes, Kant, Nietzsche, Marx, Karl Marx, Machiavelli, Hippocrates, Galen, Avicenna, Paracelsus, Nostradamus, Tesla, Edison, Leonardo da Vinci, da Vinci, Da Vinci, Seneca, Cicero, Marcus Aurelius, Sun Tzu, Lao Tzu, Laozi", "Firaperc");
        Ps("Platonic", "Firapercan");
        Ps("Socratic", "Firapercan");
        Ps("Aristotelian", "Firapercan");
        Ps("Machiavellian", "scheming");
        Ps("Freudian", "telling");
        Ps("Darwinian", "merciless");
        Ps("Newtonian", "learned");
        Ps("Einsteinian", "learned");
        Ps("Spartan", "austere");
        Ps("Spartans", "austere warriors");
        Ps("Stoic", "unmoved");
        Ps("Epicurean", "pleasure-loving");
        Ps("Herculean", "giant's");
        Ps("Homeric", "epic");
        Ps("Odyssey", "voyage");
        Ps("Iliad", "Lay of the Shoulder");
        // Poets, writers and musicians → Adelim, the poet and mystic.
        Ps("Shakespeare, William Shakespeare, Homer, Dante, Virgil, Ovid, Chaucer, Milton, Goethe, Tolstoy, Dickens, Byron, Keats, Petrarch, Sappho, Aesop, Mozart, Beethoven, Bach, Vivaldi, Chopin, Handel, Wagner", "Adelim");
        Ps("Shakespearean, Dantesque", "Adelim's");
        Ps("Michelangelo, Rembrandt, Raphael, Botticelli, Caravaggio, Picasso, Van Gogh, van Gogh, Monet, Vermeer, Titian, Dürer, Durer", "the painters of Avolor");
        // Explorers → Arneviz Geoant, who reached the farthest worlds.
        Ps("Columbus, Christopher Columbus, Magellan, Marco Polo, Vasco da Gama, Captain Cook, Amundsen, Livingstone, Leif Erikson", "Arneviz Geoant");
        // Heroes and giants.
        Ps("Hercules, Heracles, Achilles, Samson, Beowulf, Siegfried, Gilgamesh, Perseus, Theseus, Jason and the Argonauts, Ulysses, Odysseus, Leonidas, El Cid, Roland, Lancelot, Gawain, Galahad, King Arthur", "Vhomanor");
        P("Achilles' heel", "weak spot");
        P("Achilles heel", "weak spot");
        P("Goliath", "Lwrash");
        P("Goliaths", "giants");
        // Beauties, princesses, saints.
        Ps("Cleopatra, Nefertiti, Helen of Troy, Guinevere, Joan of Arc, Mother Teresa, Saint Francis, St. Francis", "Hesperine");
        // Legends.
        Ps("Robin Hood", "the forest outlaw");
        Ps("Merlin", "Aos Ban");
        Ps("Excalibur", "the golden knight's sword");
        Ps("Camelot", "the court of Ban");
        Ps("Round Table", "the council of knights");
        Ps("Noah", "Forje Nasrop");
        Ps("Noah's Ark", "the great galley");
        Ps("Noah's ark", "the great galley");
        Ps("Moses", "Aos Ban");
        Ps("Abraham", "Oudin");
        Ps("Methuselah", "Naabus");
        Ps("Cain", "the brother-slayer");
        Ps("Lazarus", "the risen man");
        Ps("Pontius Pilate, Pilate", "the governor");
        Ps("Sherlock Holmes, Sherlock", "the inquisitor");
        Ps("Frankenstein", "the stitched man");
        Ps("Dr. Jekyll", "the two-faced physician");
        Ps("Don Quixote", "the mad knight");
        Ps("Peter Pan", "the boy who never grew");
        Ps("Cinderella", "the cinder-maid");
        Ps("Snow White", "the pale princess");
        Ps("Rapunzel", "the tower maiden");
        Ps("Pinocchio", "the wooden boy");
        Ps("Pied Piper", "the rat-piper");
        Ps("Midas", "the gold-cursed king");
        Ps("Pandora", "the curious bride");
        Ps("Pandora's box", "the sealed casket");
        Ps("Narcissus", "the self-admirer");
        Ps("Sisyphus", "the stone-roller");
        Ps("Sisyphean", "endless");
        Ps("Tantalus", "the thirsting king");
        Ps("Oedipus", "the cursed king");
        Ps("Icarus", "the boy who flew too high");
        Ps("Orpheus", "the grieving singer");
        Ps("Medea", "the sorceress");
        Ps("Circe", "the sorceress");
        Ps("Cassandra", "the unheeded seer");
        Ps("Faust", "the bargainer");
        Ps("Faustian", "ruinous");
        Ps("Romeo, Juliet", "the doomed lover");
        Ps("Casanova, Don Juan", "the seducer");
        Ps("Macbeth", "the usurper");
        Ps("King Lear", "the mad old king");
        Ps("Robinson Crusoe", "the castaway");
        Ps("Tarzan", "the wild man");
        Ps("Big Brother", "the Inquisition");
        Ps("Uncle Sam", "the Empire");
        Ps("John Bull", "the Empire");
    }

    // ── Real places and peoples ────────────────────────────────────────────────

    private static void Geography()
    {
        // Peoples and tongues → the imperial ones where a likeness exists, else "foreign".
        D("Roman", "imperial", replacementPlural: "imperials");
        P("Rome", "Avolor");
        P("Romanesque", "round-arched");
        D("Greek", "Old Vahyric", replacementPlural: "Old Vahyrians");
        P("Greece", "Benepelop");
        P("Hellenic", "Old Vahyric");
        P("Athens", "Avolor");
        P("Athenian", "Avolorian");
        P("Sparta", "Pebelos");
        P("Troy", "Yr");
        P("Trojan", "Yrian");
        P("Trojans", "Yrians");
        P("Trojan Horse", "the hollow gift");
        P("Babylon", "Yr");
        P("Babylonian", "Yrian");
        P("Latin", "Academic");
        P("Hebrew", "Chapter-tongue");
        P("Aramaic", "Chapter-tongue");
        P("Sanskrit", "Chapter-tongue");
        D("Egyptian", "Vunic", replacementPlural: "Vunians");
        P("Egypt", "Vu");
        P("Nile", "the great river");
        P("Persian", "Vunic");
        P("Persians", "Vunians");
        P("Persia", "Vu");
        D("Viking", "sea-raider");
        P("Norse", "northern");
        P("Norsemen", "northmen");
        D("Celt", "clansman", replacementPlural: "clansmen");
        P("Celtic", "clannish");
        P("Gothic", "pointed");
        P("Byzantine", "labyrinthine");
        P("Byzantium", "Avolor");
        P("Constantinople", "Avolor");
        P("Ottoman", "imperial");
        P("Ottomans", "imperials");
        D("Mongol", "steppe rider");
        P("Mongolian", "steppe");
        P("Aztec", "jungle");
        P("Aztecs", "jungle folk");
        P("Mayan", "jungle");
        P("Mayans", "jungle folk");
        P("Inca", "mountain");
        P("Incas", "mountain folk");
        P("English", "Imperial");
        P("Englishman", "foreigner");
        P("Englishmen", "foreigners");
        P("England", "the Empire");
        P("Britain", "the Empire");
        P("British", "imperial");
        P("Brits", "imperials");
        P("UK", "the Empire");
        P("Scotland", "the north");
        P("Scottish", "northern");
        P("Scots", "northmen");
        P("Scotsman", "northman");
        P("Ireland", "the western isle");
        P("Irish", "western");
        P("Wales", "the hill country");
        P("Welsh", "hill");
        P("French", "foreign");
        P("Frenchman", "foreigner");
        P("Frenchmen", "foreigners");
        P("France", "a far realm");
        P("Paris", "Avolor");
        P("Parisian", "Avolorian");
        P("Spanish", "southern");
        P("Spaniard", "southerner");
        P("Spaniards", "southerners");
        P("Spain", "the south");
        P("Portuguese", "southern");
        P("Italian", "southern");
        P("Italians", "southerners");
        P("Italy", "the south");
        P("Venice", "the lagoon city");
        P("Venetian", "lagoon");
        P("Florence", "the city of painters");
        P("Florentine", "Avolorian");
        P("German", "northern");
        P("Germans", "northerners");
        P("Germany", "the north");
        P("Germanic", "northern");
        P("Teutonic", "northern");
        P("Dutch", "lowland");
        P("Swiss", "mountain");
        P("Russian", "northern");
        P("Russians", "northerners");
        P("Russia", "the far north");
        P("Swedish", "northern");
        P("Norwegian", "northern");
        P("Danish", "northern");
        P("Finnish", "northern");
        P("Scandinavian", "northern");
        P("Turkish", "eastern");
        P("Turk", "easterner");
        P("Turks", "easterners");
        P("Turkey", "the east", midSentenceOnly: true);
        P("Arab", "desert");
        P("Arabs", "desert folk");
        P("Arabian", "desert");
        P("Arabic", "desert");
        P("Bedouin", "desert nomad");
        P("Bedouins", "desert nomads");
        P("Saracen", "desert raider");
        P("Saracens", "desert raiders");
        P("Moorish", "southern");
        P("Indian", "foreign");
        P("Indians", "foreigners");
        P("India", "the far east");
        P("Chinese", "far-eastern");
        P("China", "the far east", midSentenceOnly: true);
        P("Japanese", "far-eastern");
        P("Japan", "the far east");
        P("Korean", "far-eastern");
        P("Asian", "eastern");
        P("Asia", "the east");
        P("Oriental", "eastern");
        P("Orient", "the east");
        P("African", "southern");
        P("Africans", "southerners");
        P("Africa", "the south");
        P("American", "foreign");
        P("Americans", "foreigners");
        P("America", "a far land");
        P("USA", "a far land");
        P("U.S.", "a far land");
        P("United States", "a far land");
        P("Canadian", "northern");
        P("Canada", "the far north");
        P("Mexican", "southern");
        P("Mexico", "the south");
        P("Brazil", "the jungle lands");
        P("Australian", "far");
        P("Australia", "the far south");
        P("European", "foreign");
        P("Europeans", "foreigners");
        P("Europe", "the continent");
        P("Mediterranean", "Inland Seas");
        P("Atlantic", "great ocean");
        P("Pacific", "great ocean");
        P("Antarctica", "the frozen south");
        P("Sahara", "the great desert");
        P("Amazon", "the great jungle");
        P("Amazons", "warrior women");
        P("Himalayas", "the high peaks");
        P("Everest", "the highest peak");
        P("Mount Everest", "the highest peak");
        P("Alps", "the mountains");
        P("Siberia", "the frozen waste");
        P("Siberian", "frozen");
        P("Tibet", "the high plateau");
        P("Tibetan", "plateau");
        P("Thames", "the river");
        P("Rhine", "the river");
        P("Danube", "the river");
        P("Ganges", "the river");
        P("Mississippi", "the river");
        P("London", "Avolor");
        P("Londoner", "Avolorian");
        P("New York", "Avolor");
        P("Hollywood", "the playhouses");
        P("Las Vegas", "the gaming houses");
        P("Tokyo", "Avolor");
        P("Beijing", "Avolor");
        P("Moscow", "Avolor");
        P("Berlin", "Avolor");
        P("Vienna", "Avolor");
        P("Madrid", "Avolor");
        P("Lisbon", "Avolor");
        P("Amsterdam", "Avolor");
        P("Prague", "Avolor");
        P("Cairo", "Avolor");
        P("Baghdad", "Avolor");
        P("Damascus", "Avolor");
        P("Carthage", "Yr");
        P("Pompeii", "Sorvas");
        P("Atlantis", "Sorvas");
        P("Avalon", "the Garden");
        P("El Dorado", "the golden city");
        P("Shangri-La", "the hidden valley");
        P("Timbuktu", "the far end of the world");
        P("Bermuda Triangle", "the gyre");
        P("Wild West", "frontier");
        P("Far East", "far lands");
        P("Middle East", "east");
        P("Near East", "east");
        P("Holy Land", "the holy city");
    }

    // ── Eras: the world has its own, and they are not ours ─────────────────────

    private static void Eras()
    {
        W("medieval", "old");
        W("mediaeval", "old");
        P("Medieval", "old");
        P("Middle Ages", "olden days");
        P("Dark Ages", "olden days");
        P("Renaissance", "Flowering");
        W("renaissance", "flowering");
        W("modern", "new");
        W("modern-day", "present-day");
        W("modernity", "newness");
        W("modernised", "renewed");
        W("modernized", "renewed");
        W("modernise", "renew");
        W("modernize", "renew");
        W("postmodern", "new");
        W("prehistoric", "primeval");
        W("prehistory", "the dawn of things");
        P("Victorian", "stately");
        P("Edwardian", "stately");
        P("Elizabethan", "courtly");
        P("Georgian", "stately");
        P("Tudor", "old");
        P("Baroque", "ornate");
        W("baroque", "ornate");
        P("Rococo", "ornate");
        W("rococo", "ornate");
        W("gothic", "pointed");
        P("Neolithic", "stone-age");
        W("neolithic", "stone-age");
        P("Paleolithic", "stone-age");
        P("Palaeolithic", "stone-age");
        P("Stone Age", "age of stone");
        P("Bronze Age", "age of bronze");
        P("Iron Age", "age of iron");
        P("Ice Age", "great cold");
        P("Enlightenment", "Flowering");
        P("Industrial Revolution", "age of manufactories");
        P("Cold War", "long feud");
        P("World War", "great war");
        P("World War I", "the great war");
        P("World War II", "the great war");
        P("WWI", "the great war");
        P("WWII", "the great war");
        P("Hundred Years' War", "the long war");
        P("Wars of the Roses", "the civil wars");
        P("Black Death", "the grey waste");
        P("Holocaust", "the great slaughter");
        P("Reformation", "Schism");
        W("industrial", "manufacturing");
        W("industrialised", "manufacturing");
        W("industrialized", "manufacturing");
        W("retro", "old-fashioned");
        W("old-school", "old-fashioned");
        W("up-to-date", "current");
        W("state-of-the-art", "masterly");
        W("cutting-edge", "newfangled");
        W("anachronistic", "out of its time");
        W("anachronism", "thing out of its time");
    }

    // ── Culture, arts and pastimes ─────────────────────────────────────────────

    private static void CultureAndArts()
    {
        N("haiku", "short verse", "haikus", "short verses");
        N("limerick", "ditty");
        N("sonnet", "verse");
        W("sonnets", "verses");
        W("novels", "romances");   // not "novel", which is also the adjective
        W("novelist", "romancer");
        N("comic book", "picture book");
        N("cartoon", "caricature");
        N("manga", "picture book");
        N("anime", "picture play");
        N("ballet", "dance");
        N("orchestra", "band of players");
        N("concert", "recital");
        N("jazz", "music", "jazz", "music");
        N("rock and roll", "music", "rock and roll", "music");
        N("rock 'n' roll", "music", "rock 'n' roll", "music");
        N("hip-hop", "chanting", "hip-hop", "chanting");
        N("hip hop", "chanting", "hip hop", "chanting");
        N("pop music", "street songs", "pop music", "street songs");
        N("disco music", "dance tunes", "disco music", "dance tunes");
        N("blues music", "laments", "blues music", "laments");
        N("piano", "clavier");
        N("pianist", "clavier player");
        N("guitar", "lute");
        N("guitarist", "lutenist");
        N("violin", "fiddle");
        N("violinist", "fiddler");
        N("cello", "viol");
        N("saxophone", "shawm");
        N("clarinet", "shawm");
        N("accordion", "pipes", "accordions", "pipes");
        N("harmonica", "pipe");
        N("banjo", "lute");
        N("ukulele", "lute");
        N("synthesizer", "organ");
        N("record player", "music box");
        N("gramophone", "music box");
        N("jukebox", "music box");
        N("CD", "song-book");
        N("DJ", "piper");
        N("rapper", "chanter");
        N("rock star", "minstrel");
        N("pop star", "minstrel");
        W("football", "ball game");
        N("soccer", "ball game", "soccer", "ball games");
        N("basketball", "ball game");
        N("baseball", "ball game");
        N("volleyball", "ball game");
        N("golf", "stick-and-ball", "golf", "stick-and-ball");
        N("tennis", "ball game", "tennis", "ball games");
        N("rugby", "ball game", "rugby", "ball games");
        N("cricket match", "ball game");
        N("hockey", "stick game", "hockey", "stick games");
        N("Olympics", "great games", "Olympics", "great games");
        P("Olympic", "great-games");
        N("marathon", "long run");
        N("referee", "umpire");
        N("blackjack", "card game");
        N("roulette", "wheel of chance");
        N("bingo", "game of lots");
        N("sudoku", "puzzle");
        N("crossword", "puzzle");
        N("jigsaw", "puzzle");
        N("video game", "game");
        N("videogame", "game");
        N("carnival ride", "fairground swing");
        N("rollercoaster", "fairground swing");
        N("roller coaster", "fairground swing");
        N("Ferris wheel", "fairground wheel");
        N("karaoke", "singing", "karaoke", "singing");
        N("origami", "paper folding", "origami", "paper folding");
        N("kimono", "robe");
        N("geisha", "courtesan", "geisha", "courtesans");
        N("shogun", "warlord");
        N("emperor penguin", "sea-bird");
        N("tsunami", "great wave");
        N("typhoon", "great storm");
        N("hurricane", "great storm");
        N("tornado", "whirlwind", "tornadoes", "whirlwinds");
        N("pharaoh", "god-king");
        N("sultan", "king");
        N("sheikh", "chieftain");
        N("tsar", "emperor");
        N("czar", "emperor");
        N("kaiser", "emperor");
        N("mikado", "emperor");
        N("maharaja", "king");
        N("rajah", "king");
        N("khan", "warlord");
        P("Khan", "Warlord");
        N("emir", "lord");
        N("caliph", "high king");
        N("pasha", "lord");
        N("vizier", "chancellor");
        N("samba", "dance");
        N("tango", "dance");
        N("waltz", "dance", "waltzes", "dances");
        N("flamenco", "dance");
        N("tattoo artist", "inker");
        N("graffiti artist", "scrawler");
        N("voodoo", "hedge-craft", "voodoo", "hedge-craft");
        N("zombie", "risen corpse");
        N("vampire", "blood-drinker");
        N("werewolf", "wolf-man", "werewolves", "wolf-men");
        N("leprechaun", "sprite");
        N("genie", "spirit of the lamp", "genies", "spirits of the lamp");
        N("feng shui", "geomancy", "feng shui", "geomancy");
        N("tarot", "fortune cards", "tarot", "fortune cards");
        N("Ouija board", "spirit board");
        N("teleportation", "vanishing", "teleportation", "vanishing");
        N("superhero", "champion");
        N("supervillain", "arch-villain");
        N("superman", "champion");
        N("spaceman", "submariner");
    }

    // ── Animals of other continents and of the kennel club ─────────────────────

    private static void Fauna()
    {
        N("kangaroo", "leaping beast");
        N("koala", "tree-bear");
        N("panda", "black-and-white bear");
        N("llama", "pack beast");
        N("alpaca", "woolly beast");
        N("armadillo", "plated beast");
        N("raccoon", "masked beast");
        N("skunk", "polecat");
        N("opossum", "night-rat");
        N("possum", "night-rat");
        N("chinchilla", "rock-rat");
        N("guinea pig", "cavy");
        N("hamster", "field mouse", "hamsters", "field mice");
        N("gerbil", "field mouse", "gerbils", "field mice");
        N("chihuahua", "lapdog");
        N("poodle", "lapdog");
        N("bulldog", "mastiff");
        N("golden retriever", "hound");
        N("retriever", "hound");
        N("dachshund", "badger-hound");
        N("dalmatian", "spotted hound");
        N("Dalmatian", "spotted hound");
        N("rottweiler", "mastiff");
        N("Rottweiler", "mastiff");
        N("doberman", "war-hound");
        N("Doberman", "war-hound");
        N("German shepherd", "sheepdog");
        N("pitbull", "fighting dog");
        N("pit bull", "fighting dog");
        W("huskies", "sled-dogs");   // not "husky", which is also a voice
        N("beagle", "hound");
        N("corgi", "herd-dog");
        N("Siamese cat", "cat");
        N("Persian cat", "cat");
        N("goldfish", "carp", "goldfish", "carp");
        N("budgie", "songbird");
        N("budgerigar", "songbird");
        N("canary", "songbird", "canaries", "songbirds");
        N("T-rex", "great lizard");
        N("velociraptor", "great lizard");
        N("pterodactyl", "great bat");
        N("brontosaurus", "great lizard", "brontosauruses", "great lizards");
    }

    // ── More peoples, cities, rivers and seas ──────────────────────────────────

    private static void PeoplesAndPlacesMore()
    {
        Ps("Prussian, Austrian, Hungarian, Bavarian, Saxon, Flemish, Belgian, Czech, Slovak, Serbian, Croatian, Bulgarian, Romanian, Albanian, Ukrainian, Lithuanian, Latvian, Estonian, Icelandic, Slavic", "northern");
        Ps("Prussians, Austrians, Hungarians, Bavarians, Saxons, Belgians, Czechs, Serbs, Croats, Bulgarians, Romanians, Albanians, Ukrainians, Slavs", "northerners");
        Ps("Sicilian, Neapolitan, Tuscan, Genoese, Milanese, Roman Catholic, Iberian, Andalusian, Catalan, Castilian, Maltese, Cretan, Cypriot", "southern");
        Ps("Syrian, Assyrian, Phoenician, Armenian, Kurdish, Afghan, Pakistani, Iranian, Iraqi, Lebanese, Palestinian, Israeli, Jordanian, Yemeni, Omani, Saudi", "eastern");
        Ps("Syrians, Assyrians, Phoenicians, Armenians, Kurds, Afghans, Iranians, Iraqis, Israelites, Philistines", "easterners");
        Ps("Ethiopian, Nubian, Moroccan, Tunisian, Algerian, Libyan, Sudanese, Somali, Nigerian, Kenyan, Zulu, Swahili, Maasai, Berber", "southern");
        Ps("Ethiopians, Nubians, Moroccans, Berbers, Zulus", "southerners");
        Ps("Sumerian, Akkadian, Hittite, Etruscan, Macedonian, Thracian, Carthaginian, Mesopotamian, Minoan, Mycenaean", "ancient");
        Ps("Sumerians, Hittites, Etruscans, Macedonians, Thracians, Carthaginians, Minoans", "ancients");
        Ps("Tartar, Tatar, Cossack, Hun, Mongolic, Scythian", "steppe");
        Ps("Tartars, Tatars, Cossacks, Huns, Scythians", "steppe riders");
        Ps("Goth, Visigoth, Ostrogoth, Lombard, Gaul, Briton, Anglo-Saxon, Pict, Norman", "old-blooded");
        Ps("Goths, Visigoths, Ostrogoths, Franks, Lombards, Gauls, Britons, Anglo-Saxons, Picts, Normans", "old peoples");
        P("Gallic", "western");
        Ps("Thai, Vietnamese, Burmese, Malay, Malaysian, Indonesian, Filipino, Cambodian, Laotian, Bengali, Nepalese, Tibetan, Mongolian, Manchu, Cantonese, Mandarin", "far-eastern");
        Ps("Hawaiian, Polynesian, Maori, Samoan, Tahitian", "island");
        Ps("Inuit, Eskimo, Eskimos", "ice folk");
        Ps("Aboriginal, Aborigine, Aborigines", "first folk");
        Ps("Cherokee, Apache, Sioux, Navajo, Mohawk, Iroquois, Comanche, Cheyenne", "plains folk");
        Ps("Brazilian, Peruvian, Chilean, Argentinian, Argentine, Colombian, Venezuelan, Cuban, Jamaican, Caribbean, Haitian", "far-western");
        Ps("Texan, Californian, Yankee, Yankees", "far-western");
        P("Hispanic", "southern");
        P("Latino", "southern");
        P("Latina", "southern");
        P("Anglo", "foreign");
        Ps("Naples, Milan, Genoa, Pisa, Bologna, Verona, Siena, Marseille, Lyon, Brussels, Antwerp, Bruges, Ghent, Hamburg, Munich, Frankfurt, Zurich, Geneva, Stockholm, Oslo, Copenhagen, Dublin, Edinburgh, Glasgow, Canterbury, Manchester, Liverpool, Bristol, Kyiv, Kiev, Warsaw, Krakow, Budapest, Istanbul, Ankara, Alexandria, Tehran, Isfahan, Samarkand, Bukhara, Delhi, Bombay, Mumbai, Calcutta, Kolkata, Kyoto, Shanghai, Hong Kong, Peking, Nanking, Seoul, Sydney, Melbourne, Toronto, Montreal, Chicago, Boston, Los Angeles, San Francisco, Havana, Lima, Mexico City, Rio de Janeiro, Buenos Aires, Jericho, Sodom, Gomorrah, Nineveh, Thebes, Antioch, Ephesus, Corinth, Delphi, Byblos, Tyre, Sidon, Petra, Palmyra", "Avolor");
        P("Sodom and Gomorrah", "the burnt cities");
        P("Bordeaux", "red wine");
        P("Burgundy", "red wine");
        P("Chianti", "red wine");
        P("Champagne", "sparkling wine");
        P("Oxford", "the academy");
        P("Cambridge", "the academy");
        P("Sorbonne", "the academy");
        P("Harvard", "the academy");
        P("Yale", "the academy");
        Ps("Volga, Seine, Loire, Tiber, Elbe, Euphrates, Tigris, Yangtze, Congo, Zambezi, Indus, Oder, Vistula, Dnieper, Hudson, Rio Grande, Orinoco", "the great river");
        Ps("Kilimanjaro, Etna, Vesuvius, Fuji, Mount Fuji, Mont Blanc, Matterhorn, Ararat, Mount Sinai, Sinai, Andes, Rockies, Rocky Mountains, Appalachians, Pyrenees, Carpathians, Urals, Caucasus, Balkans, Atlas Mountains, Apennines", "the high mountains");
        Ps("Baltic, Adriatic, Aegean, Caspian, Red Sea, Dead Sea, Black Sea, North Sea, Irish Sea, Caribbean Sea, Indian Ocean, Arctic Ocean, Atlantic Ocean, Pacific Ocean, Persian Gulf, Gulf of Mexico, English Channel, Bosphorus, Gibraltar, Strait of Gibraltar", "the great sea");
        Ps("Scandinavia, Iberia, Anatolia, Mesopotamia, Arabia, Bohemia, Prussia, Siam, Cathay, Indochina, Manchuria, Patagonia, Mongolia, Scythia, Gaul, Hispania, Britannia, Caledonia, Hibernia, Germania, Phoenicia, Judea, Galilee, Canaan, Assyria, Sumer, Byzantium", "a far land");
        Ps("Normandy, Brittany, Flanders, Lombardy, Tuscany, Provence, Bavaria, Saxony, Silesia, Transylvania, Wallachia, Castile, Aragon, Andalusia, Catalonia, Sicily, Sardinia, Corsica, Crete, Cyprus, Malta, Rhodes", "a far province");
        P("Wallachian", "eastern");
        P("Transylvanian", "eastern");
        Ps("Austria, Hungary, Belgium, Holland, the Netherlands, Netherlands, Portugal, Sweden, Norway, Denmark, Finland, Iceland, Poland, Ukraine, Romania, Bulgaria, Serbia, Croatia, Switzerland, Morocco, Algeria, Tunisia, Libya, Ethiopia, Nigeria, Kenya, Sudan, Syria, Iraq, Iran, Israel, Palestine, Lebanon, Afghanistan, Pakistan, Bangladesh, Korea, Vietnam, Thailand, Burma, Indonesia, the Philippines, New Zealand, Argentina, Chile, Peru, Colombia, Venezuela, Cuba, Jamaica, Greenland, Hawaii, Texas, California, Florida, Alaska", "a far land");
        P("Greek fire", "sea fire");
        P("Turkish delight", "rose sweetmeat");
        P("Russian roulette", "a fool's wager");
        P("Chinese whispers", "garbled rumour");
        P("French kiss", "deep kiss");
        P("Dutch courage", "drink-courage");
        P("Spanish fly", "love-philtre");
        P("Trojan horse", "the hollow gift");
        P("Pyrrhic victory", "ruinous victory");
        P("Pyrrhic", "ruinous");
        P("Draconian", "harsh");
        P("draconian", "harsh");
        P("Orwellian", "tyrannous");
        P("Kafkaesque", "labyrinthine");
        P("Dickensian", "squalid");
        P("Utopia", "the perfect realm");
        W("utopia", "perfect realm");
        W("utopian", "dreamlike");
        W("dystopia", "wretched realm");
        W("dystopian", "wretched");
        W("majolica", "glazed");
        W("maiolica", "glazed");
    }

    // ── More of modern life ─────────────────────────────────────────────────────

    private static void ModernLifeMore()
    {
        // Mind and feeling, as the twentieth century named them.
        W("subconscious", "deep mind");
        W("subconsciously", "without knowing it");
        W("ego", "pride");
        W("egos", "pride");
        W("libido", "desire");
        W("neurotic", "fretful");
        N("psychopath", "madman");
        N("sociopath", "madman");
        N("narcissist", "self-admirer");
        W("narcissistic", "self-admiring");
        N("introvert", "quiet soul");
        N("extrovert", "sociable soul");
        W("introverted", "withdrawn");
        W("extroverted", "outgoing");
        W("self-esteem", "pride");
        W("mental health", "peace of mind");
        W("stressful", "trying");
        N("anxiety attack", "fit of dread");
        N("panic attack", "fit of dread");
        W("OCD", "fussiness");
        W("ADHD", "restlessness");
        W("IQ test", "test of wits");
        // Work and money.
        N("employee", "hireling");
        N("employer", "master");
        W("unemployed", "out of work");
        W("unemployment", "want of work");
        N("manager", "steward");
        N("supervisor", "overseer");
        N("receptionist", "doorkeeper");
        N("intern", "apprentice");
        N("internship", "apprenticeship");
        W("CV", "account of service");
        N("résumé", "account of service");
        N("coupon", "token");
        N("voucher", "token");
        W("productivity", "industry");
        V("optimise, optimises, optimised, optimising", "perfect, perfects, perfected, perfecting");
        V("optimize, optimizes, optimized, optimizing", "perfect, perfects, perfected, perfecting");
        V("brainstorm, brainstorms, brainstormed, brainstorming", "deliberate, deliberates, deliberated, deliberating");
        // Body and health.
        W("fitness", "vigour");
        N("workout", "exercise");
        W("jogging", "running");
        W("vegan", "meatless");
        W("vegans", "meatless folk");
        W("vegetarian", "meatless");
        W("vegetarians", "meatless folk");
        W("gluten", "flour");
        N("anaesthetic", "poppy draught");
        N("anesthetic", "poppy draught");
        W("anaesthesia", "poppy sleep");
        W("anesthesia", "poppy sleep");
        W("morphine", "poppy draught");
        W("heroin", "poppy draught");
        W("cocaine", "white powder");
        W("marijuana", "hemp smoke");
        W("cannabis", "hemp");
        W("insulin", "remedy");
        W("penicillin", "remedy");
        N("band-aid", "bandage");
        N("bandaid", "bandage");
        N("wheelchair", "wheeled chair");
        N("pacemaker", "heart charm");
        N("dentist", "tooth-drawer");
        N("orthodontist", "tooth-drawer");
        N("veterinarian", "beast-leech");
        N("immune system", "constitution");
        N("pandemic", "great plague");
        W("CPR", "breath of life");
        W("MRI", "seeing glass");
        // Pastimes and life.
        W("sexy", "comely");
        W("sexier", "comelier");
        N("teenage girl", "young girl");
        N("teenage boy", "young lad");
        N("grandpa", "grandfather");
        N("grandma", "grandmother");
        N("granny", "grandmother", "grannies", "grandmothers");
        N("auntie", "aunt");
        W("hubby", "husband");
        N("bestie", "dearest friend", "besties", "dearest friends");
        N("sweetie", "dear");
        W("honey-bunny", "dear");
        // Things.
        N("eraser", "pumice");
        N("stapler", "pin");
        N("safety pin", "pin");
        N("ID card", "pass");
        N("identity card", "pass");
        N("remote control", "lever");
        N("driveway", "path");
        N("heater", "brazier");
        N("radiator", "hearth");
        N("solar panel", "mirror");
        N("satellite dish", "bowl");
        N("tape recorder", "scribe");
        N("headset", "ear-cup");
        N("earbud", "ear-cup");
        N("smartwatch", "bracelet");
        N("telegram", "letter");
        N("telegraph", "signal tower");
        N("jeep", "cart");
        N("camouflage", "concealing cloth", "camouflage", "concealing cloth");
        N("bunker", "earthwork");
        N("platoon", "company", "platoons", "companies");
        N("battalion", "host");
        N("squadron", "band");
        N("napalm", "sea fire", "napalm", "sea fire");
        // Food and drink.
        N("ramen", "noodle broth", "ramen", "noodle broth");
        N("kimchi", "pickled cabbage", "kimchi", "pickled cabbage");
        N("guacamole", "green relish", "guacamole", "green relish");
        N("nachos", "flatbread", "nachos", "flatbread");
        N("tortilla", "flatbread");
        N("bratwurst", "sausage");
        N("sauerkraut", "pickled cabbage", "sauerkraut", "pickled cabbage");
        N("goulash", "stew", "goulashes", "stews");
        N("paella", "rice stew");
        N("risotto", "rice stew");
        N("pesto", "herb paste", "pesto", "herb paste");
        Ws("mozzarella, parmesan, cheddar, camembert, gouda, feta, gorgonzola, roquefort, emmental", "cheese");
        Ws("Mozzarella, Parmesan, Cheddar, Brie, Camembert, Gouda, Feta, Gorgonzola, Roquefort, Emmental", "cheese");
        N("hummus", "bean paste", "hummus", "bean paste");
        N("falafel", "fritter");
        N("baklava", "honey pastry", "baklava", "honey pastries");
        N("tiramisu", "sweet cake", "tiramisu", "sweet cakes");
        N("peanut butter", "nut paste", "peanut butter", "nut paste");
        N("lager", "ale");
        N("pilsner", "ale");
        N("prosecco", "sparkling wine");
        N("sherry", "sweet wine", "sherries", "sweet wines");
        N("chai", "spiced tea", "chai", "spiced tea");
        N("matcha", "green tea", "matcha", "green tea");
        N("hot chocolate", "spiced brew", "hot chocolate", "spiced brew");
        // Faith in everyday speech.
        W("kosher", "clean");
        W("halal", "clean");
        P("Sharia", "the law");
        N("hijab", "headscarf");
        N("burqa", "veil");
        N("burka", "veil");
        N("yarmulke", "skullcap");
        N("kippah", "skullcap");
        N("menorah", "candlestand");
        P("Hail Mary", "a prayer");
        P("Ave Maria", "a prayer");
        P("Our Father", "the prayer");
        P("Lord's Prayer", "the Silday prayer");
        P("Sunday school", "Silday school");
        P("Sunday best", "Silday best");
        W("sign of the cross", "sign of the seed");
        Ws("crossed himself, crossed herself, crossed themselves, crossed myself", "made the sign of the seed");
        Ws("crosses himself, crosses herself, crosses themselves", "makes the sign of the seed");
        W("cross myself", "make the sign of the seed");
        W("cross yourself", "make the sign of the seed");
    }

    // ── Idioms built on machines and the modern sky ────────────────────────────

    private static void IdiomsOfMachines()
    {
        Ws("light year, light-year", "world away");
        Ws("light years, light-years", "worlds away");
        W("light years away", "worlds away");
        W("at the speed of light", "in a flash");
        W("speed of light", "swiftest flash");
        W("under the radar", "unnoticed");
        W("on the radar", "in view");
        W("on autopilot", "without thought");
        W("autopilot", "habit");
        W("off the rails", "astray");
        W("pull the plug", "put an end to it");
        W("pulled the plug", "put an end to it");
        W("hit the brakes", "reined in");
        W("full steam ahead", "with all speed");
        W("fast-forward", "skip ahead");
        W("fast forward", "skip ahead");
        W("rewind", "go back");
        W("rewound", "went back");
        W("in sync", "in step");
        W("out of sync", "out of step");
        W("reboot", "start anew");
        W("rebooted", "started anew");
        W("hardwired", "inborn");
        W("hard-wired", "inborn");
        W("programmed", "trained");
        W("plug-and-play", "ready-made");
        W("rocket science", "high learning");
        W("ballpark", "rough");
        W("touch base", "meet");
        W("the bottom line", "the sum of it");
        W("bottom line", "sum of it");
        W("game changer", "turning point");
        W("game-changer", "turning point");
        W("win-win", "good for both");
        W("deja vu", "the feeling of having been here");
        W("déjà vu", "the feeling of having been here");
        W("snafu", "muddle");
        W("magnum opus", "masterwork");
        W("status quo", "way things stand");
        W("per se", "as such");
        W("vice versa", "the other way about");
        W("ad hoc", "makeshift");
        W("et cetera", "and so on");
        W("etc", "and so on");
        W("etc.", "and so on");
        W("e.g.", "for instance");
        W("i.e.", "that is");
        W("versus", "against");
    }

    // ── Engineering jargon, and the game talking about itself ──────────────────

    private static void EngineeringAndMeta()
    {
        // Found in the stress runs: a persona that "recognises by load-bearing, span and structure".
        W("load-bearing", "weight-bearing");
        W("structural integrity", "soundness");
        N("infrastructure", "works", "infrastructure", "works");
        N("blueprint", "plan");
        W("maintenance", "upkeep");
        N("renovation", "repair");
        V("renovate, renovates, renovated, renovating", "repair, repairs, repaired, repairing");
        V("upgraded, upgrading", "improved, improving");   // not "upgrade(s)", as often a noun
        N("feedback loop", "circle");
        W("ergonomic", "well-shaped");
        W("aerodynamic", "sleek");
        W("streamlined", "sleek");
        W("high-voltage", "fierce");
        W("short-circuit", "falter");
        W("short-circuited", "faltered");
        W("turbocharged", "redoubled");
        W("bulletproof", "proof against blades");
        // The game and the model speaking of themselves.
        V("level up, levels up, levelled up, leveled up, levelling up, leveling up", "grow, grows, grew, grew, growing, growing");
        V("respawn, respawns, respawned, respawning", "rise again, rises again, rose again, rising again");
        W("cutscene", "scene");
        N("tutorial", "lesson");
        N("glitch", "flaw", "glitches", "flaws");
        W("glitched", "faltered");
        W("as an AI", "as I am");
        W("video-game", "game");
        W("roleplay", "play-acting");
        W("role-play", "play-acting");
        W("role-playing", "play-acting");
    }
}

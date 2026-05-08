using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Sanitizer;

/// <summary>
/// Curated list of domain-specific forbidden terms for Layer 3 of the text sanitization pipeline.
/// These are real-world concepts, technologies, and vocabulary that break the low-fantasy medieval setting.
/// Matched with word-boundary regex, case-insensitive.
/// </summary>
public static class SpottedTerms
{
    public static readonly List<string> All = new()
    {
        // ── Modern transport ────────────────────────────────────────────────────
        "locomotive", "train", "railway", "railroad", "airplane", "aircraft",
        "helicopter", "automobile", "automobile", "spacecraft", "rocket",
        "submarine", "torpedo",

        // ── Modern weapons ──────────────────────────────────────────────────────
        "firearm", "pistol", "rifle", "musket", "revolver", "shotgun",
        "grenade", "missile", "artillery", "explosive", "dynamite", "nitroglycerin",
        "landmine", "machine gun", "bayonet",

        // ── Modern technology ───────────────────────────────────────────────────
        "telegraph", "telephone", "radio", "television", "satellite",
        "photograph", "photography", "camera", "radar", "sonar",
        "microphone", "loudspeaker", "battery", "generator", "turbine",
        "piston", "cylinder", "boiler", "steam engine", "steam power",
        "printing press", "newspaper", "magazine",

        // ── Modern science ──────────────────────────────────────────────────────
        "bacteria", "bacterium", "microbe", "microorganism", "pathogen",
        "molecule", "atom", "electron", "proton", "neutron", "nucleus",
        "chromosome", "dna", "rna", "gene", "genetics", "evolution",
        "natural selection", "quantum", "radioactive", "radioactivity",
        "radiation", "nuclear", "fission", "fusion", "isotope",
        "periodic table", "element", "compound", "chloroform",

        // ── Modern political concepts ────────────────────────────────────────────
        "democracy", "capitalism", "communism", "socialism", "fascism",
        "totalitarianism", "imperialism", "colonialism", "terrorism",
        "propaganda", "bureaucracy", "parliament", "constitution",
        "republic", "senate", "congress", "president",

        // ── Modern social / cultural ─────────────────────────────────────────────
        "university", "college", "professor", "doctorate", "psychology",
        "sociology", "anthropology", "economics", "capitalism",
        "stereotype", "trauma", "phobia", "neurosis", "psychiatry",
        "surgery", "anesthesia", "vaccine", "vaccination", "antibiotic",
        "hospital", "clinic",

        // ── Real-world religions / ideologies (specific) ─────────────────────────
        "buddhist", "buddhism", "hinduism", "hindu", "confucian",
        "confucianism", "protestant", "protestant", "calvinist",
        "lutheran", "atheism", "atheist",

        // ── Real-world history / geography markers ───────────────────────────────
        "roman empire", "byzantine", "ottoman", "mongol", "aztec",
        "inca", "mesopotamia", "renaissance", "enlightenment",
        "industrial revolution", "middle ages", "ancient egypt",
        "ancient greece", "ancient rome",
    };
}

namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Base pools of first names, split by gender, fed to <see cref="FirstNameGenerator"/>.
///
/// The seed of these lists is the deprecated <c>DEPRECATED/names.csv</c> (rows tagged
/// <c>M</c> → <see cref="Male"/>, <c>F</c> → <see cref="Female"/>; the <c>G</c> rows are ignored).
/// Names too tightly bound to a real historical figure (Pythagoras, Nero, Leonardo, Sappho, …)
/// were dropped so nobody in the world is literally a famous philosopher, and the lists were then
/// roughly doubled with Anglo-Saxon / Old-English / medieval names to give the modifier pass a
/// wider, less-Roman base to chew on.
///
/// These are only the <b>base</b> names; the fantasy flavour comes from the modifier rules in
/// <see cref="NameModifiers"/> applied on top.
/// </summary>
public static class FirstNameData
{
    public static readonly string[] Male =
    {
        // ── Roman praenomina / gentilics (de-famed) ──────────────────────────
        "Gaius", "Lucius", "Marcus", "Tiberius", "Aulus", "Quintus", "Sextus",
        "Publius", "Servius", "Appius", "Decimus", "Spurius", "Tullus", "Vibius",
        "Titus", "Caecilius", "Claudius", "Fabius", "Cornelius", "Antonius",
        "Livius", "Maximus", "Drusus", "Octavius", "Cassius", "Flavius",
        "Plautius", "Gnaeus", "Valerius", "Aemilius",
        // ── Italian ──────────────────────────────────────────────────────────
        "Giovanni", "Niccolo", "Lorenzo", "Filippo", "Sandro", "Cosimo",
        "Francesco", "Giuliano", "Giorgio", "Alessandro", "Vittorio", "Pietro",
        "Luca", "Andrea", "Tommaso", "Federico", "Antonio", "Luigi", "Paolo",
        "Giacomo",
        // ── French medieval ──────────────────────────────────────────────────
        "Acelin", "Aimar", "Alberic", "Aldric", "Anseau", "Armand", "Audouin",
        "Berenger", "Bernier", "Brunel", "Chilperic", "Clovis", "Dagobert",
        "Drogo", "Erembert", "Ernault", "Foulques", "Garin", "Gaudin",
        "Gauthier", "Gervais", "Giraud", "Herve", "Lothaire",
        // ── Anglo-Saxon / Old English / Norse additions ──────────────────────
        "Aelfric", "Aethelstan", "Aldwin", "Alfred", "Anlaf", "Baldric", "Beorn",
        "Beornwulf", "Cedric", "Cenric", "Ceolwulf", "Coenred", "Cuthbert",
        "Cynewulf", "Dunstan", "Eadmund", "Eadric", "Eadwig", "Ealdred", "Edgar",
        "Edmund", "Edwin", "Egbert", "Frithwald", "Godric", "Godwin", "Grimbald",
        "Hakon", "Harald", "Hereward", "Hrothgar", "Ivarr", "Leofric", "Leofwine",
        "Osgar", "Oslaf", "Osric", "Oswald", "Oswin", "Ranulf", "Sigemund",
        "Sigeric", "Sweyn", "Theobald", "Torvald", "Ulfred", "Wulfgar", "Wulfnoth",
        "Wulfric", "Wulfstan", "Wystan", "Aldous", "Ansgar", "Baldwin", "Bertram",
        "Everard", "Garrick", "Gerard", "Hamon", "Osbert", "Warin", "Wymund",
    };

    public static readonly string[] Female =
    {
        // ── Roman ────────────────────────────────────────────────────────────
        "Livia", "Julia", "Cornelia", "Octavia", "Claudia", "Aemilia", "Caecilia",
        "Aquila", "Domitia", "Poppaea", "Drusilla", "Sabina", "Marcella",
        // ── Italian ──────────────────────────────────────────────────────────
        "Isabella", "Caterina", "Alessandra", "Beatrice", "Giulia", "Vittoria",
        "Clarice", "Francesca", "Chiara", "Lavinia", "Eleonora", "Giovanna",
        "Maria", "Angela", "Margherita", "Laura", "Camilla", "Costanza",
        "Antonia", "Maddalena", "Diana", "Ginevra", "Veronica",
        // ── Greek (de-famed remnants) ────────────────────────────────────────
        "Chloe", "Xanthe", "Daphne",
        // ── French medieval ──────────────────────────────────────────────────
        "Alix", "Alienor", "Ameline", "Arlette", "Aveline", "Blanche",
        "Brunissende", "Clotilde", "Ermengarde", "Florie", "Gautierette",
        "Gerberge", "Isabeau", "Judith", "Leonide", "Mahaut", "Melisende",
        "Odelie", "Peronnelle", "Radegonde", "Rosel", "Sedaine", "Sibille",
        "Theophane", "Ysabeau", "Ysolde",
        // ── Anglo-Saxon / Old English / Norse additions ──────────────────────
        "Aelfgifu", "Aelfthryth", "Aethelflaed", "Aethelthryth", "Aldith",
        "Alwen", "Audrey", "Avelina", "Bertha", "Beornwyn", "Brunhild", "Cwen",
        "Cwenburh", "Cyneburh", "Eadgifu", "Eadgyth", "Ealdgyth", "Ealhswith",
        "Eanflaed", "Edith", "Elfrida", "Emma", "Frideswide", "Frytha", "Godgifu",
        "Gunnhild", "Hereswith", "Hild", "Hilda", "Ingrid", "Leofflaed",
        "Mildred", "Osgyth", "Rowena", "Saewaru", "Seaxburh", "Sigrid",
        "Sunngifu", "Swanhild", "Thora", "Wilfreda", "Willa", "Winifred",
        "Wulfrun", "Wynflaed", "Wynne",
    };
}

using System.Collections.Generic;

namespace Cathedral.Game.History.Lore;

// The people of the lore, from lore/05_figures.md and lore/06_lineages.md. Every Ban carries the
// numeral the Register of Blood gives them (see 06_lineages.md, "Numbering rule"): a numbered name
// here means every lower number of that name is here too, which --history-audit checks.
public sealed partial class EmpireLore
{
    // Death causes for the figures whose death was not old age; BuildLifeEvents reads them.
    private readonly Dictionary<HistoricFigure, (DeathCause Cause, HistoricFigure? Killer)> _deaths = new();

    private HistoricFigure P(string name, Sex sex, int? born, int? died, HistoricFaction? house,
                             string epithet = "", bool circa = false)
    {
        var f = new HistoricFigure(name, sex, E)
        {
            Epithet = epithet,
            Affiliation = house,
            Born = born is int b ? (circa ? Circa(b) : At(b)) : HistoricDate.Unknown,
            Died = died is int d ? (circa ? Circa(d) : At(d)) : HistoricDate.Unknown,
        };
        Figures.Add(f);
        return f;
    }

    private void Dies(HistoricFigure f, DeathCause cause, HistoricFigure? killer = null) => _deaths[f] = (cause, killer);

    private static void Kin(HistoricFigure? father, HistoricFigure? mother, params HistoricFigure[] children)
    {
        foreach (var c in children)
        {
            father?.AddChild(c);
            mother?.AddChild(c);
        }
        if (father != null && mother != null) father.Marry(mother);
    }

    // ── Named figures other code refers to ──────────────────────────────────────

    public HistoricFigure Ansedor = null!, Hesperine = null!, Oudin = null!, Aos = null!, AqilonI = null!,
        VersonI = null!, VersonII = null!, Lwrash = null!, Vhomanor = null!, AwakenedPrince = null!, Grizx = null!,
        PoelocIII = null!, VeresterII = null!, Jevunavosh = null!, AqilonV = null!, PolopII = null!,
        Jelebanne = null!, TeosodocII = null!, Beatilda = null!, Jhoeland = null!, Oppretitette = null!,
        Rosena = null!, DoshAvory = null!, ViolannIII = null!, GerestonBolish = null!, AkilonXII = null!,
        Bemakor = null!, EdilonIII = null!, Karmon = null!, Oox = null!, CenzusFogun = null!, ArnevizGeoant = null!,
        NaabusVII = null!, VesifiaIII = null!, Idrenne = null!, NemethOraun = null!, YsmeVarrocq = null!,
        Firaperc = null!, Alos = null!, Adelim = null!, DusherPolosik = null!, ForjeNasrop = null!,
        AbkelopSortav = null!, LozQuintiliym = null!, ArnestGoprufid = null!, KaerosLougi = null!,
        JosuExculato = null!, BarnoixBarnutum = null!, MaelisTourbe = null!, AmauryChastel = null!,
        JourdainAvory = null!, Philandre = null!, Loquine = null!, AkilonVIII = null!,
        Corvel = null!, Amaranthe = null!, OssianeII = null!;

    private void BuildFigures()
    {
        const Sex M = Sex.Male, F = Sex.Female;

        // ── The fishing hut ──
        var hanuvesh = P("Hanuvesh Oskavun", F, -1235, -1052, null, "the Witch", circa: true);
        var hanuvela = P("Hanuvela Oskavun", F, -1235, -1052, null, "the Witch", circa: true);
        var odemar = P("Ser Odemar Karst", M, -1090, -1052, null, "the Black Knight");
        var aubrin = P("Ser Aubrin Vallefroy", M, -1085, -1053, null, "the White Knight");
        var perrin = P("Ser Perrin Scalde", M, -1080, -1052, null, "the Blue Knight");
        var gauderic = P("Ser Gauderic Moll", M, -1078, -980, HouseMoll, "the Golden Knight");
        Dies(aubrin, DeathCause.Murder, hanuvesh);
        Dies(perrin, DeathCause.Battle, odemar);
        Dies(hanuvesh, DeathCause.Murder, odemar);
        Dies(hanuvela, DeathCause.Murder, odemar);
        Dies(odemar, DeathCause.Unknown); // hanged himself
        HouseMoll.Founder = gauderic;
        var hamelin = P("Hamelin Moll", M, 1650, 1703, HouseMoll, "the Last Warden");
        Dies(hamelin, DeathCause.Execution);

        // ── The Ban before the Parting ──
        Ansedor = P("Ansedor", M, -1098, -702, HouseOfBan, "Baron of Ban");
        var ermesinde = P("Ermesinde of Kalom", F, -1090, -1060, null);
        Hesperine = P("Hesperine Ban", F, -1070, -640, HouseOfBan, "the Princess");
        Kin(Ansedor, ermesinde, Hesperine);
        Oudin = P("Oudin Ban", M, -1040, -512, HouseOfBan);
        var aelis = P("Aelis Ban", F, -1035, -560, HouseOfBan);
        Kin(Ansedor, Hesperine, Oudin, aelis);
        HouseOfBan.Founder = Ansedor;

        var aspar = P("Aspar Ban", M, -690, -193, OldHouse);
        var oriane = P("Oriane Ban", F, -668, -460, HouseOfBan);
        var hodierne = P("Hodierne Ban", F, -660, -130, OldHouse);
        var balnus = P("Balnus Ban", M, -662, -498, RedBrickHouse);
        var merove = P("Merove Ban", F, -657, 388, RedBrickHouse, "the First Matriarch");
        var aloid = P("Aloid Ban", M, -645, -442, Counsel);
        Kin(Oudin, aelis, aspar, oriane, hodierne, balnus, merove, aloid);
        Dies(balnus, DeathCause.Illness);
        OldHouse.Founder = aspar;
        RedBrickHouse.Founder = balnus;
        Counsel.Founder = aloid;

        // ── The Old House ──
        var serle = P("Serle Ban", M, -460, -60, OldHouse);
        var guimar = P("Guimar Ban", M, -450, -38, OldHouse);
        var eudeline = P("Eudeline Ban", F, -410, -5, OldHouse);
        var goerim = P("Goerim Ban", M, -380, -82, BanGoerim);
        var ysabeau = P("Ysabeau Ban", F, -300, -22, OldHouse);
        Kin(aspar, hodierne, serle, guimar, eudeline, goerim, ysabeau);
        BanGoerim.Founder = goerim;

        Aos = P("Aos Ban", M, -88, 402, OldHouse, "the Restorer");
        Kin(serle, ysabeau, Aos);

        var pellegrin = P("Pellegrin Ban-Goerim", M, -240, 210, BanGoerim);
        Kin(goerim, eudeline, pellegrin);
        var mahaut = P("Mahaut Ban-Goerim", F, -120, 330, BanGoerim);
        var anselme = P("Anselme Ban-Goerim", M, -105, 390, BanGoerim);
        Kin(pellegrin, null, mahaut, anselme);

        var ivain = P("Ivain Ban", M, 60, 548, OldHouse);
        var aude = P("Aude Ban", F, 120, 610, OldHouse);
        Kin(Aos, mahaut, ivain, aude);
        var hugon = P("Hugon Ban", M, 330, 830, OldHouse);
        var clarisse = P("Clarisse Ban", F, 360, 850, OldHouse);
        var lorin = P("Lorin Ban", M, 400, 880, OldHouse);
        Kin(ivain, aude, hugon, clarisse, lorin);

        var oriol = P("Oriol Ban-Goerim", M, 180, 660, BanGoerim);
        Kin(anselme, null, oriol);
        var bertrade = P("Bertrade Ban-Goerim", F, 400, 1004, BanGoerim);
        var thierrin = P("Thierrin Ban-Goerim", M, 520, 1010, BanGoerim);
        Kin(oriol, null, bertrade, thierrin);
        Dies(bertrade, DeathCause.Fire);

        var anseau = P("Anseau Ban", M, 610, 1004, OldHouse);
        Firaperc = P("Firaperc Ban", M, 790, 1004, OldHouse);
        Kin(hugon, bertrade, anseau, Firaperc);
        Dies(anseau, DeathCause.Fire);
        Dies(Firaperc, DeathCause.Fire);

        Alos = P("Alos Ban-Goerim", M, 930, 1150, BanGoerim);
        Adelim = P("Adelim Ban-Goerim", M, 945, 1162, BanGoerim);
        Kin(thierrin, null, Alos, Adelim);
        var josserand = P("Josserand Ban-Goerim", M, 1000, 1400, BanGoerim);
        Kin(Alos, null, josserand);
        var clarin = P("Clarin Ban-Goerim", M, 1080, 1540, BanGoerim, "the Last");
        Kin(josserand, null, clarin);

        // ── The Counsel before the empire ──
        var calvor = P("Calvor Ban-Aloid", M, -580, -385, Counsel);
        Kin(aloid, oriane, calvor);
        var pellion = P("Pellion Ban-Aloid", M, -512, -318, Counsel);
        Kin(calvor, null, pellion);
        var taurec = P("Taurec Ban-Aloid", M, -448, -250, Counsel);
        Kin(pellion, null, taurec);
        var sestin = P("Sestin Ban-Aloid", M, -380, -181, Counsel);
        Kin(taurec, null, sestin);
        var mauron = P("Mauron Ban-Aloid", M, -312, -120, Counsel);
        Kin(sestin, null, mauron);
        var isbarre = P("Isbarre Ban-Aloid", M, -240, -41, Counsel);
        Kin(mauron, null, isbarre);
        var ludric = P("Ludric Ban-Aloid", M, -165, -15, Counsel);
        Kin(isbarre, null, ludric);
        var sibille = P("Sibille Ban-Aloid", F, -160, -50, Counsel);
        VersonI = P("Verson I Ban-Aloid", M, -82, -40, Counsel);
        AqilonI = P("Aqilon I Ban-Aloid", M, -76, 2, Counsel, "the Lame");
        var orbanne = P("Orbanne Ban-Aloid", F, -70, 40, Counsel);
        Kin(ludric, sibille, VersonI, AqilonI, orbanne);
        Dies(VersonI, DeathCause.Murder, AqilonI);
        Dies(AqilonI, DeathCause.Illness);

        // ── The imperial line of Pyr ──
        VersonII = P("Verson II Ban-Aloid", M, -48, 96, Counsel, "the Regent");
        var iselde = P("Iselde Ban-Aloid", F, -40, 110, Counsel);
        Kin(AqilonI, orbanne, VersonII, iselde);
        var poelocI = P("Poeloc I Ban-Aloid", M, 30, 171, Counsel);
        Kin(VersonII, iselde, poelocI);
        var aqilonII = P("Aqilon II Ban-Aloid", M, 90, 214, Counsel);
        Kin(poelocI, null, aqilonII);
        var poelocII = P("Poeloc II Ban-Aloid", M, 140, 236, Counsel, "the Brief");
        PoelocIII = P("Poeloc III Ban-Aloid", M, 165, 330, Counsel, "the Convener");
        Kin(aqilonII, null, poelocII, PoelocIII);
        var teosodocI = P("Teosodoc I Ban-Aloid", M, 255, 402, Counsel);
        Kin(PoelocIII, null, teosodocI);
        var aqilonIII = P("Aqilon III Ban-Aloid", M, 320, 470, Counsel);
        Kin(teosodocI, null, aqilonIII);
        var veresterI = P("Verester I Ban-Aloid", M, 380, 545, Counsel);
        Kin(aqilonIII, null, veresterI);
        VeresterII = P("Verester II Ban-Aloid", M, 520, 640, Counsel, "the Salter");
        Kin(veresterI, null, VeresterII);
        var aqilonIV = P("Aqilon IV Ban-Aloid", M, 575, 706, Counsel);
        Kin(VeresterII, null, aqilonIV);
        var versonIII = P("Verson III Ban-Aloid", M, 640, 775, Counsel);
        Kin(aqilonIV, null, versonIII);
        var polopI = P("Polop I Ban-Aloid", M, 710, 858, Counsel, "the Harbourer");
        Kin(versonIII, null, polopI);
        var ossianeI = P("Ossiane I Ban-Aloid", F, 790, 927, Counsel);
        Kin(polopI, null, ossianeI);
        var ferauld = P("Ferauld Ban-Aloid", M, 780, 900, Counsel);
        var gaudemar = P("Gaudemar Ban-Aloid", M, 850, 990, Counsel);
        Kin(ferauld, ossianeI, gaudemar);
        var edilonI = P("Edilon I Ban-Aloid", M, 930, 1058, Counsel, "the Indulgent");
        Kin(gaudemar, null, edilonI);
        AqilonV = P("Aqilon V Ban-Aloid", M, 990, 1131, Counsel, "of the Springs");
        Kin(edilonI, null, AqilonV);
        var edilonII = P("Edilon II Ban-Aloid", M, 1080, 1172, Counsel);
        PolopII = P("Polop II Ban-Aloid", M, 1100, 1262, Counsel, "the Grieving");
        Kin(AqilonV, null, edilonII, PolopII);
        Dies(edilonII, DeathCause.Accident);
        Jelebanne = P("Jelebanne Ban-Aloid", F, 1195, 1392, Counsel, "Queen in the Steam");
        var versonIV = P("Verson IV Ban-Aloid", M, 1200, 1344, Counsel);
        Kin(PolopII, null, Jelebanne, versonIV);
        var poelocIV = P("Poeloc IV Ban-Aloid", M, 1280, 1410, Counsel);
        Kin(versonIV, null, poelocIV);
        var hadelmine = P("Hadelmine Ban-Aloid", F, 1360, 1478, Counsel);
        Kin(poelocIV, null, hadelmine);
        var ormund = P("Ormund Ban-Aloid", M, 1430, 1552, Counsel);
        Kin(null, hadelmine, ormund);
        var sabellon = P("Sabellon Ban-Aloid", M, 1500, 1640, Counsel);
        Kin(ormund, null, sabellon);
        TeosodocII = P("Teosodoc II Ban-Aloid", M, 1575, 1703, Counsel);
        Kin(sabellon, null, TeosodocII);

        // ── The line of Belune ──
        var evrast = P("Evrast Ban-Aloid", M, 1190, 1350, Counsel);
        var corvel = Corvel = P("Corvel Ban-Aloid", M, 1250, 1450, KingdomOfBelune);
        Kin(evrast, Jelebanne, corvel);
        var amaranthe = Amaranthe = P("Amaranthe Ban-Aloid", F, 1330, 1530, KingdomOfBelune);
        Kin(corvel, null, amaranthe);
        var ossianeII = OssianeII = P("Ossiane II Ban-Aloid", F, 1420, 1618, KingdomOfBelune);
        Kin(null, amaranthe, ossianeII);
        Philandre = P("Philandre Ban-Aloid", M, 1500, 1703, KingdomOfBelune);
        Kin(null, ossianeII, Philandre);
        Loquine = P("Loquine Ban-Aloid", F, 1660, 1851, KingdomOfBelune);
        Kin(Philandre, null, Loquine);
        Dies(Philandre, DeathCause.Murder);
        Dies(TeosodocII, DeathCause.Murder);

        // ── The Red Brick House ──
        var naabusI = P("Naabus I Ban-Balnus", M, -520, 610, RedBrickHouse);
        var ottaline = P("Ottaline Ban-Balnus", F, -505, 1011, RedBrickHouse);
        Kin(balnus, merove, naabusI, ottaline);
        Lwrash = P("Lwrash Ban-Balnus", M, -310, -6, RedBrickHouse, "the Red Giant");
        var violannI = P("Violann I Ban-Balnus", F, -250, 1905, RedBrickHouse, "the Eldest");
        var diferonI = P("Diferon I Ban-Balnus", M, -180, 1850, RedBrickHouse);
        Kin(naabusI, ottaline, Lwrash, violannI, diferonI);

        var naabusII = P("Naabus II Ban-Balnus", M, -60, 2010, RedBrickHouse);
        var akilonI = P("Akilon I Ban-Balnus", M, 5, 1480, RedBrickHouse);
        var vesifiaI = P("Vesifia I Ban-Balnus", F, 40, 2090, RedBrickHouse, "Mother of the House");
        Kin(diferonI, violannI, naabusII, akilonI, vesifiaI);

        var naabusIII = P("Naabus III Ban-Balnus", M, 150, 1320, RedBrickHouse);
        var akilonII = P("Akilon II Ban-Balnus", M, 210, 1395, RedBrickHouse);
        Kin(akilonI, vesifiaI, naabusIII, akilonII);
        var akilonIII = P("Akilon III Ban-Balnus", M, 330, 1602, RedBrickHouse);
        var akilonIV = P("Akilon IV Ban-Balnus", M, 470, 1760, RedBrickHouse);
        Kin(naabusII, vesifiaI, akilonIII, akilonIV);

        Oppretitette = P("Oppretitette Ban-Balnus", F, 480, 2081, RedBrickHouse, "the Long-Armed");
        var gavaud = P("Gavaud Ban-Balnus", M, 520, 1870, RedBrickHouse);
        Kin(naabusIII, ottaline, Oppretitette, gavaud);

        var akilonV = P("Akilon V Ban-Balnus", M, 610, 1805, RedBrickHouse);
        var akilonVI = P("Akilon VI Ban-Balnus", M, 760, 2050, RedBrickHouse);
        Kin(akilonII, violannI, akilonV, akilonVI);
        var naabusIV = P("Naabus IV Ban-Balnus", M, 700, 2105, RedBrickHouse);
        Kin(akilonIII, vesifiaI, naabusIV);
        var akilonVII = P("Akilon VII Ban-Balnus", M, 890, 2240, RedBrickHouse);
        var violannII = P("Violann II Ban-Balnus", F, 900, 2280, RedBrickHouse);
        Kin(akilonIV, vesifiaI, akilonVII, violannII);

        AkilonVIII = P("Akilon VIII Ban-Balnus", M, 1010, 1703, RedBrickHouse, "Keeper of the Well");
        AkilonXII = P("Akilon XII Ban-Balnus", M, 1300, 2530, RedBrickHouse);
        Bemakor = P("Bemakor Ban-Balnus", M, 1380, 2532, RedBrickHouse);
        var clemence = P("Clemence Ban-Balnus", F, 1600, 1690, RedBrickHouse);
        Kin(akilonV, Oppretitette, AkilonVIII, AkilonXII, Bemakor, clemence);
        Kin(TeosodocII, clemence);

        Rosena = P("Rosena Ban-Balnus", F, 1000, 2390, RedBrickHouse, "Queen of Prunil");
        Kin(gavaud, violannII, Rosena);
        var akilonIX = P("Akilon IX Ban-Balnus", M, 1105, 2300, RedBrickHouse);
        var akilonX = P("Akilon X Ban-Balnus", M, 1190, 2210, RedBrickHouse);
        Kin(naabusIV, violannII, akilonIX, akilonX);
        var diferonII = P("Diferon II Ban-Balnus", M, 1200, 2460, RedBrickHouse);
        var akilonXI = P("Akilon XI Ban-Balnus", M, 1240, 2502, RedBrickHouse);
        Kin(akilonVI, violannI, diferonII, akilonXI);

        var naabusV = P("Naabus V Ban-Balnus", M, 1500, 2600, RedBrickHouse);
        Kin(diferonII, violannII, naabusV);
        ViolannIII = P("Violann III Ban-Balnus", F, 1650, 3020, RedBrickHouse);
        Kin(akilonXI, Rosena, ViolannIII);
        Oox = P("Oox Ban-Balnus", M, 1750, 2645, RedBrickHouse, "the Fallen God");
        Kin(Bemakor, Loquine, Oox);
        Dies(Oox, DeathCause.BuriedAlive);

        var vesifiaII = P("Vesifia II Ban-Balnus", F, 1900, 2640, RedBrickHouse);
        var isaure = P("Isaure Ban-Balnus", F, 2100, 2900, RedBrickHouse);
        Kin(naabusV, ViolannIII, vesifiaII, isaure);

        EdilonIII = P("Edilon III Ban-Balnus", M, 2150, 2632, RedBrickHouse);
        Karmon = P("Karmon Ban-Balnus", M, 2170, 2780, Knights);
        Kin(AkilonXII, vesifiaII, EdilonIII, Karmon);

        var naabusVI = P("Naabus VI Ban-Balnus", M, 2230, 2833, RedBrickHouse);
        var orieuse = P("Orieuse Ban-Balnus", F, 2240, 3100, RedBrickHouse);
        Kin(Karmon, isaure, naabusVI, orieuse);
        var diferonIII = P("Diferon III Ban-Balnus", M, 2290, 2982, RedBrickHouse);
        var gerbaude = P("Gerbaude Ban-Balnus", F, 2320, 3579, RedBrickHouse);
        var akilonXIII = P("Akilon XIII Ban-Balnus", M, 2330, 3181, RedBrickHouse);
        Kin(naabusVI, orieuse, diferonIII, gerbaude, akilonXIII);
        var aldegonde = P("Aldegonde Ban-Balnus", F, 2420, 3579, RedBrickHouse);
        Kin(diferonIII, ViolannIII, aldegonde);
        NaabusVII = P("Naabus VII Ban-Balnus", M, 2410, 3579, RedBrickHouse, "the Last Emperor");
        Kin(akilonXIII, gerbaude, NaabusVII);
        VesifiaIII = P("Vesifia III Ban-Balnus", F, 2579, null, RedBrickHouse, "the Last Empress");
        Kin(NaabusVII, aldegonde, VesifiaIII);

        Dies(AkilonVIII, DeathCause.Murder);
        Dies(AkilonXII, DeathCause.Murder);
        Dies(Bemakor, DeathCause.Battle);
        Dies(gerbaude, DeathCause.Hatching);
        Dies(aldegonde, DeathCause.Hatching);
        Dies(NaabusVII, DeathCause.Hatching);

        // ── The Eleventh Chapter and the wars ──
        AwakenedPrince = P("Cassimir Oltenne", M, -75, -31, TheWakeful, "the Awakened Prince");
        Dies(AwakenedPrince, DeathCause.Illness);
        TheWakeful.Founder = AwakenedPrince;
        var haskElder = P("Hask the Elder", M, -90, -20, HouseHask);
        Vhomanor = P("Vhomanor Hask", M, -44, -1, HouseHask, "the Hero of Pebelos");
        var adelhaid = P("Adelhaid of Dokur", F, -40, 30, HouseHask);
        Kin(haskElder, null, Vhomanor);
        var vhomer = P("Vhomer Hask", M, -20, 121, HouseHask);
        Kin(Vhomanor, adelhaid, vhomer);
        Dies(Vhomanor, DeathCause.Battle, AqilonI);
        Dies(vhomer, DeathCause.Execution);
        Dies(Lwrash, DeathCause.Battle, Vhomanor);

        // ── The Vahyrian and Pyrean empires ──
        Grizx = P("Grizx Perorin", M, 198, 247, ChildrenOfRede, "the Hesitant");
        Dies(Grizx, DeathCause.Accident);
        var kesunavosh = P("Kesunavosh", M, 500, 571, KingdomOfVu, "Tide-King of Vu");
        Jevunavosh = P("Jevunavosh", M, 548, 601, KingdomOfVu, "the Silent King");
        var ilavena = P("Ilavena of Osk", F, 552, 601, KingdomOfVu);
        Kin(kesunavosh, null, Jevunavosh);
        var tavosh = P("Tavosh", M, 575, 597, KingdomOfVu);
        var imaravun = P("Imaravun", F, 580, 660, KingdomOfVu, "Sister Salt");
        Kin(Jevunavosh, ilavena, tavosh, imaravun);
        Dies(Jevunavosh, DeathCause.BuriedAlive);
        Dies(tavosh, DeathCause.Battle);

        ForjeNasrop = P("Forje Nasrop", M, 560, 630, IISTG);
        AbkelopSortav = P("Abkelop Sortav", M, 650, 712, IISTG);
        LozQuintiliym = P("Loz Quintiliym", M, 695, 760, IISTG);
        ArnestGoprufid = P("Arnest Goprufid", M, 715, 790, IISTG);
        KaerosLougi = P("Kaeros Lougi", M, 748, 812, IISTG);
        JosuExculato = P("Josu Exculato", M, 770, 811, IISTG, circa: true);
        BarnoixBarnutum = P("Barnoix Barnutum", M, 790, 860, IISTG);
        DusherPolosik = P("Dusher Polosik", M, 960, 1060, null);
        NemethOraun = P("Nemeth Oraun", M, -930, -870, null, "the Descended", circa: true);

        // ── The coup ──
        Beatilda = P("Beatilda Ban-Aloid", F, 1683, 1703, Counsel, "the Princess of Varam");
        Kin(TeosodocII, clemence, Beatilda);
        var maudrin = P("Maudrin Ban-Aloid", M, 1662, 1703, Counsel);
        var ysoline = P("Ysoline Ban-Aloid", F, 1666, 1703, Counsel);
        Kin(TeosodocII, clemence, maudrin, ysoline);
        Dies(maudrin, DeathCause.Murder);
        Dies(ysoline, DeathCause.Murder);
        Dies(Beatilda, DeathCause.Hatching);
        Jhoeland = P("Jhoeland Leneu", M, 1672, 1760, TicklersGuild, "the Seagull", circa: true);

        // ── The Nine Pearls and the Cosmic Empire ──
        DoshAvory = P("Dosh Avory", M, 1695, 1779, HouseAvory);
        HouseAvory.Founder = DoshAvory;
        GerestonBolish = P("Gereston Bolish", M, 1782, 1850, IISTG);
        YsmeVarrocq = P("Ysme Varrocq", F, 2505, 2530, null);
        Dies(YsmeVarrocq, DeathCause.Execution);
        MaelisTourbe = P("Maelis Tourbe", M, 2490, 2566, IISTG);
        AmauryChastel = P("Amaury Chastel", M, 2505, 2580, Inquisition, "Grand Inquisitor");
        CenzusFogun = P("Cenzus Fogun", M, 2572, 2661, Inquisition, "the Inquisitor");
        Dies(CenzusFogun, DeathCause.Illness);
        ArnevizGeoant = P("Arneviz Geoant", M, 2790, null, IISTG);
        JourdainAvory = P("Jourdain Avory", M, 2470, 2540, HouseAvory, "the Last Governor");
        Idrenne = P("Idrenne Sauval", F, 3530, 3579, IISTG, "captain of the Patience of Rhesis");
        Dies(Idrenne, DeathCause.Hatching);
    }
}

using System.Linq;

namespace Cathedral.Game.History.Lore;

// The dated history of the empire, from lore/01_chronology.md. Births and deaths are not written here:
// BuildLifeEvents derives them from the figures' own dates, so a date lives in one place only.
public sealed partial class EmpireLore
{
    public War AqilonicWars = null!, SaltWar = null!, WarOfTheRegency = null!;

    private void C(int round, string title, string text, params HistoricInfo[] involved)
        => Chronology.Record(new ChronicleEvent(At(round), E, title, text, involved));

    private void Cc(int round, string title, string text, params HistoricInfo[] involved)
        => Chronology.Record(new ChronicleEvent(Circa(round), E, title, text, involved));

    private void Accede(int round, HistoricFigure who, HistoricFaction office, string title)
        => Chronology.Record(new AccessionEvent(At(round), E, who, office, title));

    private void Founded(HistoricFaction f)
    {
        if (f.Founded.IsKnown) Chronology.Record(new FactionFoundedEvent(f.Founded, E, f));
    }

    private void Battle(int round, War? war, string name, HistoricFaction? victor, HistoricFaction? loser, Place? place = null)
        => Chronology.Record(new BattleEvent(At(round), E, war, name, victor, loser, place));

    private War MakeWar(string name, int start, int end, WarOutcome outcome, HistoricFaction[] attackers,
                        HistoricFaction[] defenders, string why)
    {
        var w = new War(name, E) { Started = At(start), Ended = At(end), Outcome = outcome };
        w.Attackers.AddRange(attackers);
        w.Defenders.AddRange(defenders);
        Wars.Add(w);
        Chronology.Record(new WarStartedEvent(At(start), E, w, why));
        Chronology.Record(new WarEndedEvent(At(end), E, w));
        return w;
    }

    private void BuildEvents()
    {
        foreach (var f in Factions) Founded(f);
        foreach (var r in Religions.Where(r => r.Founded.IsKnown))
            Chronology.Record(new ReligionFoundedEvent(r.Founded, E, r));

        // ── The fishing hut ──
        C(-1203, "The Pale Swimmer", "A blood elf drawn in through the eye of Pyr's south vortex is taken in the net of the Oskavun twins at Tavunesh Cove, and chained in their hut.", FishingHut);
        Cc(-1150, "The witches of the fishing hut", "The twins, unageing on the elf's blood, sell it as sea-physic and kill whoever comes too close.", FishingHut);
        C(-1056, "The grey waste", "Hesperine, daughter of the Baron of Ban, falls ill with a black bile corruption no physician can slow.", Hesperine, Ansedor);
        C(-1053, "The four knights", "Ansedor sends four knights across the Sea of Vu for the witches' physic.", Ansedor, FishingHut);
        C(-1052, "The Delivery", "The golden knight carries the chained elf to Ban. Hesperine is healed, and Ansedor drinks and is made young.", Ansedor, Hesperine, FishingHut, BanKeep);
        C(-1051, "The marriage of Ban", "Ansedor takes his daughter Hesperine as his wife. The household is sworn to silence.", Ansedor, Hesperine);
        Cc(-980, "The half-elves", "The Ban begin to call themselves half-elves, descended from an elf prince and a princess.", HouseOfBan);
        C(-903, "The Ten Chapters", "Hesperine and Oudin complete the Ten Chapters from the elf's account.", Hesperine, Oudin, Principism);
        Cc(-800, "The week of eleven days", "Principists in Benepelop adopt the eleven-day week, one day per chapter.", Principism);
        C(-512, "The Parting of the Houses", "At Oudin's death his three sons divide the family's offices: the Old House, the Red Brick House and the Counsel.", Oudin, OldHouse, RedBrickHouse, Counsel);
        C(-505, "The Red Brick House", "The Red Brick House is built on the Clay Hill of Avolor to hold the elf.", RedBrickHouse, RedBrickHouseBuilding);
        Cc(-420, "The refining", "The Balnus learn to refine the blood, and daily drinking becomes survivable.", RedBrickHouse);
        Cc(-300, "The Closing of the Well", "Matriarch Merove rules that the Counsel shall no longer be told what its baptismal draught is.", RedBrickHouse, Counsel);

        // ── The Eleventh Chapter ──
        C(-41, "The forgery", "Aos Ban and Aqilon Ban-Aloid write an Eleventh Chapter in the archaic hand of the Hesperine Codex.", Aos, AqilonI, EarlyQothism);
        C(-40, "The vote on the Eleventh Chapter", "Verson Ban-Aloid, who had found out the forgery, is poisoned by his brother on the morning of the vote; the chapter fails by one vote.", VersonI, AqilonI, Benepelop);
        C(-40, "The Garden Riots", "The devout of Avolor rise under the Awakened Prince; five Ekklesia members who voted no are killed. Aqilon is given the Mandate of Quelling.", AwakenedPrince, AqilonI, Benepelop, TheWakeful);
        C(-39, "The Quelling", "Aqilon massacres the rioters, imprisons the Awakened Prince, purges the Ekklesia and has the chapter adopted.", AqilonI, AwakenedPrince, Benepelop, TowerOfSalts);
        Accede(-39, AqilonI, Benepelop, "Warden of the Garden");

        // ── The Aqilonic Wars ──
        AqilonicWars = MakeWar("the Aqilonic Wars", -12, 0, WarOutcome.DefenderWon,
            new[] { Napel, Penaros, Pebelos, NorthernCoalition }, new[] { Benepelop },
            "for heresy and illegitimate tyranny");
        Battle(-12, AqilonicWars, "The siege of Kalom", Benepelop, Napel, Kalom);
        C(-12, "The corpse catapults", "Lwrash Ban-Balnus retakes Kalom by catapulting bodies and rotten meat over its walls until plague does the work.", Lwrash, Kalom);
        Battle(-11, AqilonicWars, "The taking of Yr", Benepelop, Napel, Yr);
        Battle(-9, AqilonicWars, "The Burning of Yr", Benepelop, NorthernCoalition, Yr);
        Battle(-8, AqilonicWars, "The Rain Crossing", Benepelop, Penaros, Perostro);
        Battle(-6, AqilonicWars, "The Battle of Avenas", Benepelop, Pebelos, Avenas);
        C(-6, "The causeway duel", "Vhomanor Hask kills Lwrash in single combat on the causeway of Avenas.", Vhomanor, Lwrash, Avenas);
        Battle(-1, AqilonicWars, "The Burial of Sorvas", Benepelop, Pebelos, Sorvas);
        C(-1, "The fall on the road", "On the road to Dokur Aqilon's horse falls on him and breaks his leg.", AqilonI);
        C(0, "The Foundation", "Pebelos capitulates at Dokur. Aqilon is acknowledged Emperor of the Half-Elves, and his leg is cut off.", AqilonI, Empire, Dokur);
        Accede(0, AqilonI, Empire, "Emperor of the Half-Elves");
        C(1, "The man at the gate", "The regent Verson II declares his returning father an impostor and imprisons him.", VersonII, AqilonI, TowerOfSalts);

        // ── The imperial succession ──
        (int From, HistoricFigure Who)[] emperors =
        {
            (1, VersonII), (96, Fig("Poeloc I")), (171, Fig("Aqilon II")), (214, Fig("Poeloc II")), (236, PoelocIII),
            (330, Fig("Teosodoc I")), (402, Fig("Aqilon III")), (470, Fig("Verester I")), (545, VeresterII),
            (640, Fig("Aqilon IV")), (706, Fig("Verson III")), (775, Fig("Polop I")), (858, Fig("Ossiane I")),
            (927, Fig("Gaudemar")), (990, Fig("Edilon I")), (1058, AqilonV), (1131, Fig("Edilon II")),
            (1172, PolopII), (1262, Fig("Verson IV")), (1344, Fig("Poeloc IV")), (1410, Fig("Hadelmine")),
            (1478, Fig("Ormund")), (1552, Fig("Sabellon")), (1640, TeosodocII), (1703, Oppretitette),
            (2081, AkilonXII), (2530, EdilonIII), (2632, Fig("Naabus VI")), (2833, Fig("Diferon III")),
            (2982, Fig("Akilon XIII")), (3181, NaabusVII),
        };
        foreach (var (from, who) in emperors)
            Accede(from, who, Empire, who.Sex == Sex.Female ? "Empress of the Half-Elves" : "Emperor of the Half-Elves");

        (int From, HistoricFigure Who)[] patriarchs =
        {
            (-512, Fig("Aspar")), (-193, Fig("Serle")), (-60, Aos), (402, Fig("Ivain")), (548, Fig("Hugon")), (830, Fig("Anseau")),
        };
        foreach (var (from, who) in patriarchs) Accede(from, who, OldHouse, "Patriarch");

        (int From, HistoricFigure Who)[] matriarchs = { (-498, Fig("Merove")), (388, Fig("Ottaline")), (1011, Oppretitette) };
        foreach (var (from, who) in matriarchs) Accede(from, who, RedBrickHouse, "Matriarch");

        (int From, HistoricFigure Who)[] beluneKings =
        {
            (1321, Jelebanne), (1392, Fig("Corvel")), (1450, Fig("Amaranthe")), (1530, Fig("Ossiane II")),
            (1618, Philandre), (1704, Bemakor),
        };
        foreach (var (from, who) in beluneKings)
            Accede(from, who, KingdomOfBelune, who.Sex == Sex.Female ? "Queen of Belune" : "King of Belune");

        // ── The Vahyrian Empire ──
        Cc(80, "The Aqilonic Roads", "Stone highways are laid from Avolor to Dokur.", Empire);
        C(118, "The Hask Rising", "Vhomer Hask, son of Vhomanor, raises Pebelos against the empire; he is crushed in 121.", Fig("Vhomer Hask"), Pebelos, Empire);
        C(211, "The burning of the Foundling House", "The foundling Grizx, aged thirteen, sets the Foundling House of Perorigak on fire and is put in the Blind Cells.", Grizx, FoundlingHouse, BlindCells);
        C(222, "Out of the dark", "Grizx is released after four thousand days in darkness, blinded by the daylight, and sees Rede.", Grizx);
        C(247, "The Hesitant vanishes", "Grizx walks into the Pereth at night and is not seen again.", Grizx);
        C(249, "The first burnings", "The Children of Rede begin burning people alive in Penaros and Napel.", ChildrenOfRede, RedeCult);
        C(262, "The Convocation of Avolor", "Poeloc III convenes the Old House under Aos: Early Qothism is made official, Adswarism, Silihism and Principlurism tolerated. The First Schism.", PoelocIII, Aos, EarlyQothism, Adswarism, Silihism, Principlurism);
        C(263, "The execution of the Children of Rede", "Two hundred and twelve Children of Rede are burnt at Perorigak.", ChildrenOfRede, PoelocIII);
        Cc(590, "The sowing", "Verester II buries heretics alive so that Qoth may grow truth from their error.", VeresterII, EarlyQothism);
        C(593, "The Vunic merchants", "Twenty-two Vunic salt merchants are buried alive in the market of Avolor for refusing the silent day.", VeresterII, KingdomOfVu);
        C(594, "The salt stops", "Jevunavosh halts all salt shipments to the empire.", Jevunavosh, KingdomOfVu, MinesOfOsk);
        SaltWar = MakeWar("the Salt War", 596, 601, WarOutcome.AttackerWon, new[] { Empire }, new[] { KingdomOfVu },
            "for the salt of Osk");
        Battle(596, SaltWar, "The Straits of Omavosh", KingdomOfVu, Empire);
        Battle(597, SaltWar, "Cape Lemavosh", KingdomOfVu, Empire);
        C(598, "The semi-submerged galley", "Forje Nasrop builds a galley that rides low, decked like a turtle, that no Vunic ship can board or ram.", ForjeNasrop, IISTG);
        Battle(601, SaltWar, "The fall of Hanavun", Empire, KingdomOfVu, Hanavun);
        C(601, "The Salt Mound", "Jevunavosh is buried alive under salt in the Garden of Lilies. The Hundred Gardens Palace becomes the Salt Palace. The empire holds all Pyr.", Jevunavosh, VeresterII, SaltPalace);

        // ── The Pyrean Empire and the first expeditions ──
        C(688, "The Southern Throat", "Abkelop Sortav reaches the south vortex.", AbkelopSortav, IISTG);
        C(731, "The Northern Throat", "Loz Quintiliym reaches the north vortex.", LozQuintiliym, IISTG);
        C(752, "The submarine galley", "Arnest Goprufid builds a galley that rows under the water.", ArnestGoprufid, IISTG);
        C(779, "Out through the Throat", "Kaeros Lougi rides the south vortex out into the cosmic sea and returns by its eye.", KaerosLougi, IISTG);
        C(806, "Josu reaches Belune", "Josu Exculato reaches Belune and cannot return.", JosuExculato, Belune);
        C(833, "The round trip", "Barnoix Barnutum goes to Belune and back, carrying Josu's journal as his proof.", BarnoixBarnutum, JosuExculato, Belune);
        C(851, "The first mission", "The first Qothist mission sails for Belune.", EarlyQothism, Belune);
        C(1004, "The Burning Council", "A council called on Firaperc's thesis of the two faces of Qoth burns with the Old House and everyone in it. The Second Schism.", Firaperc, Fig("Anseau"), OldHouse, OldHouseBuilding, EarlyQothism);
        C(1004, "The Quarrel of Inheritance", "Alos and Adelim Ban-Goerim fight over the Old House's inheritance.", Alos, Adelim, BanGoerim);
        C(1009, "The Plain Church", "Dusher Polosik, priest of Lusk, founds a church without a hierarchy.", DusherPolosik, PrincipativeQothism, LuskChapel);
        C(1011, "The three churches", "The emperor recognises the Genesive, Figurative and Principative churches.", GenesiveQothism, FigurativeQothism, PrincipativeQothism, Empire);
        Cc(1065, "The springs of Belune", "Aqilon V plants trading posts and sanatoria at Belune's hot springs.", AqilonV, Belune);
        Cc(1090, "The Exchange Plagues", "The sick of Pyr bring plagues that kill nine tenths of Belune's natives, over fifty rounds.", Belune);
        C(1212, "The Imperial Thermal Sanatorium", "Polop II builds a palace-hospital on Belune for his daughter Jelebanne.", PolopII, Jelebanne, Belune);
        C(1321, "The Kingdom of Belune", "Belune is made a kingdom of the empire and Jelebanne crowned its queen.", Jelebanne, KingdomOfBelune, Belune);

        // ── The coup ──
        C(1660, "A union of the houses", "Teosodoc II marries Clemence Ban-Balnus, daughter of Oppretitette.", TeosodocII, Fig("Clemence"));
        C(1686, "The baptism of Beatilda", "Beatilda drinks the blood at the Red Brick House on her 1100th day.", Beatilda, RedBrickHouse);
        C(1694, "Prunil sighted", "The IISTG sights Prunil.", IISTG, Prunil);
        Cc(1697, "The glimpse", "Beatilda sees the chained elf in the Well-Room.", Beatilda, RedBrickHouseBuilding);
        C(1701, "The contract", "The Red Brick House commissions Beatilda's death from the Ticklers' Guild; Jhoeland Leneu enters her guard.", Beatilda, Jhoeland, RedBrickHouse, TicklersGuild);
        C(1702, "The Fire in the Lily Wing", "Beatilda's death is faked in a fire at the Salt Palace.", Beatilda, Jhoeland, SaltPalace);
        C(1703, "The Night of the Well", "The elf is freed from the Red Brick House; the Keeper of the Well is killed at its door.", Beatilda, Jhoeland, AkilonVIII, Fig("Hamelin Moll"), RedBrickHouseBuilding);
        C(1703, "The Seagull's price", "On the eve of sailing Jhoeland betrays Beatilda to the Guild. The elf goes back to its cell.", Jhoeland, Beatilda, TicklersGuild, Perostro);
        C(1703, "The Red Night", "Teosodoc II is murdered and the Ban-Aloid purged. Oppretitette takes the throne.", TeosodocII, Oppretitette, Counsel, RedBrickHouse, SaltGuard);
        C(1703, "The Red Tribunal", "Beatilda is condemned to the ice dungeon of Vaur.", Beatilda, RedTribunals, Vaur);
        C(1703, "The hatching of Varam", "Days after Beatilda reaches Vaur, Varam hatches.", Varam, Beatilda, Beatildism);

        // ── The Nine Pearls ──
        C(1704, "The Contestation", "Risings in Napel and Pebelos, riots in Avolor; the silent day is made optional.", Empire, Oppretitette, TicklersGuild);
        C(1705, "The conquest of Prunil begins", "Rosena Ban-Balnus lands on Prunil.", Rosena, Prunil);
        C(1713, "The Gorges", "Rosena's troops drive the resisting cities of Prunil off the cliffs.", Rosena, Prunil);
        C(1722, "The Queen of Prunil", "Prunil is integrated into the empire with Rosena as its queen.", Rosena, Prunil);
        C(1731, "Green Avoria", "Dosh Avory begins the conquest of Green Avoria.", DoshAvory, GreenAvoria);
        C(1744, "Blue Avoria", "Dosh Avory begins the conquest of Blue Avoria.", DoshAvory, BlueAvoria);
        C(1760, "Golden Avoria", "Dosh Avory begins the conquest of Golden Avoria.", DoshAvory, GoldenAvoria);
        C(1780, "New Pyr", "Violann III begins the conquest of New Pyr.", ViolannIII, NewPyr);
        C(1792, "Aqilonia", "Violann III begins the conquest of Aqilonia.", ViolannIII, Aqilonia);
        C(1811, "New Varam", "Gereston Bolish discovers New Varam.", GerestonBolish, NewVaram);
        C(1829, "The Nine Pearls", "New Varam is taken; the empire holds nine worlds.", Empire, NewVaram);

        // ── The Cosmic Empire ──
        C(2530, "The Assassination", "Akilon XII is killed in his bed by the courtesan Ysme Varrocq, paid by his brother Bemakor through a Tickler cell.", AkilonXII, YsmeVarrocq, Bemakor, TicklersGuild);
        C(2530, "The broken tribunal", "Edilon III, accused by the regent Bemakor, is tried by a Red Tribunal; his brother Karmon breaks in and kills the judges.", EdilonIII, Karmon, Bemakor, RedTribunals);
        WarOfTheRegency = MakeWar("the War of the Regency", 2530, 2532, WarOutcome.DefenderWon,
            new[] { KingdomOfBelune }, new[] { Empire }, "for the regency");
        Battle(2532, WarOfTheRegency, "The Causeway of Oranse", Empire, KingdomOfBelune);
        C(2533, "The three pillars", "Edilon III founds the Principian Inquisition and the Plebeian Tribunal; the Ticklers' Guild and the Red Tribunals are outlawed.", EdilonIII, Inquisition, PlebeianTribunal, TicklersGuild, RedTribunals);
        C(2537, "The Obstinate", "Maelis Tourbe's first iron submarine is launched.", MaelisTourbe, IISTG, Calvassa);
        C(2540, "The Second Expansion", "Iron submarines carry the empire out toward hundreds of worlds.", Empire, IISTG, Knights);
        C(2561, "The broken submarine", "Oox's submarine breaks apart in the cosmic sea; he swims to Zuilkansia.", Oox, Zuilkansia);
        C(2566, "The god fallen from the sky", "Oox, worshipped on Zuilkansia, seizes the rescue submarine and begins to conquer as a god.", Oox, Zuilkansia, FallenGod);
        C(2598, "Ossomire", "Cenzus Fogun conquers his first world.", CenzusFogun, Ossomire);
        C(2644, "The taking of Oox", "The Inquisition captures Oox on Trulhex.", Oox, Inquisition, Trulhex);
        C(2645, "Buried twice", "Oox is buried alive in the Burial Fields of Ossomire; he digs himself out and is buried again under a stone.", Oox, Inquisition, Ossomire);
        C(2661, "Death of Fogun", "Cenzus Fogun dies of fever on Salsuge among the pilgrims' seed-graves.", CenzusFogun, Salsuge);
        Cc(2700, "The Red Pyramids", "The first Red Pyramids rise on Pyr to house the growing Balnus.", RedBrickHouse, RedPyramidsOfPyr);
        C(2815, "The far voyages", "Arneviz Geoant begins his voyages to the farthest worlds.", ArnevizGeoant, IISTG, Ysthane);
        C(2871, "Geoant sails", "Arneviz Geoant sails for a world beyond the star-tables and does not return.", ArnevizGeoant, GeoantsMark);
        Cc(3100, "The Count of Worlds", "Two hundred and ninety-nine worlds visited, two hundred and twenty-nine held. The expansion stops.", Empire, IISTG);
        C(3420, "The exile of Vesifia", "Vesifia III, twenty-five metres tall, is sent to Golden Avoria, where the Great Red Pyramid rises around her.", VesifiaIII, GoldenAvoria);
        C(3569, "The Deep Expeditions", "Naabus VII sends four submarines to the sea floor to find elf medicine in the ruins of the Eternal Temple.", NaabusVII, IISTG, Nadirine);
        C(3579, "The elf of the temple", "The Patience of Rhesis returns with a small, red, pig-nosed creature from the sea floor.", Idrenne, NaabusVII);
        C(3579, "The Hatching", "Pyr hatches. Its light goes out of every sky, and the empire ends.", Pyr, Empire);

        // ── After the Hatching ──
        Chronology.Record(new ChronicleEvent(HistoricDate.AH(10), E, "The Empire of Belune", "Belune crowns its own king; the empire of Belune lasts two reigns.", Belune));
        Chronology.Record(new ChronicleEvent(HistoricDate.AH(40), E, "The last sailing", "The last IISTG submarine known to have sailed.", IISTG));
        Chronology.Record(new ChronicleEvent(HistoricDate.AH(70), E, "The first goblins", "The Complaint of the Wardens of Mirelle records green, pig-nosed creatures in the old mines.", Mirelle));
        Chronology.Record(new ChronicleEvent(HistoricDate.Circa(HistoryCalendar.HatchingRound + 150), E, "The Last Empress", "Pilgrims to the Great Red Pyramid report breathing in the stones.", VesifiaIII, GoldenAvoria, LastEmpressCult));
    }

    /// <summary>Birth and death events, derived from each figure's own dates and recorded cause.</summary>
    private void BuildLifeEvents()
    {
        foreach (var f in Figures)
        {
            if (f.Born.IsKnown) Chronology.Record(new BirthEvent(f.Born, E, f));
            if (!f.Died.IsKnown) continue;
            var (cause, killer) = _deaths.TryGetValue(f, out var d) ? d : (DeathCause.Natural, null);
            Chronology.Record(new DeathEvent(f.Died, E, f, cause, killer));
        }
    }

    /// <summary>A figure by the start of its name. For wiring the succession tables above only.</summary>
    private HistoricFigure Fig(string nameStart)
        => Figures.First(f => f.Name == nameStart || f.Name.StartsWith(nameStart + " "));
}

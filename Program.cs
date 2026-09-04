using System.Linq;
using Cathedral.LLM;
using Cathedral.Game;

// A shipped build is compiled WinExe and owns no console. If it was launched from a terminal
// anyway — which is the only way --cli is driven against a packaged build — join that terminal so
// the diagnostic stream and the CLI driver have somewhere to talk. No-op in a console build, and
// no-op when double-clicked. Touches no game state, so it may precede the seed.
Cathedral.ConsoleAttach.AttachToParentIfPresent();

// One log file per run, truncated at launch, carrying the console output and llama-server's output
// together. Opened immediately after the console is attached so that nothing worth keeping is
// printed before it exists.
Cathedral.GameLog.Initialize();

// A shipped build answers only the handful of options a player might need to get out of trouble;
// every development flag below is stripped here, before anything can read it. Inert in a
// development build. This has to precede the seed parse for the same reason the seed parse
// precedes everything else: an option removed after it has been acted on has not been removed.
args = Cathedral.ShipArguments.Filter(args);

// ── Master seed: resolved FIRST, before any other flag is looked at ──────────
// Every other flag handler below sets a static on some mode class, and touching one of those runs
// its static initializers — several of which ask GameRng for a stream. The first such ask resolves
// the master seed permanently, so parsing --seed any later than this silently produced a run on the
// boot default with --seed on the command line (which is exactly what happened with
// --skip-childhood) — and, now, one that stops at the moon-selection screen it was meant to skip.
// Nothing above this line may touch game state.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedSeed))
    {
        Cathedral.Config.Rng.Seed = parsedSeed;
        // Pinned by hand, which is a different thing from a seed recovered from a save below: this
        // one names the world to play, and so skips the screen that would ask which world to play.
        Cathedral.Config.Rng.SeedPinned = true;
        break;
    }
}

// The save's own seed is resolved here too, for the same reason and under the same rule: it is pure
// file IO and touches no game state. The world — terrain, where each location sits, and the people
// inside it — is generated from the master seed at startup, so a run can only be continued if the
// process boots on the seed that run was played with. Reading it here means Continue rebuilds nothing.
//
// --seed still wins outright, which is what keeps every scripted run reproducible; the consequence is
// that under --seed a save from a different seed is simply unloadable, and Continue greys out.
{
    string? savePathOverride = null;
    bool noSave = false;
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--save-path" && i + 1 < args.Length) savePathOverride = args[i + 1];
        if (args[i] == "--no-save") noSave = true;
    }
    bool cliActive = System.Array.IndexOf(args, "--cli") >= 0
                  || System.Array.IndexOf(args, "--cli-script") >= 0;
    Cathedral.Game.Save.SaveFile.Configure(savePathOverride, noSave, cliActive);

    if (Cathedral.Config.Rng.Seed == null)
        Cathedral.Config.Rng.Seed = Cathedral.Game.Save.SaveFile.PeekSeed();

    // Say what was found, always. Until now a normal launch said nothing about the save at all —
    // SaveFile only speaks up when saving is disabled or a save is the wrong version — so log.txt
    // could not answer the first question anyone asks about the main menu: why is Continue the way
    // it is? Continue is enabled exactly when a readable save exists, and this is the line that
    // makes that checkable from a player's log rather than by guessing.
    if (!Cathedral.Game.Save.SaveFile.Disabled)
    {
        var peeked = Cathedral.Game.Save.SaveFile.Read();
        Console.WriteLine(peeked == null
            ? $"SaveFile: no readable save at {Cathedral.Game.Save.SaveFile.Path_} — Continue will be greyed out."
            : $"SaveFile: readable save at {Cathedral.Game.Save.SaveFile.Path_} (seed {peeked.Seed}, day {peeked.Days:F0}) — Continue will be enabled.");
    }
}

Cathedral.GameRng.Initialize(Cathedral.Config.Rng.Seed);

// Check for help option
if (args.Length >= 1 && (args[0] == "--help" || args[0] == "-h"))
{
    // From the running executable, not a literal: the shipped build is ProscribedPalimpsest.exe
    // and the development one is Cathedral.exe, and a usage line naming the wrong one is exactly
    // the sort of thing nobody notices until a player pastes it into a bug report.
    Console.WriteLine($"Usage: {System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "Cathedral"} [options]");
    Console.WriteLine();

    // A shipped build lists what it will actually answer. Printing the full development list
    // would be worse than printing nothing: every line of it is an instruction that silently
    // does nothing, and the reader has no way to tell which.
    if (Cathedral.ShipArguments.IsRestricted)
    {
        Console.WriteLine("Options:");
        Console.WriteLine("  --cpu                              Run the language model on the CPU (use if the GPU misbehaves)");
        Console.WriteLine("  --gpu                              Run the language model on the GPU, overriding detection");
        Console.WriteLine("  --no-llm-probe                     Skip hardware detection at startup");
        Console.WriteLine("  --silent                           Start with no audio device opened");
        Console.WriteLine("  --help, -h                         Show this help message");
        Console.WriteLine();
        Console.WriteLine("Everything else is configured in the game's Settings screen.");
        return;
    }

    Console.WriteLine("Options:");
    Console.WriteLine("  (no args)                          Launch the narrative exploration game");
    Console.WriteLine("  --music                            Run the procedural ambient music PoC");
    Console.WriteLine("  --fight-area [options]             Run the fight area generator test");
    Console.WriteLine("    --mode <random|...>              Fight area generation mode (default: random)");
    Console.WriteLine("  --draw <folder>                    Display previously saved layered ASCII art");
    Console.WriteLine("  --img-to-txt <image> [options]    Convert an image to ASCII text art");
    Console.WriteLine("    --width <w>                      Max output width (default: terminal width)");
    Console.WriteLine("    --height <h>                     Max output height (default: terminal height)");
    Console.WriteLine("    --contrast <c>                   Contrast multiplier (default: 1.0)");
    Console.WriteLine("    --negative                       Invert brightness");
    Console.WriteLine("    --auto-contrast                  Automatically stretch contrast");
    Console.WriteLine("    --stretch                        Stretch/shrink to exact width/height (ignore aspect ratio)");
    Console.WriteLine("  --cli                              Drive the game from stdin and observe it as text (for scripted/automated verification)");
    Console.WriteLine("  --cli-script <file>                Run a newline-separated command script at startup (implies --cli)");
    Console.WriteLine("  --cli-timeout <seconds>            Hard limit for a --cli run before it closes itself (default 300)");
    Console.WriteLine("  --debug                            Enable debug mode (override LLM/RNG decisions via console) + viewers");
    Console.WriteLine("  --view                             Show LLM and scene viewers without console decision overriding");
    Console.WriteLine("  --dialogue-view                    Open a window graphing every dialogue tree (neutral replica text per node)");
    Console.WriteLine("  --dialogue-audit                   Print the dialogue-tree shape report (reply counts, branch lengths, bad tokens) and exit");
    Console.WriteLine("  --npc-audit                        Print the NPC generation report (determinism, trait resolution, body/skill/inventory shape) and exit");
    Console.WriteLine("  --item-audit                       Print the item catalogue report (identity, reachability, weights, trade coverage) and exit");
    Console.WriteLine("  --building-audit                   Print the building/schedule report (section partition, locks, beds, hall staffing) and exit");
    Console.WriteLine("  --playground                       Replace all LLM calls with instant placeholders (no server needed)");
    Console.WriteLine("  --skip-childhood                   Skip the childhood reminescence + get-up phases; randomly fill starting skills/items as if they had run");
    Console.WriteLine("  --mm                               After the childhood reminescence phase, fill every empty memory slot with random unheld modiMentis");
    Console.WriteLine("  --weapons                          Give the protagonist a starter weapon loadout (Arming Sword, Hunting Bow, Round Shield)");
    Console.WriteLine("  --location-type <name>             DEBUG: force which scene factory builds every location (forest, village,");
    Console.WriteLine("                                     cave…), ignoring the biome underfoot. --start-at only finds a biome the");
    Console.WriteLine("                                     world happens to contain; this builds one regardless");
    Console.WriteLine("  --npc-hostile                      DEBUG: every NPC counts the protagonist an enemy from the start. reconcile");
    Console.WriteLine("                                     and appease apply only to an enemy, and earning a HUMAN one in script means");
    Console.WriteLine("                                     a crime, a catch, a lost confrontation and a fight walked out of");
    Console.WriteLine("  --npc-affinity <level>             DEBUG: start every NPC at this affinity (DistantAcquaintance, CloseFriend…)");
    Console.WriteLine("                                     instead of Stranger. Six verbs are gated on already knowing somebody;");
    Console.WriteLine("                                     earning it in-script makes the test a test of that conversation instead");
    Console.WriteLine("  --auto-dialogue                    DEBUG: settle every dialogue as an immediate success instead of holding");
    Console.WriteLine("                                     it. A dozen verbs only OPEN a conversation; this lets their test assert");
    Console.WriteLine("                                     about the verb rather than walk somebody else's tree");
    Console.WriteLine("  --npc-static                       DEBUG: pin every NPC to one area all day instead of following their schedule.");
    Console.WriteLine("  --silent                           DEBUG: open no audio device — no music, no sound effects. Used by the test suite.");
    Console.WriteLine("  --hidden                           DEBUG: create the window without showing it — no window on screen, no focus");
    Console.WriteLine("                                     taken. The GL context and every mode still run. Used by the test suite.");
    Console.WriteLine("                                     Where somebody stands at a given hour is drawn from the location seed, so an");
    Console.WriteLine("                                     NPC verb's test cannot otherwise name the room");
    Console.WriteLine("  --location-id <n>                  DEBUG: build every scene as location id <n>, whatever vertex you stand on.");
    Console.WriteLine("                                     A scene is a pure function of its id, so this is what makes --verb-probe's");
    Console.WriteLine("                                     reported rooms and objects actually exist in the run");
    Console.WriteLine("  --grant-item <id[,id…]>            DEBUG: put named items in the starting pack (e.g. axe, pick, shovel, fishing_rod).");
    Console.WriteLine("                                     Five verbs are tool-gated and refused outright without one, and the starting");
    Console.WriteLine("                                     kit is random — so their success test is unwritable without this");
    Console.WriteLine("  --fill-party                       DEBUG: after the childhood phase, fill the party to max_companions with generated");
    Console.WriteLine("                                     NPCs — the last slot a beast, every slot before it a human");
    Console.WriteLine("  --cpu                              Run LLM on CPU only, overriding the setting and the first-run probe");
    Console.WriteLine("  --gpu                              Run LLM on the installed GPU backend (models/llama/backends/), same override");
    Console.WriteLine("  --no-llm-probe                     Skip first-run compute-device detection; use whatever is already saved");
    Console.WriteLine("  --no-developer-keys                Disable the developer keyboard shortcuts (D/M/F/G/H/J, C/V, W/S zoom),");
    Console.WriteLine("                                     leaving arrows/Space/Escape — i.e. behave as a shipped build does");
    Console.WriteLine("  --seed <n>                         Fix the master RNG seed for a reproducible run (world, spawn, dice).");
    Console.WriteLine("                                     Names the world outright, so New skips the moon-selection screen");
    Console.WriteLine("  --start-at <name>                  DEBUG: spawn on the first biome/location matching <name> (e.g. village, farm)");
    Console.WriteLine("  --start-area <name>                DEBUG: open narration in the first area of the location matching <name>");
    Console.WriteLine("                                     (e.g. pigsty, smithy). --start-at picks the location, this picks the room:");
    Console.WriteLine("                                     without it a script lands in whichever area was built first and has to walk");
    Console.WriteLine("  --observe-only <name>              DEBUG: an observation phase may only look at objects matching <name> (e.g. pig).");
    Console.WriteLine("                                     Which object a phase opens on is otherwise a persona choice out of a dozen,");
    Console.WriteLine("                                     so a script that wants to act on one thing cannot count on reaching it");
    Console.WriteLine("  --period <name>                    DEBUG: arrive at every location at this time of day (dawn…night) instead of a random one");
    Console.WriteLine("  --dither [mode[:levels[:scale]]]   Retune the final full-screen dither layer (mode off|bayer|mono|noise).");
    Console.WriteLine("                                     Resting state is bayer:6:1; game events pulse it for 0.15s.");
    Console.WriteLine("                                     In-game: F cycles the mode, G the palette depth, H the grain, J the event pulses");
    Console.WriteLine("  --no-encounters                    DEBUG: never roll a random travel encounter. For scripted runs: an");
    Console.WriteLine("                                     encounter puts the game in EncounterPrompt, where a script waiting");
    Console.WriteLine("                                     for LocationInteraction hangs until its timeout");
    Console.WriteLine("  --allow-reentry                    DEBUG: let a world-map click on your own vertex re-enter that location.");
    Console.WriteLine("                                     The game refuses it — arriving somewhere already opens it, so a visit");
    Console.WriteLine("                                     costs a journey. Scripts need it: `travel here` is how they get in");
    Console.WriteLine("  --start-fight <creature>           DEBUG: begin a fight on reaching the world map (wolf, bear, bandit, brigand).");
    Console.WriteLine("                                     The only way a script can reach fight mode: the real routes in are a random");
    Console.WriteLine("                                     travel encounter (which scripts disable) or provoking an NPC through dialogue");
    Console.WriteLine("  --encounter-on-arrival <creature>  DEBUG: force a travel encounter on the FINAL step of the next journey, once.");
    Console.WriteLine("                                     That step raises the step and arrival events from one call, so the fight begins");
    Console.WriteLine("                                     with no path left to walk — the case that stranded the run in Traveling");
    Console.WriteLine("  --spawn-beast <name>               DEBUG: put a beast (wolf, boar, bear, black bear, stray dog, fox) in the opening");
    Console.WriteLine("                                     area of every scene, at every period. A wilderness factory only rolls one in");
    Console.WriteLine("                                     10-40% of the time and then lets it roam, so appease/tame are otherwise luck");
    Console.WriteLine("  --goal-only <verb-id>              DEBUG: the playground's goal choice must land on this verb (e.g. tame). Without");
    Console.WriteLine("                                     it --playground draws uniformly over every goal the observed object offers.");
    Console.WriteLine("                                     The CLI's `goal` command sets the same thing, for a script that needs to change it");
    Console.WriteLine("  --advance-days <n>                 DEBUG: push the world clock forward <n> days on first arrival at the world");
    Console.WriteLine("                                     map. The clock only moves on travel and work, and a wound takes 100-1000");
    Console.WriteLine("                                     days to close, so this is how a script sees healing without simulating years");
    Console.WriteLine("  --save-path <file>                 Use <file> as the save instead of %APPDATA%\\Cathedral\\save.json. Saving is");
    Console.WriteLine("                                     OFF by default under --cli, so a scripted run cannot clobber a real save;");
    Console.WriteLine("                                     pass this to turn it back on against a file of the script's own");
    Console.WriteLine("  --no-save                          Disable reading and writing the save entirely");
    Console.WriteLine("  --black-bile                       DEBUG: fill all four humor queues with black bile after creation, so the");
    Console.WriteLine("                                     next journey starves the protagonist. The only way a script can stage a");
    Console.WriteLine("                                     starvation death (old age uses --advance-days, wounds --start-fight)");
    Console.WriteLine("  --grant-mm <id[,id...]>[:lvl]      DEBUG: grant the named modi mentis at <lvl> (default 1) after character");
    Console.WriteLine("                                     creation. Fighting skills are gated behind their modi mentis, so this is what");
    Console.WriteLine("                                     makes a given skill reachable — and level sets a buff's vital-heat cost");
    Console.WriteLine("  --mm-audit                         Print the modus-mentis content audit (hard-rule violations, coverage, soft stats) and exit");
    Console.WriteLine("  --verb-audit                       Print the verb-coverage audit (verbs per observable vs targets, dead verbs,");
    Console.WriteLine("                                     unresolvable modus-mentis and tool ids, landmark counts) and exit");
    Console.WriteLine("  --outcome-audit                    Print the outcome catalogue (every consequence, who produces it, what produces nothing) and exit");
    Console.WriteLine("  --verb-probe                       Print, per verb, the flags that reach it from a cold start (for writing cli tests) and exit");
    Console.WriteLine("  --mm-reach-csv [path]              Write one CSV row per modus mentis: which of the five routes reaches it (childhood,");
    Console.WriteLine("                                     fight, action, dialogue, work) and which bodies can hold it (default mm_reachability.csv) and exit");
    Console.WriteLine("  --crime-audit                      Print the crime audit (contextual verb legality, the morality choice rules,");
    Console.WriteLine("                                     enmity surviving a rebuild and a save) and exit");
    Console.WriteLine("  --llm-probe-audit                  Re-measure every compute device (prompt-read and generate rates, cost per");
    Console.WriteLine("                                     request) and print which one wins and why, ignoring the cached result, and exit");
    Console.WriteLine("  --help, -h                         Show this help message");
    return;
}

// Dialogue-tree shape audit: print the report (warnings are listed, not fatal) and exit. Headless —
// no LLM, no game state, no window; the text counterpart of --dialogue-view.
if (args.Length >= 1 && args[0] == "--dialogue-audit")
{
    Console.WriteLine(Cathedral.Game.Dialogue.Tree.DialogueTreeAudit.BuildReport());
    return;
}

// ── Debug targeting flags, parsed BEFORE any early-returning mode ───────────
// The audits and --verb-probe return without ever reaching the rest of the argument parsing, so a
// flag read further down is invisible to them. --verb-probe in particular exists to report the
// situations that reach each verb, and it has to be able to sweep under the same conditions a test
// will run under — otherwise it reports that six affinity-gated verbs are unreachable, which is
// only true because it never saw the flag that would have unlocked them.

// --npc-affinity <level>: start every NPC at this affinity instead of Stranger.
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] != "--npc-affinity") continue;
    if (Enum.TryParse<Cathedral.Game.Dialogue.Affinity.AffinityLevel>(args[i + 1], ignoreCase: true, out var lvl))
        Cathedral.Config.Debug.NpcAffinity = lvl;
    else
        Console.Error.WriteLine($"[debug] --npc-affinity: '{args[i + 1]}' is not an affinity level — ignored. "
                                + $"Try: {string.Join(", ", Enum.GetNames<Cathedral.Game.Dialogue.Affinity.AffinityLevel>())}");
    break;
}

// --auto-dialogue: settle every conversation as a success without entering Dialogue mode.
if (args.Any(a => a == "--auto-dialogue")) Cathedral.Config.Debug.AutoDialogue = true;

if (args.Any(a => a == "--npc-static")) Cathedral.Config.Debug.NpcStatic = true;

// --silent: open no audio device. The test suite passes this on every script.
if (args.Any(a => a == "--silent")) Cathedral.Config.Debug.Silent = true;

// --hidden: create the window without showing it. What run_tests.sh uses so a suite run does not put
// a hundred windows on screen and take the keyboard focus a hundred times.
if (args.Any(a => a == "--hidden")) Cathedral.Config.Debug.HiddenWindow = true;

// --npc-hostile: every NPC counts the protagonist an enemy from the start.
if (args.Any(a => a == "--npc-hostile")) Cathedral.Config.Debug.NpcHostile = true;

// --location-type <name>: force which factory builds every location, ignoring the biome underfoot.
// --start-at only finds a biome the generated world happens to contain; this builds one regardless.
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] != "--location-type") continue;
    Cathedral.Config.Debug.LocationType = args[i + 1];
    break;
}

// --location-id <n>: build every scene as that location id. What makes --verb-probe's findings and
// a real run agree — see Config.Debug.LocationId.
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] != "--location-id") continue;
    if (int.TryParse(args[i + 1], out int locId)) Cathedral.Config.Debug.LocationId = locId;
    else Console.Error.WriteLine($"[debug] --location-id: '{args[i + 1]}' is not a number — ignored.");
    break;
}


// NPC generation audit: spawn a sample of every archetype twice, check determinism, trait
// resolution and body/skill/inventory shape, then exit. Headless — no LLM, no window, no world.
if (args.Length >= 1 && args[0] == "--npc-audit")
{
    Console.WriteLine(Cathedral.Game.Npc.Generation.NpcAudit.BuildReport());
    return;
}

// Item catalogue audit: report identity clashes, unreachable liquids, unweighed items and thin
// trade tags, then exit. Headless — reads ItemRegistry only, so no LLM, no window, no world.
if (args.Length >= 1 && args[0] == "--item-audit")
{
    Console.WriteLine(Cathedral.Game.Narrative.ItemAudit.BuildReport());
    return;
}

// Building + schedule audit: generate every inhabited location type across a sample of ids and check
// the invariants that fail silently — section partition, node-id collisions, doors bypassed by
// area-graph edges, workers with no bed, empty shop counters. Headless: no LLM, no window.
if (args.Length >= 1 && args[0] == "--building-audit")
{
    Console.WriteLine(Cathedral.Game.Scene.Building.BuildingAudit.BuildReport());
    return;
}

// Verb-coverage audit: how much there is to DO in each location type, against the design targets.
// Headless — builds scenes and asks every verb whether it applies; no LLM, no window.
if (args.Length >= 1 && args[0] == "--verb-audit")
{
    Console.WriteLine(Cathedral.Game.Scene.VerbAudit.BuildReport());
    return;
}

// Verb probe: for each verb, the exact flags that reach it from a cold start. What a CLI test for
// that verb has to open with. Headless: builds scenes and asks every verb; no LLM, no window.
// Outcome audit: the catalogue of consequences, who produces each, and the two silent faults —
// an outcome nothing produces, and a verb that rolls but changes nothing. Headless.
if (args.Length >= 1 && args[0] == "--outcome-audit")
{
    Console.WriteLine(Cathedral.Game.Narrative.OutcomeAudit.BuildReport());
    return;
}

if (args.Length >= 1 && args[0] == "--save-audit")
{
    Cathedral.Game.Save.SaveAudit.Run();
    return;
}

if (args.Length >= 1 && args[0] == "--verb-probe")
{
    Console.WriteLine(Cathedral.Game.Scene.VerbProbe.BuildReport());
    return;
}

// Modus-mentis reachability: one CSV row per modus mentis, saying which of the five routes a player
// can actually reach it by — childhood, fight, action, dialogue, work — and which bodies can hold
// the lesson. The question the mm-grants skill raises and cannot settle, since reading the verbs
// route of five. Headless: no LLM, no window.
if (args.Length >= 1 && args[0] == "--mm-reach-csv")
{
    string reachPath = args.Length >= 2 && !args[1].StartsWith("--") ? args[1] : "mm_reachability.csv";
    string reachCsv  = Cathedral.Game.Narrative.MmReachAudit.BuildCsv(out string reachSummary);
    System.IO.File.WriteAllText(reachPath, reachCsv, new System.Text.UTF8Encoding(true));
    Console.WriteLine();
    Console.WriteLine(reachSummary);
    Console.WriteLine($"[mm-reach-csv] written to {System.IO.Path.GetFullPath(reachPath)}");
    return;
}

// Crime audit: contextual verb legality, the coded choice rules, and enmity outliving a visit —
// the three parts of the crime system that fail silently and that a --cli walk cannot reach.
// Headless: builds scenes and asks the rules directly; no LLM, no window.
if (args.Length >= 1 && args[0] == "--crime-audit")
{
    Console.WriteLine(Cathedral.Game.Scene.CrimeAudit.BuildReport());
    return;
}

// Modus-mentis content audit mode: print the report (violations are listed, not fatal) and exit.
if (args.Length >= 1 && args[0] == "--mm-audit")
{
    Console.WriteLine(Cathedral.Game.Narrative.ModusMentisRuleValidator.BuildAuditReport());
    return;
}

// Compute-device audit: re-run the probe and print what it measured, ignoring the cached answer.
// The decision used to be invisible — a wrong one showed up as a game that sat on its loading bar,
// with nothing in the log to say the GPU had been picked on a rate that did not matter.
if (args.Length >= 1 && args[0] == "--llm-probe-audit")
{
    Console.WriteLine(Cathedral.LLM.LlamaProbe.BuildAuditReport());
    return;
}

// Validate the modus-mentis hard rules at every launch: any violation aborts the run.
try
{
    Cathedral.Game.Narrative.ModusMentisRuleValidator.ValidateOrThrow();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ MODUS MENTIS VALIDATION FAILED:\n{ex.Message}");
    Console.WriteLine("Run with --mm-audit for the full content report.");
    return;
}

// Validate the childhood-reminescence outcome rules: the protagonist must always leave the
// childhood phase with at least one Semantic, one Procedural and one Sensory modusMentis.
try
{
    Cathedral.Game.Narrative.Reminescence.ReminescenceRuleValidator.ValidateOrThrow();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ REMINESCENCE VALIDATION FAILED:\n{ex.Message}");
    return;
}

// Check for procedural music PoC mode
if (args.Length >= 1 && args[0] == "--music")
{
    Cathedral.Audio.MusicModeLauncher.Launch(args);
    return;
}

// Check for dialogue-tree viewer mode (graph every registered dialogue tree; no LLM/game needed)
if (args.Length >= 1 && args[0] == "--dialogue-view")
{
    Cathedral.Debug.DialogueViewLauncher.Launch();
    return;
}

// Check for fight area generator test mode
if (args.Length >= 1 && args[0] == "--fight-area")
{
    string mode = "random";
    var modeArgs = new System.Collections.Generic.Dictionary<string, string>();
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--mode" && i + 1 < args.Length)
        {
            mode = args[++i];
        }
        else if (args[i].StartsWith("--") && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        {
            string key = args[i][2..]; // strip "--"
            modeArgs[key] = args[++i];
        }
    }
    Cathedral.Fight.FightAreaTestLauncher.Launch(mode, modeArgs);
    return;
}

// Check for draw mode (display previously saved layered ASCII art)
if (args.Length >= 2 && args[0] == "--draw")
{
    string folderPath = args[1];
    Cathedral.Game.ImageToTextModeLauncher.LaunchDrawMode(folderPath);
    return;
}

// Check for image-to-text converter mode
if (args.Length >= 2 && args[0] == "--img-to-txt")
{
    string imagePath = args[1];
    int maxImageWidth = 0;   // 0 means use full terminal width
    int maxImageHeight = 0;  // 0 means use full terminal height
    bool useNegative = false;
    bool autoContrast = false;
    float manualContrast = 1.0f; // 1.0 = no change, >1.0 = increase, <1.0 = decrease
    bool stretchToFit = false;
    
    // Parse optional arguments
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--width" && i + 1 < args.Length && int.TryParse(args[i + 1], out int w))
        {
            maxImageWidth = w;
            i++; // Skip next arg
        }
        else if (args[i] == "--height" && i + 1 < args.Length && int.TryParse(args[i + 1], out int h))
        {
            maxImageHeight = h;
            i++; // Skip next arg
        }
        else if (args[i] == "--contrast" && i + 1 < args.Length && float.TryParse(args[i + 1], out float c))
        {
            manualContrast = c;
            i++; // Skip next arg
        }
        else if (args[i] == "--negative")
        {
            useNegative = true;
        }
        else if (args[i] == "--auto-contrast")
        {
            autoContrast = true;
        }
        else if (args[i] == "--stretch")
        {
            stretchToFit = true;
        }
    }
    
    Cathedral.Game.ImageToTextModeLauncher.Launch(imagePath, maxImageWidth, maxImageHeight, useNegative, autoContrast, manualContrast, stretchToFit);
    return;
}

// Check for --playground flag (replace all LLM calls with instant placeholders)
if (args.Any(a => a == "--playground"))
{
    Cathedral.Game.PlaygroundMode.IsActive = true;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("*** PLAYGROUND MODE ACTIVE ***");
    Console.WriteLine("All LLM calls are replaced with placeholder text. No LLM server needed.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --debug flag (can be combined with default launch)
if (args.Any(a => a == "--debug"))
{
    Cathedral.Game.DebugMode.IsActive = true;

    // The viewers are windows, and --hidden means no windows. This matters because every CLI script
    // passes --debug — it is what `strategy` needs — so without this a suite run opens the graph and
    // LLM viewers a hundred-odd times over, which is the thing --hidden exists to stop.
    Cathedral.Game.DebugMode.ShowViewers = !Cathedral.Config.Debug.HiddenWindow;

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("*** DEBUG MODE ACTIVE ***");
    if (Cathedral.Config.Debug.HiddenWindow)
        Console.WriteLine("Viewers suppressed by --hidden.");
    if (args.Any(a => a == "--cli" || a == "--cli-script"))
        Console.WriteLine("Combined with --cli: outcomes follow the preset `strategy` (no console prompts).");
    else
        Console.WriteLine("LLM critic decisions and dice rolls will prompt for manual override via console.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --cli flag (drive the game from stdin and observe it as text)
if (args.Any(a => a == "--cli") || args.Any(a => a == "--cli-script"))
{
    Cathedral.Game.Cli.CliMode.IsActive = true;
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--cli-script" && i + 1 < args.Length)
            Cathedral.Game.Cli.CliMode.ScriptPath = args[i + 1];
        if (args[i] == "--cli-timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out int secs))
            Cathedral.Game.Cli.CliMode.RunTimeout = TimeSpan.FromSeconds(Math.Max(5, secs));
    }
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("*** CLI MODE ACTIVE ***");
    Console.WriteLine("The game is driven from stdin. Type `help` for the command list.");
    if (Cathedral.Game.Cli.CliMode.ScriptPath != null)
        Console.WriteLine($"Script: {Cathedral.Game.Cli.CliMode.ScriptPath}");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --view flag: viewers only, no console overriding
if (args.Any(a => a == "--view"))
{
    // --hidden wins: asking for both is contradictory, and the one that says "put nothing on screen"
    // is the more specific instruction. Said out loud, because a silently ignored --view is worse.
    Cathedral.Game.DebugMode.ShowViewers = !Cathedral.Config.Debug.HiddenWindow;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("*** VIEW MODE ACTIVE ***");
    Console.WriteLine(Cathedral.Config.Debug.HiddenWindow
        ? "Viewers suppressed by --hidden, which overrides --view."
        : "LLM and scene viewers will open. No console decision overriding.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --skip-childhood flag (skip ChildhoodReminescence + GetUp, randomly fill outcomes)
if (args.Any(a => a == "--skip-childhood"))
{
    Cathedral.Game.SkipChildhoodMode.IsActive = true;
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("*** SKIP-CHILDHOOD MODE ACTIVE ***");
    Console.WriteLine("Childhood reminescence + get-up phases will be skipped; starting skills/items are randomized.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --mm flag (fill empty memory slots with random unheld modiMentis after childhood)
if (args.Any(a => a == "--mm"))
{
    Cathedral.Game.FillMemoryMode.IsActive = true;
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("*** FILL-MEMORY MODE ACTIVE ***");
    Console.WriteLine("After the childhood reminescence phase, empty memory slots are filled with random unheld modiMentis.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --fill-party flag (fill the companion roster to its heart-derived ceiling after childhood)
if (args.Any(a => a == "--fill-party"))
{
    Cathedral.Game.FillPartyMode.IsActive = true;
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("*** FILL-PARTY MODE ACTIVE ***");
    Console.WriteLine("After the childhood phase, the party is filled to max_companions with generated NPCs (last one a beast).");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --weapons flag (give protagonist starter weapons)
// --npc-static: pin every NPC to one area all day, so a test can name the room they are in.
// --grant-item <id[,id…]>: seed the starting pack. What makes a tool-gated verb's success test
// writable at all — see GrantItemMode.
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] != "--grant-item") continue;
    Cathedral.Game.GrantItemMode.ItemIds = args[i + 1]
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    break;
}

if (args.Any(a => a == "--weapons"))
{
    Cathedral.Game.WeaponsMode.IsActive = true;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("*** WEAPONS MODE ACTIVE ***");
    Console.WriteLine("Protagonist will start with an Arming Sword, a Hunting Bow, and a Round Shield.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --cpu flag (run LLM on CPU only)
if (args.Any(a => a == "--cpu"))
{
    Cathedral.Config.Debug.ForcedLlmDevice = Cathedral.LLM.LlamaComputeDevice.Cpu;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("*** CPU-ONLY MODE ***");
    Console.WriteLine("LLM will run entirely on CPU (no GPU layer offloading).");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --gpu flag (force the installed GPU backend, overriding the probe)
if (args.Any(a => a == "--gpu"))
{
    Cathedral.Config.Debug.ForcedLlmDevice = Cathedral.LLM.LlamaComputeDevice.Gpu;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("*** GPU MODE ***");
    Console.WriteLine("LLM will use the installed GPU backend. Falls back to CPU if it cannot serve the model.");
    Console.ResetColor();
    Console.WriteLine();
}

// Check for --no-llm-probe (skip first-run hardware detection)
if (args.Any(a => a == "--no-llm-probe"))
{
    Cathedral.LLM.LlamaProbe.Enabled = false;
}

// Check for --no-developer-keys: behave as a shipped build does, in a development build. The only
// way to exercise the shipped keyboard without building and running a shipped executable, which
// cannot be driven by --cli.
if (args.Any(a => a == "--no-developer-keys"))
{
    Cathedral.Config.Debug.DeveloperKeys = false;
}

// Reported every run, so a --cli script (and a player's log) can tell which keyboard is live.
// ASCII only: this goes to a console whose code page mangles an em dash when redirected.
Console.WriteLine(Cathedral.Config.Debug.DeveloperKeys
    ? "Developer keys: enabled (D/M/F/G/H/J, C/V, W/S zoom)"
    : "Developer keys: disabled - arrows rotate, Space re-centres, Escape opens the menu");

// (--seed is parsed and locked in at the very top of this file — see the comment there.)

// Debug placement/time flags. Both are inert unless passed, and exist so a --cli script can reach a
// feature directly instead of depending on where the seed happened to put the protagonist.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--start-at" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.StartAt = args[i + 1];

    if (args[i] == "--start-area" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.StartArea = args[i + 1];

    if (args[i] == "--observe-only" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.ObserveOnly = args[i + 1];

    if (args[i] == "--period" && i + 1 < args.Length &&
        Enum.TryParse<Cathedral.Game.Narrative.TimePeriod>(args[i + 1], ignoreCase: true, out var forced))
        Cathedral.Config.Debug.ForcedPeriod = forced;

    if (args[i] == "--no-encounters")
        Cathedral.Config.Debug.NoEncounters = true;

    if (args[i] == "--allow-reentry")
        Cathedral.Config.Debug.AllowReentry = true;

    if (args[i] == "--start-fight" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.StartFight = args[i + 1];

    if (args[i] == "--encounter-on-arrival" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.EncounterOnArrival = args[i + 1];

    if (args[i] == "--spawn-beast" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.SpawnBeast = args[i + 1];

    if (args[i] == "--goal-only" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.GoalOnly = args[i + 1];

    if (args[i] == "--advance-days" && i + 1 < args.Length &&
        double.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var advDays))
        Cathedral.Config.Debug.AdvanceDays = advDays;

    // --black-bile: sour every humor queue, so the next journey starves the protagonist.
    if (args[i] == "--black-bile")
        Cathedral.Config.Debug.BlackBile = true;

    // --grant-mm <id[,id...]>[:level]
    if (args[i] == "--grant-mm" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
    {
        var spec  = args[i + 1];
        int level = 1;
        int colon = spec.LastIndexOf(':');
        if (colon > 0 && int.TryParse(spec[(colon + 1)..], out var lvl))
        {
            level = lvl;
            spec  = spec[..colon];
        }
        var ids = spec.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => s.Length > 0)
                      .ToArray();
        if (ids.Length > 0)
            Cathedral.Config.Debug.GrantModiMentis = (ids, level);
    }
}

// Check for --dither [mode[:levels[:scale]]] — turns on the final full-screen shader layer.
// Bare --dither is bayer:4:2; F/G/H retune it live once the window is up.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] != "--dither") continue;

    // The flag is this run's instruction and outranks the saved on/off state, which is applied
    // at startup only when nothing on the command line has spoken for the layer.
    Cathedral.Config.PostProcess.DitherModeSetByFlag = true;
    Cathedral.Config.PostProcess.DitherMode = 1;

    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
    {
        string[] parts = args[i + 1].Split(':');
        Cathedral.Config.PostProcess.DitherMode = parts[0].ToLowerInvariant() switch
        {
            "off" => 0,
            "bayer" => 1,
            "mono" => 2,
            "noise" => 3,
            _ => 1
        };
        if (parts.Length > 1 && int.TryParse(parts[1], out int levels))
            Cathedral.Config.PostProcess.Levels = Math.Max(2, levels);
        if (parts.Length > 2 && int.TryParse(parts[2], out int scale))
            Cathedral.Config.PostProcess.PixelScale = Math.Max(1, scale);
    }

    Console.WriteLine($"Post-process: dither mode={Cathedral.Config.PostProcess.DitherMode} " +
                      $"levels={Cathedral.Config.PostProcess.Levels} pixelScale={Cathedral.Config.PostProcess.PixelScale}");
    break;
}

// Validate narrative structure at startup
try
{
    Cathedral.Game.Narrative.NarrativeValidator.ValidateNarrativeStructure();
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ NARRATIVE STRUCTURE VALIDATION FAILED: {ex.Message}");
    Console.WriteLine("Please fix the issues before continuing.");
    return;
}

Console.WriteLine("=== Cathedral - Location Travel Mode ===\n");
Console.WriteLine("Launching the integrated narrative exploration system...");
Console.WriteLine("Press Ctrl+C to exit at any time.\n");

Cathedral.Game.LocationTravelModeLauncher.Launch();

// A --cli run fails its build step when any `expect` assertion failed.
if (Cathedral.Game.Cli.CliMode.IsActive && Cathedral.Game.Cli.CliMode.HasFailedAssertion)
{
    Console.WriteLine("[cli] run finished with FAILED assertions");
    Environment.ExitCode = 1;
}


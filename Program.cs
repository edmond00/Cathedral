using System.Linq;
using Cathedral.LLM;
using Cathedral.Game;

// ── Master seed: resolved FIRST, before any other flag is looked at ──────────
// Every other flag handler below sets a static on some mode class, and touching one of those runs
// its static initializers — several of which ask GameRng for a stream. The first such ask resolves
// the master seed permanently, so parsing --seed any later than this silently produced a time-based
// run with --seed on the command line (which is exactly what happened with --skip-childhood).
// Nothing above this line may touch game state.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedSeed))
    {
        Cathedral.Config.Rng.Seed = parsedSeed;
        break;
    }
}
Cathedral.GameRng.Initialize(Cathedral.Config.Rng.Seed);

// Check for help option
if (args.Length >= 1 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Usage: Cathedral [options]");
    Console.WriteLine();
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
    Console.WriteLine("  --fill-party                       DEBUG: after the childhood phase, fill the party to max_companions with generated");
    Console.WriteLine("                                     NPCs — the last slot a beast, every slot before it a human");
    Console.WriteLine("  --cpu                              Run LLM on CPU only (no GPU offloading)");
    Console.WriteLine("  --seed <n>                         Fix the master RNG seed for a reproducible run (world, spawn, dice)");
    Console.WriteLine("  --start-at <name>                  DEBUG: spawn on the first biome/location matching <name> (e.g. village, farm)");
    Console.WriteLine("  --period <name>                    DEBUG: arrive at every location at this time of day (dawn…night) instead of a random one");
    Console.WriteLine("  --dither [mode[:levels[:scale]]]   Retune the final full-screen dither layer (mode off|bayer|mono|noise).");
    Console.WriteLine("                                     Resting state is bayer:6:1; game events pulse it for 0.15s.");
    Console.WriteLine("                                     In-game: F cycles the mode, G the palette depth, H the grain, J the event pulses");
    Console.WriteLine("  --no-encounters                    DEBUG: never roll a random travel encounter. For scripted runs: an");
    Console.WriteLine("                                     encounter puts the game in EncounterPrompt, where a script waiting");
    Console.WriteLine("                                     for LocationInteraction hangs until its timeout");
    Console.WriteLine("  --mm-audit                         Print the modus-mentis content audit (hard-rule violations, coverage, soft stats) and exit");
    Console.WriteLine("  --verb-audit                       Print the verb-coverage audit (verbs per observable vs targets, dead verbs,");
    Console.WriteLine("                                     unresolvable modus-mentis and tool ids, landmark counts) and exit");
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

// Modus-mentis content audit mode: print the report (violations are listed, not fatal) and exit.
if (args.Length >= 1 && args[0] == "--mm-audit")
{
    Console.WriteLine(Cathedral.Game.Narrative.ModusMentisRuleValidator.BuildAuditReport());
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
    Cathedral.Game.DebugMode.ShowViewers = true;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("*** DEBUG MODE ACTIVE ***");
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
    Cathedral.Game.DebugMode.ShowViewers = true;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("*** VIEW MODE ACTIVE ***");
    Console.WriteLine("LLM and scene viewers will open. No console decision overriding.");
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
    Cathedral.Config.LLM.GpuLayers = 0;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("*** CPU-ONLY MODE ***");
    Console.WriteLine("LLM will run entirely on CPU (no GPU layer offloading).");
    Console.ResetColor();
    Console.WriteLine();
}

// (--seed is parsed and locked in at the very top of this file — see the comment there.)

// Debug placement/time flags. Both are inert unless passed, and exist so a --cli script can reach a
// feature directly instead of depending on where the seed happened to put the protagonist.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--start-at" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        Cathedral.Config.Debug.StartAt = args[i + 1];

    if (args[i] == "--period" && i + 1 < args.Length &&
        Enum.TryParse<Cathedral.Game.Narrative.TimePeriod>(args[i + 1], ignoreCase: true, out var forced))
        Cathedral.Config.Debug.ForcedPeriod = forced;

    if (args[i] == "--no-encounters")
        Cathedral.Config.Debug.NoEncounters = true;
}

// Check for --dither [mode[:levels[:scale]]] — turns on the final full-screen shader layer.
// Bare --dither is bayer:4:2; F/G/H retune it live once the window is up.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] != "--dither") continue;

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


using System.Linq;
using Cathedral.LLM;
using Cathedral.Game;

// Check for help option
if (args.Length >= 1 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Usage: Cathedral [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  (no args)                          Launch the narrative exploration game");
    Console.WriteLine("  --fight                            Run the fight loop (turn-based combat test)");
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
    Console.WriteLine("  --dialogue                         Run the dialogue system demo (NPC conversation test)");
    Console.WriteLine("  --debug                            Enable debug mode (override LLM/RNG decisions via console)");
    Console.WriteLine("  --help, -h                         Show this help message");
    return;
}

// Check for fight mode
if (args.Length >= 1 && args[0] == "--fight")
{
    Cathedral.Fight.FightModeLauncher.Launch();
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

// Check for dialogue demo mode
if (args.Length >= 1 && args[0] == "--dialogue")
{
    Cathedral.Game.Dialogue.Demo.DialogueDemoLauncher.Launch();
    return;
}

// Check for --debug flag (can be combined with default launch)
if (args.Any(a => a == "--debug"))
{
    Cathedral.Game.DebugMode.IsActive = true;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("*** DEBUG MODE ACTIVE ***");
    Console.WriteLine("LLM critic decisions and dice rolls will prompt for manual override via console.");
    Console.ResetColor();
    Console.WriteLine();
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


using Cathedral.LLM;
using Cathedral.Game;

// Check for image-to-text converter mode
if (args.Length >= 2 && args[0] == "--img-to-txt")
{
    string imagePath = args[1];
    int maxImageWidth = 0;   // 0 means use full terminal width
    int maxImageHeight = 0;  // 0 means use full terminal height
    
    // Parse optional width/height arguments to constrain image size within terminal
    for (int i = 2; i < args.Length - 1; i++)
    {
        if (args[i] == "--width" && int.TryParse(args[i + 1], out int w))
            maxImageWidth = w;
        else if (args[i] == "--height" && int.TryParse(args[i + 1], out int h))
            maxImageHeight = h;
    }
    
    Cathedral.Game.ImageToTextModeLauncher.Launch(imagePath, maxImageWidth, maxImageHeight);
    return;
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


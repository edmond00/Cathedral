namespace Cathedral.Audio;

/// <summary>
/// Standalone interactive console for the procedural ambient music PoC.
/// Launch with: dotnet run -- --music
///
/// Key bindings:
///   1–4        Set number of active tracks
///   S / s      Sadness  +0.1 / -0.1
///   F / f      Fear     +0.1 / -0.1
///   M / m      Mystery  +0.1 / -0.1
///   N          Reset mood to Neutral
///   C          SFX: ButtonClick
///   R          SFX: NarrativeReveal
///   E          SFX: MemoryFragment
///   U          SFX: TransitionUp
///   D          SFX: TransitionDown
///   Q / Escape Quit
/// </summary>
public static class MusicModeLauncher
{
    public static void Launch(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Clear();
        Console.CursorVisible = false;
        Console.Title = "Cathedral — Procedural Ambient Music PoC";

        using var engine = new AmbianceEngine();

        bool deviceFound = engine.Start();

        if (!deviceFound)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: No MIDI output device found. Notes will not sound.");
            Console.WriteLine("         Install 'Microsoft GS Wavetable Synth' or a virtual MIDI port.");
            Console.ResetColor();
            Console.WriteLine();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"MIDI device: {engine.DeviceName}");
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.WriteLine("Cathedral — Procedural Ambient Music PoC");
        Console.WriteLine("Press H for help. Press Q or Escape to quit.");
        Console.WriteLine();

        // UI refresh loop
        var uiTask = Task.Run(() => UiRefreshLoop(engine));

        // Input loop (blocks main thread)
        InputLoop(engine);

        // Shutdown
        Console.CursorVisible = true;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private static void InputLoop(AmbianceEngine engine)
    {
        while (true)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(30);
                continue;
            }

            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    return;

                case ConsoleKey.D1: engine.SetActiveTrackCount(1); break;
                case ConsoleKey.D2: engine.SetActiveTrackCount(2); break;
                case ConsoleKey.D3: engine.SetActiveTrackCount(3); break;
                case ConsoleKey.D4: engine.SetActiveTrackCount(4); break;

                case ConsoleKey.S when key.Modifiers == ConsoleModifiers.None:
                    engine.SetMood(engine.CurrentMood.WithSadness(+0.1f)); break;
                case ConsoleKey.S when key.Modifiers == ConsoleModifiers.Shift:
                    engine.SetMood(engine.CurrentMood.WithSadness(-0.1f)); break;

                case ConsoleKey.F when key.Modifiers == ConsoleModifiers.None:
                    engine.SetMood(engine.CurrentMood.WithFear(+0.1f)); break;
                case ConsoleKey.F when key.Modifiers == ConsoleModifiers.Shift:
                    engine.SetMood(engine.CurrentMood.WithFear(-0.1f)); break;

                case ConsoleKey.M when key.Modifiers == ConsoleModifiers.None:
                    engine.SetMood(engine.CurrentMood.WithMystery(+0.1f)); break;
                case ConsoleKey.M when key.Modifiers == ConsoleModifiers.Shift:
                    engine.SetMood(engine.CurrentMood.WithMystery(-0.1f)); break;

                case ConsoleKey.I when key.Modifiers == ConsoleModifiers.None:
                    engine.SetMood(engine.CurrentMood.WithIntensity(+0.1f)); break;
                case ConsoleKey.I when key.Modifiers == ConsoleModifiers.Shift:
                    engine.SetMood(engine.CurrentMood.WithIntensity(-0.1f)); break;

                case ConsoleKey.N:
                    engine.SetMood(MusicMoodState.Neutral);
                    engine.SetActiveTrackCount(1);
                    break;

                // Preset moods — archetype demos
                case ConsoleKey.T:
                    engine.SetMood(MusicMoodState.Tavern); break;
                case ConsoleKey.L:
                    engine.SetMood(MusicMoodState.Lament); break;
                case ConsoleKey.X:
                    engine.SetMood(MusicMoodState.DarkDungeon); break;
                case ConsoleKey.B:
                    engine.SetMood(MusicMoodState.Battle); break;
                case ConsoleKey.W:
                    engine.SetMood(MusicMoodState.WorldView); break;

                // SFX demos
                case ConsoleKey.C:
                    engine.PlaySoundEffect(SoundEffectType.ButtonClick); break;
                case ConsoleKey.R:
                    engine.PlaySoundEffect(SoundEffectType.NarrativeReveal); break;
                case ConsoleKey.E when key.Modifiers == ConsoleModifiers.None:
                    engine.PlaySoundEffect(SoundEffectType.MemoryFragment); break;
                case ConsoleKey.U:
                    engine.PlaySoundEffect(SoundEffectType.TransitionUp); break;
                case ConsoleKey.D when key.Modifiers == ConsoleModifiers.None:
                    engine.PlaySoundEffect(SoundEffectType.TransitionDown); break;

                case ConsoleKey.H:
                    ShowHelp(); break;
            }
        }
    }

    // ── UI refresh ─────────────────────────────────────────────────────────────

    private static void UiRefreshLoop(AmbianceEngine engine)
    {
        while (true)
        {
            try
            {
                DrawStatus(engine);
            }
            catch { /* console resize, etc. */ }
            Thread.Sleep(200);
        }
    }

    private static void DrawStatus(AmbianceEngine engine)
    {
        int top = 4; // rows below the header
        MusicMoodState mood = engine.CurrentMood;

        SetCursor(0, top);
        ClearLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("┌─ Status ");
        Console.Write(new string('─', 48));
        Console.WriteLine('┐');
        Console.ResetColor();

        // Device
        SetCursor(0, top + 1); ClearLine();
        WriteField("  Device",     engine.DeviceName, engine.IsDeviceOpen ? ConsoleColor.Green : ConsoleColor.DarkRed);

        // Scale & BPM
        SetCursor(0, top + 2); ClearLine();
        WriteField("  Scale",      engine.CurrentScaleName, ConsoleColor.Yellow);
        Console.Write("   ");
        WriteField("BPM", $"{engine.CurrentBpm:F1}", ConsoleColor.Yellow);

        // Active tracks
        SetCursor(0, top + 3); ClearLine();
        string trackBar = BuildTrackBar(engine.ActiveTrackCount);
        WriteField("  Tracks", trackBar, ConsoleColor.White);

        // Mood bars
        SetCursor(0, top + 4); ClearLine();
        WriteBar("  Sadness", mood.Sadness, ConsoleColor.Magenta);

        SetCursor(0, top + 5); ClearLine();
        WriteBar("  Fear   ", mood.Fear,    ConsoleColor.DarkRed);

        SetCursor(0, top + 6); ClearLine();
        WriteBar("  Mystery", mood.Mystery, ConsoleColor.DarkCyan);

        SetCursor(0, top + 7); ClearLine();
        WriteBar("  Intensity", mood.Intensity, ConsoleColor.Green);

        SetCursor(0, top + 8); ClearLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("└");
        Console.Write(new string('─', 57));
        Console.WriteLine('┘');
        Console.ResetColor();

        SetCursor(0, top + 10); ClearLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  S/s=Sadness  F/f=Fear  M/m=Mystery  I/i=Intensity  1-4=Tracks  T/L/X/B/W=Preset  H=Help  Q=Quit");
        Console.ResetColor();
    }

    private static void WriteField(string label, string value, ConsoleColor valueColor)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{label,-12}: ");
        Console.ForegroundColor = valueColor;
        Console.Write(value);
        Console.ResetColor();
    }

    private static void WriteBar(string label, float value, ConsoleColor color)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{label,-12}: ");
        Console.ForegroundColor = color;
        int filled = (int)(value * 30);
        Console.Write('[');
        Console.Write(new string('█', filled));
        Console.Write(new string('░', 30 - filled));
        Console.Write($"] {value:F2}");
        Console.ResetColor();
    }

    private static string BuildTrackBar(int active)
    {
        var sb = new System.Text.StringBuilder();
        var names = new[] { "Drone", "Melody", "Counter", "Texture" };
        for (int i = 0; i < 4; i++)
        {
            if (i < active)
                sb.Append($"[{names[i]}] ");
            else
                sb.Append($" {names[i]}  ");
        }
        return sb.ToString().TrimEnd();
    }

    private static void SetCursor(int x, int y)
    {
        if (y >= Console.BufferHeight) return;
        Console.SetCursorPosition(x, y);
    }

    private static void ClearLine()
    {
        int width = Math.Min(Console.WindowWidth, 80);
        Console.Write(new string(' ', width));
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    // ── Help ───────────────────────────────────────────────────────────────────

    private static void ShowHelp()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Cathedral — Procedural Music PoC — Key Bindings");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("  1 / 2 / 3 / 4    Set number of active tracks");
        Console.WriteLine();
        Console.WriteLine("  S                Sadness  +0.1 (more melancholic)");
        Console.WriteLine("  Shift+S          Sadness  -0.1");
        Console.WriteLine("  F                Fear     +0.1  (dissonance, broken rhythms)");
        Console.WriteLine("  Shift+F          Fear     -0.1");
        Console.WriteLine("  M                Mystery  +0.1");
        Console.WriteLine("  Shift+M          Mystery  -0.1");
        Console.WriteLine("  I                Intensity +0.1  (Drone►0.10, Melody►0.35, Counter►0.60, Texture►0.85)");
        Console.WriteLine("  Shift+I          Intensity -0.1");
        Console.WriteLine("  N                Reset to Neutral mood (1 track)");
        Console.WriteLine();
        Console.WriteLine("  T                Preset: Tavern       (bright, lively)");
        Console.WriteLine("  L                Preset: Lament       (sad, slow, calm)");
        Console.WriteLine("  X                Preset: Dark Dungeon (mysterious, sparse)");
        Console.WriteLine("  B                Preset: Battle       (tense, staccato, fast)");
        Console.WriteLine("  W                Preset: WorldView    (exploration)");
        Console.WriteLine();
        Console.WriteLine("  C                SFX: ButtonClick");
        Console.WriteLine("  R                SFX: NarrativeReveal");
        Console.WriteLine("  E                SFX: MemoryFragment");
        Console.WriteLine("  U                SFX: TransitionUp");
        Console.WriteLine("  D                SFX: TransitionDown");
        Console.WriteLine();
        Console.WriteLine("  H                This help screen");
        Console.WriteLine("  Q / Escape       Quit");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Press any key to return...");
        Console.ResetColor();
        Console.ReadKey(intercept: true);
        Console.Clear();
        Console.WriteLine("Cathedral — Procedural Ambient Music PoC");
        Console.WriteLine("Press H for help. Press Q or Escape to quit.");
        Console.WriteLine();
    }
}

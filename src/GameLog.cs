using System;
using System.IO;
using System.Text;

namespace Cathedral;

/// <summary>
/// One log file for the whole run: everything the game prints, plus everything llama-server
/// prints, in the order it happened.
///
/// <para><b>What problem this solves.</b> Diagnosing a player's install used to mean asking for a
/// directory: <c>logs/llm_session_&lt;stamp&gt;/</c> with a subdirectory per slot and a further one
/// per request, none of which contains the game's own console output — and the console output is
/// where the model name, the compute device, the audio device and every startup failure are
/// reported. The two halves had to be read side by side and neither was a file anyone could
/// attach to a bug report. <c>log.txt</c> is that file.</para>
///
/// <para><b>Truncated on every launch.</b> A log that grows forever is one nobody can read and
/// that quietly fills a disk — and the question being asked is almost always "what happened
/// *this* time". The previous run's log is gone the moment a new one starts, deliberately: if a
/// player is to send it, it has to describe the run they just had.</para>
///
/// <para><b>It never takes the game down.</b> Opening the file, writing to it and closing it are
/// all best-effort. An install in a directory the player cannot write to loses its log and nothing
/// else — the same rule the LLM session logging follows.</para>
/// </summary>
public static class GameLog
{
    /// <summary>Beside the working directory, which for a double-clicked game is its own folder.</summary>
    public const string FileName = "log.txt";

    private static readonly object Gate = new();
    private static StreamWriter? _file;
    private static TextWriter? _realConsoleOut;

    /// <summary>Whether the log file is open. False when it could not be created.</summary>
    public static bool IsOpen => _file != null;

    /// <summary>The absolute path being written to, or null.</summary>
    public static string? Path { get; private set; }

    /// <summary>
    /// Opens <c>log.txt</c> (replacing any previous one) and redirects the console through it, so
    /// that everything written from here on is both shown and recorded.
    ///
    /// <para>Call once, as early as possible — anything printed before this is shown but not
    /// logged.</para>
    /// </summary>
    public static void Initialize()
    {
        if (_file != null) return;

        try
        {
            var path = System.IO.Path.Combine(Environment.CurrentDirectory, FileName);

            // FileMode.Create truncates: one run, one log.
            // FileShare.Read so the file can be tailed or opened in an editor while the game runs.
            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            _file = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                // The run that matters most is the one that crashed, and a buffered log loses its
                // last and most interesting lines. Correctness over throughput.
                AutoFlush = true
            };
            Path = path;

            _realConsoleOut = Console.Out;
            Console.SetOut(new TeeWriter(_realConsoleOut, _file));
            // stderr goes to the same file rather than a second one: an error is only meaningful
            // next to the output that preceded it.
            Console.SetError(new TeeWriter(Console.Error, _file));

            WriteLine($"=== {Config.Name.WindowTitle} — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        }
        catch (Exception ex)
        {
            _file = null;
            Path = null;
            Console.Error.WriteLine($"Could not open {FileName} ({ex.Message}). The game runs normally; this run is not logged.");
        }
    }

    /// <summary>
    /// Writes a line to the log <b>without</b> echoing it to the console.
    ///
    /// <para>This is how llama-server's output gets in. It is far too verbose to show — model
    /// loading alone is hundreds of lines of tensor bookkeeping — but it is exactly what is needed
    /// when the model fails to load, so it belongs in the file and not on screen.</para>
    /// </summary>
    public static void WriteToFileOnly(string line)
    {
        if (_file == null) return;
        try
        {
            lock (Gate) _file.WriteLine(line);
        }
        catch { /* a full disk costs the log, not the game */ }
    }

    /// <summary>Writes to both console and log, like an ordinary <c>Console.WriteLine</c>.</summary>
    public static void WriteLine(string line) => Console.WriteLine(line);

    /// <summary>Flushes and closes the file, restoring the plain console.</summary>
    public static void Close()
    {
        lock (Gate)
        {
            if (_file == null) return;
            try
            {
                if (_realConsoleOut != null) Console.SetOut(_realConsoleOut);
                _file.Flush();
                _file.Dispose();
            }
            catch { }
            _file = null;
            Path = null;
        }
    }

    /// <summary>
    /// Writes to the console and to the file at once. Only the three primitives are overridden —
    /// every other <see cref="TextWriter"/> overload is implemented in terms of them, so this
    /// catches formatted writes, interpolation and <c>Write(object)</c> without listing them.
    /// </summary>
    private sealed class TeeWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly TextWriter _target;

        public TeeWriter(TextWriter console, TextWriter target)
        {
            _console = console;
            _target = target;
        }

        public override Encoding Encoding => _console.Encoding;

        public override void Write(char value)
        {
            _console.Write(value);
            try { lock (Gate) _target.Write(value); } catch { }
        }

        public override void Write(string? value)
        {
            _console.Write(value);
            try { lock (Gate) _target.Write(value); } catch { }
        }

        public override void WriteLine(string? value)
        {
            _console.WriteLine(value);
            try { lock (Gate) _target.WriteLine(value); } catch { }
        }

        public override void Flush()
        {
            _console.Flush();
            try { lock (Gate) _target.Flush(); } catch { }
        }
    }
}

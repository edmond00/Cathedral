using System;
using System.Collections.Generic;
using System.IO;

namespace Cathedral;

/// <summary>
/// <c>--demo-audit</c>: the demo run limit, exercised against throwaway folders. Headless — no window,
/// no LLM, and it never touches the real state files.
///
/// <para>Every case is a way a player might try to get another run, or a way the files might
/// honestly go wrong, and each asserts that the answer is never "one more run than was paid for".
/// Output follows the other audits: a <c>✗</c> line is a failure, which is what run_tests.sh greps.</para>
/// </summary>
public static class DemoLimitAudit
{
    private const int Max = 3;

    public static void Run()
    {
        Console.WriteLine("=== DEMO LIMIT AUDIT ===\n");
        string root = Path.Combine(Path.GetTempPath(), "pp-demo-audit-" + Guid.NewGuid().ToString("N"));
        var failures = new List<string>();
        int passed = 0;

        void Case(string name, Func<DemoStore, string?> body)
        {
            string dir = Path.Combine(root, (passed + failures.Count).ToString("D2"));
            string? problem;
            try { problem = body(DemoStore.InDirectory(dir)); }
            catch (Exception e) { problem = "threw " + e.GetType().Name + ": " + e.Message; }

            if (problem is null) { passed++; Console.WriteLine($"  ✓ {name}"); }
            else { failures.Add(name); Console.WriteLine($"  ✗ {name} — {problem}"); }
        }

        try
        {
            Case("a fresh installation has every run", s =>
            {
                var st = s.Load(Max);
                return st is { State: DemoState.Available, RunsUsed: 0 } ? null : $"got {st}";
            });

            Case("looking at the menu writes nothing", s =>
            {
                s.Load(Max);
                return File.Exists(s.PrimaryPath) || File.Exists(s.SecondaryPath) ? "a copy was created" : null;
            });

            Case("a run is recorded in both copies before it starts", s =>
            {
                if (!s.TryConsume(Max, out _)) return "refused the first run";
                // A different store object over the same files: what a relaunch after a crash sees.
                var after = new DemoStore(s.PrimaryPath, s.SecondaryPath).Load(Max);
                if (after.RunsUsed != 1) return $"relaunch sees {after.RunsUsed} run(s)";
                return File.ReadAllText(s.PrimaryPath) == File.ReadAllText(s.SecondaryPath) ? null : "copies differ";
            });

            Case($"the limit holds at {Max}", s =>
            {
                for (int i = 0; i < Max; i++) if (!s.TryConsume(Max, out _)) return $"refused run {i + 1}";
                if (s.TryConsume(Max, out var st)) return "granted a run past the limit";
                if (st.State != DemoState.Exhausted) return $"state {st.State}";
                return s.Load(Max).RunsUsed == Max ? null : "the refused run was counted";
            });

            Case("a limit of zero allows nothing", s =>
                s.TryConsume(0, out _) ? "granted a run" : null);

            Case("editing the count is detected", s =>
            {
                Spend(s, 2);
                Rewrite(s.PrimaryPath, t => t.Replace("runs=2", "runs=0"));
                Rewrite(s.SecondaryPath, t => t.Replace("runs=2", "runs=0"));
                return ExpectLocked(s);
            });

            Case("editing the count in one copy is detected", s =>
            {
                Spend(s, 2);
                Rewrite(s.SecondaryPath, t => t.Replace("runs=2", "runs=0"));
                return ExpectLocked(s);
            });

            Case("editing the signature is detected", s =>
            {
                Spend(s, 1);
                Rewrite(s.PrimaryPath, t => t.Replace("mac=", "mac=00"));
                return ExpectLocked(s);
            });

            Case("a corrupted copy locks rather than resets", s =>
            {
                Spend(s, 1);
                File.WriteAllText(s.PrimaryPath, "\0\0garbage");
                return ExpectLocked(s);
            });

            Case("an empty copy locks rather than resets", s =>
            {
                Spend(s, 1);
                File.WriteAllText(s.SecondaryPath, "");
                return ExpectLocked(s);
            });

            Case("a lock is not cleared by looking again", s =>
            {
                Spend(s, 1);
                File.WriteAllText(s.PrimaryPath, "x");
                s.Load(Max);
                return s.Load(Max).State == DemoState.Locked ? null : "unlocked itself";
            });

            Case("deleting the primary copy keeps the count", s =>
            {
                Spend(s, 2);
                File.Delete(s.PrimaryPath);
                var st = s.Load(Max);
                if (st.RunsUsed != 2) return $"count became {st.RunsUsed}";
                return File.Exists(s.PrimaryPath) ? null : "the missing copy was not restored";
            });

            Case("deleting the secondary copy keeps the count", s =>
            {
                Spend(s, 2);
                File.Delete(s.SecondaryPath);
                if (!s.TryConsume(Max, out var st)) return "refused the last run";
                return st.RunsUsed == 3 && s.Load(Max).State == DemoState.Exhausted ? null : $"got {st}";
            });

            Case("copies that disagree take the higher count", s =>
            {
                Spend(s, 1);
                string older = File.ReadAllText(s.SecondaryPath);
                Spend(s, 1);
                File.WriteAllText(s.SecondaryPath, older);        // a backup of one copy, restored
                var st = s.Load(Max);
                if (st.RunsUsed != 2) return $"count became {st.RunsUsed}";
                return File.ReadAllText(s.SecondaryPath) == File.ReadAllText(s.PrimaryPath) ? null : "not repaired";
            });

            Case("copies from two installations lock", s =>
            {
                Spend(s, 2);
                File.WriteAllText(s.SecondaryPath, DemoStore.Serialize(new string('A', 32), 0));
                return ExpectLocked(s);
            });

            Case("a run that cannot be recorded does not start", s =>
            {
                // A file where each copy's folder should be: neither can be written.
                foreach (var p in new[] { s.PrimaryPath, s.SecondaryPath })
                {
                    string folder = Path.GetDirectoryName(p)!;
                    Directory.CreateDirectory(Path.GetDirectoryName(folder)!);
                    File.WriteAllText(folder, "");
                }
                return s.TryConsume(Max, out _) ? "started a run with nothing recorded" : null;
            });

            Case("the state lives apart from the save and the install folder", _ =>
            {
                var d = DemoStore.Default();
                string save = Path.GetFullPath(Cathedral.Game.Save.SaveFile.Path_);
                string install = Path.GetFullPath(AppContext.BaseDirectory);
                foreach (var p in new[] { d.PrimaryPath, d.SecondaryPath })
                {
                    string full = Path.GetFullPath(p);
                    if (full == save) return $"{p} is the save";
                    if (full.StartsWith(install, StringComparison.OrdinalIgnoreCase)) return $"{p} is in the install folder";
                }
                return Path.GetDirectoryName(d.PrimaryPath) == Path.GetDirectoryName(d.SecondaryPath)
                    ? "both copies share a folder" : null;
            });
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* temp; the OS will have it */ }
        }

        Console.WriteLine();
        Console.WriteLine("Not covered, by design: a key extracted from the executable signs anything, and");
        Console.WriteLine("deleting or restoring BOTH copies together is a fresh installation. See DemoLimit.");
        Console.WriteLine();
        Console.WriteLine(failures.Count == 0
            ? $"All {passed} demo-limit checks passed."
            : $"--- {failures.Count} FAILURE(S) of {passed + failures.Count} demo-limit checks");
        Environment.ExitCode = failures.Count;
    }

    private static void Spend(DemoStore s, int runs)
    {
        for (int i = 0; i < runs; i++)
            if (!s.TryConsume(Max, out var st)) throw new InvalidOperationException($"setup refused a run: {st}");
    }

    private static void Rewrite(string path, Func<string, string> edit) =>
        File.WriteAllText(path, edit(File.ReadAllText(path)));

    private static string? ExpectLocked(DemoStore s)
    {
        var st = s.Load(Max);
        if (st.State != DemoState.Locked) return $"state {st.State} with {st.RunsUsed} used";
        return s.TryConsume(Max, out _) ? "granted a run anyway" : null;
    }
}

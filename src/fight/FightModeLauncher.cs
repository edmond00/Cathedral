using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Cathedral.Fight.Generators;
using Cathedral.Game;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>
/// Entry point for the <c>--fight</c> CLI option.
/// Generates an arena, creates fighters, and opens the fight window.
/// </summary>
public static class FightModeLauncher
{
    public static void Launch()
    {
        Console.WriteLine("=== Cathedral Fight Mode ===");

        var rng        = new Random();
        var registry   = ModusMentisRegistry.Instance;
        var skillReg   = FightingSkillRegistry.Instance;
        var seed       = Environment.TickCount;

        // ── 1. Generate arena ──────────────────────────────────────
        var generator = new ArenaGenerator { Seed = seed };
        var area      = generator.Generate();
        Console.WriteLine($"Arena generated (seed={seed})");

        // ── 2. Create protagonist ──────────────────────────────────
        var protagonist = new Protagonist();
        protagonist.InitializeModiMentis(registry, 15);

        // Give the protagonist a weapon
        var sword = new Content.IronSword();
        protagonist.TryAcquireItem(sword);

        // ── 3. Create enemies ──────────────────────────────────────
        var enemy1 = new EnemyCombatant("Thug", SpeciesRegistry.Human);
        enemy1.InitializeModiMentis(registry, 8);

        var enemy2 = new EnemyCombatant("Brute", SpeciesRegistry.Human);
        enemy2.InitializeModiMentis(registry, 8);

        // ── 4. Wrap in Fighter ─────────────────────────────────────
        var partyFighter = new Fighter(protagonist,
            FightArea.ZoneColStart + 2, FightArea.PlayerRowStart + 1,
            isPlayerControlled: true, FighterFaction.Party);

        var enemyFighter1 = new Fighter(enemy1,
            FightArea.ZoneColStart + 2, FightArea.EnemyRowStart + 1,
            isPlayerControlled: false, FighterFaction.Enemy);

        var enemyFighter2 = new Fighter(enemy2,
            FightArea.ZoneColStart + 5, FightArea.EnemyRowStart + 1,
            isPlayerControlled: false, FighterFaction.Enemy);

        // ── 5. Roll initiative ─────────────────────────────────────
        foreach (var f in new[] { partyFighter, enemyFighter1, enemyFighter2 })
            f.InitiativeRoll = rng.Next(1, 7) + f.InitiativeValue;

        // Sort by initiative (descending), break ties by faction (party first)
        var fighters = new List<Fighter> { partyFighter, enemyFighter1, enemyFighter2 };
        fighters.Sort((a, b) =>
        {
            int cmp = b.InitiativeRoll.CompareTo(a.InitiativeRoll);
            if (cmp != 0) return cmp;
            return a.Faction == FighterFaction.Party ? -1 : 1;
        });

        Console.WriteLine("Initiative order:");
        foreach (var f in fighters)
            Console.WriteLine($"  {f.DisplayName}: {f.InitiativeRoll}");

        // ── 6. Build state ─────────────────────────────────────────
        var state = new FightState(area, fighters);
        state.AddLog("Fight begins!");
        fighters[0].StartTurn();

        // ── 7. Open window ─────────────────────────────────────────
        var native = new NativeWindowSettings
        {
            ClientSize  = new Vector2i(
                Config.Terminal.MainWidth  * Config.Terminal.MainCellSize,
                Config.Terminal.MainHeight * Config.Terminal.MainCellSize),
            Title       = "Cathedral - Fight",
            Flags       = ContextFlags.Default,
            API         = ContextAPI.OpenGL,
            APIVersion  = new Version(3, 3),
            WindowBorder = WindowBorder.Resizable,
        };

        using var window = new FightModeWindow(GameWindowSettings.Default, native, state);
        window.Run();
    }
}

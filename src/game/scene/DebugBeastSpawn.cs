using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Scene;

/// <summary>
/// Puts one beast in a scene's opening area, on the <c>--spawn-beast &lt;name&gt;</c> flag.
///
/// <para>Inert at the flag's default. It exists because everything a script can do <i>to</i> a beast —
/// appease it, tame it, track it — starts with one being where the script opens, and no factory
/// guarantees that: a wolf is rolled 10–20% of the time, a boar 25–40%, and whichever is rolled is
/// then given a roaming schedule that moves it between areas with the hour. Hunting for a seed that
/// lines both up is not a test.</para>
///
/// <para>The spawned beast is an ordinary scene NPC in every other respect — the caller's
/// first-contact pass flags it an enemy exactly as it flags a rolled one — except that its schedule
/// is <see cref="NpcSchedule.Always"/>, so it is there whatever <c>--period</c> says.</para>
/// </summary>
public static class DebugBeastSpawn
{
    /// <summary>The archetypes reachable by name, keyed by what a player would call the animal.</summary>
    private static readonly Dictionary<string, Func<NamedNpcArchetype>> Archetypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["wolf"]       = () => new WolfArchetype(),
            ["boar"]       = () => new BoarArchetype(),
            ["bear"]       = () => new BearArchetype(),
            ["black bear"] = () => new BlackBearArchetype(),
            ["black_bear"] = () => new BlackBearArchetype(),
            ["stray dog"]  = () => new StrayDogArchetype(),
            ["stray_dog"]  = () => new StrayDogArchetype(),
            ["dog"]        = () => new StrayDogArchetype(),
            ["fox"]        = () => new FoxArchetype(),
            ["cat"]        = () => new StrayCatArchetype(),
            ["stray cat"]  = () => new StrayCatArchetype(),
        };

    /// <summary>
    /// Adds the beast named by <c>--spawn-beast</c> to <paramref name="scene"/>'s opening area — the
    /// same area <see cref="SceneSyntheticGraphFactory.ResolveEntryArea"/> opens narration in, so
    /// <c>--start-area</c> moves the beast along with the player. Does nothing at the flag's default,
    /// and reports an unknown name on stderr rather than spawning something else.
    /// </summary>
    public static void Apply(Scene scene, int locationId)
    {
        var wanted = Config.Debug.SpawnBeast;
        if (string.IsNullOrWhiteSpace(wanted)) return;

        if (!Archetypes.TryGetValue(wanted.Trim(), out var factory))
        {
            Console.Error.WriteLine($"[debug] --spawn-beast: unknown beast '{wanted}'. " +
                                    $"Known: {string.Join(", ", Archetypes.Keys)}");
            return;
        }

        var area = SceneSyntheticGraphFactory.ResolveEntryArea(scene);
        if (area == null) return;

        // Seeded off the master seed and the location, like every other scene draw, so the beast is
        // the same individual on every replay of the same --seed.
        var rng    = GameRng.For($"debug-beast|{locationId}|{wanted}");
        var entity = factory().Spawn(rng, area.ContextDescription);

        // Seeded off the location, so the "new" beast is the same individual every visit — which is
        // the one the player may already have killed or tamed. Re-adding it would make this flag lie
        // about the world and quietly break any script testing that a departure sticks.
        if (scene.DepartedNpcs.Contains(entity.PersistentId))
        {
            Console.WriteLine($"[debug] --spawn-beast: {entity.DisplayName} left this location for good — not respawned");
            return;
        }

        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area);

        Console.WriteLine($"[debug] --spawn-beast: {entity.DisplayName} ({wanted}) placed in {area.DisplayName}, all periods");
    }
}

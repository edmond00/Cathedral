using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Global registry of all available <see cref="Verb"/> types.
/// Discovers verb subclasses via reflection at startup (same pattern as ModusMentisRegistry).
/// </summary>
public class VerbRegistry
{
    private static VerbRegistry? _instance;
    private readonly Dictionary<string, Verb> _verbs = new();

    private VerbRegistry()
    {
        var verbType = typeof(Verb);
        var concreteTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(verbType) && !t.IsAbstract
                     && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in concreteTypes)
        {
            if (Activator.CreateInstance(type) is Verb verb)
                _verbs[verb.VerbId] = verb;
        }

        Console.WriteLine($"VerbRegistry: Discovered {_verbs.Count} verb(s): {string.Join(", ", _verbs.Keys)}");
    }

    /// <summary>Singleton instance (lazy-initialized).</summary>
    public static VerbRegistry Instance => _instance ??= new VerbRegistry();

    /// <summary>Returns all registered verbs.</summary>
    public IReadOnlyCollection<Verb> GetAll() => _verbs.Values;

    /// <summary>Gets a verb by its ID, or null if not found.</summary>
    public Verb? Get(string verbId) => _verbs.TryGetValue(verbId, out var v) ? v : null;

    /// <summary>Returns verbs that are possible given the current scene, PoV, target element, and actor.</summary>
    public List<Verb> GetApplicable(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        return _verbs.Values.Where(v => v.IsPossible(scene, pov, target, actor)).ToList();
    }
}

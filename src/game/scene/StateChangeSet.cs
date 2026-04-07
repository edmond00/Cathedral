using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cathedral.Game.Scene;

/// <summary>
/// JSON-serializable delta representing all state changes on scene elements
/// compared to their initial factory state.
/// Maps element UUID → list of enum value names (string-serialized).
/// </summary>
public class StateChangeSet
{
    /// <summary>Element UUID → list of active state enum value names.</summary>
    public Dictionary<Guid, List<string>> Changes { get; set; } = new();

    /// <summary>
    /// Records the current state properties of the given element as a delta.
    /// Overwrites any previous entry for this element.
    /// </summary>
    public void Capture(Element element)
    {
        if (element.StateProperties.Count == 0)
        {
            Changes.Remove(element.Id);
            return;
        }

        Changes[element.Id] = element.StateProperties
            .Select(e => $"{e.GetType().Name}.{e}")
            .ToList();
    }

    /// <summary>
    /// Applies stored state changes to a scene's elements.
    /// For each element with recorded changes, sets StateProperties to the stored values.
    /// Requires that the element's enum types are available for parsing.
    /// </summary>
    public void Apply(Scene scene)
    {
        foreach (var (elementId, stateNames) in Changes)
        {
            if (!scene.Elements.TryGetValue(elementId, out var element))
                continue;

            var restored = new List<Enum>();
            foreach (var name in stateNames)
            {
                var dotIndex = name.IndexOf('.');
                if (dotIndex < 0) continue;

                var typeName  = name[..dotIndex];
                var valueName = name[(dotIndex + 1)..];

                // Search the element's current state property types for a matching enum type
                var enumType = element.StateProperties
                    .Select(e => e.GetType())
                    .FirstOrDefault(t => t.Name == typeName);

                if (enumType != null && Enum.TryParse(enumType, valueName, out var parsed) && parsed is Enum enumVal)
                    restored.Add(enumVal);
            }

            element.StateProperties = restored;
        }
    }

    /// <summary>Returns true if there are no recorded changes.</summary>
    public bool IsEmpty => Changes.Count == 0;

    /// <summary>Serializes to JSON string for save/load.</summary>
    public string ToJson() => JsonSerializer.Serialize(Changes);

    /// <summary>Deserializes from JSON string.</summary>
    public static StateChangeSet FromJson(string json)
    {
        var changes = JsonSerializer.Deserialize<Dictionary<Guid, List<string>>>(json);
        return new StateChangeSet { Changes = changes ?? new() };
    }
}

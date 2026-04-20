using System;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Verb for moving the PoV to a different area.
/// Possible when the target is a reachable <see cref="Area"/> connected via the scene's directed graph.
/// </summary>
public class MoveToAreaVerb : Verb
{
    public override string VerbId         => "move";
    public override string DisplayName    => "Move";
    public override int    BaseDifficulty => 1;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not Area targetArea) return false;
        if (targetArea.Id == pov.Where.Id) return false; // can't move to same area

        return scene.AreaGraph.TryGetValue(pov.Where.Id, out var reachable)
            && reachable.Contains(targetArea.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is Area area && !string.IsNullOrWhiteSpace(area.TransitionDescription))
            return area.TransitionDescription;
        return $"move to the {target.DisplayName.ToLowerInvariant()}";
    }

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not Area targetArea)
            throw new InvalidOperationException("MoveToAreaVerb target must be an Area");

        pov.Where = targetArea;
        pov.Focus = null;
        Console.WriteLine($"MoveToAreaVerb: Moved to {targetArea.DisplayName}");
    }
}

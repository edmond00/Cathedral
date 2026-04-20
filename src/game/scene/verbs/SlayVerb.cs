using System;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Kills a living NPC without combat. Only possible when the player is in the same area
/// as the NPC and the NPC is currently alive.
/// On execution: sets <c>IsAlive = false</c>, spawns a <see cref="Cathedral.Game.Npc.Corpse.CorpseSpot"/>
/// in the current area, and registers it in the scene.
/// </summary>
public class SlayVerb : Verb
{
    public override string VerbId         => "slay";
    public override string DisplayName    => "Slay";
    public override int    BaseDifficulty => 5;

    /// <summary>Slaying a living person is never a legal action.</summary>
    public override bool IsLegal => false;

    /// <summary>Slaying is an attack — it can be attempted even under direct threat.</summary>
    public override bool CanBeUsedUnderThreat => true;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc npc) return false;
        if (!npc.IsAlive) return false;
        if (pov.InSpot != null) return false;  // can't slay from inside a spot

        // NPC must be present at the current area and time
        return scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == npc.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"slay the {target.DisplayName.ToLowerInvariant()}";

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not SceneNpc npc)
            throw new InvalidOperationException("SlayVerb target must be a SceneNpc");

        Console.WriteLine($"SlayVerb: Slaying {npc.DisplayName}");

        // Kill the NPC
        npc.Entity.IsAlive = false;

        // Spawn and register a corpse at the current area
        var corpse = npc.Entity.GenerateCorpse(pov.Where);
        scene.AddSpotToArea(pov.Where, corpse);

        pov.Focus = null;
        Console.WriteLine($"SlayVerb: Corpse '{corpse.DisplayName}' placed in '{pov.Where.DisplayName}'");
    }
}

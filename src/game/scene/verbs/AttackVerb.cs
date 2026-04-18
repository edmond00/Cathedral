using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Initiates a direct physical attack on an NPC, starting a fight.
/// Attacking is always illegal. Can be attempted even when the enemy is right there
/// (it is inherently a combat verb).
/// </summary>
public class AttackVerb : Verb
{
    public override string VerbId      => "attack";
    public override string DisplayName => "Attack";

    /// <summary>Attacking a person is never a legal action.</summary>
    public override bool IsLegal => false;

    /// <summary>Attack is a combat verb — valid to attempt even under direct threat.</summary>
    public override bool CanBeUsedUnderThreat => true;

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not SceneNpc sceneNpc) return false;
        if (!sceneNpc.IsAlive) return false;
        if (pov.InSpot != null) return false;  // can't attack from inside a spot
        if (sceneNpc.Entity is ShallowNpcEntity) return false;  // shallow NPCs have no anatomy — use Slay instead

        return scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"attack {target.DisplayName}";

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc) return;
        scene.PendingFightRequest = new FightRequest(npc);
    }
}

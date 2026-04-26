using System;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Reminescence;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// REMEMBER — the only action available in the childhood reminescence phase.
/// Targets a <see cref="FragmentPointOfInterest"/>; on Execute it applies the fragment's
/// outcomes (skill grants, item grants, childhood-history mutations) and queues the
/// transition to the next reminescence (or the phase exit when the fragment is terminal).
/// </summary>
public class RememberVerb : Verb
{
    public override string VerbId         => "remember";
    public override string DisplayName    => "Remember";
    public override int    BaseDifficulty => 0;
    public override char?  DifficultyGlyphOverride => '○';

    /// <summary>Possible only on a <see cref="FragmentPointOfInterest"/> in a reminescence-phase scene.</summary>
    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (scene.Phase != NarrationPhase.ChildhoodReminescence) return false;
        if (target is not FragmentPointOfInterest) return false;
        return true;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is FragmentPointOfInterest frag)
            return $"try to remember what was {frag.Fragment.Name}";
        return "try to remember";
    }

    public override void Execute(Scene scene, PoV pov, Protagonist actor, Element target)
    {
        if (target is not FragmentPointOfInterest fragmentPoi)
            throw new InvalidOperationException("RememberVerb target must be a FragmentPointOfInterest");

        var data = fragmentPoi.Fragment;
        var outcome = data.Outcome;

        // 1. Update childhood history (location is set on the very first REMEMBER).
        if (outcome.SetChildhoodLocation != null)
            actor.ChildhoodHistory.Location = outcome.SetChildhoodLocation;

        // 2. Grant skills via the standard acquisition procedure (Procedural/Semantic/Sensory > WM > RM).
        foreach (var skillId in outcome.SkillIds)
        {
            var template = ModusMentisRegistry.Instance.GetModusMentis(skillId);
            if (template == null)
            {
                Console.WriteLine($"RememberVerb: skill '{skillId}' is not registered — skipping.");
                continue;
            }
            // Each protagonist gets a fresh instance with Level=1 so future runs / instances don't share state.
            var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
            instance.Level = 1;
            actor.AcquireModusMentis(instance);
            Console.WriteLine($"RememberVerb: granted modus mentis '{skillId}'");
        }

        // 3. Grant items via wear → container → hold → overflow procedure.
        foreach (var itemFactory in outcome.Items)
        {
            var item = itemFactory();
            actor.AcquireItem(item);
            Console.WriteLine($"RememberVerb: acquired item '{item.DisplayName}'");
        }

        // 4. Record the fragment in the protagonist's childhood history.
        var origin = scene.CurrentReminescenceId ?? "";
        actor.ChildhoodHistory.RecordFragment(origin, data.Name, data.Summary);

        // 5. Queue the phase transition: NarrativeController consumes this on the next frame
        //    to either rebuild the scene with the next reminescence or exit the phase.
        scene.PendingReminescenceTransition = new ReminescenceTransitionRequest(
            FromReminescenceId: origin,
            NextReminescenceId: outcome.NextReminescenceId,
            FragmentName:       data.Name);

        // 6. Capture state change so the scene log reflects the consumed fragment.
        scene.StateChanges.Capture(fragmentPoi);
    }
}

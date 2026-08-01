using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// Implemented by an observation object whose neutral text depends on the time of day.
///
/// <para>Observation objects are built once at graph-build time and reused across periods, so they
/// cannot capture the period at construction. The controller stamps it alongside the verb refresh in
/// <c>NarrativeController.RefreshSceneVerbs</c>, which is reached only through <c>ApplyTimePeriod</c>
/// — the single writer of the period. Stamping there is what guarantees the description and the
/// offered verbs can never disagree about what time it is: a door that reads "seems locked" always
/// offers UNLOCK rather than OPEN.</para>
///
/// Mirrors <see cref="INpcContextLabelStampable"/>, which pushes live NPC context in the same way.
/// </summary>
public interface IPeriodStampable
{
    void StampPeriod(TimePeriod period);
}

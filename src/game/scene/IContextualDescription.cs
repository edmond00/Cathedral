using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// Implemented by a <see cref="PointOfInterest"/> whose neutral description depends on where it is
/// being looked at from and when — a door reads differently from the street than from the hall, and
/// says something different about its lock at night.
///
/// <para><see cref="SceneViewAdapter"/> prefers this over the static <see cref="Element.Descriptions"/>
/// list when the observing area and period are known, which keeps the adapter from having to know
/// anything about doors.</para>
/// </summary>
public interface IContextualDescription
{
    /// <summary>
    /// The neutral description of this object as seen from <paramref name="viewingArea"/> during
    /// <paramref name="when"/>. Returns a bare noun phrase carrying its own article — it is embedded
    /// mid-sentence by <c>NeutralNarration.ObservationDetail</c>.
    /// </summary>
    string DescribeFrom(Area viewingArea, TimePeriod when);
}

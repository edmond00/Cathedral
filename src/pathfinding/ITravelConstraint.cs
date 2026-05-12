// ITravelConstraint.cs - Interface for travel constraints applied to a pathfinding graph
namespace Cathedral.Pathfinding
{
    /// <summary>
    /// Defines which nodes are traversable and adjusts movement costs for a particular
    /// travel mode. Different travel modes (on foot, by ship, mounted, …) plug in their
    /// own constraint so the underlying world graph can stay agnostic.
    /// </summary>
    public interface ITravelConstraint
    {
        /// <summary>
        /// Returns true if the given node can be entered. Forbidden nodes are removed
        /// from the search frontier entirely.
        /// </summary>
        bool IsTraversable(int nodeId);

        /// <summary>
        /// Multiplier applied on top of the underlying edge cost when moving from one
        /// node to another. 1.0 means no change. Use values > 1 to discourage a
        /// transition without forbidding it outright (e.g. rough terrain).
        /// </summary>
        float GetCostMultiplier(int fromNode, int toNode);

        /// <summary>
        /// Human-readable name for the travel mode this constraint represents.
        /// Used for logging and UI hints.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Trivial constraint that allows travel everywhere with no cost adjustment.
    /// Useful as a default when no specific constraint applies.
    /// </summary>
    public sealed class UnrestrictedTravelConstraint : ITravelConstraint
    {
        public string Name => "unrestricted";
        public bool IsTraversable(int nodeId) => true;
        public float GetCostMultiplier(int fromNode, int toNode) => 1.0f;
    }
}

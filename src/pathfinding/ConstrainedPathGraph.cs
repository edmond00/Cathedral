// ConstrainedPathGraph.cs - Wraps an IPathGraph with an ITravelConstraint
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Cathedral.Pathfinding
{
    /// <summary>
    /// Adapter that overlays an <see cref="ITravelConstraint"/> on top of an existing
    /// <see cref="IPathGraph"/>. Untraversable nodes are filtered out of the adjacency
    /// list and edge costs are scaled by the constraint's cost multiplier.
    ///
    /// Multiple constrained views can wrap the same base graph (e.g. one for foot
    /// travel, one for sea travel) without rebuilding it.
    /// </summary>
    public sealed class ConstrainedPathGraph : IPathGraph
    {
        private readonly IPathGraph _base;
        private readonly ITravelConstraint _constraint;

        public ConstrainedPathGraph(IPathGraph baseGraph, ITravelConstraint constraint)
        {
            _base = baseGraph ?? throw new ArgumentNullException(nameof(baseGraph));
            _constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
        }

        public ITravelConstraint Constraint => _constraint;
        public IPathGraph BaseGraph => _base;

        public int NodeCount => _base.NodeCount;

        public Vector3 GetNodePosition(int nodeId) => _base.GetNodePosition(nodeId);

        public bool ContainsNode(int nodeId) => _base.ContainsNode(nodeId);

        public IEnumerable<int> GetConnectedNodes(int nodeId)
        {
            foreach (int n in _base.GetConnectedNodes(nodeId))
            {
                if (_constraint.IsTraversable(n))
                    yield return n;
            }
        }

        public float GetMoveCost(int fromNode, int toNode)
        {
            if (!_constraint.IsTraversable(toNode) || !_constraint.IsTraversable(fromNode))
                return float.MaxValue;

            float baseCost = _base.GetMoveCost(fromNode, toNode);
            if (baseCost >= float.MaxValue) return float.MaxValue;

            float mult = _constraint.GetCostMultiplier(fromNode, toNode);
            return baseCost * mult;
        }

        public float GetHeuristic(int fromNode, int toNode) => _base.GetHeuristic(fromNode, toNode);
    }
}

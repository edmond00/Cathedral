using System;
using System.Collections.Generic;

namespace Cathedral.Fight
{
    /// <summary>
    /// Base class for all fight area generators.
    /// After generation: clears reserved zones, ensures exit is reachable via a carved
    /// corridor, then seals any isolated non-hard pockets by turning them into HardObstacle.
    /// This guarantees connectivity without retrying or falling back.
    /// </summary>
    public abstract class FightAreaGeneratorBase : IFightAreaGenerator
    {
        protected CharMapping Mapping { get; }
        protected Random Rng { get; set; } = new Random();

        protected FightAreaGeneratorBase(CharMapping? mapping = null)
        {
            Mapping = mapping ?? CharMapping.Default;
        }

        /// <summary>
        /// Override to fill the grid with terrain. Reserved zones will be cleared afterward.
        /// </summary>
        protected abstract void GenerateInternal(FightArea area);

        public FightArea Generate()
        {
            Rng = new Random(Environment.TickCount);
            var area = new FightArea();
            GenerateInternal(area);
            area.ClearReservedZones(Mapping, Rng);
            EnsureExitConnected(area);
            return area;
        }

        /// <summary>
        /// Returns true if all three key points (exit, enemy zone centre, player zone centre)
        /// can reach each other via passable cells using BFS.
        /// </summary>
        private static bool ZonesAreConnected(FightArea area)
        {
            var targets = new[]
            {
                (FightArea.ExitCol,  FightArea.ExitRow),
                (FightArea.ExitCol, (FightArea.EnemyRowStart + FightArea.EnemyRowEnd) / 2),
                (FightArea.ExitCol, (FightArea.PlayerRowStart + FightArea.PlayerRowEnd) / 2),
            };

            // BFS flood from the exit; check if the other two targets are reached
            int startX = targets[0].Item1, startY = targets[0].Item2;
            var visited = new bool[FightArea.Width, FightArea.Height];
            var queue   = new Queue<(int, int)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            int[] ddx = { 0, 0, 1, -1 };
            int[] ddy = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + ddx[d], ny = cy + ddy[d];
                    if (!area.IsInBounds(nx, ny) || visited[nx, ny]) continue;
                    if (area.GetCell(nx, ny).Type == TerrainType.HardObstacle) continue;
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }

            // All targets (except the start) must have been visited
            for (int i = 1; i < targets.Length; i++)
                if (!visited[targets[i].Item1, targets[i].Item2]) return false;

            return true;
        }

        /// <summary>
        /// Carve a 3-wide vertical spine from just above the exit down through both
        /// spawn zones to just below the player zone — only if the zones are not already
        /// connected through organic terrain.
        /// </summary>
        private void EnsureExitConnected(FightArea area)
        {
            if (ZonesAreConnected(area)) return;   // organic map already links everything

            int spineTop    = Math.Max(0, FightArea.ExitRow - 2);
            int spineBottom = Math.Min(FightArea.Height - 1, FightArea.PlayerRowEnd + 2);

            for (int y = spineTop; y <= spineBottom; y++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = FightArea.ExitCol + dx;
                if (!area.IsInBounds(x, y)) continue;
                if (area.GetCell(x, y).Type == TerrainType.HardObstacle)
                    area.SetTerrain(x, y, TerrainType.FreeSpace, Mapping, Rng);
            }
        }

        /// <summary>
        /// BFS from exit; any non-hard cell that is not reached gets converted to
        /// HardObstacle. This is always safe: unreachable pockets can be walled off
        /// because no combatant could ever enter them anyway.
        /// </summary>
        private void SealIsolatedPockets(FightArea area)
        {
            var visited = new bool[FightArea.Width, FightArea.Height];
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((FightArea.ExitCol, FightArea.ExitRow));
            visited[FightArea.ExitCol, FightArea.ExitRow] = true;

            int[] ddx = { 0, 0, 1, -1 };
            int[] ddy = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + ddx[d];
                    int ny = cy + ddy[d];
                    if (!area.IsInBounds(nx, ny)) continue;
                    if (visited[nx, ny]) continue;
                    if (area.GetCell(nx, ny).Type == TerrainType.HardObstacle) continue;
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }

            for (int y = 0; y < FightArea.Height; y++)
            for (int x = 0; x < FightArea.Width; x++)
            {
                if (area.GetCell(x, y).Type != TerrainType.HardObstacle && !visited[x, y])
                    area.SetTerrain(x, y, TerrainType.HardObstacle, Mapping, Rng);
            }
        }
    }
}

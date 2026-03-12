using System.Collections.Generic;

namespace Cathedral.Fight
{
    /// <summary>
    /// Validates that every non-HardObstacle cell is reachable from the exit tile via BFS.
    /// Uses 4-directional connectivity.
    /// </summary>
    public static class FightAreaValidator
    {
        public static bool IsConnected(FightArea area)
        {
            var visited = new bool[FightArea.Width, FightArea.Height];
            var queue = new Queue<(int x, int y)>();

            queue.Enqueue((FightArea.ExitCol, FightArea.ExitRow));
            visited[FightArea.ExitCol, FightArea.ExitRow] = true;

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];
                    if (!area.IsInBounds(nx, ny)) continue;
                    if (visited[nx, ny]) continue;
                    if (area.GetCell(nx, ny).Type == TerrainType.HardObstacle) continue;

                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }

            // Every non-HardObstacle cell must have been visited
            for (int y = 0; y < FightArea.Height; y++)
            for (int x = 0; x < FightArea.Width; x++)
            {
                if (area.GetCell(x, y).Type != TerrainType.HardObstacle && !visited[x, y])
                    return false;
            }

            return true;
        }
    }
}

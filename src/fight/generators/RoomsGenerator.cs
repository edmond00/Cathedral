using System;
using System.Collections.Generic;

namespace Cathedral.Fight.Generators
{
    /// <summary>
    /// BSP room-and-corridor generator. Fills the area with hard obstacles,
    /// then carves out rooms and connects them with corridors.
    /// Suitable for buildings, dungeons, and structured interiors.
    /// </summary>
    public class RoomsGenerator : FightAreaGeneratorBase
    {
        public int RoomCount      { get; set; } = 6;
        public int MinRoomSize    { get; set; } = 5;
        public int MaxRoomSize    { get; set; } = 12;
        public int CorridorWidth  { get; set; } = 2;
        public float ObstacleInRoom    { get; set; } = 0.06f; // chance per cell of soft terrain
        public float DangerousInRoom   { get; set; } = 0.012f; // separate, low chance of dangerous terrain

        /// <summary>
        /// When true, carves one large central room that spans from just above the enemy
        /// zone to just below the player zone, containing both spawn areas.
        /// Additional rooms are still placed around the arena.
        /// </summary>
        public bool CentralArena { get; set; } = false;

        public RoomsGenerator(CharMapping? mapping = null) : base(mapping) { }

        private record Rect(int X, int Y, int W, int H)
        {
            public int CenterX => X + W / 2;
            public int CenterY => Y + H / 2;
        }

        protected override void GenerateInternal(FightArea area)
        {
            // Start with walls everywhere
            area.Fill(TerrainType.HardObstacle, Mapping, Rng);

            var rooms = new List<Rect>();

            if (CentralArena)
            {
                // One large room spanning both spawn zones (enemy top → player bottom),
                // centred horizontally on the zone columns with some padding.
                int arenaX = Math.Max(1, FightArea.ZoneColStart - 5);
                int arenaY = Math.Max(1, FightArea.EnemyRowStart - 3);
                int arenaW = Math.Min(FightArea.Width  - 2 - arenaX,
                                      (FightArea.ZoneColEnd - FightArea.ZoneColStart + 1) + 10);
                int arenaH = Math.Min(FightArea.Height - 2 - arenaY,
                                      (FightArea.PlayerRowEnd - FightArea.EnemyRowStart + 1) + 6);
                rooms.Add(new Rect(arenaX, arenaY, arenaW, arenaH));

                // Also seed a small room around the exit so it connects to the arena
                rooms.Add(new Rect(Math.Max(1, FightArea.ExitCol - 3),
                                   Math.Max(1, FightArea.ExitRow - 2), 7, 5));
            }
            else
            {
                // Seed rooms at exit and spawn zones — generator carves these naturally,
                // giving each zone an organic room instead of an artificial clear
                int zoneW = FightArea.ZoneColEnd - FightArea.ZoneColStart + 1 + 4; // 15 wide
                int zoneH = FightArea.EnemyRowEnd - FightArea.EnemyRowStart + 1 + 4; // 7 tall
                rooms.Add(new Rect(Math.Max(1, FightArea.ExitCol - 3),
                                   Math.Max(1, FightArea.ExitRow - 2), 7, 5));
                rooms.Add(new Rect(Math.Max(1, FightArea.ZoneColStart - 2),
                                   Math.Max(1, FightArea.EnemyRowStart - 2), zoneW, zoneH));
                rooms.Add(new Rect(Math.Max(1, FightArea.ZoneColStart - 2),
                                   Math.Max(1, FightArea.PlayerRowStart - 2), zoneW, zoneH));
            }

            // Attempt to place rooms without too much overlap
            const int PlacementAttempts = 60;
            for (int attempt = 0; attempt < PlacementAttempts && rooms.Count < RoomCount + 3; attempt++)
            {
                int w = Rng.Next(MinRoomSize, MaxRoomSize + 1);
                int h = Rng.Next(MinRoomSize, MaxRoomSize + 1);
                int x = Rng.Next(1, FightArea.Width  - w - 1);
                int y = Rng.Next(1, FightArea.Height - h - 1);
                var candidate = new Rect(x, y, w, h);

                bool overlaps = false;
                foreach (var existing in rooms)
                {
                    if (RectsOverlapWithPadding(candidate, existing, 2))
                    { overlaps = true; break; }
                }

                if (!overlaps)
                    rooms.Add(candidate);
            }

            // Carve rooms
            foreach (var room in rooms)
            {
                for (int ry = room.Y; ry < room.Y + room.H; ry++)
                for (int rx = room.X; rx < room.X + room.W; rx++)
                {
                    if (!area.IsInBounds(rx, ry)) continue;

                    // Sprinkle terrain variety inside rooms
                    float roll = (float)Rng.NextDouble();
                    TerrainType t;
                    if      (roll < DangerousInRoom)         t = TerrainType.DangerousTerrain;
                    else if (roll < DangerousInRoom + ObstacleInRoom) t = TerrainType.SoftObstacle;
                    else                                     t = TerrainType.FreeSpace;

                    area.SetTerrain(rx, ry, t, Mapping, Rng);
                }
            }

            // Connect rooms with L-shaped corridors in order
            for (int i = 1; i < rooms.Count; i++)
                CarveCorridorBetween(area, rooms[i - 1], rooms[i]);
        }

        private void CarveCorridorBetween(FightArea area, Rect a, Rect b)
        {
            int x1 = a.CenterX, y1 = a.CenterY;
            int x2 = b.CenterX, y2 = b.CenterY;

            // Horizontal then vertical (or vice versa, randomly)
            if (Rng.Next(2) == 0)
            {
                CarveHorizontal(area, y1, Math.Min(x1, x2), Math.Max(x1, x2));
                CarveVertical  (area, x2, Math.Min(y1, y2), Math.Max(y1, y2));
            }
            else
            {
                CarveVertical  (area, x1, Math.Min(y1, y2), Math.Max(y1, y2));
                CarveHorizontal(area, y2, Math.Min(x1, x2), Math.Max(x1, x2));
            }
        }

        private void CarveHorizontal(FightArea area, int row, int xStart, int xEnd)
        {
            for (int x = xStart; x <= xEnd; x++)
            for (int dy = 0; dy < CorridorWidth; dy++)
            {
                int y = row + dy;
                if (!area.IsInBounds(x, y)) continue;
                if (area.GetCell(x, y).Type == TerrainType.HardObstacle)
                    area.SetTerrain(x, y, TerrainType.FreeSpace, Mapping, Rng);
            }
        }

        private void CarveVertical(FightArea area, int col, int yStart, int yEnd)
        {
            for (int y = yStart; y <= yEnd; y++)
            for (int dx = 0; dx < CorridorWidth; dx++)
            {
                int x = col + dx;
                if (!area.IsInBounds(x, y)) continue;
                if (area.GetCell(x, y).Type == TerrainType.HardObstacle)
                    area.SetTerrain(x, y, TerrainType.FreeSpace, Mapping, Rng);
            }
        }

        private static bool RectsOverlapWithPadding(Rect a, Rect b, int pad)
        {
            return a.X - pad < b.X + b.W + pad &&
                   a.X + a.W + pad > b.X - pad &&
                   a.Y - pad < b.Y + b.H + pad &&
                   a.Y + a.H + pad > b.Y - pad;
        }
    }
}

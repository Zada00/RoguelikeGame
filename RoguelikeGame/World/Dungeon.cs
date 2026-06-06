using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Dungeon
{
    public int GridWidth { get; }
    public int GridHeight { get; }
    public int RoomWidth { get; }
    public int RoomHeight { get; }
    public int Depth { get; }

    private readonly Room[,] _rooms;
    private readonly Random _rng = new();
    private readonly DiffSettings _diff;
    private Room? _stairsRoom;

    public int CurrentGridX { get; private set; }
    public int CurrentGridY { get; private set; }
    public Room CurrentRoom => _rooms[CurrentGridX, CurrentGridY];

    public Dungeon(int gridWidth, int gridHeight, int roomWidth, int roomHeight, int depth, Difficulty difficulty)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        RoomWidth = roomWidth;
        RoomHeight = roomHeight;
        Depth = depth;
        _diff = DifficultySettings.Get(difficulty);
        _rooms = new Room[gridWidth, gridHeight];

        // 1) lag tomme rom (bare vegg)
        for (int gx = 0; gx < GridWidth; gx++)
            for (int gy = 0; gy < GridHeight; gy++)
                _rooms[gx, gy] = new Room(RoomWidth, RoomHeight, (RoomTheme)_rng.Next(4));

        // 2) koble dem sammen (setter dører)
        GenerateMaze();

        // 3) bygg layout + dekor + monstre (nå som dørene er kjent)
        for (int gx = 0; gx < GridWidth; gx++)
            for (int gy = 0; gy < GridHeight; gy++)
            {
                bool isStart = gx == 0 && gy == 0;
                var room = _rooms[gx, gy];
                room.Build(_rng, isStart);
                room.Decorate(_rng, isStart);
                room.CarveDoorCorridors();
                room.SpawnMonsters(_rng, isStart, depth, _diff.DensityDiv, _diff.Cap, _diff.StatMul);
            }

        PlaceStairs();
        SpawnBoss(depth);
        CurrentRoom.IsVisited = true;
    }

    private void SpawnBoss(int depth)
    {
        if (_stairsRoom == null) return;
        int cx = RoomWidth / 2, cy = RoomHeight / 2;
        var boss = Monster.RandomBoss(cx, cy, depth, _rng);
        boss.Scale(_diff.StatMul);
        _stairsRoom.Monsters.Add(boss);
        _stairsRoom.HadMonsters = true;
    }

    private void PlaceStairs()
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int gx = _rng.Next(GridWidth);
            int gy = _rng.Next(GridHeight);
            if (gx == 0 && gy == 0) continue;

            var room = _rooms[gx, gy];
            for (int t = 0; t < 60; t++)
            {
                int x = _rng.Next(1, RoomWidth - 1);
                int y = _rng.Next(1, RoomHeight - 1);
                bool center = x == RoomWidth / 2 && y == RoomHeight / 2;
                if (room.IsWalkable(x, y) && !center
                    && room.MonsterAt(x, y) == null && room.GetDoorAt(x, y) == null)
                {
                    room.Stairs = new Point(x, y);
                    _stairsRoom = room;
                    return;
                }
            }
        }
    }

    private void GenerateMaze()
    {
        var rng = _rng;
        var visited = new bool[GridWidth, GridHeight];
        var stack = new Stack<(int x, int y)>();

        stack.Push((0, 0));
        visited[0, 0] = true;

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();

            var neighbors = new List<(int x, int y, Direction from, Direction to)>();
            if (cy > 0              && !visited[cx, cy - 1]) neighbors.Add((cx, cy - 1, Direction.North, Direction.South));
            if (cy < GridHeight - 1 && !visited[cx, cy + 1]) neighbors.Add((cx, cy + 1, Direction.South, Direction.North));
            if (cx > 0              && !visited[cx - 1, cy]) neighbors.Add((cx - 1, cy, Direction.West,  Direction.East));
            if (cx < GridWidth - 1  && !visited[cx + 1, cy]) neighbors.Add((cx + 1, cy, Direction.East,  Direction.West));

            if (neighbors.Count == 0) { stack.Pop(); continue; }

            var (nx, ny, from, to) = neighbors[rng.Next(neighbors.Count)];
            SetDoor(_rooms[cx, cy], from);
            SetDoor(_rooms[nx, ny], to);
            visited[nx, ny] = true;
            stack.Push((nx, ny));
        }
    }

    private static void SetDoor(Room room, Direction dir)
    {
        switch (dir)
        {
            case Direction.North: room.HasDoorNorth = true; break;
            case Direction.South: room.HasDoorSouth = true; break;
            case Direction.East:  room.HasDoorEast  = true; break;
            case Direction.West:  room.HasDoorWest  = true; break;
        }
    }

    public Point TransitionTo(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: CurrentGridY--; break;
            case Direction.South: CurrentGridY++; break;
            case Direction.East:  CurrentGridX++; break;
            case Direction.West:  CurrentGridX--; break;
        }

        CurrentRoom.IsVisited = true;

        Direction enterFrom = dir switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East  => Direction.West,
            Direction.West  => Direction.East,
            _ => Direction.North
        };

        var doorPos = CurrentRoom.GetDoorPosition(enterFrom);
        return enterFrom switch
        {
            Direction.North => new Point(doorPos.X, doorPos.Y + 1),
            Direction.South => new Point(doorPos.X, doorPos.Y - 1),
            Direction.East  => new Point(doorPos.X - 1, doorPos.Y),
            Direction.West  => new Point(doorPos.X + 1, doorPos.Y),
            _ => doorPos
        };
    }

    public void RevealNeighbors(int gx, int gy)
    {
        MarkSeen(gx - 1, gy);
        MarkSeen(gx + 1, gy);
        MarkSeen(gx, gy - 1);
        MarkSeen(gx, gy + 1);
    }

    private void MarkSeen(int gx, int gy)
    {
        var room = GetRoom(gx, gy);
        if (room != null) room.IsVisited = true;
    }

    public void RevealAll()
    {
        for (int gx = 0; gx < GridWidth; gx++)
            for (int gy = 0; gy < GridHeight; gy++)
                _rooms[gx, gy].IsVisited = true;
    }

    public Room? GetRoom(int gridX, int gridY)
    {
        if (gridX < 0 || gridY < 0 || gridX >= GridWidth || gridY >= GridHeight)
            return null;
        return _rooms[gridX, gridY];
    }
}

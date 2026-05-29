using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Dungeon
{
    public int GridWidth { get; }
    public int GridHeight { get; }
    public int RoomWidth { get; }
    public int RoomHeight { get; }

    // Et 2D rutenett av rom. _rooms[gx, gy] er rommet i den grid-cellen.
    private readonly Room[,] _rooms;

    public int CurrentGridX { get; private set; }
    public int CurrentGridY { get; private set; }
    public Room CurrentRoom => _rooms[CurrentGridX, CurrentGridY];

    public Dungeon(int gridWidth, int gridHeight, int roomWidth, int roomHeight)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        RoomWidth = roomWidth;
        RoomHeight = roomHeight;
        _rooms = new Room[gridWidth, gridHeight];

        for (int gx = 0; gx < GridWidth; gx++)
            for (int gy = 0; gy < GridHeight; gy++)
                _rooms[gx, gy] = new Room(RoomWidth, RoomHeight);

        GenerateMaze();
        CurrentRoom.IsVisited = true;
    }

    // Klassisk maze-algoritme: "recursive backtracker".
    // Vi starter i (0,0) og gjør en tilfeldig dybde-først-vandring til alle
    // rom er besøkt. Hver gang vi går fra rom A til rom B, lager vi en dør 
    // mellom dem. Resultatet er en labyrint der hvert rom kan nås fra alle andre.
    private void GenerateMaze()
    {
        var rng = new Random();
        var visited = new bool[GridWidth, GridHeight];
        var stack = new Stack<(int x, int y)>();

        stack.Push((0, 0));
        visited[0, 0] = true;

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();

            // Finn naboer i grid-en som ennå ikke er besøkt.
            var neighbors = new List<(int x, int y, Direction from, Direction to)>();
            if (cy > 0 && !visited[cx, cy - 1]) neighbors.Add((cx, cy - 1, Direction.North, Direction.South));
            if (cy < GridHeight - 1 && !visited[cx, cy + 1]) neighbors.Add((cx, cy + 1, Direction.South, Direction.North));
            if (cx > 0 && !visited[cx - 1, cy]) neighbors.Add((cx - 1, cy, Direction.West, Direction.East));
            if (cx < GridWidth - 1 && !visited[cx + 1, cy]) neighbors.Add((cx + 1, cy, Direction.East, Direction.West));

            if (neighbors.Count == 0)
            {
                stack.Pop();   // blindgate, gå tilbake
                continue;
            }

            var (nx, ny, from, to) = neighbors[rng.Next(neighbors.Count)];

            // Lag dør i begge rom (en dør "ut" av nåværende rom og en "inn" i naborommet).
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
            case Direction.East: room.HasDoorEast = true; break;
            case Direction.West: room.HasDoorWest = true; break;
        }
    }

    // Bytter til naborommet i gitt retning og returnerer den nye spillerposisjonen.
    public Point TransitionTo(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: CurrentGridY--; break;
            case Direction.South: CurrentGridY++; break;
            case Direction.East: CurrentGridX++; break;
            case Direction.West: CurrentGridX--; break;
        }

        CurrentRoom.IsVisited = true;

        // Vi går "inn" i det nye rommet fra motsatt side - så plasser
        // spilleren ett skritt innenfor den døra.
        Direction enterFrom = dir switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => Direction.North
        };

        var doorPos = CurrentRoom.GetDoorPosition(enterFrom);
        return enterFrom switch
        {
            Direction.North => new Point(doorPos.X, doorPos.Y + 1),
            Direction.South => new Point(doorPos.X, doorPos.Y - 1),
            Direction.East => new Point(doorPos.X - 1, doorPos.Y),
            Direction.West => new Point(doorPos.X + 1, doorPos.Y),
            _ => doorPos
        };
    }

    // Henter rommet på en gitt grid-posisjon. Returnerer null hvis utenfor.
    public Room? GetRoom(int gridX, int gridY)
    {
        if (gridX < 0 || gridY < 0 || gridX >= GridWidth || gridY >= GridHeight)
            return null;
        return _rooms[gridX, gridY];
    }

}
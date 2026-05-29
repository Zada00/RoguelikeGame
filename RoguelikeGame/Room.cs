using SadConsole;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal enum Direction
{
    North,
    South,
    East,
    West
}

internal class Room
{
    public int Width { get; }
    public int Height { get; }

    private readonly Tile[,] _tiles;

    // Hvilke vegger har dører. Settes av Dungeon når labyrinten genereres.
    public bool HasDoorNorth { get; set; }
    public bool HasDoorSouth { get; set; }
    public bool HasDoorEast { get; set; }
    public bool HasDoorWest { get; set; }

    // Brukes senere til kartoversikt med fog of war.
    public bool IsVisited { get; set; }

    private static readonly Color FloorColor = new(70, 70, 70);
    private static readonly Color WallColor = new(150, 120, 90);
    private static readonly Color DoorColor = new(220, 170, 70);

    public Room(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];

        // Et enkelt rektangulært rom: vegger rundt kanten, gulv inni.
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                bool isEdge = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                _tiles[x, y] = isEdge ? Wall() : Floor();
            }
    }

    // Hvor ligger døra på en gitt vegg? Midt på vegen.
    public Point GetDoorPosition(Direction dir) => dir switch
    {
        Direction.North => new Point(Width / 2, 0),
        Direction.South => new Point(Width / 2, Height - 1),
        Direction.West => new Point(0, Height / 2),
        Direction.East => new Point(Width - 1, Height / 2),
        _ => throw new ArgumentException("Ukjent retning")
    };

    // Returnerer hvilken dør som er på en gitt rute - eller null hvis ingen.
    public Direction? GetDoorAt(int x, int y)
    {
        if (HasDoorNorth && x == Width / 2 && y == 0) return Direction.North;
        if (HasDoorSouth && x == Width / 2 && y == Height - 1) return Direction.South;
        if (HasDoorWest && x == 0 && y == Height / 2) return Direction.West;
        if (HasDoorEast && x == Width - 1 && y == Height / 2) return Direction.East;
        return null;
    }

    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
        if (GetDoorAt(x, y) != null) return true;
        return _tiles[x, y].IsWalkable;
    }

    public void Render(ICellSurface surface)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                Tile t = _tiles[x, y];
                surface.SetGlyph(x, y, t.Glyph, t.Foreground, t.Background);
            }

        // Tegn dørene oppå veggene.
        DrawDoorIfPresent(surface, Direction.North);
        DrawDoorIfPresent(surface, Direction.South);
        DrawDoorIfPresent(surface, Direction.East);
        DrawDoorIfPresent(surface, Direction.West);
    }

    private void DrawDoorIfPresent(ICellSurface surface, Direction dir)
    {
        bool hasDoor = dir switch
        {
            Direction.North => HasDoorNorth,
            Direction.South => HasDoorSouth,
            Direction.East => HasDoorEast,
            Direction.West => HasDoorWest,
            _ => false
        };
        if (!hasDoor) return;

        var pos = GetDoorPosition(dir);
        surface.SetGlyph(pos.X, pos.Y, '+', DoorColor, Color.Black);
    }

    private static Tile Floor() => new()
    {
        Glyph = '.',
        Foreground = FloorColor,
        Background = Color.Black,
        IsWalkable = true
    };

    private static Tile Wall() => new()
    {
        Glyph = '#',
        Foreground = WallColor,
        Background = Color.Black,
        IsWalkable = false
    };
}
using SadConsole;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal enum Direction { North, South, East, West }

internal class Room
{
    public int Width { get; }
    public int Height { get; }
    private readonly Tile[,] _tiles;

    public bool HasDoorNorth { get; set; }
    public bool HasDoorSouth { get; set; }
    public bool HasDoorEast { get; set; }
    public bool HasDoorWest { get; set; }
    public bool IsVisited { get; set; }

    public Room(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                bool isEdge = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                _tiles[x, y] = isEdge ? Wall() : Floor();
            }
    }

    public Point GetDoorPosition(Direction dir) => dir switch
    {
        Direction.North => new Point(Width / 2, 0),
        Direction.South => new Point(Width / 2, Height - 1),
        Direction.West => new Point(0, Height / 2),
        Direction.East => new Point(Width - 1, Height / 2),
        _ => throw new ArgumentException("Unknown direction")
    };

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
        surface.SetGlyph(pos.X, pos.Y, Glyph.Door, Color.White, Color.Black);
    }

    // Hvit forgrunn = vis flisens egne farger uendret.
    private static Tile Floor() => new()
    { Glyph = Glyph.Floor, Foreground = Color.White, Background = Color.Black, IsWalkable = true };

    private static Tile Wall() => new()
    { Glyph = Glyph.Wall, Foreground = Color.White, Background = Color.Black, IsWalkable = false };
}
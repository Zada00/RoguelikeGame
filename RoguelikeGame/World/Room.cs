using SadConsole;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal enum Direction { North, South, East, West }

internal enum RoomTheme { Stone, Moss, Crypt, Cave }

internal class Room
{
    public int Width { get; }
    public int Height { get; }
    public RoomTheme Theme { get; }
    public Color FloorBackground { get; }
    public List<Monster> Monsters { get; } = new();
    public Point? Stairs { get; set; }

    private readonly Tile[,] _tiles;
    private readonly int _floorGlyph;
    private readonly int _wallGlyph;

    public bool HasDoorNorth { get; set; }
    public bool HasDoorSouth { get; set; }
    public bool HasDoorEast { get; set; }
    public bool HasDoorWest { get; set; }
    public bool IsVisited { get; set; }
    public bool HadMonsters { get; set; }
    public bool RewardGranted { get; set; }

    public Room(int width, int height, RoomTheme theme)
    {
        Width = width;
        Height = height;
        Theme = theme;

        (_floorGlyph, _wallGlyph, FloorBackground) = theme switch
        {
            RoomTheme.Moss  => (Glyph.MossFloor,  Glyph.MossWall,  new Color(46, 50, 50)),
            RoomTheme.Crypt => (Glyph.CryptFloor, Glyph.CryptWall, new Color(56, 56, 64)),
            RoomTheme.Cave  => (Glyph.CaveFloor,  Glyph.CaveWall,  new Color(60, 50, 40)),
            _               => (Glyph.Floor,      Glyph.Wall,      new Color(44, 44, 54)),
        };

        _tiles = new Tile[width, height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                bool isEdge = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                _tiles[x, y] = isEdge ? Wall() : Floor();
            }
    }

    public void Decorate(Random rng, bool isStart)
    {
        int rubble = rng.Next(0, 6);
        for (int n = 0; n < rubble; n++)
        {
            int x = rng.Next(1, Width - 1);
            int y = rng.Next(1, Height - 1);
            if (_tiles[x, y].IsWalkable)
                _tiles[x, y] = Rubble();
        }

        if (isStart) return;

        switch (rng.Next(6))
        {
            case 1:
                _tiles[3, 3] = Pillar();
                _tiles[Width - 4, 3] = Pillar();
                _tiles[3, Height - 4] = Pillar();
                _tiles[Width - 4, Height - 4] = Pillar();
                break;
            case 2:
                for (int y = 3; y <= Height - 4; y += 2)
                {
                    _tiles[6, y] = Pillar();
                    _tiles[Width - 7, y] = Pillar();
                }
                break;
            case 3:
                _tiles[Width / 2 - 3, Height / 2 - 1] = Pillar();
                _tiles[Width / 2 + 3, Height / 2 - 1] = Pillar();
                _tiles[Width / 2 - 3, Height / 2 + 1] = Pillar();
                _tiles[Width / 2 + 3, Height / 2 + 1] = Pillar();
                break;
            case 4:
            case 5:
                int count = rng.Next(2, 5);
                for (int n = 0; n < count; n++)
                {
                    int x = rng.Next(3, Width - 3);
                    int y = rng.Next(3, Height - 3);
                    _tiles[x, y] = Pillar();
                }
                break;
        }

        _tiles[Width / 2, Height / 2] = Floor();
    }

    public void SpawnMonsters(Random rng, bool isStart, int depth)
    {
        if (isStart) return;

        // Grunnpool: nærkjempere. Fra etasje 2 dukker også skyttere opp.
        var makers = new List<Func<int, int, int, Monster>>
        { Monster.Rat, Monster.Goblin, Monster.Skeleton, Monster.Slime };
        if (depth >= 2)
        {
            makers.Add(Monster.Cultist);
            makers.Add(Monster.Seer);
        }

        int count = rng.Next(1, 4) + (depth - 1);
        if (count > 7) count = 7;

        for (int n = 0; n < count; n++)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = rng.Next(1, Width - 1);
                int y = rng.Next(1, Height - 1);
                bool center = x == Width / 2 && y == Height / 2;
                if (IsWalkable(x, y) && !center && MonsterAt(x, y) == null)
                {
                    Monsters.Add(makers[rng.Next(makers.Count)](x, y, depth));
                    break;
                }
            }
        }

        HadMonsters = Monsters.Count > 0;
    }

    public Monster? MonsterAt(int x, int y) => Monsters.FirstOrDefault(m => m.X == x && m.Y == y);

    public bool IsStairs(int x, int y) => Stairs.HasValue && Stairs.Value.X == x && Stairs.Value.Y == y;

    public Point GetDoorPosition(Direction dir) => dir switch
    {
        Direction.North => new Point(Width / 2, 0),
        Direction.South => new Point(Width / 2, Height - 1),
        Direction.West  => new Point(0, Height / 2),
        Direction.East  => new Point(Width - 1, Height / 2),
        _ => throw new ArgumentException("Unknown direction")
    };

    public Direction? GetDoorAt(int x, int y)
    {
        if (HasDoorNorth && x == Width / 2 && y == 0)          return Direction.North;
        if (HasDoorSouth && x == Width / 2 && y == Height - 1) return Direction.South;
        if (HasDoorWest  && x == 0 && y == Height / 2)         return Direction.West;
        if (HasDoorEast  && x == Width - 1 && y == Height / 2) return Direction.East;
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

        if (Stairs.HasValue)
            surface.SetGlyph(Stairs.Value.X, Stairs.Value.Y, Glyph.StairsDown, Color.White, Color.Black);

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
            Direction.East  => HasDoorEast,
            Direction.West  => HasDoorWest,
            _ => false
        };
        if (!hasDoor) return;

        var pos = GetDoorPosition(dir);
        surface.SetGlyph(pos.X, pos.Y, Glyph.Door, Color.White, Color.Black);
    }

    private Tile Floor() => new()
    { Glyph = _floorGlyph, Foreground = Color.White, Background = Color.Black, IsWalkable = true };

    private Tile Wall() => new()
    { Glyph = _wallGlyph, Foreground = Color.White, Background = Color.Black, IsWalkable = false };

    private static Tile Pillar() => new()
    { Glyph = Glyph.Pillar, Foreground = Color.White, Background = Color.Black, IsWalkable = false };

    private static Tile Rubble() => new()
    { Glyph = Glyph.Rubble, Foreground = Color.White, Background = Color.Black, IsWalkable = true };
}

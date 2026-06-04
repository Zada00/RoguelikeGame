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
                _tiles[x, y] = Wall();
    }

    public void Build(Random rng, bool isStart)
    {
        if (isStart) { CarveRect(1, 1, Width - 2, Height - 2); return; }

        switch (rng.Next(5))
        {
            case 0: // full sal
                CarveRect(1, 1, Width - 2, Height - 2);
                break;

            case 1: // mindre rom i varierende størrelse
            {
                int rw = rng.Next(9, Width - 3);
                int rh = rng.Next(6, Height - 3);
                int rx = 1 + rng.Next(0, Width - 2 - rw);
                int ry = 1 + rng.Next(0, Height - 2 - rh);
                CarveRect(rx, ry, rx + rw - 1, ry + rh - 1);
                break;
            }

            case 2: // kors / pluss
            {
                int cx = Width / 2, cy = Height / 2;
                CarveRect(1, cy - 2, Width - 2, cy + 1);
                CarveRect(cx - 2, 1, cx + 1, Height - 2);
                break;
            }

            case 3: // diamant
            {
                int cx = Width / 2, cy = Height / 2;
                double a = (Width - 2) * 0.5, b = (Height - 2) * 0.5;
                for (int x = 1; x < Width - 1; x++)
                    for (int y = 1; y < Height - 1; y++)
                        if (Math.Abs(x - cx) / a + Math.Abs(y - cy) / b <= 1.0)
                            SetFloor(x, y);
                break;
            }

            case 4: // søylesal
            {
                CarveRect(1, 1, Width - 2, Height - 2);
                for (int x = 3; x < Width - 2; x += 4)
                    for (int y = 2; y < Height - 2; y += 3)
                        _tiles[x, y] = Pillar();
                break;
            }
        }
    }

    public void Decorate(Random rng, bool isStart)
    {
        int rubble = rng.Next(0, 6);
        for (int n = 0; n < rubble; n++)
        {
            int x = rng.Next(1, Width - 1);
            int y = rng.Next(1, Height - 1);
            if (_tiles[x, y].IsWalkable) _tiles[x, y] = Rubble();
        }

        if (isStart) return;

        int cx = Width / 2, cy = Height / 2;
        int pillars = rng.Next(0, 4);
        for (int n = 0; n < pillars; n++)
        {
            int x = rng.Next(2, Width - 2);
            int y = rng.Next(2, Height - 2);
            if (_tiles[x, y].IsWalkable && (Math.Abs(x - cx) > 2 || Math.Abs(y - cy) > 2))
                _tiles[x, y] = Pillar();
        }

        PlaceFeatures(rng);
    }

    // Plasser 1-3 flerfelts-objekter på ledige gulvflater.
    private void PlaceFeatures(Random rng)
    {
        int n = rng.Next(1, 4);
        for (int k = 0; k < n; k++)
        {
            switch (rng.Next(4))
            {
                case 0: PlaceCluster(rng, 3, 2, Water); break;
                case 1: PlaceCluster(rng, 2, 2, Crate); break;
                case 2: PlaceCluster(rng, 3, 2, Crack); break;
                case 3: PlaceStatue(rng); break;
            }
        }
    }

    private bool RegionFloor(int x0, int y0, int w, int h)
    {
        int cx = Width / 2, cy = Height / 2;
        for (int x = x0; x < x0 + w; x++)
            for (int y = y0; y < y0 + h; y++)
            {
                if (x < 1 || y < 1 || x >= Width - 1 || y >= Height - 1) return false;
                if (_tiles[x, y].Glyph != _floorGlyph) return false;
                if (Math.Abs(x - cx) <= 2 && Math.Abs(y - cy) <= 2) return false;
            }
        return true;
    }

    public bool IsWater(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height && _tiles[x, y].Glyph == Glyph.Water;

    private void PlaceCluster(Random rng, int w, int h, Func<Tile> make)
    {
        for (int t = 0; t < 15; t++)
        {
            int x0 = rng.Next(1, Math.Max(2, Width - 1 - w));
            int y0 = rng.Next(1, Math.Max(2, Height - 1 - h));
            if (RegionFloor(x0, y0, w, h))
            {
                for (int x = x0; x < x0 + w; x++)
                    for (int y = y0; y < y0 + h; y++)
                        _tiles[x, y] = make();
                return;
            }
        }
    }

    private void PlaceStatue(Random rng)
    {
        for (int t = 0; t < 15; t++)
        {
            int x = rng.Next(2, Width - 2);
            int y = rng.Next(3, Height - 2);
            if (RegionFloor(x, y - 1, 1, 2))
            {
                _tiles[x, y] = StatueBottom();
                _tiles[x, y - 1] = StatueTop();
                return;
            }
        }
    }

    public void CarveDoorCorridors()
    {
        int cx = Width / 2, cy = Height / 2;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                SetFloor(cx + dx, cy + dy);

        if (HasDoorNorth) Corridor(cx, cy, Width / 2, 1);
        if (HasDoorSouth) Corridor(cx, cy, Width / 2, Height - 2);
        if (HasDoorWest)  Corridor(cx, cy, 1, Height / 2);
        if (HasDoorEast)  Corridor(cx, cy, Width - 2, Height / 2);
    }

    private void Corridor(int ax, int ay, int bx, int by)
    {
        for (int x = Math.Min(ax, bx); x <= Math.Max(ax, bx); x++) SetFloor(x, ay);
        for (int y = Math.Min(ay, by); y <= Math.Max(ay, by); y++) SetFloor(bx, y);
    }

    private void CarveRect(int x0, int y0, int x1, int y1)
    {
        x0 = Math.Max(1, x0); y0 = Math.Max(1, y0);
        x1 = Math.Min(Width - 2, x1); y1 = Math.Min(Height - 2, y1);
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                SetFloor(x, y);
    }

    private void SetFloor(int x, int y)
    {
        if (x < 1 || y < 1 || x >= Width - 1 || y >= Height - 1) return;
        _tiles[x, y] = Floor();
    }

    public void SpawnMonsters(Random rng, bool isStart, int depth, int densityDiv, int cap, double statMul)
    {
        if (isStart) return;

        var makers = new List<Func<int, int, int, Monster>>
        { Monster.Rat, Monster.Goblin, Monster.Skeleton, Monster.Slime };
        if (depth >= 2)
        {
            makers.Add(Monster.Cultist);
            makers.Add(Monster.Seer);
        }

        // antall skalerer med romstørrelse (gangbare gulvruter)
        int floorCount = 0;
        for (int fx = 1; fx < Width - 1; fx++)
            for (int fy = 1; fy < Height - 1; fy++)
                if (_tiles[fx, fy].IsWalkable) floorCount++;

        int count = floorCount / densityDiv + (depth - 1);
        count = Math.Clamp(count, 1, cap);

        for (int n = 0; n < count; n++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = rng.Next(1, Width - 1);
                int y = rng.Next(1, Height - 1);
                bool center = x == Width / 2 && y == Height / 2;
                if (_tiles[x, y].IsWalkable && !center && MonsterAt(x, y) == null)
                {
                    var m = makers[rng.Next(makers.Count)](x, y, depth);
                    m.Scale(statMul);
                    Monsters.Add(m);
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

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    private bool AdjacentToWalkable(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (InBounds(x + dx, y + dy) && IsWalkable(x + dx, y + dy))
                    return true;
        return false;
    }

    public void Render(ICellSurface surface)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                Tile t = _tiles[x, y];
                if (!t.IsWalkable && t.Glyph == _wallGlyph)
                {
                    // fjell/vegg: vegg-glyf der det grenser til gulv, ellers svart
                    if (AdjacentToWalkable(x, y))
                        surface.SetGlyph(x, y, _wallGlyph, Color.White, Color.Black);
                    else
                        surface.SetGlyph(x, y, Glyph.Void, Color.White, Color.Black);
                }
                else
                {
                    // gulv, grus, søyler og flerfelts-objekter tegner sin egen glyf
                    surface.SetGlyph(x, y, t.Glyph, t.Foreground, t.Background);
                }
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

    private static Tile Water() => new()
    { Glyph = Glyph.Water, Foreground = Color.White, Background = Color.Black, IsWalkable = true };

    private static Tile Crate() => new()
    { Glyph = Glyph.Crate, Foreground = Color.White, Background = Color.Black, IsWalkable = false };

    private static Tile Crack() => new()
    { Glyph = Glyph.Crack, Foreground = Color.White, Background = Color.Black, IsWalkable = false };

    private static Tile StatueTop() => new()
    { Glyph = Glyph.StatueTop, Foreground = Color.White, Background = Color.Black, IsWalkable = false };

    private static Tile StatueBottom() => new()
    { Glyph = Glyph.StatueBottom, Foreground = Color.White, Background = Color.Black, IsWalkable = false };
}

using System;
using System.Collections.Generic;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class RootScreen : ScreenSurface
{
    private const int MapWidth = 20;
    private const int MapHeight = 12;

    private readonly ScreenSurface _status;
    private readonly Dungeon _dungeon;
    private readonly Player _player;
    private readonly Random _rng = new();

    private enum FadeState { None, Out, In }
    private FadeState _fade = FadeState.None;
    private double _fadeTimer;
    private Direction _pendingDoor;
    private const double FadeDuration = 0.2;

    private readonly List<Point> _effectTiles = new();
    private Color _effectColor;
    private double _effectTimer;
    private string _lastAction = "";

    public RootScreen(Character character) : base(MapWidth, MapHeight)
    {
        Font = GameFonts.Tiles;
        FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);

        _status = new ScreenSurface(80, 1) { UsePixelPositioning = true };
        _status.Position = new Point(0, MapHeight * GameFonts.Tiles.GlyphHeight);
        Children.Add(_status);

        _dungeon = new Dungeon(gridWidth: 5, gridHeight: 5, roomWidth: MapWidth, roomHeight: MapHeight);
        _player = new Player(character, MapWidth / 2, MapHeight / 2);

        ApplyExploreAbility();
        Render();
        DrawStatus();
    }

    private void ApplyExploreAbility()
    {
        switch (_player.Character.Ability)
        {
            case Ability.RevealNeighbors:
                _dungeon.RevealNeighbors(_dungeon.CurrentGridX, _dungeon.CurrentGridY);
                break;
            case Ability.RevealAll:
                _dungeon.RevealAll();
                break;
        }
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_fade != FadeState.None) return true;

        if (keyboard.IsKeyPressed(Keys.M))
        {
            var mapScreen = new MapScreen(_dungeon);
            Children.Add(mapScreen);
            mapScreen.IsFocused = true;
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.Q)) { BasicAttack(); return true; }
        if (keyboard.IsKeyPressed(Keys.W)) { HeavyAttack(); return true; }
        if (keyboard.IsKeyPressed(Keys.E)) { SpecialAttack(); return true; }

        int dx = 0, dy = 0;
        if (keyboard.IsKeyPressed(Keys.Up)) dy = -1;
        else if (keyboard.IsKeyPressed(Keys.Down)) dy = 1;
        else if (keyboard.IsKeyPressed(Keys.Left)) dx = -1;
        else if (keyboard.IsKeyPressed(Keys.Right)) dx = 1;

        if (dx == 0 && dy == 0) return false;

        _player.Facing = FacingFrom(dx, dy);

        int nx = _player.X + dx;
        int ny = _player.Y + dy;
        var room = _dungeon.CurrentRoom;

        var door = room.GetDoorAt(nx, ny);
        if (door.HasValue)
        {
            _pendingDoor = door.Value;
            _fade = FadeState.Out;
            _fadeTimer = 0;
            return true;
        }

        if (room.MonsterAt(nx, ny) != null)
        {
            return true;
        }

        if (room.IsWalkable(nx, ny))
        {
            _player.X = nx;
            _player.Y = ny;
            Render();
            DrawStatus();
        }
        return true;
    }

    // -------------------------------------------------------- attacks
    private void BasicAttack()
    {
        var c = _player.Character;
        ShowEffect(Resolve(c.BasicShape), Glow(c.Color, 45));
        _lastAction = c.Ranged ? "Shot!" : "Strike!";
        Render();
        DrawStatus();
    }

    private void HeavyAttack()
    {
        var c = _player.Character;
        ShowEffect(Resolve(c.HeavyShape), Glow(c.Color, 90));
        _lastAction = "Heavy: " + HeavyName(c.HeavyShape);
        Render();
        DrawStatus();
    }

    private void SpecialAttack()
    {
        switch (_player.Character.Special)
        {
            case Special.Blink:
                Blink(); _lastAction = "Teleport!"; break;
            case Special.Whirlwind:
                ShowEffect(Around(1), new Color(245, 150, 70)); _lastAction = "Whirlwind!"; break;
            case Special.Bash:
                ShowEffect(Resolve(AttackShape.Sweep), new Color(150, 190, 235)); _lastAction = "Shield bash!"; break;
            case Special.Volley:
                {
                    var (dx, dy) = Offset(_player.Facing);
                    ShowEffect(Ray(_player.X, _player.Y, dx, dy, 9), new Color(150, 220, 90));
                    _lastAction = "Volley!"; break;
                }
            case Special.Nova:
                ShowEffect(Around(2), new Color(130, 235, 140)); _lastAction = "Death nova!"; break;
            case Special.HolyLight:
                ShowEffect(Around(2), new Color(245, 225, 140)); _lastAction = "Holy light!"; break;
            default:
                _lastAction = "No special"; break;
        }
        Render();
        DrawStatus();
    }

    // -------------------------------------------------------- attack shapes
    private List<Point> Resolve(AttackShape shape)
    {
        var (dx, dy) = Offset(_player.Facing);
        int px = _player.X, py = _player.Y;
        int perpX = dy, perpY = dx;     // perpendicular to facing
        var t = new List<Point>();

        switch (shape)
        {
            case AttackShape.Jab:
                t.Add(new Point(px + dx, py + dy));
                break;
            case AttackShape.Thrust:
                t.AddRange(Ray(px, py, dx, dy, 2));
                break;
            case AttackShape.Bolt:
                t.AddRange(Ray(px, py, dx, dy, 5));
                break;
            case AttackShape.Arrow:
                t.AddRange(Ray(px, py, dx, dy, 6));
                break;
            case AttackShape.Sweep:
                {
                    var c = new Point(px + dx, py + dy);
                    t.Add(c);
                    t.Add(new Point(c.X + perpX, c.Y + perpY));
                    t.Add(new Point(c.X - perpX, c.Y - perpY));
                    break;
                }
            case AttackShape.WideSweep:
                {
                    var c = new Point(px + dx, py + dy);
                    for (int k = -2; k <= 2; k++)
                        t.Add(new Point(c.X + perpX * k, c.Y + perpY * k));
                    break;
                }
            case AttackShape.Cone:
                {
                    t.Add(new Point(px + dx, py + dy));
                    var c2 = new Point(px + dx * 2, py + dy * 2);
                    t.Add(c2);
                    t.Add(new Point(c2.X + perpX, c2.Y + perpY));
                    t.Add(new Point(c2.X - perpX, c2.Y - perpY));
                    break;
                }
            case AttackShape.Blast:
                {
                    int cx = px + dx * 2, cy = py + dy * 2;
                    for (int ax = -1; ax <= 1; ax++)
                        for (int ay = -1; ay <= 1; ay++)
                            t.Add(new Point(cx + ax, cy + ay));
                    break;
                }
            case AttackShape.TripleLine:
                t.AddRange(Ray(px, py, dx, dy, 6));
                t.AddRange(Ray(px + perpX, py + perpY, dx, dy, 6));
                t.AddRange(Ray(px - perpX, py - perpY, dx, dy, 6));
                break;
            case AttackShape.Cross:
                t.AddRange(Ray(px, py, 0, -1, 2));
                t.AddRange(Ray(px, py, 0, 1, 2));
                t.AddRange(Ray(px, py, -1, 0, 2));
                t.AddRange(Ray(px, py, 1, 0, 2));
                break;
            case AttackShape.Slam:
                t.AddRange(Around(1));
                break;
        }
        return t;
    }

    // En stråle fra (sx,sy) i retning (dx,dy), stopper i vegg/kant.
    private List<Point> Ray(int sx, int sy, int dx, int dy, int len)
    {
        var room = _dungeon.CurrentRoom;
        var list = new List<Point>();
        int x = sx, y = sy;
        for (int i = 0; i < len; i++)
        {
            x += dx; y += dy;
            if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight) break;
            if (!room.IsWalkable(x, y)) break;
            list.Add(new Point(x, y));
        }
        return list;
    }

    private List<Point> Around(int radius)
    {
        var t = new List<Point>();
        for (int ax = -radius; ax <= radius; ax++)
            for (int ay = -radius; ay <= radius; ay++)
            {
                if (ax == 0 && ay == 0) continue;
                t.Add(new Point(_player.X + ax, _player.Y + ay));
            }
        return t;
    }

    private void ShowEffect(IEnumerable<Point> tiles, Color color)
    {
        _effectTiles.Clear();
        _effectTiles.AddRange(tiles);
        _effectColor = color;
        _effectTimer = 0.14;
    }

    private static Color Glow(Color c, int amt) =>
        new(Math.Min(255, c.R + amt), Math.Min(255, c.G + amt), Math.Min(255, c.B + amt));

    private static string HeavyName(AttackShape s) => s switch
    {
        AttackShape.WideSweep => "wide sweep",
        AttackShape.Slam => "ground slam",
        AttackShape.Cone => "flurry",
        AttackShape.Blast => "blast",
        AttackShape.TripleLine => "volley spread",
        AttackShape.Cross => "death cross",
        _ => "swing",
    };

    private void Blink()
    {
        var room = _dungeon.CurrentRoom;
        for (int i = 0; i < 100; i++)
        {
            int x = _rng.Next(1, room.Width - 1);
            int y = _rng.Next(1, room.Height - 1);
            if (room.IsWalkable(x, y)) { _player.X = x; _player.Y = y; break; }
        }
    }

    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (_effectTimer > 0)
        {
            _effectTimer -= delta.TotalSeconds;
            if (_effectTimer <= 0) { _effectTiles.Clear(); Render(); }
        }

        if (_fade == FadeState.None) return;

        _fadeTimer += delta.TotalSeconds;
        double t = Math.Clamp(_fadeTimer / FadeDuration, 0, 1);

        if (_fade == FadeState.Out)
        {
            Tint = new Color(0, 0, 0, (int)(t * 255));
            if (t >= 1)
            {
                var newPos = _dungeon.TransitionTo(_pendingDoor);
                _player.X = newPos.X;
                _player.Y = newPos.Y;
                ApplyExploreAbility();
                Render();
                _fade = FadeState.In;
                _fadeTimer = 0;
            }
        }
        else
        {
            Tint = new Color(0, 0, 0, (int)((1 - t) * 255));
            if (t >= 1) { Tint = Color.Transparent; _fade = FadeState.None; }
        }
    }

    private void Render()
    {
        _dungeon.CurrentRoom.Render(Surface);

        foreach (var m in _dungeon.CurrentRoom.Monsters)
            Surface.SetGlyph(m.X, m.Y, m.TileIndex, Color.White, _dungeon.CurrentRoom.FloorBackground);

        foreach (var p in _effectTiles)
            if (p.X >= 0 && p.Y >= 0 && p.X < MapWidth && p.Y < MapHeight)
                Surface.SetGlyph(p.X, p.Y, Glyph.Solid, _effectColor, _effectColor);

        Surface.SetGlyph(_player.X, _player.Y, _player.TileIndex,
                         Color.White, _dungeon.CurrentRoom.FloorBackground);
        Surface.IsDirty = true;
    }

    private static Direction FacingFrom(int dx, int dy)
    {
        if (dy < 0) return Direction.North;
        if (dy > 0) return Direction.South;
        if (dx < 0) return Direction.West;
        return Direction.East;
    }

    private static (int dx, int dy) Offset(Direction d) => d switch
    {
        Direction.North => (0, -1),
        Direction.South => (0, 1),
        Direction.West => (-1, 0),
        Direction.East => (1, 0),
        _ => (0, 1)
    };

    private void DrawStatus()
    {
        var s = _status.Surface;
        s.Clear();
        var c = _player.Character;

        int x = 1;
        s.Print(x, 0, c.Name, c.Color);
        x += c.Name.Length + 2;
        s.Print(x, 0, $"HP {_player.Hp}/{c.MaxHp}", new Color(210, 120, 120));
        x += 11;
        s.Print(x, 0, "Q/W/E: attack", new Color(120, 120, 120));
        x += 14;
        if (_lastAction.Length > 0)
            s.Print(x, 0, _lastAction, new Color(220, 210, 140));
        s.IsDirty = true;
    }
}
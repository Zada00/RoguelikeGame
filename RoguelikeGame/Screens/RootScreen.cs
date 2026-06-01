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
        List<Point> tiles = c.Ranged ? LineInFront(5) : new() { Front() };
        ShowEffect(tiles, Brighten(c.Color));
        _lastAction = c.Ranged ? "Shot!" : "Strike!";
        Render();
        DrawStatus();
    }

    private void HeavyAttack()
    {
        ShowEffect(ArcInFront(), new Color(235, 140, 70));
        _lastAction = "Heavy swing!";
        Render();
        DrawStatus();
    }

    private void SpecialAttack()
    {
        switch (_player.Character.Special)
        {
            case Special.Blink: Blink(); _lastAction = "Teleport!"; break;
            case Special.Whirlwind: ShowEffect(Around(1), new Color(245, 150, 70)); _lastAction = "Whirlwind!"; break;
            case Special.Bash: ShowEffect(ArcInFront(), new Color(150, 190, 235)); _lastAction = "Shield bash!"; break;
            case Special.Volley: ShowEffect(LineInFront(9), new Color(150, 220, 90)); _lastAction = "Volley!"; break;
            case Special.Nova: ShowEffect(Around(2), new Color(130, 235, 140)); _lastAction = "Death nova!"; break;
            case Special.HolyLight: ShowEffect(Around(2), new Color(245, 225, 140)); _lastAction = "Holy light!"; break;
            default: _lastAction = "No special"; break;
        }
        Render();
        DrawStatus();
    }

    // ruta rett foran spilleren
    private Point Front()
    {
        var (dx, dy) = Offset(_player.Facing);
        return new Point(_player.X + dx, _player.Y + dy);
    }

    // en linje fremover som stopper i vegg/kant
    private List<Point> LineInFront(int maxLen)
    {
        var tiles = new List<Point>();
        var (dx, dy) = Offset(_player.Facing);
        var room = _dungeon.CurrentRoom;
        int x = _player.X, y = _player.Y;
        for (int i = 0; i < maxLen; i++)
        {
            x += dx; y += dy;
            if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight) break;
            if (!room.IsWalkable(x, y)) break;
            tiles.Add(new Point(x, y));
        }
        return tiles;
    }

    // tre ruter i en bue foran
    private List<Point> ArcInFront()
    {
        var (dx, dy) = Offset(_player.Facing);
        var c = new Point(_player.X + dx, _player.Y + dy);
        return new List<Point>
        {
            c,
            new Point(c.X + dy, c.Y + dx),
            new Point(c.X - dy, c.Y - dx),
        };
    }

    // alle ruter innenfor en radius rundt spilleren
    private List<Point> Around(int radius)
    {
        var tiles = new List<Point>();
        for (int ax = -radius; ax <= radius; ax++)
            for (int ay = -radius; ay <= radius; ay++)
            {
                if (ax == 0 && ay == 0) continue;
                tiles.Add(new Point(_player.X + ax, _player.Y + ay));
            }
        return tiles;
    }

    private void ShowEffect(IEnumerable<Point> tiles, Color color)
    {
        _effectTiles.Clear();
        _effectTiles.AddRange(tiles);
        _effectColor = color;
        _effectTimer = 0.14;
    }

    private static Color Brighten(Color c) =>
        new(Math.Min(255, c.R + 45), Math.Min(255, c.G + 45), Math.Min(255, c.B + 45));

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
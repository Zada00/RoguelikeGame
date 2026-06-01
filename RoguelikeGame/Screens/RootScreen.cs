using System;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class RootScreen : ScreenSurface
{
    private const int MapWidth = 20;
    private const int MapHeight = 12;
    private const double MoveSpeed = 6.0;   // ruter per sekund

    private readonly ScreenSurface _status;
    private readonly ScreenSurface _playerSurface;
    private Dungeon _dungeon;
    private readonly Player _player;

    private double _px, _py;   // spillerens flyt-posisjon (i ruter, med desimaler)
    private int _depth = 1;

    private enum FadeState { None, Out, In }
    private FadeState _fade = FadeState.None;
    private double _fadeTimer;
    private Direction _pendingDoor;
    private const double FadeDuration = 0.18;
    private double _doorCooldown;

    public RootScreen(Character character) : base(MapWidth, MapHeight)
    {
        Font = GameFonts.Tiles;
        FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);

        _status = new ScreenSurface(80, 1) { UsePixelPositioning = true };
        _status.Position = new Point(0, MapHeight * GameFonts.Tiles.GlyphHeight);
        Children.Add(_status);

        _dungeon = new Dungeon(5, 5, MapWidth, MapHeight, _depth);
        _player = new Player(character, MapWidth / 2, MapHeight / 2);
        _px = _player.X;
        _py = _player.Y;

        // Spilleren som egen pikselplassert flate -> kan gli jevnt mellom ruter.
        _playerSurface = new ScreenSurface(1, 1) { UsePixelPositioning = true };
        _playerSurface.Font = GameFonts.Tiles;
        _playerSurface.FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);
        _playerSurface.Surface.DefaultBackground = Color.Transparent;
        _playerSurface.Surface.SetGlyph(0, 0, character.TileIndex, Color.White, Color.Transparent);
        Children.Add(_playerSurface);

        ApplyExploreAbility();
        RenderMap();
        UpdatePlayerSurface();
    }

    private void ApplyExploreAbility()
    {
        switch (_player.Character.Ability)
        {
            case Ability.RevealNeighbors:
                _dungeon.RevealNeighbors(_dungeon.CurrentGridX, _dungeon.CurrentGridY); break;
            case Ability.RevealAll:
                _dungeon.RevealAll(); break;
        }
    }

    // Bare M håndteres her nå; bevegelse skjer i Update (sanntid).
    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.M))
        {
            var mapScreen = new MapScreen(_dungeon);
            Children.Add(mapScreen);
            mapScreen.IsFocused = true;
            return true;
        }
        return false;
    }

    public override void Update(TimeSpan delta)
    {
        base.Update(delta);
        double dt = delta.TotalSeconds;

        if (_doorCooldown > 0) _doorCooldown -= dt;

        if (_fade != FadeState.None) { UpdateFade(dt); return; }
        if (!IsFocused) return;   // f.eks. når kartet er åpent

        // Hold-taster leses hver frame.
        var kb = Game.Instance.Keyboard;
        double vx = 0, vy = 0;
        if (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)) vy -= 1;
        if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)) vy += 1;
        if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A)) vx -= 1;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) vx += 1;

        if (vx != 0 || vy != 0)
        {
            if (vx != 0 && vy != 0) { vx *= 0.70710678; vy *= 0.70710678; }  // diagonal-normalisering
            MovePlayer(vx * MoveSpeed * dt, vy * MoveSpeed * dt);
            UpdatePlayerSurface();
        }
    }

    private void MovePlayer(double dx, double dy)
    {
        var room = _dungeon.CurrentRoom;

        // Flytt én akse om gangen, så man glir langs vegger.
        double nx = _px + dx;
        if (CellFree(nx, _py, room)) _px = nx;
        double ny = _py + dy;
        if (CellFree(_px, ny, room)) _py = ny;

        int cx = (int)Math.Round(_px);
        int cy = (int)Math.Round(_py);
        _player.X = cx;
        _player.Y = cy;

        if (room.IsStairs(cx, cy)) { Descend(); return; }

        if (_doorCooldown <= 0)
        {
            var door = room.GetDoorAt(cx, cy);
            if (door.HasValue)
            {
                _pendingDoor = door.Value;
                _fade = FadeState.Out;
                _fadeTimer = 0;
            }
        }
    }

    private bool CellFree(double fx, double fy, Room room)
    {
        int cx = (int)Math.Round(fx);
        int cy = (int)Math.Round(fy);
        if (!room.IsWalkable(cx, cy)) return false;
        if (room.MonsterAt(cx, cy) != null) return false;
        return true;
    }

    private void UpdateFade(double dt)
    {
        _fadeTimer += dt;
        double t = Math.Clamp(_fadeTimer / FadeDuration, 0, 1);

        if (_fade == FadeState.Out)
        {
            Tint = new Color(0, 0, 0, (int)(t * 255));
            _playerSurface.Tint = Tint;
            if (t >= 1)
            {
                var pos = _dungeon.TransitionTo(_pendingDoor);
                _px = pos.X; _py = pos.Y;
                _player.X = pos.X; _player.Y = pos.Y;
                ApplyExploreAbility();
                RenderMap();
                UpdatePlayerSurface();
                _fade = FadeState.In;
                _fadeTimer = 0;
                _doorCooldown = 0.3;
            }
        }
        else
        {
            Tint = new Color(0, 0, 0, (int)((1 - t) * 255));
            _playerSurface.Tint = Tint;
            if (t >= 1)
            {
                Tint = Color.Transparent;
                _playerSurface.Tint = Color.Transparent;
                _fade = FadeState.None;
            }
        }
    }

    private void Descend()
    {
        _depth++;
        _dungeon = new Dungeon(5, 5, MapWidth, MapHeight, _depth);
        _px = MapWidth / 2; _py = MapHeight / 2;
        _player.X = MapWidth / 2; _player.Y = MapHeight / 2;
        _doorCooldown = 0.3;
        ApplyExploreAbility();
        RenderMap();
        UpdatePlayerSurface();
    }

    private void UpdatePlayerSurface()
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        _playerSurface.Position = new Point(
            (int)Math.Round(_px * tile),
            (int)Math.Round(_py * tile));
    }

    private void RenderMap()
    {
        _dungeon.CurrentRoom.Render(Surface);
        foreach (var m in _dungeon.CurrentRoom.Monsters)
            Surface.SetGlyph(m.X, m.Y, m.TileIndex, Color.White, _dungeon.CurrentRoom.FloorBackground);
        Surface.IsDirty = true;
        DrawStatus();
    }

    private void DrawStatus()
    {
        var s = _status.Surface;
        s.Clear();
        var c = _player.Character;
        int x = 1;
        s.Print(x, 0, c.Name, c.Color); x += c.Name.Length + 2;
        s.Print(x, 0, $"HP {_player.Hp}/{c.MaxHp}", new Color(210, 120, 120)); x += 11;
        s.Print(x, 0, $"Depth {_depth}", new Color(150, 200, 150)); x += 9;
        s.Print(x, 0, "Arrows/WASD   M: map", new Color(120, 120, 120));
        s.IsDirty = true;
    }
}
using System;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class RootScreen : ScreenSurface
{
    private const int MapWidth = 20;
    private const int MapHeight = 12;
    private static readonly Color FloorBackground = new(44, 44, 54);

    private readonly ScreenSurface _status;
    private readonly Dungeon _dungeon;
    private readonly Player _player;
    private readonly Random _rng = new();

    private enum FadeState { None, Out, In }
    private FadeState _fade = FadeState.None;
    private double _fadeTimer;
    private Direction _pendingDoor;
    private const double FadeDuration = 0.2;

    public RootScreen(Character character) : base(MapWidth, MapHeight)
    {
        // Bytt DENNE flatens font til tileset-et (16x16 grafiske fliser).
        Font = GameFonts.Tiles;
        FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);

        // Statuslinje som eget barn, med standard tekst-font, plassert nederst.
        _status = new ScreenSurface(80, 1) { UsePixelPositioning = true };
        _status.Position = new Point(0, MapHeight * GameFonts.Tiles.GlyphHeight);
        Children.Add(_status);

        _dungeon = new Dungeon(gridWidth: 5, gridHeight: 5, roomWidth: MapWidth, roomHeight: MapHeight);
        _player = new Player(character, MapWidth / 2, MapHeight / 2);

        Render();
        DrawStatus();
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

        if (_player.Character.Ability == Ability.Blink && keyboard.IsKeyPressed(Keys.Space))
        {
            Blink();
            return true;
        }

        int dx = 0, dy = 0;
        if (keyboard.IsKeyPressed(Keys.Up)) dy = -1;
        else if (keyboard.IsKeyPressed(Keys.Down)) dy = 1;
        else if (keyboard.IsKeyPressed(Keys.Left)) dx = -1;
        else if (keyboard.IsKeyPressed(Keys.Right)) dx = 1;

        if (_player.Character.Ability == Ability.DiagonalMove)
        {
            if (keyboard.IsKeyPressed(Keys.Q)) { dx = -1; dy = -1; }
            else if (keyboard.IsKeyPressed(Keys.E)) { dx = 1; dy = -1; }
            else if (keyboard.IsKeyPressed(Keys.Z)) { dx = -1; dy = 1; }
            else if (keyboard.IsKeyPressed(Keys.C)) { dx = 1; dy = 1; }
        }

        if (dx == 0 && dy == 0) return false;

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
        }
        return true;
    }

    private void Blink()
    {
        var room = _dungeon.CurrentRoom;
        for (int i = 0; i < 100; i++)
        {
            int x = _rng.Next(1, room.Width - 1);
            int y = _rng.Next(1, room.Height - 1);
            if (room.IsWalkable(x, y))
            {
                _player.X = x;
                _player.Y = y;
                break;
            }
        }
        Render();
    }

    public override void Update(TimeSpan delta)
    {
        base.Update(delta);
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
                Render();
                _fade = FadeState.In;
                _fadeTimer = 0;
            }
        }
        else
        {
            Tint = new Color(0, 0, 0, (int)((1 - t) * 255));
            if (t >= 1)
            {
                Tint = Color.Transparent;
                _fade = FadeState.None;
            }
        }
    }

    private void Render()
    {
        _dungeon.CurrentRoom.Render(Surface);
        // Spilleren tegnes med hvit forgrunn (egne farger) og gulv-farget bakgrunn.
        Surface.SetGlyph(_player.X, _player.Y, _player.TileIndex, Color.White, FloorBackground);
        Surface.IsDirty = true;
    }

    private void DrawStatus()
    {
        var s = _status.Surface;
        s.Clear();
        var c = _player.Character;

        int x = 1;
        s.Print(x, 0, c.Name, c.Color);
        x += c.Name.Length + 2;
        s.Print(x, 0, $"HP {_player.Hp}/{c.MaxHp}", new Color(210, 120, 120));
        x += 12;

        string controls = c.Ability switch
        {
            Ability.DiagonalMove => "QEZC: diagonal   M: map",
            Ability.Blink => "Space: teleport   M: map",
            _ => "Arrows: move   M: map"
        };
        s.Print(x, 0, controls, new Color(120, 120, 120));
        s.IsDirty = true;
    }
}
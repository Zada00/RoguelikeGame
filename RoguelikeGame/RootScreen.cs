using System;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class RootScreen : ScreenSurface
{
    private readonly Dungeon _dungeon;
    private readonly Player _player;

    // --- Fade-tilstand ---
    private enum FadeState { None, Out, In }
    private FadeState _fade = FadeState.None;
    private double _fadeTimer;
    private Direction _pendingDoor;
    private const double FadeDuration = 0.5; // sekunder hver vei

    public RootScreen() : base(80, 25)
    {
        _dungeon = new Dungeon(gridWidth: 5, gridHeight: 5, roomWidth: 80, roomHeight: 25);
        _player = new Player(40, 12);
        Render();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        // Mens en overgang pågår, ignorer all input.
        if (_fade != FadeState.None) return true;

        if (keyboard.IsKeyPressed(Keys.M))
        {
            var mapScreen = new MapScreen(_dungeon);
            Children.Add(mapScreen);
            mapScreen.IsFocused = true;
            return true;
        }

        int dx = 0, dy = 0;
        if (keyboard.IsKeyPressed(Keys.Up)) dy = -1;
        else if (keyboard.IsKeyPressed(Keys.Down)) dy = 1;
        else if (keyboard.IsKeyPressed(Keys.Left)) dx = -1;
        else if (keyboard.IsKeyPressed(Keys.Right)) dx = 1;

        if (dx == 0 && dy == 0) return false;

        int nx = _player.X + dx;
        int ny = _player.Y + dy;
        var room = _dungeon.CurrentRoom;

        // Går vi mot en dør? Start en fade i stedet for å bytte rom med en gang.
        var door = room.GetDoorAt(nx, ny);
        if (door.HasValue)
        {
            _pendingDoor = door.Value;
            _fade = FadeState.Out;
            _fadeTimer = 0;
            return true;
        }

        // Ellers: vanlig bevegelse hvis ruta er gangbar.
        if (room.IsWalkable(nx, ny))
        {
            _player.X = nx;
            _player.Y = ny;
            Render();
        }

        return true;
    }

    // Kalles automatisk på hver frame. Her driver vi fade-animasjonen frem.
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (_fade == FadeState.None) return;

        _fadeTimer += delta.TotalSeconds;
        double t = Math.Clamp(_fadeTimer / FadeDuration, 0, 1);

        if (_fade == FadeState.Out)
        {
            // 0 -> 255: skjermen blir gradvis svart.
            Tint = new Color(0, 0, 0, (int)(t * 255));
            IsDirty = true;

            if (t >= 1)
            {
                // Nå er alt svart - bytt rom uten at spilleren ser det.
                var newPos = _dungeon.TransitionTo(_pendingDoor);
                _player.X = newPos.X;
                _player.Y = newPos.Y;
                Render();

                _fade = FadeState.In;
                _fadeTimer = 0;
            }
        }
        else // FadeState.In
        {
            // 255 -> 0: det nye rommet avsløres.
            Tint = new Color(0, 0, 0, (int)((1 - t) * 255));
            IsDirty = true;

            if (t >= 1)
            {
                Tint = Color.Transparent; // helt klart igjen
                _fade = FadeState.None;
            }
        }
    }

    private void Render()
    {
        _dungeon.CurrentRoom.Render(Surface);
        Surface.SetGlyph(_player.X, _player.Y, _player.Glyph, _player.Color);
    }
}
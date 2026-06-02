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
    private const double MoveSpeed = 6.0;
    private const double ManaRegen = 2.0;

    private static readonly Color EnemyShotColor = new(255, 110, 80);

    private readonly ScreenSurface _status;
    private readonly ScreenSurface _playerSurface;
    private readonly List<ScreenSurface> _monsterViews = new();
    private readonly List<Bullet> _bullets = new();
    private readonly List<ScreenSurface> _bulletViews = new();
    private Dungeon _dungeon;
    private readonly Player _player;

    private double _px, _py;
    private double _playerVx, _playerVy;
    private int _depth = 1;
    private bool _dead;
    private double _hurtTimer;
    private double _fireCooldown;

    private double _mana;
    private int _lastManaShown;
    private bool _rightWasDown;

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
        _px = _player.X; _py = _player.Y;
        _mana = character.Mana;
        _lastManaShown = (int)_mana;

        _playerSurface = new ScreenSurface(1, 1) { UsePixelPositioning = true };
        _playerSurface.Font = GameFonts.Tiles;
        _playerSurface.FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);
        _playerSurface.Surface.DefaultBackground = Color.Transparent;
        _playerSurface.Surface.SetGlyph(0, 0, character.TileIndex, Color.White, Color.Transparent);
        Children.Add(_playerSurface);

        ApplyExploreAbility();
        RenderMap();
        RebuildMonsterViews();
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

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_dead) return true;
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

        if (_dead) return;
        if (_doorCooldown > 0) _doorCooldown -= dt;
        if (_fireCooldown > 0) _fireCooldown -= dt;

        if (_fade != FadeState.None) { UpdateFade(dt); return; }
        if (!IsFocused) return;

        double maxMana = _player.Character.Mana;
        if (_mana < maxMana) _mana = Math.Min(maxMana, _mana + ManaRegen * dt);
        if ((int)_mana != _lastManaShown) { _lastManaShown = (int)_mana; DrawStatus(); }

        // ---- bevegelse ----
        var kb = Game.Instance.Keyboard;
        double vx = 0, vy = 0;
        if (kb.IsKeyDown(Keys.Up)    || kb.IsKeyDown(Keys.W)) vy -= 1;
        if (kb.IsKeyDown(Keys.Down)  || kb.IsKeyDown(Keys.S)) vy += 1;
        if (kb.IsKeyDown(Keys.Left)  || kb.IsKeyDown(Keys.A)) vx -= 1;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) vx += 1;

        if (vx != 0 || vy != 0)
        {
            if (vx != 0 && vy != 0) { vx *= 0.70710678; vy *= 0.70710678; }
            _playerVx = vx * MoveSpeed;
            _playerVy = vy * MoveSpeed;
            MovePlayer(vx * MoveSpeed * dt, vy * MoveSpeed * dt);
            UpdatePlayerSurface();
        }
        else { _playerVx = 0; _playerVy = 0; }

        // ---- skyting: venstre mus ----
        var mouse = Game.Instance.Mouse;
        if (mouse.LeftButtonDown && _fireCooldown <= 0 && TryAim(out double adx, out double ady))
        {
            Fire(adx, ady);
            _fireCooldown = _player.Character.FireInterval;
        }

        // ---- special: høyre mus ----
        bool rightDown = mouse.RightButtonDown;
        if (rightDown && !_rightWasDown)
        {
            TryAim(out double sdx, out double sdy);
            DoSpecial(sdx, sdy);
        }
        _rightWasDown = rightDown;

        UpdateBullets(dt);

        if (_fade == FadeState.None && !_dead)
            UpdateMonsters(dt);

        if (_hurtTimer > 0) { _hurtTimer -= dt; Tint = new Color(180, 0, 0, 50); }
        else Tint = Color.Transparent;
    }

    private bool TryAim(out double dx, out double dy)
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        var mouse = Game.Instance.Mouse;
        double pcx = _px * tile + tile / 2.0;
        double pcy = _py * tile + tile / 2.0;
        double ax = mouse.ScreenPosition.X - pcx;
        double ay = mouse.ScreenPosition.Y - pcy;
        if (ax == 0 && ay == 0) { dx = 0; dy = 1; return false; }
        double len = Math.Sqrt(ax * ax + ay * ay);
        dx = ax / len; dy = ay / len;
        return true;
    }

    private void Fire(double dirX, double dirY)
    {
        var c = _player.Character;
        double baseAngle = Math.Atan2(dirY, dirX);
        int count = Math.Max(1, c.ShotCount);
        double spreadRad = c.ShotSpread * Math.PI / 180.0;

        for (int i = 0; i < count; i++)
        {
            double a = baseAngle;
            if (count > 1) a += (i - (count - 1) / 2.0) * (spreadRad / (count - 1));
            SpawnBullet(Math.Cos(a) * c.ShotSpeed, Math.Sin(a) * c.ShotSpeed, c.Attack, c.ShotLife, c.Color, false);
        }
    }

    private void DoSpecial(double dirX, double dirY)
    {
        var c = _player.Character;
        if (c.Special == Special.None) return;

        int cost = SpecialCost(c.Special);
        if (_mana < cost) return;

        double baseAngle = Math.Atan2(dirY, dirX);

        switch (c.Special)
        {
            case Special.Blink:     BlinkToMouse(); break;
            case Special.Whirlwind: RadialBurst(8, c.ShotSpeed, 0.5, c.Attack, new Color(245, 150, 70)); break;
            case Special.Nova:      RadialBurst(14, c.ShotSpeed, 0.7, c.Attack, new Color(130, 235, 140)); break;
            case Special.Volley:    Fan(baseAngle, 5, 28, c.ShotSpeed + 2, c.ShotLife, c.Attack, new Color(150, 220, 90)); break;
            case Special.Bash:      Fan(baseAngle, 6, 70, c.ShotSpeed, 0.3, c.Attack + 2, new Color(150, 190, 235)); break;
            case Special.HolyLight: _player.Hp = Math.Min(c.MaxHp, _player.Hp + 8); DrawStatus(); break;
        }

        _mana -= cost;
        _lastManaShown = (int)_mana;
        DrawStatus();
    }

    private static int SpecialCost(Special s) => s switch
    {
        Special.Blink     => 5,
        Special.Whirlwind => 4,
        Special.Bash      => 4,
        Special.Volley    => 4,
        Special.Nova      => 6,
        Special.HolyLight => 5,
        _                 => 0,
    };

    private void RadialBurst(int count, double speed, double life, int dmg, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            double a = i * (2 * Math.PI / count);
            SpawnBullet(Math.Cos(a) * speed, Math.Sin(a) * speed, dmg, life, color, false);
        }
    }

    private void Fan(double baseAngle, int count, double spreadDeg, double speed, double life, int dmg, Color color)
    {
        double spreadRad = spreadDeg * Math.PI / 180.0;
        for (int i = 0; i < count; i++)
        {
            double a = baseAngle + (i - (count - 1) / 2.0) * (spreadRad / Math.Max(1, count - 1));
            SpawnBullet(Math.Cos(a) * speed, Math.Sin(a) * speed, dmg, life, color, false);
        }
    }

    private void BlinkToMouse()
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        var mouse = Game.Instance.Mouse;
        double targetX = mouse.ScreenPosition.X / (double)tile;
        double targetY = mouse.ScreenPosition.Y / (double)tile;

        double dx = targetX - _px, dy = targetY - _py;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 0.01) return;

        double ux = dx / dist, uy = dy / dist;
        var room = _dungeon.CurrentRoom;
        for (double d = dist; d >= 0; d -= 0.5)
        {
            int cx = (int)Math.Round(_px + ux * d);
            int cy = (int)Math.Round(_py + uy * d);
            if (room.IsWalkable(cx, cy))
            {
                _px += ux * d; _py += uy * d;
                _player.X = (int)Math.Round(_px);
                _player.Y = (int)Math.Round(_py);
                UpdatePlayerSurface();
                return;
            }
        }
    }

    private void SpawnBullet(double vx, double vy, int damage, double life, Color color, bool hostile)
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        _bullets.Add(new Bullet(_px, _py, vx, vy, damage, life, color, hostile));
        AddBulletView(_px, _py, color);
    }

    private void SpawnEnemyBullet(double sx, double sy, double vx, double vy, int damage, double life)
    {
        _bullets.Add(new Bullet(sx, sy, vx, vy, damage, life, EnemyShotColor, true));
        AddBulletView(sx, sy, EnemyShotColor);
    }

    private void AddBulletView(double fx, double fy, Color color)
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        var v = new ScreenSurface(1, 1) { UsePixelPositioning = true };
        v.Font = GameFonts.Tiles;
        v.FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);
        v.Surface.DefaultBackground = Color.Transparent;
        v.Surface.SetGlyph(0, 0, Glyph.Bullet, Glow(color, 40), Color.Transparent);
        v.Position = new Point((int)Math.Round(fx * tile), (int)Math.Round(fy * tile));
        Children.Add(v);
        _bulletViews.Add(v);
    }

    private void UpdateBullets(double dt)
    {
        var room = _dungeon.CurrentRoom;
        int tile = GameFonts.Tiles.GlyphWidth;
        bool monsterDied = false;

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.Fx += b.Vx * dt;
            b.Fy += b.Vy * dt;
            b.Life -= dt;

            bool remove = false;
            if (b.Life <= 0) remove = true;
            else if (!room.IsWalkable((int)Math.Round(b.Fx), (int)Math.Round(b.Fy))) remove = true;
            else if (b.Hostile)
            {
                double ddx = _px - b.Fx, ddy = _py - b.Fy;
                if (ddx * ddx + ddy * ddy < 0.5 * 0.5)
                {
                    _player.Hp -= b.Damage;
                    _hurtTimer = 0.12;
                    DrawStatus();
                    remove = true;
                    if (_player.Hp <= 0 && !_dead) Die();
                }
            }
            else
            {
                foreach (var m in room.Monsters)
                {
                    double ddx = m.Fx - b.Fx, ddy = m.Fy - b.Fy;
                    if (ddx * ddx + ddy * ddy < 0.45 * 0.45)
                    {
                        m.Hp -= b.Damage;
                        remove = true;
                        if (m.Hp <= 0) { room.Monsters.Remove(m); monsterDied = true; }
                        break;
                    }
                }
            }

            if (remove)
            {
                Children.Remove(_bulletViews[i]);
                _bulletViews.RemoveAt(i);
                _bullets.RemoveAt(i);
            }
            else
            {
                _bulletViews[i].Position = new Point(
                    (int)Math.Round(b.Fx * tile), (int)Math.Round(b.Fy * tile));
            }
        }

        if (monsterDied) RebuildMonsterViews();
    }

    private void UpdateMonsters(double dt)
    {
        var room = _dungeon.CurrentRoom;
        int tile = GameFonts.Tiles.GlyphWidth;
        var monsters = room.Monsters;
        int dmg = 0;

        for (int i = 0; i < monsters.Count && i < _monsterViews.Count; i++)
        {
            var m = monsters[i];
            dmg += m.UpdateRealtime(dt, _px, _py, _playerVx, _playerVy, room);

            if (m.HasPendingShot)
                SpawnEnemyBullet(m.Fx, m.Fy,
                    m.PendingShotDx * m.ShotSpeed, m.PendingShotDy * m.ShotSpeed,
                    Math.Max(1, m.Attack), 3.0);

            _monsterViews[i].Position = new Point(
                (int)Math.Round(m.Fx * tile), (int)Math.Round(m.Fy * tile));
        }

        if (dmg > 0)
        {
            _player.Hp -= dmg;
            _hurtTimer = 0.12;
            DrawStatus();
            if (_player.Hp <= 0 && !_dead) Die();
        }
    }

    private void MovePlayer(double dx, double dy)
    {
        var room = _dungeon.CurrentRoom;

        double nx = _px + dx;
        if (CellFree(nx, _py, room)) _px = nx;
        double ny = _py + dy;
        if (CellFree(_px, ny, room)) _py = ny;

        int cx = (int)Math.Round(_px);
        int cy = (int)Math.Round(_py);
        _player.X = cx; _player.Y = cy;

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
        return room.IsWalkable(cx, cy);
    }

    private void UpdateFade(double dt)
    {
        _fadeTimer += dt;
        double t = Math.Clamp(_fadeTimer / FadeDuration, 0, 1);

        if (_fade == FadeState.Out)
        {
            Tint = new Color(0, 0, 0, (int)(t * 255));
            if (t >= 1)
            {
                var pos = _dungeon.TransitionTo(_pendingDoor);
                _px = pos.X; _py = pos.Y;
                _player.X = pos.X; _player.Y = pos.Y;
                ClearBullets();
                ApplyExploreAbility();
                RenderMap();
                RebuildMonsterViews();
                UpdatePlayerSurface();
                _fade = FadeState.In;
                _fadeTimer = 0;
                _doorCooldown = 0.3;
            }
        }
        else
        {
            Tint = new Color(0, 0, 0, (int)((1 - t) * 255));
            if (t >= 1) { Tint = Color.Transparent; _fade = FadeState.None; }
        }
    }

    private void Descend()
    {
        _depth++;
        _dungeon = new Dungeon(5, 5, MapWidth, MapHeight, _depth);
        _px = MapWidth / 2; _py = MapHeight / 2;
        _player.X = MapWidth / 2; _player.Y = MapHeight / 2;
        _doorCooldown = 0.3;
        ClearBullets();
        ApplyExploreAbility();
        RenderMap();
        RebuildMonsterViews();
        UpdatePlayerSurface();
    }

    private void Die()
    {
        _dead = true;
        _player.Hp = 0;
        var over = new GameOverScreen(_player.Character.Name);
        Game.Instance.Screen = over;
        over.IsFocused = true;
    }

    private void ClearBullets()
    {
        foreach (var v in _bulletViews) Children.Remove(v);
        _bulletViews.Clear();
        _bullets.Clear();
    }

    private void RebuildMonsterViews()
    {
        foreach (var v in _monsterViews) Children.Remove(v);
        _monsterViews.Clear();

        int tile = GameFonts.Tiles.GlyphWidth;
        foreach (var m in _dungeon.CurrentRoom.Monsters)
        {
            var v = new ScreenSurface(1, 1) { UsePixelPositioning = true };
            v.Font = GameFonts.Tiles;
            v.FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);
            v.Surface.DefaultBackground = Color.Transparent;
            v.Surface.SetGlyph(0, 0, m.TileIndex, Color.White, Color.Transparent);
            v.Position = new Point((int)Math.Round(m.Fx * tile), (int)Math.Round(m.Fy * tile));
            Children.Add(v);
            _monsterViews.Add(v);
        }

        Children.Remove(_playerSurface);
        Children.Add(_playerSurface);
    }

    private void UpdatePlayerSurface()
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        _playerSurface.Position = new Point(
            (int)Math.Round(_px * tile), (int)Math.Round(_py * tile));
    }

    private void RenderMap()
    {
        _dungeon.CurrentRoom.Render(Surface);
        Surface.IsDirty = true;
        DrawStatus();
    }

    private static Color Glow(Color c, int amt) =>
        new(Math.Min(255, c.R + amt), Math.Min(255, c.G + amt), Math.Min(255, c.B + amt));

    private void DrawStatus()
    {
        var s = _status.Surface;
        s.Clear();
        var c = _player.Character;
        int x = 1;
        s.Print(x, 0, c.Name, c.Color); x += c.Name.Length + 2;
        s.Print(x, 0, $"HP {_player.Hp}/{c.MaxHp}", new Color(210, 120, 120)); x += 11;
        s.Print(x, 0, $"MP {(int)_mana}/{c.Mana}", new Color(120, 170, 230)); x += 10;
        s.Print(x, 0, $"Depth {_depth}", new Color(150, 200, 150)); x += 9;
        s.Print(x, 0, "L: shoot  R: special", new Color(120, 120, 120));
        s.IsDirty = true;
    }
}

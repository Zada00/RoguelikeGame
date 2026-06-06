using System;
using System.Collections.Generic;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class RootScreen : ScreenSurface
{
    private const int MapWidth = 24;
    private const int MapHeight = 14;
    private const double ManaRegen = 2.0;

    private readonly ScreenSurface _status;
    private readonly ScreenSurface _notify;
    private readonly ScreenSurface _bossBar;
    private readonly ScreenSurface _playerSurface;
    private readonly ScreenSurface _rewardPanel;
    private readonly List<ScreenSurface> _monsterViews = new();
    private readonly List<Bullet> _bullets = new();
    private readonly List<ScreenSurface> _bulletViews = new();
    private Dungeon _dungeon;
    private readonly Player _player;
    private readonly Random _rng = new();
    private readonly Difficulty _difficulty;

    private double _px, _py;
    private double _playerVx, _playerVy;
    private double _moveSpeed = 6.0;
    private int _depth = 1;
    private bool _dead;
    private double _hurtTimer;
    private double _fireCooldown;
    private double _enterGrace;

    // oppgraderinger
    private int _bonusDamage;
    private int _bonusMaxHp;
    private int _bonusMaxMana;
    private int _bonusShotCount;
    private int _attackSpeedLevels;
    private int _bonusRangeLevels;
    private int _pendingUpgrades;

    private double _mana;
    private int _lastManaShown;
    private bool _rightWasDown;

    private class Reward { public string Label = ""; public Action Apply = () => { }; }
    private readonly List<Reward> _rewardOptions = new();
    private bool _choosingReward;

    private enum FadeState { None, Out, In }
    private FadeState _fade = FadeState.None;
    private double _fadeTimer;
    private Direction _pendingDoor;
    private const double FadeDuration = 0.18;
    private double _doorCooldown;

    private int MaxHp => _player.Character.MaxHp + _bonusMaxHp;
    private int MaxMana => _player.Character.Mana + _bonusMaxMana;
    private int Damage => _player.Character.Attack + _bonusDamage;
    private int ShotCountEff => Math.Max(1, _player.Character.ShotCount + _bonusShotCount);
    private double FireIntervalEff => Math.Max(0.05, _player.Character.FireInterval - 0.03 * _attackSpeedLevels);
    private double ShotLifeEff => _player.Character.ShotLife + 0.15 * _bonusRangeLevels;

    public RootScreen(Character character, Difficulty difficulty) : base(MapWidth, MapHeight)
    {
        _difficulty = difficulty;

        Font = GameFonts.Tiles;
        FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.One);

        UsePixelPositioning = true;
        Position = CenterOffset();

        _status = new ScreenSurface(80, 1) { UsePixelPositioning = true };
        _status.Position = new Point(0, MapHeight * GameFonts.Tiles.GlyphHeight);
        Children.Add(_status);

        _notify = new ScreenSurface(22, 1) { UsePixelPositioning = true };
        _notify.Position = new Point(MapWidth * 32 - 22 * 8 - 4, 4);
        _notify.IsVisible = false;
        Children.Add(_notify);

        _bossBar = new ScreenSurface(40, 1) { UsePixelPositioning = true };
        _bossBar.Position = new Point((MapWidth * 32 - 40 * 8) / 2, 4);
        _bossBar.IsVisible = false;
        Children.Add(_bossBar);

        _dungeon = new Dungeon(5, 5, MapWidth, MapHeight, _depth, _difficulty);
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

        _rewardPanel = new ScreenSurface(40, 9) { UsePixelPositioning = true };
        _rewardPanel.Position = new Point(160, 120);
        _rewardPanel.IsVisible = false;
        Children.Add(_rewardPanel);

        ApplyExploreAbility();
        RenderMap();
        RebuildMonsterViews();
        UpdatePlayerSurface();
    }

    // Sentrer hele kartet i vinduet (samme tall som SetWindowSizeInCells i Program.cs).
    private Point CenterOffset()
    {
        int tile = GameFonts.Tiles.GlyphWidth;
        int mapPxW = MapWidth * tile;
        int mapPxH = (MapHeight + 1) * tile;
        int winPxW = 104 * 8;
        int winPxH = 34 * 16;
        int ox = Math.Max(0, (winPxW - mapPxW) / 2);
        int oy = Math.Max(0, (winPxH - mapPxH) / 2);
        return new Point(ox, oy);
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

        if (_choosingReward)
        {
            if (keyboard.IsKeyPressed(Keys.U) || keyboard.IsKeyPressed(Keys.Escape))
            {
                _choosingReward = false;
                _rewardPanel.IsVisible = false;
                return true;
            }

            int pick = -1;
            if (keyboard.IsKeyPressed(Keys.D1)) pick = 0;
            else if (keyboard.IsKeyPressed(Keys.D2)) pick = 1;
            else if (keyboard.IsKeyPressed(Keys.D3)) pick = 2;

            if (pick >= 0 && pick < _rewardOptions.Count)
            {
                _rewardOptions[pick].Apply();
                _pendingUpgrades--;
                if (_player.Hp > MaxHp) _player.Hp = MaxHp;
                UpdateNotify();
                DrawStatus();

                if (_pendingUpgrades > 0)
                    OpenUpgradeChoice();          // rull nye valg, bli i vinduet
                else
                {
                    _choosingReward = false;
                    _rewardPanel.IsVisible = false;
                }
            }
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.U) && _pendingUpgrades > 0)
        {
            OpenUpgradeChoice();
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.M))
        {
            var mapScreen = new MapScreen(_dungeon, _player.Character.TileIndex) { UsePixelPositioning = true };
            mapScreen.Position = new Point(-Position.X, -Position.Y);
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
        if (_choosingReward) return;   // pause kun mens panelet er åpent

        if (_doorCooldown > 0) _doorCooldown -= dt;
        if (_fireCooldown > 0) _fireCooldown -= dt;
        if (_enterGrace > 0) _enterGrace -= dt;

        if (_fade != FadeState.None) { UpdateFade(dt); return; }
        if (!IsFocused) return;

        double maxMana = MaxMana;
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
            double mul = _dungeon.CurrentRoom.IsWater((int)Math.Round(_px), (int)Math.Round(_py)) ? 0.5 : 1.0;
            _playerVx = vx * _moveSpeed * mul;
            _playerVy = vy * _moveSpeed * mul;
            MovePlayer(vx * _moveSpeed * mul * dt, vy * _moveSpeed * mul * dt);
            UpdatePlayerSurface();
        }
        else { _playerVx = 0; _playerVy = 0; }

        // ---- skyting ----
        var mouse = Game.Instance.Mouse;
        if (mouse.LeftButtonDown && _fireCooldown <= 0 && TryAim(out double adx, out double ady))
        {
            Fire(adx, ady);
            _fireCooldown = FireIntervalEff;
        }

        // ---- special ----
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

        // ---- rom ryddet? gjør oppgradering(er) tilgjengelig (ingen pause) ----
        var room = _dungeon.CurrentRoom;
        if (room.HadMonsters && !room.RewardGranted && room.Monsters.Count == 0)
        {
            room.RewardGranted = true;
            _pendingUpgrades += 1 + DifficultySettings.Get(_difficulty).RewardBonus;
            UpdateNotify();
        }

        if (_hurtTimer > 0) { _hurtTimer -= dt; Tint = new Color(180, 0, 0, 50); }
        else Tint = Color.Transparent;
    }

    // ---------------------------------------------------------- oppgraderinger
    private void UpdateNotify()
    {
        var s = _notify.Surface;
        if (_pendingUpgrades > 0)
        {
            s.DefaultBackground = new Color(45, 33, 12);
            s.Clear();
            s.Print(1, 0, $"Upgrade! Press U ({_pendingUpgrades})", new Color(245, 220, 120));
            _notify.IsVisible = true;
        }
        else
        {
            s.DefaultBackground = Color.Transparent;
            s.Clear();
            _notify.IsVisible = false;
        }
        s.IsDirty = true;
    }

    private void OpenUpgradeChoice()
    {
        var pool = new List<Reward>
        {
            new() { Label = "+2 Damage",     Apply = () => _bonusDamage += 2 },
            new() { Label = "+5 Max HP",     Apply = () => { _bonusMaxHp += 5; _player.Hp += 5; } },
            new() { Label = "+0.8 Speed",    Apply = () => _moveSpeed += 0.8 },
            new() { Label = "+3 Max Mana",   Apply = () => _bonusMaxMana += 3 },
            new() { Label = "+1 Projectile", Apply = () => _bonusShotCount += 1 },
            new() { Label = "+ Attack Speed",Apply = () => _attackSpeedLevels += 1 },
            new() { Label = "+ Range",       Apply = () => _bonusRangeLevels += 1 },
            new() { Label = "Full Heal",     Apply = () => _player.Hp = MaxHp },
            new() { Label = "Restore Mana",  Apply = () => _mana = MaxMana },
        };

        _rewardOptions.Clear();
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = _rng.Next(pool.Count);
            _rewardOptions.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        _choosingReward = true;
        Tint = Color.Transparent;
        DrawRewardPanel();
        Children.Remove(_rewardPanel);
        Children.Add(_rewardPanel);
        _rewardPanel.IsVisible = true;
    }

    private void DrawRewardPanel()
    {
        var s = _rewardPanel.Surface;
        s.DefaultBackground = new Color(20, 20, 30);
        s.Clear();
        s.Print(2, 1, "CHOOSE AN UPGRADE", new Color(150, 220, 150));
        for (int i = 0; i < _rewardOptions.Count; i++)
            s.Print(2, 3 + i, $"[{i + 1}]  {_rewardOptions[i].Label}", new Color(225, 215, 150));
        s.Print(2, 7, "1/2/3 to pick   U/Esc to close", new Color(150, 150, 150));
        s.IsDirty = true;
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
        int count = ShotCountEff;
        double spreadDeg = c.ShotSpread;
        if (spreadDeg <= 0 && count > 1) spreadDeg = 10.0 * (count - 1);
        double spreadRad = spreadDeg * Math.PI / 180.0;
        double baseAngle = Math.Atan2(dirY, dirX);
        double life = ShotLifeEff;

        for (int i = 0; i < count; i++)
        {
            double a = baseAngle;
            if (count > 1) a += (i - (count - 1) / 2.0) * (spreadRad / (count - 1));
            SpawnBullet(Math.Cos(a) * c.ShotSpeed, Math.Sin(a) * c.ShotSpeed, Damage, life, c.Color, false);
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
            case Special.Whirlwind: RadialBurst(8, c.ShotSpeed, 0.5, Damage, new Color(245, 150, 70)); break;
            case Special.Nova:      RadialBurst(14, c.ShotSpeed, 0.7, Damage, new Color(130, 235, 140)); break;
            case Special.Volley:    Fan(baseAngle, 5, 28, c.ShotSpeed + 2, ShotLifeEff, Damage, new Color(150, 220, 90)); break;
            case Special.Bash:      Fan(baseAngle, 6, 70, c.ShotSpeed, 0.3, Damage + 2, new Color(150, 190, 235)); break;
            case Special.HolyLight: _player.Hp = Math.Min(MaxHp, _player.Hp + 8); DrawStatus(); break;
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
        _bullets.Add(new Bullet(_px, _py, vx, vy, damage, life, color, hostile));
        AddBulletView(_px, _py, color);
    }

    private void SpawnEnemyBullet(double sx, double sy, double vx, double vy, int damage, double life, Color color)
    {
        _bullets.Add(new Bullet(sx, sy, vx, vy, damage, life, color, true));
        AddBulletView(sx, sy, color);
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

            foreach (var (sdx, sdy) in m.PendingShots)
                SpawnEnemyBullet(m.Fx, m.Fy, sdx * m.ShotSpeed, sdy * m.ShotSpeed,
                    Math.Max(1, m.Attack), 3.0, m.BulletColor);

            _monsterViews[i].Position = new Point(
                (int)Math.Round(m.Fx * tile), (int)Math.Round(m.Fy * tile));
        }

        UpdateBossBar();

        if (dmg > 0 && _enterGrace <= 0)
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

        if (room.IsStairs(cx, cy) && !room.HasLivingBoss) { Descend(); return; }

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
                _enterGrace = 0.5;
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
        _dungeon = new Dungeon(5, 5, MapWidth, MapHeight, _depth, _difficulty);
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
        UpdateBossBar();
    }

    private void UpdateBossBar()
    {
        Monster? boss = null;
        foreach (var m in _dungeon.CurrentRoom.Monsters)
            if (m.IsBoss) { boss = m; break; }

        var s = _bossBar.Surface;
        if (boss == null) { _bossBar.IsVisible = false; s.Clear(); s.IsDirty = true; return; }

        s.DefaultBackground = new Color(30, 12, 16);
        s.Clear();
        int barLen = 22;
        double frac = Math.Clamp(boss.Hp / (double)boss.MaxHp, 0, 1);
        int filled = (int)Math.Round(barLen * frac);
        string bar = new string('=', filled) + new string('-', barLen - filled);
        s.Print(1, 0, boss.Name, new Color(255, 160, 130));
        s.Print(1 + boss.Name.Length + 1, 0, bar, new Color(255, 90, 80));
        _bossBar.IsVisible = true;
        s.IsDirty = true;
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
        s.Print(x, 0, $"HP {_player.Hp}/{MaxHp}", new Color(210, 120, 120)); x += 11;
        s.Print(x, 0, $"MP {(int)_mana}/{MaxMana}", new Color(120, 170, 230)); x += 10;
        s.Print(x, 0, $"DMG {Damage}", new Color(220, 180, 120)); x += 8;
        s.Print(x, 0, $"Depth {_depth}", new Color(150, 200, 150));
        s.IsDirty = true;
    }
}

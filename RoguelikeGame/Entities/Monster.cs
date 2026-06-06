using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Monster
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Fx { get; set; }
    public double Fy { get; set; }

    public string Name { get; }
    public int TileIndex { get; }
    public int MaxHp { get; private set; }
    public int Hp { get; set; }
    public int Attack { get; private set; }
    public int Defense { get; }

    public double Speed { get; }
    public double AggroRange { get; }
    public double AttackInterval { get; }
    public bool Awake { get; set; }
    private double _attackCooldown;

    // ---- skyting ----
    public bool CanShoot { get; }
    public double ShootInterval { get; }
    public double ShotSpeed { get; }
    public double ShootRange { get; }
    public bool Predictive { get; }
    public bool KeepsDistance { get; }
    private double _shootCooldown;

    // ---- boss ----
    public bool IsBoss { get; }
    public int Pattern { get; }            // 0 ring, 1 spiral, 2 aimed fan
    public Color BulletColor { get; }
    private double _spiralAngle;

    // settes hver tick: én eller flere skudd-retninger
    public readonly List<(double dx, double dy)> PendingShots = new();

    public Monster(string name, int tileIndex, int maxHp, int attack, int defense,
                   double speed, double aggroRange, double attackInterval, int x, int y,
                   bool canShoot = false, double shootInterval = 0, double shotSpeed = 0,
                   double shootRange = 0, bool predictive = false, bool keepsDistance = false,
                   bool isBoss = false, int pattern = 0, Color? bulletColor = null)
    {
        Name = name;
        TileIndex = tileIndex;
        MaxHp = maxHp;
        Hp = maxHp;
        Attack = attack;
        Defense = defense;
        Speed = speed;
        AggroRange = aggroRange;
        AttackInterval = attackInterval;
        X = x; Y = y;
        Fx = x; Fy = y;
        CanShoot = canShoot;
        ShootInterval = shootInterval;
        ShotSpeed = shotSpeed;
        ShootRange = shootRange;
        Predictive = predictive;
        KeepsDistance = keepsDistance;
        IsBoss = isBoss;
        Pattern = pattern;
        BulletColor = bulletColor ?? new Color(255, 110, 80);
    }

    public void Scale(double mul)
    {
        MaxHp = Math.Max(1, (int)Math.Round(MaxHp * mul));
        Hp = MaxHp;
        Attack = Math.Max(1, (int)Math.Round(Attack * mul));
    }

    private static Monster Make(string name, int tile, int hp, int atk, int def,
                                double speed, double aggro, double interval, int x, int y, int depth)
        => new(name, tile, hp + (depth - 1) * 2, atk + (depth - 1), def, speed, aggro, interval, x, y);

    public static Monster Rat(int x, int y, int d)      => Make("Rat",      Glyph.Rat,      3, 1, 0, 4.5, 999, 0.5, x, y, d);
    public static Monster Goblin(int x, int y, int d)   => Make("Goblin",   Glyph.Goblin,   6, 3, 1, 3.2, 6,   0.8, x, y, d);
    public static Monster Skeleton(int x, int y, int d) => Make("Skeleton", Glyph.Skeleton, 9, 5, 2, 2.0, 999, 1.2, x, y, d);
    public static Monster Slime(int x, int y, int d)    => Make("Slime",    Glyph.Slime,    7, 2, 1, 1.4, 999, 0.9, x, y, d);

    public static Monster Cultist(int x, int y, int d)
        => new("Cultist", Glyph.Cultist, 6 + (d - 1) * 2, 2 + (d - 1), 1,
               2.6, 999, 1.0, x, y,
               canShoot: true, shootInterval: 1.4, shotSpeed: 7, shootRange: 9,
               predictive: false, keepsDistance: false);

    public static Monster Seer(int x, int y, int d)
        => new("Seer", Glyph.Seer, 5 + (d - 1) * 2, 2 + (d - 1), 0,
               2.2, 999, 1.0, x, y,
               canShoot: true, shootInterval: 1.8, shotSpeed: 9, shootRange: 11,
               predictive: true, keepsDistance: true);

    // Tilfeldig boss, skalert med dybde.
    public static Monster RandomBoss(int x, int y, int depth, Random rng)
    {
        int hp = 40 + (depth - 1) * 14;
        int atk = 3 + (depth - 1);

        Monster b = rng.Next(3) switch
        {
            0 => new("Warden", Glyph.BossWarden, hp, atk, 2, 1.5, 9999, 0.9, x, y,
                     canShoot: true, shootInterval: 1.7, shotSpeed: 6.5, shootRange: 9999,
                     isBoss: true, pattern: 0, bulletColor: new Color(255, 120, 90)),
            1 => new("Hive", Glyph.BossHive, hp + 10, atk, 2, 1.3, 9999, 0.9, x, y,
                     canShoot: true, shootInterval: 1.4, shotSpeed: 6.0, shootRange: 9999,
                     isBoss: true, pattern: 1, bulletColor: new Color(150, 230, 120)),
            _ => new("Overseer", Glyph.BossOverseer, hp, atk + 1, 1, 1.7, 9999, 0.9, x, y,
                     canShoot: true, shootInterval: 1.1, shotSpeed: 8.0, shootRange: 9999,
                     isBoss: true, pattern: 2, bulletColor: new Color(190, 130, 255)),
        };
        b.Awake = true;
        return b;
    }

    public int UpdateRealtime(double dt, double pfx, double pfy, double pvx, double pvy, Room room)
    {
        PendingShots.Clear();
        if (_attackCooldown > 0) _attackCooldown -= dt;
        if (_shootCooldown > 0) _shootCooldown -= dt;

        double dx = pfx - Fx, dy = pfy - Fy;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (!Awake)
        {
            if (dist <= AggroRange) Awake = true;
            else return 0;
        }

        int contact = 0;
        if (dist < 0.9 && _attackCooldown <= 0)
        {
            _attackCooldown = AttackInterval;
            contact = Math.Max(1, Attack);
        }

        double step = Speed * dt;
        if (room.IsWater((int)Math.Round(Fx), (int)Math.Round(Fy))) step *= 0.5;

        double ux = dist > 0.001 ? dx / dist : 0;
        double uy = dist > 0.001 ? dy / dist : 0;

        if (IsBoss)
        {
            if (dist > 1.5) MoveBy(ux * step, uy * step, room);   // sakte jakt
            if (CanShoot && _shootCooldown <= 0)
            {
                EmitBossPattern(ux, uy);
                _shootCooldown = ShootInterval;
            }
        }
        else
        {
            if (CanShoot && KeepsDistance)
            {
                const double minR = 4.0, maxR = 7.0;
                if (dist < minR) MoveBy(-ux * step, -uy * step, room);
                else if (dist > maxR) MoveBy(ux * step, uy * step, room);
            }
            else if (dist >= 0.9)
            {
                MoveBy(ux * step, uy * step, room);
            }

            if (CanShoot && _shootCooldown <= 0 && dist <= ShootRange)
            {
                double tx = pfx, ty = pfy;
                if (Predictive && ShotSpeed > 0.001)
                {
                    double lead = dist / ShotSpeed;
                    tx = pfx + pvx * lead;
                    ty = pfy + pvy * lead;
                }
                double sdx = tx - Fx, sdy = ty - Fy;
                double sl = Math.Sqrt(sdx * sdx + sdy * sdy);
                if (sl > 0.001)
                {
                    PendingShots.Add((sdx / sl, sdy / sl));
                    _shootCooldown = ShootInterval;
                }
            }
        }

        X = (int)Math.Round(Fx);
        Y = (int)Math.Round(Fy);
        return contact;
    }

    private void EmitBossPattern(double aimx, double aimy)
    {
        switch (Pattern)
        {
            case 0: // ring
            {
                int n = 12;
                for (int i = 0; i < n; i++)
                {
                    double a = i * (2 * Math.PI / n);
                    PendingShots.Add((Math.Cos(a), Math.Sin(a)));
                }
                break;
            }
            case 1: // spiral
            {
                int n = 10;
                for (int i = 0; i < n; i++)
                {
                    double a = _spiralAngle + i * (2 * Math.PI / n);
                    PendingShots.Add((Math.Cos(a), Math.Sin(a)));
                }
                _spiralAngle += 0.5;
                break;
            }
            default: // sikter mot deg, vifte
            {
                double baseA = Math.Atan2(aimy, aimx);
                int n = 5;
                double spread = 0.5;
                for (int i = 0; i < n; i++)
                {
                    double a = baseA + (i - (n - 1) / 2.0) * (spread / (n - 1));
                    PendingShots.Add((Math.Cos(a), Math.Sin(a)));
                }
                break;
            }
        }
    }

    private void MoveBy(double mx, double my, Room room)
    {
        double nfx = Fx + mx;
        if (room.IsWalkable((int)Math.Round(nfx), (int)Math.Round(Fy))) Fx = nfx;
        double nfy = Fy + my;
        if (room.IsWalkable((int)Math.Round(Fx), (int)Math.Round(nfy))) Fy = nfy;
    }
}

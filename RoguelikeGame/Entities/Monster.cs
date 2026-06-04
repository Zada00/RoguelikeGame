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

    public bool HasPendingShot { get; private set; }
    public double PendingShotDx { get; private set; }
    public double PendingShotDy { get; private set; }

    public Monster(string name, int tileIndex, int maxHp, int attack, int defense,
                   double speed, double aggroRange, double attackInterval, int x, int y,
                   bool canShoot = false, double shootInterval = 0, double shotSpeed = 0,
                   double shootRange = 0, bool predictive = false, bool keepsDistance = false)
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
    }

    // Skaler HP og skade etter vanskelighetsgrad.
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

    public int UpdateRealtime(double dt, double pfx, double pfy, double pvx, double pvy, Room room)
    {
        HasPendingShot = false;
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
                PendingShotDx = sdx / sl;
                PendingShotDy = sdy / sl;
                HasPendingShot = true;
                _shootCooldown = ShootInterval;
            }
        }

        X = (int)Math.Round(Fx);
        Y = (int)Math.Round(Fy);
        return contact;
    }

    private void MoveBy(double mx, double my, Room room)
    {
        double nfx = Fx + mx;
        if (room.IsWalkable((int)Math.Round(nfx), (int)Math.Round(Fy))) Fx = nfx;
        double nfy = Fy + my;
        if (room.IsWalkable((int)Math.Round(Fx), (int)Math.Round(nfy))) Fy = nfy;
    }
}

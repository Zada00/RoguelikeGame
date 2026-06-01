namespace RoguelikeGame;

internal class Monster
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Fx { get; set; }   // flyt-posisjon for jevn bevegelse
    public double Fy { get; set; }

    public string Name { get; }
    public int TileIndex { get; }
    public int MaxHp { get; }
    public int Hp { get; set; }
    public int Attack { get; }
    public int Defense { get; }

    public double Speed { get; }          // ruter per sekund
    public double AggroRange { get; }      // hvor nær du må være før den våkner (stor = alltid)
    public double AttackInterval { get; }  // sekunder mellom berørings-treff
    public bool Awake { get; set; }
    private double _attackCooldown;

    public Monster(string name, int tileIndex, int maxHp, int attack, int defense,
                   double speed, double aggroRange, double attackInterval, int x, int y)
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
    }

    // Dypere etasjer => mer HP og skade.
    private static Monster Make(string name, int tile, int hp, int atk, int def,
                                double speed, double aggro, double interval, int x, int y, int depth)
        => new(name, tile, hp + (depth - 1) * 2, atk + (depth - 1), def, speed, aggro, interval, x, y);

    //                                       hp atk def  speed aggro interval
    public static Monster Rat(int x, int y, int d) => Make("Rat", Glyph.Rat, 3, 1, 0, 4.5, 999, 0.5, x, y, d);
    public static Monster Goblin(int x, int y, int d) => Make("Goblin", Glyph.Goblin, 6, 3, 1, 3.2, 6, 0.8, x, y, d);
    public static Monster Skeleton(int x, int y, int d) => Make("Skeleton", Glyph.Skeleton, 9, 5, 2, 2.0, 999, 1.2, x, y, d);
    public static Monster Slime(int x, int y, int d) => Make("Slime", Glyph.Slime, 7, 2, 1, 1.4, 999, 0.9, x, y, d);

    // Sanntids-tikk. Returnerer skade påført spilleren denne framen (0 hvis ingen).
    public int UpdateRealtime(double dt, double playerFx, double playerFy, Room room)
    {
        if (_attackCooldown > 0) _attackCooldown -= dt;

        double dx = playerFx - Fx;
        double dy = playerFy - Fy;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (!Awake)
        {
            if (dist <= AggroRange) Awake = true;
            else return 0;   // sover fortsatt
        }

        // Nær nok til å skade (berøring), på cooldown.
        if (dist < 0.9)
        {
            if (_attackCooldown <= 0)
            {
                _attackCooldown = AttackInterval;
                return Math.Max(1, Attack);
            }
            return 0;
        }

        // Beveg mot spilleren, én akse om gangen (glir langs vegger).
        double step = Speed * dt;
        double mx = dx / dist * step;
        double my = dy / dist * step;

        double nfx = Fx + mx;
        if (room.IsWalkable((int)Math.Round(nfx), (int)Math.Round(Fy))) Fx = nfx;
        double nfy = Fy + my;
        if (room.IsWalkable((int)Math.Round(Fx), (int)Math.Round(nfy))) Fy = nfy;

        X = (int)Math.Round(Fx);
        Y = (int)Math.Round(Fy);
        return 0;
    }
}
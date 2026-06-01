namespace RoguelikeGame;

internal class Monster
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Name { get; }
    public int TileIndex { get; }
    public int MaxHp { get; }
    public int Hp { get; set; }
    public int Attack { get; }
    public int Defense { get; }

    public Monster(string name, int tileIndex, int maxHp, int attack, int defense, int x, int y)
    {
        Name = name;
        TileIndex = tileIndex;
        MaxHp = maxHp;
        Hp = maxHp;
        Attack = attack;
        Defense = defense;
        X = x;
        Y = y;
    }

    // Monsterets tur: angrip spilleren hvis vi står inntil, ellers ta ett skritt mot ham.
    // Returnerer skade påført spilleren (0 hvis ingen).
    public int TakeTurn(Player player, Room room)
    {
        int dxToPlayer = player.X - X;
        int dyToPlayer = player.Y - Y;
        int dist = Math.Abs(dxToPlayer) + Math.Abs(dyToPlayer);

        // Inntil spilleren (nabo-rute)? Angrip.
        if (dist == 1)
            return Math.Max(1, Attack - player.Character.Defense);

        // Ellers: ta ett skritt nærmere, langs aksen med størst avstand.
        int stepX = 0, stepY = 0;
        if (Math.Abs(dxToPlayer) >= Math.Abs(dyToPlayer))
            stepX = Math.Sign(dxToPlayer);
        else
            stepY = Math.Sign(dyToPlayer);

        int nx = X + stepX;
        int ny = Y + stepY;

        // Bare flytt hvis ruta er ledig (ikke vegg, ikke et annet monster, ikke spilleren).
        bool blocked = !room.IsWalkable(nx, ny)
                       || room.MonsterAt(nx, ny) != null
                       || (nx == player.X && ny == player.Y);
        if (!blocked)
        {
            X = nx;
            Y = ny;
        }
        return 0;
    }

    // Fabrikkmetoder for hver monster-type med ferdige stats.
    public static Monster Rat(int x, int y) => new("Rat", Glyph.Rat, 3, 2, 0, x, y);
    public static Monster Goblin(int x, int y) => new("Goblin", Glyph.Goblin, 6, 3, 1, x, y);
    public static Monster Skeleton(int x, int y) => new("Skeleton", Glyph.Skeleton, 8, 4, 2, x, y);
    public static Monster Slime(int x, int y) => new("Slime", Glyph.Slime, 5, 2, 1, x, y);
}
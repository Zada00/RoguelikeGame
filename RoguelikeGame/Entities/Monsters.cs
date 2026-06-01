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

    // Dypere etasjer gir sterkere monstre.
    private static Monster Scaled(string name, int tile, int hp, int atk, int def, int x, int y, int depth)
        => new(name, tile, hp + (depth - 1) * 2, atk + (depth - 1), def, x, y);

    public static Monster Rat(int x, int y, int depth) => Scaled("Rat", Glyph.Rat, 3, 2, 0, x, y, depth);
    public static Monster Goblin(int x, int y, int depth) => Scaled("Goblin", Glyph.Goblin, 6, 3, 1, x, y, depth);
    public static Monster Skeleton(int x, int y, int depth) => Scaled("Skeleton", Glyph.Skeleton, 8, 4, 2, x, y, depth);
    public static Monster Slime(int x, int y, int depth) => Scaled("Slime", Glyph.Slime, 5, 2, 1, x, y, depth);

    // Monsterets tur: angrip hvis inntil spilleren, ellers ta ett skritt mot ham.
    // Returnerer skade påført spilleren (0 hvis ingen).
    public int TakeTurn(Player player, Room room)
    {
        int dxToPlayer = player.X - X;
        int dyToPlayer = player.Y - Y;
        int dist = Math.Abs(dxToPlayer) + Math.Abs(dyToPlayer);

        if (dist == 1)
            return Math.Max(1, Attack - player.Character.Defense);

        int stepX = 0, stepY = 0;
        if (Math.Abs(dxToPlayer) >= Math.Abs(dyToPlayer))
            stepX = Math.Sign(dxToPlayer);
        else
            stepY = Math.Sign(dyToPlayer);

        int nx = X + stepX;
        int ny = Y + stepY;
        bool blocked = !room.IsWalkable(nx, ny)
                       || room.MonsterAt(nx, ny) != null
                       || (nx == player.X && ny == player.Y);
        if (!blocked) { X = nx; Y = ny; }
        return 0;
    }
}

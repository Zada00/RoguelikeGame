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

    // Fabrikkmetoder for hver monster-type med ferdige stats.
    public static Monster Rat(int x, int y) => new("Rat", Glyph.Rat, 3, 2, 0, x, y);
    public static Monster Goblin(int x, int y) => new("Goblin", Glyph.Goblin, 6, 3, 1, x, y);
    public static Monster Skeleton(int x, int y) => new("Skeleton", Glyph.Skeleton, 8, 4, 2, x, y);
    public static Monster Slime(int x, int y) => new("Slime", Glyph.Slime, 5, 2, 1, x, y);
}
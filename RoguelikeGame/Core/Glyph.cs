namespace RoguelikeGame;

internal static class Glyph
{
    public const int Solid = 0, Floor = 1, Wall = 2, Door = 3,
                     Void = 4, Rubble = 5, Pillar = 6, StairsDown = 7;

    public const int Bullet = 15;

    public const int MossFloor = 16, CryptFloor = 17,
                     MossWall = 18, CryptWall = 19,
                     CaveFloor = 20, CaveWall = 21;

    public const int Warrior = 8, Guardian = 9, Rogue = 10, Mage = 11,
                     Scout = 12, Necromancer = 13, Priest = 14;

    public const int Rat = 24, Goblin = 25, Skeleton = 26, Slime = 27,
                     Cultist = 28, Seer = 29;

    public const int Water = 30, Crate = 31, StatueTop = 32, StatueBottom = 33, Crack = 34;

    public const int BossWarden = 35, BossHive = 36, BossOverseer = 37;
}

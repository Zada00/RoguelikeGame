namespace RoguelikeGame;

// Which tile in tiles.png each thing uses. Order matches the generator.
internal static class Glyph
{
    public const int Solid = 0, Floor = 1, Wall = 2, Door = 3,
                     Void = 4, Rubble = 5, Pillar = 6;

    // theme variants
    public const int MossFloor = 16, CryptFloor = 17,
                     MossWall = 18, CryptWall = 19,
                     CaveFloor = 20, CaveWall = 21;

    public const int Warrior = 8, Guardian = 9, Rogue = 10, Mage = 11,
                     Scout = 12, Necromancer = 13, Priest = 14;

    public const int Rat = 24, Goblin = 25, Skeleton = 26, Slime = 27;
}

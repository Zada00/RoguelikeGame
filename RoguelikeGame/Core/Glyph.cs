namespace RoguelikeGame;

// Hvilken rute i tiles.png hver ting bruker. Rekkefølgen matcher generatoren.
internal static class Glyph
{
    public const int Solid = 0, Floor = 1, Wall = 2, Door = 3,
                     Void = 4, Rubble = 5, Pillar = 6;

    public const int Warrior = 8, Guardian = 9, Rogue = 10, Mage = 11,
                     Scout = 12, Necromancer = 13, Priest = 14;
}
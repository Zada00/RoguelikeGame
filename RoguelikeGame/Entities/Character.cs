using SadRogue.Primitives;

namespace RoguelikeGame;

internal enum Ability { None, DiagonalMove, Blink, RevealNeighbors, RevealAll }

internal class Character
{
    public string Name { get; }
    public string Description { get; }
    public int TileIndex { get; }   // hvilken sprite i tileset-et
    public Color Color { get; }     // farge brukt i menyteksten
    public int MaxHp { get; }
    public Ability Ability { get; }

    public Character(string name, string description, int tileIndex, Color color, int maxHp, Ability ability)
    {
        Name = name;
        Description = description;
        TileIndex = tileIndex;
        Color = color;
        MaxHp = maxHp;
        Ability = ability;
    }

    public static readonly List<Character> All = new()
    {
        new("Warrior",     "Balanced bruiser. Tough, and hits hard (once combat arrives).", Glyph.Warrior,     new Color(210, 90, 50),   15, Ability.None),
        new("Guardian",    "Unshakable tank with the most life of all.",                    Glyph.Guardian,    new Color(120, 145, 165), 22, Ability.None),
        new("Rogue",       "Quick and nimble. Can move diagonally.",                        Glyph.Rogue,       new Color(80, 200, 120),  10, Ability.DiagonalMove),
        new("Mage",        "Fragile, but can teleport with a keypress.",                    Glyph.Mage,        new Color(205, 95, 220),   8, Ability.Blink),
        new("Scout",       "Knows the terrain. Reveals neighboring rooms on the map.",      Glyph.Scout,       new Color(180, 220, 90),  11, Ability.RevealNeighbors),
        new("Necromancer", "Senses the dead everywhere. Reveals the whole map.",            Glyph.Necromancer, new Color(150, 80, 190),   9, Ability.RevealAll),
        new("Priest",      "A healer who shines once the fighting starts.",                 Glyph.Priest,      new Color(235, 225, 170), 13, Ability.None),
    };
}
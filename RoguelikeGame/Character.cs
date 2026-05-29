using SadRogue.Primitives;

namespace RoguelikeGame;

// Hva slags spesial-evne en helt har. Mer kan legges til senere.
internal enum Ability
{
    None,
    DiagonalMove,
    Blink,
    RevealNeighbors,
    RevealAll
}

internal class Character
{
    public string Name { get; }
    public string Description { get; }
    public int Glyph { get; }
    public Color Color { get; }
    public int MaxHp { get; }
    public Ability Ability { get; }

    public Character(string name, string description, int glyph, Color color, int maxHp, Ability ability)
    {
        Name = name;
        Description = description;
        Glyph = glyph;
        Color = color;
        MaxHp = maxHp;
        Ability = ability;
    }

    // HELE rollelista. Vil du legge til en ny helt: legg til én linje her.
    public static readonly List<Character> All = new()
    {
        new("Warrior",     "Balanced bruiser. Tough, and hits hard (once combat arrives).", '@', new Color(210, 90, 50),   15, Ability.None),
        new("Guardian",    "Unshakable tank with the most life of all.",                    '@', new Color(120, 145, 165), 22, Ability.None),
        new("Rogue",       "Quick and nimble. Can move diagonally.",                        '@', new Color(80, 200, 120),  10, Ability.DiagonalMove),
        new("Mage",        "Fragile, but can teleport with a keypress.",                    '@', new Color(205, 95, 220),   8, Ability.Blink),
        new("Scout",       "Knows the terrain. Reveals neighboring rooms on the map.",      '@', new Color(180, 220, 90),  11, Ability.RevealNeighbors),
        new("Necromancer", "Senses the dead everywhere. Reveals the whole map.",            '@', new Color(150, 80, 190),   9, Ability.RevealAll),
        new("Priest",      "A healer who shines once the fighting starts.",                 '@', new Color(235, 225, 170), 13, Ability.None),
    };
}
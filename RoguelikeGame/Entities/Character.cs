using SadRogue.Primitives;

namespace RoguelikeGame;

internal enum Ability { None, RevealNeighbors, RevealAll }

internal enum Special { None, Blink, Whirlwind, Bash, Volley, Nova, HolyLight }

// Formen på et angrep – brukes for både basic og heavy.
internal enum AttackShape
{
    Jab,        // 1 rute foran (nærkamp)
    Thrust,     // kort 2-ruters støt
    Bolt,       // 5-ruters avstandslinje
    Arrow,      // 6-ruters avstandslinje
    Sweep,      // 3-ruters bue
    WideSweep,  // 5-ruters bue
    Cone,       // kjegle som vider seg ut foran
    Blast,      // 3x3 eksplosjon to ruter frem
    TripleLine, // tre parallelle linjer frem
    Cross,      // pluss-form i fire retninger
    Slam,       // ring rundt spilleren
}

internal class Character
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int TileIndex { get; init; }
    public Color Color { get; init; }

    public int MaxHp { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int Vision { get; init; }
    public int Speed { get; init; }
    public int Mana { get; init; }

    public bool Ranged { get; init; }
    public AttackShape BasicShape { get; init; }
    public AttackShape HeavyShape { get; init; }
    public Special Special { get; init; }
    public Ability Ability { get; init; }

    public static readonly List<Character> All = new()
    {
        new() { Name = "Warrior",     Description = "Balanced bruiser. Tough, and hits hard.",   TileIndex = Glyph.Warrior,     Color = new(210, 90, 50),   MaxHp = 15, Attack = 6, Defense = 3, Vision = 4, Speed = 4, Mana = 0,  Ranged = false, BasicShape = AttackShape.Jab,    HeavyShape = AttackShape.WideSweep,  Special = Special.Whirlwind, Ability = Ability.None },
        new() { Name = "Guardian",    Description = "Unshakable tank. Most life and defense.",   TileIndex = Glyph.Guardian,    Color = new(120, 145, 165), MaxHp = 22, Attack = 4, Defense = 6, Vision = 4, Speed = 3, Mana = 0,  Ranged = false, BasicShape = AttackShape.Jab,    HeavyShape = AttackShape.Slam,       Special = Special.Bash,      Ability = Ability.None },
        new() { Name = "Rogue",       Description = "Quick and nimble. Fast and sharp-eyed.",    TileIndex = Glyph.Rogue,       Color = new(80, 200, 120),  MaxHp = 10, Attack = 5, Defense = 2, Vision = 5, Speed = 6, Mana = 0,  Ranged = false, BasicShape = AttackShape.Thrust, HeavyShape = AttackShape.Cone,       Special = Special.None,      Ability = Ability.None },
        new() { Name = "Mage",        Description = "Fragile, but slings spells and teleports.", TileIndex = Glyph.Mage,        Color = new(205, 95, 220),  MaxHp = 8,  Attack = 3, Defense = 1, Vision = 5, Speed = 4, Mana = 12, Ranged = true,  BasicShape = AttackShape.Bolt,   HeavyShape = AttackShape.Blast,      Special = Special.Blink,     Ability = Ability.None },
        new() { Name = "Scout",       Description = "Sharp-eyed archer. Reveals nearby rooms.",  TileIndex = Glyph.Scout,       Color = new(180, 220, 90),  MaxHp = 11, Attack = 4, Defense = 2, Vision = 7, Speed = 5, Mana = 2,  Ranged = true,  BasicShape = AttackShape.Arrow,  HeavyShape = AttackShape.TripleLine, Special = Special.Volley,    Ability = Ability.RevealNeighbors },
        new() { Name = "Necromancer", Description = "Senses the dead. Reveals the whole map.",   TileIndex = Glyph.Necromancer, Color = new(150, 80, 190),  MaxHp = 9,  Attack = 3, Defense = 2, Vision = 6, Speed = 4, Mana = 10, Ranged = true,  BasicShape = AttackShape.Bolt,   HeavyShape = AttackShape.Cross,      Special = Special.Nova,      Ability = Ability.RevealAll },
        new() { Name = "Priest",      Description = "A healer who shines once fighting starts.", TileIndex = Glyph.Priest,      Color = new(235, 225, 170), MaxHp = 13, Attack = 3, Defense = 3, Vision = 5, Speed = 4, Mana = 8,  Ranged = false, BasicShape = AttackShape.Jab,    HeavyShape = AttackShape.WideSweep,  Special = Special.HolyLight, Ability = Ability.None },
    };
}
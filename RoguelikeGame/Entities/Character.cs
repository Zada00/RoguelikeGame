using SadRogue.Primitives;

namespace RoguelikeGame;

internal enum Ability { None, RevealNeighbors, RevealAll }

internal enum Special { None, Blink, Whirlwind, Bash, Volley, Nova, HolyLight }

internal enum AttackShape
{
    Jab, Thrust, Bolt, Arrow, Sweep, WideSweep, Cone, Blast, TripleLine, Cross, Slam,
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

    public double FireInterval { get; init; }
    public double ShotSpeed { get; init; }
    public double ShotLife { get; init; }
    public int ShotCount { get; init; }
    public double ShotSpread { get; init; }

    public static readonly List<Character> All = new()
    {
        new() { Name = "Warrior",     Description = "Short-range bruiser. Special: whirlwind of shots.", TileIndex = Glyph.Warrior,     Color = new(210, 90, 50),   MaxHp = 15, Attack = 6, Defense = 3, Vision = 4, Speed = 4, Mana = 8,  Ranged = false, BasicShape = AttackShape.Jab,   HeavyShape = AttackShape.WideSweep,  Special = Special.Whirlwind, Ability = Ability.None,            FireInterval = 0.50, ShotSpeed = 9,  ShotLife = 0.45, ShotCount = 1, ShotSpread = 0 },
        new() { Name = "Guardian",    Description = "Tanky. Special: wide point-blank blast.",          TileIndex = Glyph.Guardian,    Color = new(120, 145, 165), MaxHp = 22, Attack = 4, Defense = 6, Vision = 4, Speed = 3, Mana = 8,  Ranged = false, BasicShape = AttackShape.Jab,   HeavyShape = AttackShape.Slam,       Special = Special.Bash,      Ability = Ability.None,            FireInterval = 0.55, ShotSpeed = 8,  ShotLife = 0.45, ShotCount = 1, ShotSpread = 0 },
        new() { Name = "Rogue",       Description = "Rapid fire. Small, fast shots. No special.",       TileIndex = Glyph.Rogue,       Color = new(80, 200, 120),  MaxHp = 10, Attack = 5, Defense = 2, Vision = 5, Speed = 6, Mana = 0,  Ranged = false, BasicShape = AttackShape.Thrust,HeavyShape = AttackShape.Cone,       Special = Special.None,      Ability = Ability.None,            FireInterval = 0.16, ShotSpeed = 14, ShotLife = 0.6,  ShotCount = 1, ShotSpread = 0 },
        new() { Name = "Mage",        Description = "Fast bolts. Special: blink toward cursor.",        TileIndex = Glyph.Mage,        Color = new(205, 95, 220),  MaxHp = 8,  Attack = 3, Defense = 1, Vision = 5, Speed = 4, Mana = 14, Ranged = true,  BasicShape = AttackShape.Bolt,  HeavyShape = AttackShape.Blast,      Special = Special.Blink,     Ability = Ability.None,            FireInterval = 0.32, ShotSpeed = 12, ShotLife = 0.9,  ShotCount = 1, ShotSpread = 0 },
        new() { Name = "Scout",       Description = "Long-range sniper. Special: arrow fan.",           TileIndex = Glyph.Scout,       Color = new(180, 220, 90),  MaxHp = 11, Attack = 4, Defense = 2, Vision = 7, Speed = 5, Mana = 10, Ranged = true,  BasicShape = AttackShape.Arrow, HeavyShape = AttackShape.TripleLine, Special = Special.Volley,    Ability = Ability.RevealNeighbors, FireInterval = 0.28, ShotSpeed = 16, ShotLife = 1.1,  ShotCount = 1, ShotSpread = 0 },
        new() { Name = "Necromancer", Description = "Spread of three bolts. Special: death nova.",      TileIndex = Glyph.Necromancer, Color = new(150, 80, 190),  MaxHp = 9,  Attack = 3, Defense = 2, Vision = 6, Speed = 4, Mana = 12, Ranged = true,  BasicShape = AttackShape.Bolt,  HeavyShape = AttackShape.Cross,      Special = Special.Nova,      Ability = Ability.RevealAll,       FireInterval = 0.45, ShotSpeed = 10, ShotLife = 0.8,  ShotCount = 3, ShotSpread = 36 },
        new() { Name = "Priest",      Description = "Steady holy bolts. Special: heal self.",           TileIndex = Glyph.Priest,      Color = new(235, 225, 170), MaxHp = 13, Attack = 3, Defense = 3, Vision = 5, Speed = 4, Mana = 10, Ranged = false, BasicShape = AttackShape.Jab,   HeavyShape = AttackShape.WideSweep,  Special = Special.HolyLight, Ability = Ability.None,            FireInterval = 0.40, ShotSpeed = 11, ShotLife = 0.85, ShotCount = 1, ShotSpread = 0 },
    };
}

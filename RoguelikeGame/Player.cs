using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Player
{
    public int X { get; set; }
    public int Y { get; set; }

    // Spilleren peker nå på en valgt Character, og henter utseende derfra.
    public Character Character { get; }
    public int Hp { get; set; }

    public int Glyph => Character.Glyph;
    public Color Color => Character.Color;

    public Player(Character character, int x, int y)
    {
        Character = character;
        Hp = character.MaxHp;
        X = x;
        Y = y;
    }
}
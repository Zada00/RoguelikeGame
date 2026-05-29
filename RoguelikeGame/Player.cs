using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Player
{
    public int X { get; set; }
    public int Y { get; set; }

    // '@' er den klassiske roguelike-spilleren.
    public int Glyph { get; } = '@';
    public Color Color { get; } = Color.Yellow;

    public Player(int x, int y)
    {
        X = x;
        Y = y;
    }
}
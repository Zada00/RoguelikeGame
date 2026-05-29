using SadRogue.Primitives;

namespace RoguelikeGame;

// En enkelt rute på kartet.
// Vi bruker 'struct' i stedet for 'class' fordi det er en liten,
// enkel verditype – det er litt mer effektivt når vi har tusenvis av ruter.
internal struct Tile
{
    public int Glyph;
    public Color Foreground;
    public Color Background;
    public bool IsWalkable;
}
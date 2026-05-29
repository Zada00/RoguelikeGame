namespace RoguelikeGame;

internal class Player
{
    public int X { get; set; }
    public int Y { get; set; }
    public Character Character { get; }
    public int Hp { get; set; }

    public int TileIndex => Character.TileIndex;

    public Player(Character character, int x, int y)
    {
        Character = character;
        Hp = character.MaxHp;
        X = x;
        Y = y;
    }
}
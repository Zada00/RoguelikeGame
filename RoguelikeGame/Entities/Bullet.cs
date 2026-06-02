using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Bullet
{
    public double Fx;
    public double Fy;
    public double Vx;
    public double Vy;
    public int Damage;
    public double Life;
    public Color Color;
    public bool Hostile;   // true = fiendens skudd (treffer spilleren)

    public Bullet(double x, double y, double vx, double vy, int damage, double life, Color color, bool hostile = false)
    {
        Fx = x;
        Fy = y;
        Vx = vx;
        Vy = vy;
        Damage = damage;
        Life = life;
        Color = color;
        Hostile = hostile;
    }
}

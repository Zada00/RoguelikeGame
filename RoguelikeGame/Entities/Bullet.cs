using SadRogue.Primitives;

namespace RoguelikeGame;

internal class Bullet
{
    public double Fx;
    public double Fy;
    public double Vx;   // ruter per sekund
    public double Vy;
    public int Damage;
    public double Life; // sekunder igjen før den forsvinner
    public Color Color;

    public Bullet(double x, double y, double vx, double vy, int damage, double life, Color color)
    {
        Fx = x;
        Fy = y;
        Vx = vx;
        Vy = vy;
        Damage = damage;
        Life = life;
        Color = color;
    }
}

using Microsoft.Xna.Framework;

namespace QuantumOuija.Board;

public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);

    public bool Contains(Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    public Rectangle ToRectangle() =>
        new((int)MathF.Round(X), (int)MathF.Round(Y), (int)MathF.Round(Width), (int)MathF.Round(Height));
}

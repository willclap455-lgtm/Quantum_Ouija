using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuantumOuija.Board;

namespace QuantumOuija.Rendering;

public static class DrawPrimitives
{
    public static void DrawLine(
        this SpriteBatch spriteBatch,
        Texture2D pixel,
        Vector2 start,
        Vector2 end,
        Color color,
        float thickness = 1f)
    {
        var edge = end - start;
        var length = edge.Length();
        if (length <= 0.001f)
        {
            return;
        }

        var angle = MathF.Atan2(edge.Y, edge.X);
        spriteBatch.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0);
    }

    public static void DrawRectangleOutline(
        this SpriteBatch spriteBatch,
        Texture2D pixel,
        RectF rectangle,
        Color color,
        float thickness = 1f)
    {
        var topLeft = new Vector2(rectangle.Left, rectangle.Top);
        var topRight = new Vector2(rectangle.Right, rectangle.Top);
        var bottomRight = new Vector2(rectangle.Right, rectangle.Bottom);
        var bottomLeft = new Vector2(rectangle.Left, rectangle.Bottom);

        spriteBatch.DrawLine(pixel, topLeft, topRight, color, thickness);
        spriteBatch.DrawLine(pixel, topRight, bottomRight, color, thickness);
        spriteBatch.DrawLine(pixel, bottomRight, bottomLeft, color, thickness);
        spriteBatch.DrawLine(pixel, bottomLeft, topLeft, color, thickness);
    }
}

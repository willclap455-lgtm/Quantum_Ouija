using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuantumOuija.Board;

namespace QuantumOuija.Rendering;

public sealed class BoardRenderer
{
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D boardTexture,
        Texture2D pixel,
        BoardModel board,
        BoardViewTransform transform,
        bool showGrid,
        bool showRegions)
    {
        spriteBatch.Draw(boardTexture, transform.DestinationRectangle, Color.White);

        if (showGrid)
        {
            DrawGrid(spriteBatch, pixel, board, transform);
        }

        if (showRegions)
        {
            DrawRegions(spriteBatch, pixel, board, transform);
        }
    }

    private static void DrawGrid(SpriteBatch spriteBatch, Texture2D pixel, BoardModel board, BoardViewTransform transform)
    {
        var color = new Color(80, 210, 180, 42);
        var spacing = board.GridSpacingPixels;

        for (var x = 0; x <= board.Width; x += spacing)
        {
            var start = transform.BoardToScreen(new Vector2(x, 0));
            var end = transform.BoardToScreen(new Vector2(x, board.Height));
            spriteBatch.DrawLine(pixel, start, end, color);
        }

        for (var y = 0; y <= board.Height; y += spacing)
        {
            var start = transform.BoardToScreen(new Vector2(0, y));
            var end = transform.BoardToScreen(new Vector2(board.Width, y));
            spriteBatch.DrawLine(pixel, start, end, color);
        }
    }

    private static void DrawRegions(SpriteBatch spriteBatch, Texture2D pixel, BoardModel board, BoardViewTransform transform)
    {
        foreach (var region in board.Regions)
        {
            var color = region.Token.IsTerminator
                ? new Color(220, 60, 80, 120)
                : new Color(170, 120, 255, 105);

            for (var i = 0; i < region.Vertices.Count; i++)
            {
                var a = transform.BoardToScreen(region.Vertices[i]);
                var b = transform.BoardToScreen(region.Vertices[(i + 1) % region.Vertices.Count]);
                spriteBatch.DrawLine(pixel, a, b, color, 2f);
            }
        }
    }
}

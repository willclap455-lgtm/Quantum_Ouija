using Microsoft.Xna.Framework;
using QuantumOuija.Board;

namespace QuantumOuija.Rendering;

public sealed class BoardViewTransform
{
    public BoardViewTransform(RectF destination, int boardWidth, int boardHeight)
    {
        Destination = destination;
        BoardWidth = boardWidth;
        BoardHeight = boardHeight;
        Scale = MathF.Min(destination.Width / boardWidth, destination.Height / boardHeight);
        Offset = new Vector2(destination.X, destination.Y);
    }

    public RectF Destination { get; }
    public int BoardWidth { get; }
    public int BoardHeight { get; }
    public float Scale { get; }
    public Vector2 Offset { get; }

    public static BoardViewTransform Fit(int boardWidth, int boardHeight, int windowWidth, int windowHeight, int bottomUiHeight)
    {
        var available = new RectF(24, 18, windowWidth - 48, windowHeight - bottomUiHeight - 30);
        var scale = MathF.Min(available.Width / boardWidth, available.Height / boardHeight);
        var width = boardWidth * scale;
        var height = boardHeight * scale;
        var x = available.X + (available.Width - width) * 0.5f;
        var y = available.Y + (available.Height - height) * 0.5f;
        return new BoardViewTransform(new RectF(x, y, width, height), boardWidth, boardHeight);
    }

    public Vector2 BoardToScreen(Vector2 boardPoint) => Offset + boardPoint * Scale;

    public Vector2 ScreenToBoard(Vector2 screenPoint) => (screenPoint - Offset) / Scale;

    public Rectangle DestinationRectangle => Destination.ToRectangle();
}

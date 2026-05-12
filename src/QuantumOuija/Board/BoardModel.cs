using Microsoft.Xna.Framework;
using QuantumOuija.Simulation;

namespace QuantumOuija.Board;

public sealed class BoardModel
{
    private readonly BoardRegion[] _regionsByPriority;

    public BoardModel(int width, int height, int gridSpacingPixels, IEnumerable<BoardRegion> regions, BoardToken defaultToken)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (gridSpacingPixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gridSpacingPixels));
        }

        Width = width;
        Height = height;
        GridSpacingPixels = gridSpacingPixels;
        DefaultToken = defaultToken;
        Regions = regions.ToArray();
        _regionsByPriority = Regions.OrderByDescending(region => region.Priority).ToArray();
    }

    public int Width { get; }
    public int Height { get; }
    public int GridSpacingPixels { get; }
    public int MaxNodeX => Width / GridSpacingPixels;
    public int MaxNodeY => Height / GridSpacingPixels;
    public BoardToken DefaultToken { get; }
    public IReadOnlyList<BoardRegion> Regions { get; }

    public Vector2 NodeToWorld(GridNode node)
    {
        var clamped = ClampNode(node);
        return new Vector2(
            MathHelper.Clamp(clamped.X * GridSpacingPixels, 0, Width),
            MathHelper.Clamp(clamped.Y * GridSpacingPixels, 0, Height));
    }

    public GridNode WorldToNode(Vector2 point) =>
        ClampNode(new GridNode(
            (int)MathF.Round(point.X / GridSpacingPixels),
            (int)MathF.Round(point.Y / GridSpacingPixels)));

    public GridNode ClampNode(GridNode node) =>
        new(
            Math.Clamp(node.X, 0, MaxNodeX),
            Math.Clamp(node.Y, 0, MaxNodeY));

    public GridNode WrapNode(GridNode node)
    {
        var x = Wrap(node.X, MaxNodeX + 1);
        var y = Wrap(node.Y, MaxNodeY + 1);
        return new GridNode(x, y);
    }

    public BoardToken ResolveToken(Vector2 boardPoint)
    {
        foreach (var region in _regionsByPriority)
        {
            if (region.Contains(boardPoint))
            {
                return region.Token;
            }
        }

        return DefaultToken;
    }

    private static int Wrap(int value, int exclusiveMax)
    {
        var wrapped = value % exclusiveMax;
        return wrapped < 0 ? wrapped + exclusiveMax : wrapped;
    }
}

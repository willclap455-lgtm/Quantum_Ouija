using Microsoft.Xna.Framework;

namespace QuantumOuija.Board;

public sealed class BoardRegion
{
    private readonly Vector2[] _vertices;

    public BoardRegion(string id, BoardToken token, IEnumerable<Vector2> vertices, int priority)
    {
        Id = id;
        Token = token;
        Priority = priority;
        _vertices = vertices.ToArray();

        if (_vertices.Length < 3)
        {
            throw new ArgumentException("A board region must have at least three vertices.", nameof(vertices));
        }

        Bounds = CalculateBounds(_vertices);
    }

    public string Id { get; }
    public BoardToken Token { get; }
    public int Priority { get; }
    public IReadOnlyList<Vector2> Vertices => _vertices;
    public RectF Bounds { get; }

    public static BoardRegion Rectangle(string id, BoardToken token, RectF bounds, int priority) =>
        new(
            id,
            token,
            new[]
            {
                new Vector2(bounds.Left, bounds.Top),
                new Vector2(bounds.Right, bounds.Top),
                new Vector2(bounds.Right, bounds.Bottom),
                new Vector2(bounds.Left, bounds.Bottom)
            },
            priority);

    public bool Contains(Vector2 point)
    {
        if (!Bounds.Contains(point))
        {
            return false;
        }

        return ContainsPointInPolygon(point);
    }

    private bool ContainsPointInPolygon(Vector2 point)
    {
        var inside = false;
        var j = _vertices.Length - 1;

        for (var i = 0; i < _vertices.Length; i++)
        {
            var vi = _vertices[i];
            var vj = _vertices[j];
            var crossesY = vi.Y > point.Y != vj.Y > point.Y;

            if (crossesY)
            {
                var xAtY = (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X;
                if (point.X < xAtY)
                {
                    inside = !inside;
                }
            }

            j = i;
        }

        return inside;
    }

    private static RectF CalculateBounds(IReadOnlyList<Vector2> vertices)
    {
        var minX = vertices[0].X;
        var maxX = vertices[0].X;
        var minY = vertices[0].Y;
        var maxY = vertices[0].Y;

        for (var i = 1; i < vertices.Count; i++)
        {
            minX = MathF.Min(minX, vertices[i].X);
            maxX = MathF.Max(maxX, vertices[i].X);
            minY = MathF.Min(minY, vertices[i].Y);
            maxY = MathF.Max(maxY, vertices[i].Y);
        }

        return new RectF(minX, minY, maxX - minX, maxY - minY);
    }
}

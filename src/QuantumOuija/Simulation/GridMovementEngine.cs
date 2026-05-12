using QuantumOuija.Board;

namespace QuantumOuija.Simulation;

public sealed class GridMovementEngine
{
    private readonly MovementOptions _options;

    public GridMovementEngine(MovementOptions options)
    {
        _options = options;
    }

    public IReadOnlyList<GridNode> GenerateNodes(
        BoardModel board,
        GridNode start,
        IReadOnlyList<int> directions,
        IReadOnlyList<int> distances)
    {
        if (directions.Count != distances.Count)
        {
            throw new ArgumentException("Direction and distance arrays must have the same length.", nameof(distances));
        }

        var current = _options.WrapAtBoardEdges ? board.WrapNode(start) : board.ClampNode(start);
        var nodes = new List<GridNode> { current };

        for (var i = 0; i < directions.Count; i++)
        {
            var delta = ToDelta(directions[i]);
            var distance = distances[i];

            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distances), "Distances cannot be negative.");
            }

            for (var step = 0; step < distance; step++)
            {
                var candidate = new GridNode(current.X + delta.X, current.Y + delta.Y);
                current = _options.WrapAtBoardEdges ? board.WrapNode(candidate) : board.ClampNode(candidate);
                nodes.Add(current);
            }
        }

        return nodes;
    }

    private static GridNode ToDelta(int direction) =>
        (MovementDirection)direction switch
        {
            MovementDirection.North => new GridNode(0, -1),
            MovementDirection.East => new GridNode(1, 0),
            MovementDirection.South => new GridNode(0, 1),
            MovementDirection.West => new GridNode(-1, 0),
            MovementDirection.Northwest => new GridNode(-1, -1),
            MovementDirection.Northeast => new GridNode(1, -1),
            MovementDirection.Southwest => new GridNode(-1, 1),
            MovementDirection.Southeast => new GridNode(1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be in the range 1-8.")
        };
}

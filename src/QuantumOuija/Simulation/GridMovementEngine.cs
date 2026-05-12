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
        var stuckMovementCount = 0;
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
                current = MoveOneNode(board, current, ref delta, ref stuckMovementCount);
                nodes.Add(current);
            }
        }

        return nodes;
    }

    private GridNode MoveOneNode(
        BoardModel board,
        GridNode current,
        ref GridNode delta,
        ref int stuckMovementCount)
    {
        var next = new GridNode(current.X + delta.X, current.Y + delta.Y);

        if (_options.WrapAtBoardEdges)
        {
            return board.WrapNode(next);
        }

        if (_options.ReflectAtBoardEdges)
        {
            next = ReflectOffEdges(board, current, ref delta);
        }

        var resolved = board.ClampNode(next);
        if (resolved == current)
        {
            stuckMovementCount++;
            if (stuckMovementCount >= _options.StuckMovementThreshold)
            {
                delta = new GridNode(-delta.X, -delta.Y);
                resolved = board.ClampNode(new GridNode(current.X + delta.X, current.Y + delta.Y));
                stuckMovementCount = 0;
            }
        }
        else
        {
            stuckMovementCount = 0;
        }

        return resolved;
    }

    private static GridNode ReflectOffEdges(BoardModel board, GridNode current, ref GridNode delta)
    {
        var nextX = current.X + delta.X;
        var nextY = current.Y + delta.Y;

        if (nextX < 0 || nextX > board.MaxNodeX)
        {
            delta = delta with { X = -delta.X };
            nextX = current.X + delta.X;
        }

        if (nextY < 0 || nextY > board.MaxNodeY)
        {
            delta = delta with { Y = -delta.Y };
            nextY = current.Y + delta.Y;
        }

        return new GridNode(nextX, nextY);
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

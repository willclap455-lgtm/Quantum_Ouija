using QuantumOuija.Board;
using QuantumOuija.Rng;

namespace QuantumOuija.Simulation;

public sealed class QuantumPathGenerator
{
    public const int MinimumPathCount = 30;
    public const int MaximumPathCount = 60;

    private const int MovementSegmentsPerPath = 30;

    private readonly IQuantumRandomProvider _randomProvider;
    private readonly GridMovementEngine _movementEngine;

    public QuantumPathGenerator(IQuantumRandomProvider randomProvider, GridMovementEngine movementEngine)
    {
        _randomProvider = randomProvider;
        _movementEngine = movementEngine;
    }

    public Task<int> GeneratePathCountAsync(CancellationToken cancellationToken) =>
        _randomProvider.NextIntAsync(MinimumPathCount, MaximumPathCount, cancellationToken);

    public async Task<GeneratedPath> GeneratePathAsync(
        int pathIndex,
        BoardModel board,
        GridNode startNode,
        CancellationToken cancellationToken)
    {
        var directions = await _randomProvider.NextIntsAsync(MovementSegmentsPerPath, 1, 8, cancellationToken).ConfigureAwait(false);
        var distances = await _randomProvider.NextIntsAsync(MovementSegmentsPerPath, 3, 25, cancellationToken).ConfigureAwait(false);
        var nodes = _movementEngine.GenerateNodes(board, startNode, directions, distances);
        var endNode = nodes[^1];
        var finalToken = board.ResolveToken(board.NodeToWorld(endNode));

        return new GeneratedPath(pathIndex, startNode, endNode, nodes, directions, distances, finalToken);
    }
}

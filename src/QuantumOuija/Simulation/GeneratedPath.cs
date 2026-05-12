using QuantumOuija.Board;

namespace QuantumOuija.Simulation;

public sealed record GeneratedPath(
    int Index,
    GridNode StartNode,
    GridNode EndNode,
    IReadOnlyList<GridNode> Nodes,
    IReadOnlyList<int> Directions,
    IReadOnlyList<int> Distances,
    BoardToken FinalToken);

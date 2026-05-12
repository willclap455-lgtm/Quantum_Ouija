namespace QuantumOuija.Simulation;

public sealed record MovementOptions
{
    public bool WrapAtBoardEdges { get; init; }
}

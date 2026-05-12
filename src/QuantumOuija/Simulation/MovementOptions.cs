namespace QuantumOuija.Simulation;

public sealed record MovementOptions
{
    public bool WrapAtBoardEdges { get; init; }
    public bool ReflectAtBoardEdges { get; init; } = true;
    public int StuckMovementThreshold { get; init; } = 10;
}

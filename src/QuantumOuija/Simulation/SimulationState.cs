namespace QuantumOuija.Simulation;

public enum SimulationState
{
    Idle,
    GeneratingPathCount,
    GeneratingPath,
    AnimatingPath,
    PausingBetweenPaths,
    Complete,
    Cancelled,
    Error
}

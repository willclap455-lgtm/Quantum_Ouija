namespace QuantumOuija.Rng;

public interface IQuantumRandomProvider
{
    string Name { get; }
    bool IsDeterministic { get; }

    Task<int> NextIntAsync(int minInclusive, int maxInclusive, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> NextIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        CancellationToken cancellationToken);
}

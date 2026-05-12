using System.Security.Cryptography;

namespace QuantumOuija.Rng;

public sealed class FallbackRandomProvider : IQuantumRandomProvider
{
    private readonly object _sync = new();
    private readonly Random? _seededRandom;

    public FallbackRandomProvider(int? seed = null)
    {
        Seed = seed;
        _seededRandom = seed is null ? null : new Random(seed.Value);
    }

    public string Name => Seed is null ? "Local cryptographic fallback" : $"Seeded replay fallback ({Seed})";
    public bool IsDeterministic => Seed is not null;
    public int? Seed { get; }

    public Task<int> NextIntAsync(int minInclusive, int maxInclusive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRange(minInclusive, maxInclusive);
        return Task.FromResult(NextInt(minInclusive, maxInclusive));
    }

    public Task<IReadOnlyList<int>> NextIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        CancellationToken cancellationToken)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        ValidateRange(minInclusive, maxInclusive);

        var values = new int[count];
        for (var i = 0; i < values.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            values[i] = NextInt(minInclusive, maxInclusive);
        }

        return Task.FromResult<IReadOnlyList<int>>(values);
    }

    private int NextInt(int minInclusive, int maxInclusive)
    {
        var exclusiveMax = checked(maxInclusive + 1);

        if (_seededRandom is null)
        {
            return RandomNumberGenerator.GetInt32(minInclusive, exclusiveMax);
        }

        lock (_sync)
        {
            return _seededRandom.Next(minInclusive, exclusiveMax);
        }
    }

    private static void ValidateRange(int minInclusive, int maxInclusive)
    {
        if (minInclusive > maxInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(minInclusive), "Minimum cannot be greater than maximum.");
        }

        if (maxInclusive == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInclusive), "The inclusive maximum must be less than int.MaxValue.");
        }
    }
}

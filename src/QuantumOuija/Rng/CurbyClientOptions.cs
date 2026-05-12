namespace QuantumOuija.Rng;

public sealed record CurbyClientOptions
{
    public Uri Endpoint { get; init; } = new("https://random.colorado.edu/api/curbyq/round/latest/data");
    public int MaxRetryAttempts { get; init; } = 3;
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(300);
    public TimeSpan MinimumRequestInterval { get; init; } = TimeSpan.FromMilliseconds(250);
}

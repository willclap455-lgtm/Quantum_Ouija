namespace QuantumOuija.Rng;

public sealed record CurbyClientOptions
{
    public Uri BaseUri { get; init; } = new("https://random.colorado.edu/api");
    public string ChainId { get; init; } = "bafyriqci6f3st2mg7gq733ho4zvvth32zpy2mtiylixwmhoz6d627eo3jfpmbxepe54u2zdvymonq5sp3armtm4rodxsynsirr5g3xsbd3q4s";
    public int MaxRetryAttempts { get; init; } = 3;
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(300);
    public TimeSpan MinimumRequestInterval { get; init; } = TimeSpan.FromMilliseconds(250);
    public int MinimumSeedPoolSize { get; init; } = 32;
    public int MaximumSeedPoolSize { get; init; } = 1024;
}

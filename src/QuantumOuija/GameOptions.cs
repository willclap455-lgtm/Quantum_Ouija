namespace QuantumOuija;

public sealed record GameOptions
{
    public int WindowWidth { get; init; } = 1280;
    public int WindowHeight { get; init; } = 900;
    public int GridSpacingPixels { get; init; } = 8;
    public float PlanchetteSpeedPixelsPerSecond { get; init; } = 1450f;
    public float PauseBetweenPathsSeconds { get; init; } = 0.18f;
    public bool WrapMovement { get; init; }
    public bool WobblePlanchette { get; init; } = true;
    public bool ShowDebugGrid { get; set; }
    public bool ShowDebugRegions { get; set; }
    public bool ShowDebugNodes { get; set; }
    public string CurbyBaseUri { get; init; } = "https://random.colorado.edu/api";
    public string CurbyChainId { get; init; } = "bafyriqci6f3st2mg7gq733ho4zvvth32zpy2mtiylixwmhoz6d627eo3jfpmbxepe54u2zdvymonq5sp3armtm4rodxsynsirr5g3xsbd3q4s";
}

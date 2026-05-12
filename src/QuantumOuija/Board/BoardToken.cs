namespace QuantumOuija.Board;

public readonly record struct BoardToken(RegionType Type, string Value)
{
    public static readonly BoardToken Empty = new(RegionType.Empty, string.Empty);
    public static readonly BoardToken Space = new(RegionType.Space, " ");

    public bool IsTerminator => Type is RegionType.Yes or RegionType.No or RegionType.Goodbye;

    public bool IsRenderableText => Type is RegionType.Letter or RegionType.Number;

    public override string ToString() => Type == RegionType.Space ? "SPACE" : Value;
}

namespace QuantumOuija;

internal static class Program
{
    private static void Main()
    {
        using var game = new QuantumOuijaGame(new GameOptions());
        game.Run();
    }
}

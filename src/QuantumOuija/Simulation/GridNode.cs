using Microsoft.Xna.Framework;

namespace QuantumOuija.Simulation;

public readonly record struct GridNode(int X, int Y)
{
    public Vector2 ToWorld(int gridSpacingPixels) => new(X * gridSpacingPixels, Y * gridSpacingPixels);
}

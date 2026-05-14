using Microsoft.Xna.Framework;

namespace QuantumOuija.Board;

public static class MiltonBradleyBoardLayout
{
    public static BoardModel Create(int boardWidth, int boardHeight, int gridSpacingPixels)
    {
        var regions = new List<BoardRegion>();

        AddRectangle(regions, "yes", new BoardToken(RegionType.Yes, "YES"), 0.000f, 0.000f, 0.328f, 0.230f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "no", new BoardToken(RegionType.No, "NO"), 0.672f, 0.000f, 0.328f, 0.230f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "goodbye", new BoardToken(RegionType.Goodbye, "GOODBYE"), 0.285f, 0.825f, 0.430f, 0.118f, boardWidth, boardHeight, 100);

        AddTokenArc(regions, "letters-row-1", "ABCDEFGHIJKLM", 0.122f, 0.850f, 0.310f, 0.155f, 0.058f, 0.145f, boardWidth, boardHeight, RegionType.Letter, 50);
        AddTokenArc(regions, "letters-row-2", "NOPQRSTUVWXYZ", 0.108f, 0.884f, 0.456f, 0.190f, 0.062f, 0.145f, boardWidth, boardHeight, RegionType.Letter, 50);
        AddTokenRow(regions, "numbers", "1234567890", 0.229f, 0.767f, 0.735f, 0.053f, 0.110f, boardWidth, boardHeight, RegionType.Number, 50);

        return new BoardModel(boardWidth, boardHeight, gridSpacingPixels, regions, BoardToken.Space);
    }

    private static void AddTokenRow(
        ICollection<BoardRegion> regions,
        string rowId,
        string values,
        float startX,
        float endX,
        float centerY,
        float tokenWidth,
        float tokenHeight,
        int boardWidth,
        int boardHeight,
        RegionType type,
        int priority)
    {
        var count = values.Length;
        for (var index = 0; index < count; index++)
        {
            var t = count == 1 ? 0.5f : index / (float)(count - 1);
            var centerX = MathHelper.Lerp(startX, endX, t);
            var value = values[index].ToString();
            AddRectangle(
                regions,
                $"{rowId}-{value}",
                new BoardToken(type, value),
                centerX - tokenWidth * 0.5f,
                centerY - tokenHeight * 0.5f,
                tokenWidth,
                tokenHeight,
                boardWidth,
                boardHeight,
                priority);
        }
    }

    private static void AddTokenArc(
        ICollection<BoardRegion> regions,
        string rowId,
        string values,
        float startX,
        float endX,
        float apexCenterY,
        float edgeDrop,
        float tokenWidth,
        float tokenHeight,
        int boardWidth,
        int boardHeight,
        RegionType type,
        int priority)
    {
        var count = values.Length;
        for (var index = 0; index < count; index++)
        {
            var t = count == 1 ? 0.5f : index / (float)(count - 1);
            var centerX = MathHelper.Lerp(startX, endX, t);
            var curveOffset = MathF.Pow(t * 2f - 1f, 2f) * edgeDrop;
            var centerY = apexCenterY + curveOffset;
            var value = values[index].ToString();
            AddRectangle(
                regions,
                $"{rowId}-{value}",
                new BoardToken(type, value),
                centerX - tokenWidth * 0.5f,
                centerY - tokenHeight * 0.5f,
                tokenWidth,
                tokenHeight,
                boardWidth,
                boardHeight,
                priority);
        }
    }

    private static void AddRectangle(
        ICollection<BoardRegion> regions,
        string id,
        BoardToken token,
        float x,
        float y,
        float width,
        float height,
        int boardWidth,
        int boardHeight,
        int priority)
    {
        regions.Add(BoardRegion.Rectangle(
            id,
            token,
            new RectF(x * boardWidth, y * boardHeight, width * boardWidth, height * boardHeight),
            priority));
    }
}

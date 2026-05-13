using Microsoft.Xna.Framework;

namespace QuantumOuija.Board;

public static class MiltonBradleyBoardLayout
{
    public static BoardModel Create(int boardWidth, int boardHeight, int gridSpacingPixels)
    {
        var regions = new List<BoardRegion>();

        AddRectangle(regions, "yes", new BoardToken(RegionType.Yes, "YES"), 0.075f, 0.125f, 0.215f, 0.105f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "no", new BoardToken(RegionType.No, "NO"), 0.710f, 0.125f, 0.215f, 0.105f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "goodbye", new BoardToken(RegionType.Goodbye, "GOODBYE"), 0.285f, 0.825f, 0.430f, 0.105f, boardWidth, boardHeight, 100);

        AddTokenArc(regions, "letters-row-1", "ABCDEFGHIJKLM", 0.172f, 0.828f, 0.335f, 0.120f, 0.047f, 0.080f, boardWidth, boardHeight, RegionType.Letter, 50);
        AddTokenArc(regions, "letters-row-2", "NOPQRSTUVWXYZ", 0.172f, 0.828f, 0.465f, 0.105f, 0.047f, 0.080f, boardWidth, boardHeight, RegionType.Letter, 50);
        AddTokenRow(regions, "numbers", "1234567890", 0.260f, 0.740f, 0.699f, 0.040f, 0.080f, boardWidth, boardHeight, RegionType.Number, 50);

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

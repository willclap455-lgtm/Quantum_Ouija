using Microsoft.Xna.Framework;

namespace QuantumOuija.Board;

public static class MiltonBradleyBoardLayout
{
    private static readonly IReadOnlyDictionary<char, TokenRegionAdjustment> LetterAdjustments =
        new Dictionary<char, TokenRegionAdjustment>
        {
            ['A'] = new(0.000f, 1.45f),
            ['M'] = new(0.000f, 1.65f),
            ['N'] = new(0.014f, 1.00f),
            ['O'] = new(0.012f, 1.00f),
            ['Z'] = new(0.000f, 1.55f)
        };

    private static readonly IReadOnlyDictionary<char, TokenRegionAdjustment> NumberAdjustments =
        new Dictionary<char, TokenRegionAdjustment>
        {
            ['2'] = new(0.000f, 1.40f),
            ['3'] = new(-0.020f, 1.45f),
            ['4'] = new(-0.018f, 1.45f),
            ['5'] = new(-0.012f, 1.45f),
            ['6'] = new(-0.006f, 1.45f),
            ['7'] = new(0.002f, 1.45f),
            ['8'] = new(0.008f, 1.45f),
            ['9'] = new(0.014f, 1.45f)
        };

    public static BoardModel Create(int boardWidth, int boardHeight, int gridSpacingPixels)
    {
        var regions = new List<BoardRegion>();

        AddRectangle(regions, "yes", new BoardToken(RegionType.Yes, "YES"), 0.075f, 0.125f, 0.215f, 0.105f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "no", new BoardToken(RegionType.No, "NO"), 0.710f, 0.125f, 0.215f, 0.105f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "goodbye", new BoardToken(RegionType.Goodbye, "GOODBYE"), 0.285f, 0.825f, 0.430f, 0.105f, boardWidth, boardHeight, 100);

        AddTokenArc(regions, "letters-row-1", "ABCDEFGHIJKLM", 0.172f, 0.828f, 0.335f, 0.120f, 0.047f, 0.080f, boardWidth, boardHeight, RegionType.Letter, 50, LetterAdjustments);
        AddTokenArc(regions, "letters-row-2", "NOPQRSTUVWXYZ", 0.172f, 0.828f, 0.465f, 0.105f, 0.047f, 0.080f, boardWidth, boardHeight, RegionType.Letter, 50, LetterAdjustments);
        AddTokenRow(regions, "numbers", "1234567890", 0.260f, 0.740f, 0.699f, 0.040f, 0.080f, boardWidth, boardHeight, RegionType.Number, 50, NumberAdjustments);

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
        int priority,
        IReadOnlyDictionary<char, TokenRegionAdjustment>? adjustments = null)
    {
        var count = values.Length;
        for (var index = 0; index < count; index++)
        {
            var t = count == 1 ? 0.5f : index / (float)(count - 1);
            var valueChar = values[index];
            var adjustment = GetAdjustment(adjustments, valueChar);
            var centerX = MathHelper.Lerp(startX, endX, t) + adjustment.CenterXOffset;
            var tokenWidthAdjusted = tokenWidth * adjustment.WidthMultiplier;
            var value = valueChar.ToString();
            AddRectangle(
                regions,
                $"{rowId}-{value}",
                new BoardToken(type, value),
                centerX - tokenWidthAdjusted * 0.5f,
                centerY - tokenHeight * 0.5f,
                tokenWidthAdjusted,
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
        int priority,
        IReadOnlyDictionary<char, TokenRegionAdjustment>? adjustments = null)
    {
        var count = values.Length;
        for (var index = 0; index < count; index++)
        {
            var t = count == 1 ? 0.5f : index / (float)(count - 1);
            var valueChar = values[index];
            var adjustment = GetAdjustment(adjustments, valueChar);
            var centerX = MathHelper.Lerp(startX, endX, t) + adjustment.CenterXOffset;
            var curveOffset = MathF.Pow(t * 2f - 1f, 2f) * edgeDrop;
            var centerY = apexCenterY + curveOffset;
            var tokenWidthAdjusted = tokenWidth * adjustment.WidthMultiplier;
            var value = valueChar.ToString();
            AddRectangle(
                regions,
                $"{rowId}-{value}",
                new BoardToken(type, value),
                centerX - tokenWidthAdjusted * 0.5f,
                centerY - tokenHeight * 0.5f,
                tokenWidthAdjusted,
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

    private static TokenRegionAdjustment GetAdjustment(
        IReadOnlyDictionary<char, TokenRegionAdjustment>? adjustments,
        char value) =>
        adjustments is not null && adjustments.TryGetValue(value, out var adjustment)
            ? adjustment
            : TokenRegionAdjustment.Default;

    private readonly record struct TokenRegionAdjustment(float CenterXOffset, float WidthMultiplier)
    {
        public static readonly TokenRegionAdjustment Default = new(0f, 1f);
    }
}

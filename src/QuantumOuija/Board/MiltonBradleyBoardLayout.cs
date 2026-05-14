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

        AddRectangle(regions, "yes", new BoardToken(RegionType.Yes, "YES"), 0.000f, 0.000f, 0.328f, 0.230f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "no", new BoardToken(RegionType.No, "NO"), 0.672f, 0.000f, 0.328f, 0.230f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "goodbye", new BoardToken(RegionType.Goodbye, "GOODBYE"), 0.285f, 0.825f, 0.430f, 0.118f, boardWidth, boardHeight, 100);

<<<<<<< HEAD
        AddTokenArc(regions, "letters-row-1", "ABCDEFGHIJKLM", 0.122f, 0.865f, 0.310f, 0.155f, 0.058f, 0.145f, boardWidth, boardHeight, RegionType.Letter, 50);
        AddTokenArc(regions, "letters-row-2", "NOPQRSTUVWXYZ", 0.108f, 0.884f, 0.456f, 0.190f, 0.062f, 0.145f, boardWidth, boardHeight, RegionType.Letter, 50);
        AddNumberRow(regions, "numbers", "1234567890", 0.229f, 0.767f, 0.735f, 0.053f, 0.040f, 0.110f, 25f, boardWidth, boardHeight, 50);
=======
        AddTokenArc(regions, "letters-row-1", "ABCDEFGHIJKLM", 0.172f, 0.828f, 0.335f, 0.120f, 0.047f, 0.080f, boardWidth, boardHeight, RegionType.Letter, 50, LetterAdjustments);
        AddTokenArc(regions, "letters-row-2", "NOPQRSTUVWXYZ", 0.172f, 0.828f, 0.465f, 0.105f, 0.047f, 0.080f, boardWidth, boardHeight, RegionType.Letter, 50, LetterAdjustments);
        AddTokenRow(regions, "numbers", "1234567890", 0.260f, 0.740f, 0.699f, 0.040f, 0.080f, boardWidth, boardHeight, RegionType.Number, 50, NumberAdjustments);
>>>>>>> 28b5ac108330702be0dc99c7b3e21dda2e085a99

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

    private static void AddNumberRow(
        ICollection<BoardRegion> regions,
        string rowId,
        string values,
        float startX,
        float endX,
        float centerY,
        float endpointTokenWidth,
        float interiorTokenWidth,
        float tokenHeight,
        float interiorLeftOffsetPixels,
        int boardWidth,
        int boardHeight,
        int priority)
    {
        var count = values.Length;
        var interiorLeftOffset = interiorLeftOffsetPixels / boardWidth;
        for (var index = 0; index < count; index++)
        {
            var t = count == 1 ? 0.5f : index / (float)(count - 1);
            var centerX = MathHelper.Lerp(startX, endX, t);
            if (index > 0 && index < count - 1)
            {
                centerX -= interiorLeftOffset;
            }

            var tokenWidth = index is 0 || index == count - 1 ? endpointTokenWidth : interiorTokenWidth;
            var value = values[index].ToString();
            AddRectangle(
                regions,
                $"{rowId}-{value}",
                new BoardToken(RegionType.Number, value),
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

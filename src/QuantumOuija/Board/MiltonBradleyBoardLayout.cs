using Microsoft.Xna.Framework;

namespace QuantumOuija.Board;

public static class MiltonBradleyBoardLayout
{
    private static readonly IReadOnlyDictionary<char, TokenRegionAdjustment> LetterAdjustments =
        new Dictionary<char, TokenRegionAdjustment>
        {
            ['A'] = new(-0.015f, 1.25f),
	    ['B'] = new(-0.005f, 1.20f), 
	    ['H'] = new(0.005f, 1.30f),
	    ['I'] = new(0.005f, 0.75f),
	    ['J'] = new(-0.005f, 1.15f), 
	    ['K'] = new(0.000f, 1.15f),
            ['M'] = new(0.010f, 1.40f),
            // 
	    ['N'] = new(0.000f, 1.020f),
            ['O'] = new(0.000f, 1.005f),
	    ['P'] = new(-0.005f, 0.850f),
	    ['Q'] = new(-0.010f, 1.080f),
	    ['R'] = new(-0.010f, 1.040f),
	    ['S'] = new(-0.010f, 1.000f),
	    ['T'] = new(-0.010f, 1.000f),
	    ['U'] = new(-0.010f, 1.020f),
	    ['V'] = new(-0.005f, 1.000f),
	    ['W'] = new(0.005f, 1.150f),
	    ['X'] = new(0.005f, 1.030f),
	    ['Y'] = new(0.005f, 1.000f), 

            ['Z'] = new(0.000f, 1.025f)
        };

    private static readonly IReadOnlyDictionary<char, TokenRegionAdjustment> NumberAdjustments =
        new Dictionary<char, TokenRegionAdjustment>
        {
		['0'] = new(0.005f, 1.000f)
        };

    public static BoardModel Create(int boardWidth, int boardHeight, int gridSpacingPixels)
    {
        var regions = new List<BoardRegion>();

        AddRectangle(regions, "yes", new BoardToken(RegionType.Yes, "YES"), 0.000f, 0.000f, 0.328f, 0.230f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "no", new BoardToken(RegionType.No, "NO"), 0.672f, 0.000f, 0.328f, 0.230f, boardWidth, boardHeight, 100);
        AddRectangle(regions, "goodbye", new BoardToken(RegionType.Goodbye, "GOODBYE"), 0.285f, 0.825f, 0.430f, 0.118f, boardWidth, boardHeight, 100);

        AddTokenArc(regions, "letters-row-1", "ABCDEFGHIJKLM", 0.125f, 0.859f, 0.310f, 0.145f, 0.060f, 0.120f, boardWidth, boardHeight, RegionType.Letter, 50, LetterAdjustments);
        AddTokenArc(regions, "letters-row-2", "NOPQRSTUVWXYZ", 0.115f, 0.880f, 0.460f, 0.195f, 0.065f, 0.130f, boardWidth, boardHeight, RegionType.Letter, 50, LetterAdjustments);
        AddTokenRow(regions, "numbers", "1234567890", 0.220f, 0.760f, 0.740f, 0.062f, 0.120f, boardWidth, boardHeight, RegionType.Number, 50, NumberAdjustments);

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

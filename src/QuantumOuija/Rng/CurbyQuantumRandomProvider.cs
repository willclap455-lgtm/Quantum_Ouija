using System.Buffers.Binary;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuantumOuija.Rng;

public sealed class CurbyQuantumRandomProvider : IQuantumRandomProvider, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Queue<byte> _entropy = new();
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly CurbyClientOptions _options;
    private readonly IQuantumRandomProvider? _fallback;
    private DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;
    private bool _disposed;

    public CurbyQuantumRandomProvider(
        CurbyClientOptions options,
        IQuantumRandomProvider? fallback = null,
        HttpClient? httpClient = null)
    {
        _options = options;
        _fallback = fallback;
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string Name => "CU Boulder CURBy quantum beacon";
    public bool IsDeterministic => false;

    public async Task<int> NextIntAsync(int minInclusive, int maxInclusive, CancellationToken cancellationToken)
    {
        var values = await NextIntsAsync(1, minInclusive, maxInclusive, cancellationToken).ConfigureAwait(false);
        return values[0];
    }

    public async Task<IReadOnlyList<int>> NextIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        CancellationToken cancellationToken)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (minInclusive > maxInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(minInclusive), "Minimum cannot be greater than maximum.");
        }

        var values = new int[count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = await NextIntFromEntropyAsync(minInclusive, maxInclusive, cancellationToken).ConfigureAwait(false);
        }

        return values;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _requestGate.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<int> NextIntFromEntropyAsync(int minInclusive, int maxInclusive, CancellationToken cancellationToken)
    {
        var range = checked((uint)(maxInclusive - minInclusive + 1));
        var bucketSize = ((ulong)uint.MaxValue + 1UL) / range;
        var unbiasedLimit = bucketSize * range;

        while (true)
        {
            var bytes = await TakeEntropyBytesAsync(sizeof(uint), cancellationToken).ConfigureAwait(false);
            var raw = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            if (raw < unbiasedLimit)
            {
                return minInclusive + (int)(raw % range);
            }
        }
    }

    private async Task<byte[]> TakeEntropyBytesAsync(int count, CancellationToken cancellationToken)
    {
        while (_entropy.Count < count)
        {
            var fetched = await FetchEntropyWithFallbackAsync(cancellationToken).ConfigureAwait(false);
            foreach (var b in fetched)
            {
                _entropy.Enqueue(b);
            }
        }

        var bytes = new byte[count];
        for (var i = 0; i < count; i++)
        {
            bytes[i] = _entropy.Dequeue();
        }

        return bytes;
    }

    private async Task<byte[]> FetchEntropyWithFallbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await FetchEntropyAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (_fallback is not null && !cancellationToken.IsCancellationRequested)
        {
            var fallbackBytes = await _fallback.NextIntsAsync(512, 0, 255, cancellationToken).ConfigureAwait(false);
            return fallbackBytes.Select(Convert.ToByte).ToArray();
        }
    }

    private async Task<byte[]> FetchEntropyAsync(CancellationToken cancellationToken)
    {
        Exception? lastFailure = null;

        for (var attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var remainingDelay = _options.MinimumRequestInterval - (DateTimeOffset.UtcNow - _lastRequestAt);
                    if (remainingDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(remainingDelay, cancellationToken).ConfigureAwait(false);
                    }

                    using var response = await _httpClient.GetAsync(_options.Endpoint, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    _lastRequestAt = DateTimeOffset.UtcNow;

                    var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                    return NormalizeEntropyPayload(payload);
                }
                finally
                {
                    _requestGate.Release();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastFailure = ex;
                var delay = TimeSpan.FromMilliseconds(_options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("CURBy quantum random request failed after all retry attempts.", lastFailure);
    }

    private static byte[] NormalizeEntropyPayload(byte[] payload)
    {
        if (payload.Length == 0)
        {
            throw new InvalidOperationException("CURBy returned an empty entropy payload.");
        }

        var text = Encoding.UTF8.GetString(payload).Trim();
        if (text.StartsWith('{'))
        {
            var dto = JsonSerializer.Deserialize<CurbyRoundDataDto>(text, JsonOptions);
            var encoded = dto?.Data ?? dto?.Randomness ?? dto?.Value ?? dto?.Payload;
            if (!string.IsNullOrWhiteSpace(encoded))
            {
                return DecodeTextPayload(encoded) ?? Encoding.UTF8.GetBytes(encoded);
            }
        }

        return DecodeTextPayload(text) ?? payload;
    }

    private static byte[]? DecodeTextPayload(string text)
    {
        var compact = text.Trim().Trim('"');

        if (TryDecodeHex(compact, out var hexBytes))
        {
            return hexBytes;
        }

        try
        {
            var decoded = Convert.FromBase64String(compact);
            return TryDecompress(decoded) ?? decoded;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool TryDecodeHex(string text, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (text.Length == 0 || text.Length % 2 != 0 || text.Any(c => !Uri.IsHexDigit(c)))
        {
            return false;
        }

        bytes = new byte[text.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
        }

        return true;
    }

    private static byte[]? TryDecompress(byte[] bytes)
    {
        try
        {
            using var compressed = new MemoryStream(bytes);
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            using var decompressed = new MemoryStream();
            zlib.CopyTo(decompressed);
            return decompressed.ToArray();
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private sealed class CurbyRoundDataDto
    {
        [JsonPropertyName("data")]
        public string? Data { get; init; }

        [JsonPropertyName("randomness")]
        public string? Randomness { get; init; }

        [JsonPropertyName("value")]
        public string? Value { get; init; }

        [JsonPropertyName("payload")]
        public string? Payload { get; init; }

        [JsonPropertyName("round")]
        public string? Round { get; init; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; init; }
    }
}

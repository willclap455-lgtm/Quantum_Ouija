using System.Buffers.Binary;
using System.Globalization;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;

namespace QuantumOuija.Rng;

public sealed class CurbyQuantumRandomProvider : IQuantumRandomProvider, IDisposable
{
    private const string SlashPropertyName = "/";

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
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("QuantumOuija", "1.0"));
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

        if (count == 0)
        {
            return Array.Empty<int>();
        }

        try
        {
            return await NextIntsFromCurbyAsync(count, minInclusive, maxInclusive, cancellationToken).ConfigureAwait(false);
        }
        catch when (_fallback is not null && !cancellationToken.IsCancellationRequested)
        {
            return await _fallback.NextIntsAsync(count, minInclusive, maxInclusive, cancellationToken).ConfigureAwait(false);
        }
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

    private async Task<IReadOnlyList<int>> NextIntsFromCurbyAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        CancellationToken cancellationToken)
    {
        var range = new BigInteger((long)maxInclusive - minInclusive + 1);
        if (range == BigInteger.One)
        {
            return Enumerable.Repeat(minInclusive, count).ToArray();
        }

        var seeds = await SelectRandomSeedsFromSingleChainAsync(count, cancellationToken).ConfigureAwait(false);
        var states = seeds.Select(seed => new EntropySeedState(seed.Bytes)).ToArray();
        var byteCount = GetByteRequirement(range);
        var maxValue = GetMaxValueForByteCount(byteCount);
        var threshold = maxValue - (maxValue % range);
        var values = new int[count];

        for (var i = 0; i < values.Length; i++)
        {
            var state = states[i % states.Length];
            BigInteger candidate;
            do
            {
                candidate = ConvertBytesToBigInteger(state.Take(byteCount));
            }
            while (candidate >= threshold);

            var offset = (long)(candidate % range);
            values[i] = checked(minInclusive + (int)offset);
        }

        return values;
    }

    private async Task<IReadOnlyList<CurbySeed>> SelectRandomSeedsFromSingleChainAsync(
        int count,
        CancellationToken cancellationToken)
    {
        var poolRequestCount = CalculateSeedPoolRequestCount(count);
        var seedPool = await GetCurbySeedsAsync(poolRequestCount, cancellationToken).ConfigureAwait(false);
        if (seedPool.Count == 0)
        {
            throw new InvalidOperationException($"Unable to obtain entropy seeds from {_options.BaseUri} for chain {_options.ChainId}.");
        }

        var selectedSeeds = new CurbySeed[count];
        for (var i = 0; i < selectedSeeds.Length; i++)
        {
            var randomIndex = RandomNumberGenerator.GetInt32(seedPool.Count);
            selectedSeeds[i] = seedPool[randomIndex];
        }

        return selectedSeeds;
    }

    private async Task<IReadOnlyList<CurbySeed>> GetCurbySeedsAsync(
        int count,
        CancellationToken cancellationToken)
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

                    using var response = await _httpClient.GetAsync(CreatePulsesUri(count), cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    _lastRequestAt = DateTimeOffset.UtcNow;

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return ParseCurbySeeds(document.RootElement, count);
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

    private Uri CreatePulsesUri(int count)
    {
        var baseUri = _options.BaseUri.ToString().TrimEnd('/');
        var chainId = Uri.EscapeDataString(_options.ChainId);
        return new Uri($"{baseUri}/chains/{chainId}/pulses?limit={count}");
    }

    private IReadOnlyList<CurbySeed> ParseCurbySeeds(JsonElement root, int requestedCount)
    {
        var seeds = new List<CurbySeed>();
        var pulses = EnumeratePulses(root);

        foreach (var pulse in pulses.Take(requestedCount))
        {
            seeds.Add(ParseCurbySeed(pulse));
        }

        if (seeds.Count == 0)
        {
            throw new InvalidOperationException("CURBy API did not return any pulses.");
        }

        return seeds;
    }

    private CurbySeed ParseCurbySeed(JsonElement pulse)
    {
        if (!TryGetNestedElement(pulse, out var payload, "data", "content", "payload"))
        {
            throw new InvalidOperationException("CURBy response did not include a payload.");
        }

        if (!TryGetNestedElement(payload, out var saltNode, "salt"))
        {
            throw new InvalidOperationException("CURBy payload did not include a salt value.");
        }

        var saltBytes = ResolveCurbySaltBytes(saltNode);
        if (saltBytes is null || saltBytes.Length == 0)
        {
            throw new InvalidOperationException("CURBy payload did not include salt bytes.");
        }

        if (!TryGetNestedElement(payload, out var timestampNode, "timestamp") ||
            timestampNode.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(timestampNode.GetString()))
        {
            throw new InvalidOperationException("CURBy payload did not include a timestamp.");
        }

        if (!TryGetNestedElement(pulse, out var indexNode, "data", "content", "index") ||
            !indexNode.TryGetInt64(out var index))
        {
            throw new InvalidOperationException("CURBy payload did not include a pulse index.");
        }

        var chainId =
            ResolveFirstCurbyCidValue(pulse, ("data", "chainCid"), ("data.content", "chainCid"), ("data.content", "chain")) ??
            ResolveCurbyCidAtPath(pulse, "cid") ??
            _options.ChainId;
        var timestamp = DateTimeOffset.Parse(timestampNode.GetString()!, CultureInfo.InvariantCulture).ToUniversalTime();

        return new CurbySeed(saltBytes, timestamp, index, chainId);
    }

    private static IEnumerable<JsonElement> EnumeratePulses(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                yield return item;
            }
        }
        else
        {
            yield return root;
        }
    }

    private static string? ResolveFirstCurbyCidValue(JsonElement root, params (string Prefix, string Property)[] paths)
    {
        foreach (var (prefix, property) in paths)
        {
            var fullPath = prefix.Split('.').Append(property).ToArray();
            var resolved = ResolveCurbyCidAtPath(root, fullPath);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return null;
    }

    private static string? ResolveCurbyCidAtPath(JsonElement root, params string[] path)
    {
        return TryGetNestedElement(root, out var value, path)
            ? ResolveCurbyCidValue(value)
            : null;
    }

    private static string? ResolveCurbyCidValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind == JsonValueKind.Object &&
            TryGetProperty(value, SlashPropertyName, out var slashValue))
        {
            return slashValue.ValueKind == JsonValueKind.String
                ? slashValue.GetString()
                : slashValue.ToString();
        }

        return value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : value.ToString();
    }

    private static byte[]? ResolveCurbySaltBytes(JsonElement value)
    {
        var queue = new Queue<(JsonElement Value, string? Hint)>();
        queue.Enqueue((value, null));

        while (queue.Count > 0)
        {
            var (current, hint) = queue.Dequeue();
            switch (current.ValueKind)
            {
                case JsonValueKind.String:
                    if (ShouldDecodeStringValue(hint) &&
                        TryDecodeBase64Unpadded(current.GetString(), out var bytes) &&
                        bytes.Length > 0)
                    {
                        return bytes;
                    }

                    break;
                case JsonValueKind.Object:
                    foreach (var property in current.EnumerateObject())
                    {
                        queue.Enqueue((property.Value, property.Name));
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in current.EnumerateArray())
                    {
                        queue.Enqueue((item, hint));
                    }

                    break;
            }
        }

        return null;
    }

    private static bool ShouldDecodeStringValue(string? hint) =>
        string.IsNullOrEmpty(hint) ||
        string.Equals(hint, "bytes", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(hint, "salt", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(hint, "value", StringComparison.OrdinalIgnoreCase);

    private static bool TryDecodeBase64Unpadded(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var sanitized = value.Trim();
        var paddingLength = (4 - sanitized.Length % 4) % 4;
        if (paddingLength > 0)
        {
            sanitized += new string('=', paddingLength);
        }

        try
        {
            bytes = Convert.FromBase64String(sanitized);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryGetNestedElement(JsonElement root, out JsonElement value, params string[] propertyPath)
    {
        value = root;
        foreach (var segment in propertyPath)
        {
            if (value.ValueKind != JsonValueKind.Object || !TryGetProperty(value, segment, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private int CalculateSeedPoolRequestCount(int count)
    {
        var desired = Math.Max((long)count * 4, _options.MinimumSeedPoolSize);
        return (int)Math.Min(desired, _options.MaximumSeedPoolSize);
    }

    private static int GetByteRequirement(BigInteger range)
    {
        var byteCount = 1;
        var capacity = new BigInteger(256);
        while (capacity < range)
        {
            byteCount++;
            capacity *= 256;
        }

        return byteCount;
    }

    private static BigInteger GetMaxValueForByteCount(int byteCount)
    {
        var value = BigInteger.One;
        for (var i = 0; i < byteCount; i++)
        {
            value *= 256;
        }

        return value;
    }

    private static BigInteger ConvertBytesToBigInteger(byte[] bytes)
    {
        var result = BigInteger.Zero;
        foreach (var byteValue in bytes)
        {
            result = result * 256 + byteValue;
        }

        return result;
    }

    private sealed record CurbySeed(byte[] Bytes, DateTimeOffset Timestamp, long Index, string ChainId);

    private sealed class EntropySeedState
    {
        private readonly byte[] _seed;
        private readonly Queue<byte> _buffer = new();
        private ulong _counter;

        public EntropySeedState(byte[] seed)
        {
            _seed = seed;
        }

        public byte[] Take(int byteCount)
        {
            var counterBytes = new byte[sizeof(ulong)];
            while (_buffer.Count < byteCount)
            {
                BinaryPrimitives.WriteUInt64BigEndian(counterBytes, _counter);

                var input = new byte[_seed.Length + counterBytes.Length];
                Buffer.BlockCopy(_seed, 0, input, 0, _seed.Length);
                counterBytes.CopyTo(input.AsSpan(_seed.Length));

                var hash = SHA512.HashData(input);
                foreach (var byteValue in hash)
                {
                    _buffer.Enqueue(byteValue);
                }

                _counter++;
            }

            var result = new byte[byteCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = _buffer.Dequeue();
            }

            return result;
        }
    }
}

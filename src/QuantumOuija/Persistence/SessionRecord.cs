using System.Text.Json;
using QuantumOuija.Simulation;

namespace QuantumOuija.Persistence;

public sealed record SessionRecord(
    DateTimeOffset CreatedAt,
    string Question,
    string Response,
    int RequestedPathCount,
    IReadOnlyList<PathRecord> Paths,
    string RandomProviderName,
    bool IsDeterministic);

public sealed record PathRecord(
    int Index,
    GridNode StartNode,
    GridNode EndNode,
    IReadOnlyList<int> Directions,
    IReadOnlyList<int> Distances,
    string FinalToken);

public static class SessionRecordFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static SessionRecord Create(
        string question,
        string response,
        int requestedPathCount,
        IEnumerable<GeneratedPath> paths,
        string randomProviderName,
        bool isDeterministic) =>
        new(
            DateTimeOffset.UtcNow,
            question,
            response,
            requestedPathCount,
            paths.Select(path => new PathRecord(
                path.Index,
                path.StartNode,
                path.EndNode,
                path.Directions,
                path.Distances,
                path.FinalToken.ToString())).ToArray(),
            randomProviderName,
            isDeterministic);

    public static string ToJson(SessionRecord record) => JsonSerializer.Serialize(record, JsonOptions);
}

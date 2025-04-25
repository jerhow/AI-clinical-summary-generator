using ClinicalSummaryGenerator.Services.Interfaces;

namespace ClinicalSummaryGenerator.Services;

public class InMemorySummaryCache : IClinicalSummaryCache
{
    private readonly Dictionary<string, string> _cache = new();

    public Task<string?> GetAsync(string hashKey)
    {
        return Task.FromResult(_cache.TryGetValue(hashKey, out var result) ? result : null);
    }

    public Task SetAsync(string hashKey, string result)
    {
        _cache[hashKey] = result;
        return Task.CompletedTask;
    }
}

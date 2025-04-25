namespace ClinicalSummaryGenerator.Services.Interfaces;

public interface IClinicalSummaryCache
{
    Task<string?> GetAsync(string hashKey);
    Task SetAsync(string hashKey, string result);
}

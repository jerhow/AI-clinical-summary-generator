namespace ClinicalSummaryGenerator.Models;

public class SummarizeResponse
{
    public string? Summary { get; set; }
    public int? TokenUsage { get; set; } // Optional, for logging/debug
}

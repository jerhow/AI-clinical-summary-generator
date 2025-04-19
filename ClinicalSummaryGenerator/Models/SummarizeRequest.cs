namespace ClinicalSummaryApp.Models;

public class SummarizeRequest
{
    public string? ClinicalText { get; set; }
    public string? SummaryStyle { get; set; } // e.g., "brief", "detailed", "SOAP"
}

namespace ClinicalSummaryGenerator.Models;

public class StructuredSummary
{
    public List<string> Diagnoses { get; set; } = new();
    public List<string> Medications { get; set; } = new();
    public List<string> Plan { get; set; } = new();
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocumentFormat.OpenXml.Packaging;
using System.Text.Json;
using ClinicalSummaryGenerator.Services;
using ClinicalSummaryGenerator.Models;

namespace ClinicalSummaryGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly AiService _aiService;
    public int ClinicalTextMaxLength { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Summary { get; set; }
    public StructuredSummary? Structured { get; set; }

    public Dictionary<string, string> SummaryStyleLabels { get; } = new()
    {
        { "brief", "Brief" },
        { "detailed", "Detailed" },
        { "soap", "SOAP" },
        { "structured", "Structured Extraction" }
    };

    [BindProperty]
    public string? ClinicalText { get; set; }

    [BindProperty]
    public string? SummaryStyle { get; set; } = "brief";

    [BindProperty]
    public IFormFile? ClinicalNoteFile { get; set; }

    public IndexModel(IConfiguration config, AiService aiService)
    {
        _aiService = aiService;
        ClinicalTextMaxLength = config.GetValue<int>("UI:ClinicalTextMaxLength", 8000);
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        if (ClinicalNoteFile != null && ClinicalNoteFile.Length > 0)
        {
            using var stream = ClinicalNoteFile.OpenReadStream();
            using var reader = new StreamReader(stream);

            if (ClinicalNoteFile.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                ClinicalText = await reader.ReadToEndAsync();
            }
            else if (ClinicalNoteFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                using var doc = WordprocessingDocument.Open(ms, false);
                ClinicalText = string.Join("\n", doc.MainDocumentPart!.Document.Body!
                    .Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
            }
            else
            {
                ModelState.AddModelError("ClinicalNoteFile", "Only .txt and .docx files are supported.");
                return Page();
            }
        }

        if (string.IsNullOrWhiteSpace(ClinicalText)) // Always validating the final state
        {
            ModelState.AddModelError("ClinicalText", "Clinical text is required.");
            return Page();
        }

        if (action == "load") // If action was just to load the file, we're done
        {
            return Page();
        }

        try
        {
            var response = await _aiService.SummarizeAsync(ClinicalText!, SummaryStyle!);

            if (SummaryStyle == "structured")
            {
                Structured = response.Summary != null 
                    ? JsonSerializer.Deserialize<StructuredSummary>(response.Summary) 
                    : null;

                Summary = "";
            }
            else
            {
                Summary = response.Summary;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating summary: {ex.Message}");
            ErrorMessage = "Something went wrong while generating the summary. Please try again.";
        }

        return Page();
    }
}

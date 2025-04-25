using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocumentFormat.OpenXml.Packaging;
using ClinicalSummaryGenerator.Models;
using ClinicalSummaryGenerator.Services;
using ClinicalSummaryGenerator.Services.Interfaces;

namespace ClinicalSummaryGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly AiService _aiService;
    public int ClinicalTextMaxLength { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Summary { get; set; }
    public StructuredSummary? Structured { get; set; }
    private readonly IClinicalSummaryCache _cache;

    public Dictionary<string, string> SummaryStyleLabels { get; } = new()
    {
        { "brief", "Brief" },
        { "detailed", "Detailed" },
        { "soap", "SOAP" },
        { "structured", "Structured Extraction" },
        { "rawjson", "Structured Extraction (JSON)" }
    };

    [BindProperty]
    public string? ClinicalText { get; set; }

    [BindProperty]
    public string? SummaryStyle { get; set; } = "brief";

    [BindProperty]
    public IFormFile? ClinicalNoteFile { get; set; }

    public IndexModel(IConfiguration config, AiService aiService, IClinicalSummaryCache cache)
    {
        _aiService = aiService;
        _cache = cache;
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

        // Generate hash key and check the cache
        var hashKey = GenerateHash(ClinicalText + SummaryStyle);
        var cachedSummary = await _cache.GetAsync(hashKey);
        if (cachedSummary != null)
        {
            if (SummaryStyle == "structured")
            {
                Structured = JsonSerializer.Deserialize<StructuredSummary>(cachedSummary);
                Summary = ""; // summary cannot be null in this case
            }
            else
            {
                Summary = cachedSummary;
            }

            return Page();
        }

        try // No cache hit, proceed with GPT call
        {
            var response = await _aiService.SummarizeAsync(ClinicalText!, SummaryStyle!);

            if (SummaryStyle == "structured")
            {
                Structured = response.Summary != null 
                    ? JsonSerializer.Deserialize<StructuredSummary>(response.Summary) 
                    : null;

                Summary = ""; // summary cannot be null in this case
            }
            else if (SummaryStyle == "rawjson")
            {
                try
                {
                    using var doc = JsonDocument.Parse(response.Summary);
                    Summary = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                catch
                {
                    Summary = response.Summary; // Fallback: Show unformatted raw string
                }
            }
            else
            {
                Summary = response.Summary;
            }

            // Cache the result (summary)
            await _cache.SetAsync(hashKey, response.Summary!);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating summary: {ex.Message}");
            ErrorMessage = "Something went wrong while generating the summary. Please try again.";
        }

        return Page();
    }

    private static string GenerateHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

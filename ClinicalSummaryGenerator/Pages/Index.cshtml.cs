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
    private readonly ILogger<IndexModel> _logger;

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

    public IndexModel(IConfiguration config, AiService aiService, IClinicalSummaryCache cache, ILogger<IndexModel> logger)
    {
        _aiService = aiService;
        _cache = cache;
        _logger = logger;
        ClinicalTextMaxLength = config.GetValue<int>("UI:ClinicalTextMaxLength", 8000);
    }

    /// <summary>
    /// Handles the form submission for generating a clinical summary.
    /// It processes the uploaded file, validates the clinical text, and generates a summary using the AI service.
    /// The action parameter determines the type of action to perform (e.g., load or generate).
    /// If the action is "load", it simply loads the file without generating a summary.
    /// If the action is to generate, it checks the cache for an existing summary and generates a new one if not found.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public async Task<IActionResult> OnPostAsync(string action)
    {
        await ProcessUploadedFileAsync();

        if (!ValidateClinicalText())
        {
            return Page();
        }

        if (action == "load")
        {
            return Page();
        }

        _logger.LogInformation("Received form submission. SummaryStyle: {Style}, TextLength: {Length}", SummaryStyle, ClinicalText?.Length ?? 0);

        var hashKey = GenerateHash(ClinicalText + SummaryStyle);

        if (await TryLoadFromCacheAsync(hashKey))
        {
            return Page();
        }

        await GenerateSummaryAsync(hashKey);

        return Page();
    }

    /// <summary>
    /// Processes the uploaded file, reads its content, and sets the ClinicalText property.
    /// Currently supports .txt and .docx formats.
    /// </summary>
    /// <returns></returns>
    private async Task ProcessUploadedFileAsync()
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
            }
        }
    }

    /// <summary>
    /// Validates the clinical text input.
    /// </summary>
    /// <returns></returns>
    private bool ValidateClinicalText()
    {
        if (string.IsNullOrWhiteSpace(ClinicalText))
        {
            ModelState.AddModelError("ClinicalText", "Clinical text is required.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Tries to load the summary from the cache.
    /// </summary>
    /// <param name="hashKey"></param>
    /// <returns></returns>
    private async Task<bool> TryLoadFromCacheAsync(string hashKey)
    {
        var cachedSummary = await _cache.GetAsync(hashKey);

        if (cachedSummary == null)
        {
            _logger.LogInformation("Cache miss. Sending input to AI service. Hash: {Hash}", hashKey);
            return false;
        }

        _logger.LogInformation("Cache hit for input. Hash: {Hash}", hashKey);

        if (SummaryStyle == "structured")
        {
            Structured = JsonSerializer.Deserialize<StructuredSummary>(cachedSummary);
            Summary = "";
        }
        else
        {
            Summary = cachedSummary;
        }

        return true;
    }

    /// <summary>
    /// Generates a summary using the AI service and caches the result.
    /// This method is called when there is no cache hit.
    /// </summary>
    /// <param name="hashKey"></param>
    /// <returns></returns>
    private async Task GenerateSummaryAsync(string hashKey)
    {
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
                    Summary = response.Summary;
                }
            }
            else
            {
                Summary = response.Summary;
            }

            await _cache.SetAsync(hashKey, response.Summary!);
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI service failed. StatusCode: {StatusCode}", ex.StatusCode);
            ErrorMessage = "Something went wrong while generating the summary. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during summary generation.");
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
    }

    /// <summary>
    /// Generates a hash for the given input string using SHA256.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string GenerateHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

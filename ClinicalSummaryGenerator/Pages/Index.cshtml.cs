using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DocumentFormat.OpenXml.Packaging;
using ClinicalSummaryGenerator.Services;

namespace ClinicalSummaryGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly AiService _aiService;
    public int ClinicalTextMaxLength { get; set; }
    public string? SelectedSummaryStyle { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Summary { get; set; }

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

        // Always validating the final state
        if (string.IsNullOrWhiteSpace(ClinicalText))
        {
            ModelState.AddModelError("ClinicalText", "Clinical text is required.");
            return Page();
        }

        SelectedSummaryStyle = SummaryStyle;

        // If action was just to load the file, we're done
        if (action == "load")
        {
            return Page();
        }

        try
        {
            var response = await _aiService.SummarizeAsync(ClinicalText!, SummaryStyle!);
            Summary = response.Summary;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating summary: {ex.Message}");
            ErrorMessage = "Something went wrong while generating the summary. Please try again.";
        }

        return Page();
    }
}

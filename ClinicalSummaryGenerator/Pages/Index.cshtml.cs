using ClinicalSummaryGenerator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClinicalSummaryGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly AiService _aiService;
    public int ClinicalTextMaxLength { get; set; }

    public IndexModel(IConfiguration config, AiService aiService)
    {
        _aiService = aiService;
        ClinicalTextMaxLength = config.GetValue<int>("UI:ClinicalTextMaxLength", 8000);
    }

    [BindProperty]
    public string? ClinicalText { get; set; }

    [BindProperty]
    public string? SummaryStyle { get; set; } = "brief";

    public string? Summary { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ClinicalText))
        {
            ModelState.AddModelError("ClinicalText", "Clinical text is required.");
            return Page();
        }

        var response = await _aiService.SummarizeAsync(ClinicalText, SummaryStyle);
        Summary = response.Summary;

        return Page();
    }
}

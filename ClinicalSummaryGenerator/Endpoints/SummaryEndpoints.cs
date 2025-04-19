using ClinicalSummaryApp.Models;

namespace ClinicalSummaryApp.Endpoints;

public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/summarize", async (SummarizeRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.ClinicalText))
            {
                return Results.BadRequest("Clinical text is required.");
            }

            // Placeholder GPT call
            var response = new SummarizeResponse
            {
                Summary = $"[Mock Summary for style '{request.SummaryStyle ?? "default"}']\n\n" +
                          "This is a placeholder summary generated from the clinical text.",
                TokenUsage = null
            };

            return Results.Ok(response);
        });
    }
}

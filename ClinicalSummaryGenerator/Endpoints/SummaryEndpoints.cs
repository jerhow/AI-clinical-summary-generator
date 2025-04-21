using ClinicalSummaryGenerator.Models;
using ClinicalSummaryGenerator.Services;

namespace ClinicalSummaryGenerator.Endpoints;

public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/summarize", async (SummarizeRequest request, AiService aiService) =>
        {
            if (string.IsNullOrWhiteSpace(request.ClinicalText))
            {
                return Results.BadRequest("Clinical text is required.");
            }

            var gptResponse = await aiService.SummarizeAsync(request.ClinicalText, request.SummaryStyle);

            return Results.Ok(new SummarizeResponse
            {
                Summary = gptResponse.Summary,
                TokenUsage = gptResponse.TokenUsage
            });
        });
    }
}

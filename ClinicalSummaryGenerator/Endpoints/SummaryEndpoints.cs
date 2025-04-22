using ClinicalSummaryGenerator.Models;
using ClinicalSummaryGenerator.Services;
using ClinicalSummaryGenerator.Helpers;

namespace ClinicalSummaryGenerator.Endpoints;

public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/summarize", async (
            IConfiguration config, 
            HttpRequest httpRequest, 
            SummarizeRequest request, 
            AiService aiService) =>
        {
            // Check auth token
            if (!SecurityHelpers.IsAuthorized(httpRequest, config, out var error))
            {
                return Results.Unauthorized();
            }
            
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

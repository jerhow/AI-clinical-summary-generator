using ClinicalSummaryGenerator.Models;
using ClinicalSummaryGenerator.Services;
using ClinicalSummaryGenerator.Helpers;

namespace ClinicalSummaryGenerator.Endpoints;

public static class SummaryEndpoints
{
    /// <summary>
    /// Maps the summary endpoints for the application.
    /// This includes a POST endpoint for summarizing clinical text (currently the only endpoint).
    /// The endpoint requires an API key for authorization and returns a summary of the clinical text.
    /// The summary style can be specified in the request.
    /// </summary>
    /// <param name="app"></param>
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

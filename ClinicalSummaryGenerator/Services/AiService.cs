using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClinicalSummaryGenerator.Models;

namespace ClinicalSummaryGenerator.Services;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public AiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<AiChatResponse> SummarizeAsync(string clinicalText, string? summaryStyle)
    {
        var systemPrompt = _config["OpenAI:SystemPrompt"] ?? "";
        var styleKey = summaryStyle?.ToLower() ?? "brief";
        var summaryInstruction = _config[$"OpenAI:SummaryStyles:{styleKey}"] ?? "Summarize the following clinical note.";

        var messages = new List<Message>
        {
            new Message { Role = "system", Content = systemPrompt },
            new Message { Role = "user", Content = $"{summaryInstruction}\n\n{clinicalText}" }
        };

        var payload = new AiChatRequest
        {
            Messages = messages
        };

        var apiKey = _config["OpenAI:ApiKey"];
        var endpoint = _config["OpenAI:Endpoint"];
        var deployment = _config["OpenAI:Deployment"];
        var apiVersion = _config["OpenAI:ApiVersion"];

        string url = $"{endpoint}openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;
            var reason = response.ReasonPhrase ?? "No reason provided";

            Console.Error.WriteLine($"GPT Error {statusCode} ({reason}): {errorContent}");

            throw new AiServiceException(
                $"GPT request failed with status {statusCode} ({reason}): {errorContent}",
                response.StatusCode
            );
        }

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(contentStream);

        var message = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var tokens = json.RootElement.TryGetProperty("usage", out var usage) 
            ? usage.GetProperty("total_tokens").GetInt32() 
            : (int?)null;

        return new AiChatResponse
        {
            Summary = message,
            TokenUsage = tokens
        };
    }
}

public class AiServiceException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public AiServiceException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

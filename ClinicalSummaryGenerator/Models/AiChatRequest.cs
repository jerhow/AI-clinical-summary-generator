using System.Text.Json.Serialization;

namespace ClinicalSummaryGenerator.Models;

public class AiChatRequest
{
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.2f;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonPropertyName("top_p")]
    public float TopP { get; set; } = 1.0f;

    [JsonPropertyName("frequency_penalty")]
    public float FrequencyPenalty { get; set; } = 0;

    [JsonPropertyName("presence_penalty")]
    public float PresencePenalty { get; set; } = 0;

    [JsonPropertyName("stop")]
    public string[]? Stop { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

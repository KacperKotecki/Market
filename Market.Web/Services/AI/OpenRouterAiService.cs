using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Market.Web.Core.DTOs;
using Market.Web.Core.Exceptions;
using Market.Web.Core.Options;
using Microsoft.Extensions.Options;

namespace Market.Web.Services.AI;

public class OpenRouterAiService : IADescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterAiService> _logger;
    private readonly string _openruterModel;

    private readonly IPromptProvider _promptProvider;

    public OpenRouterAiService(
        HttpClient httpClient, 
        IOptions<OpenRouterOptions> options,
        ILogger<OpenRouterAiService> logger,
        IPromptProvider promptProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _promptProvider = promptProvider;

        var config = options.Value;
        _openruterModel = config.Model;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", config.Referer); 
        _httpClient.DefaultRequestHeaders.Add("X-Title", config.AppTitle);
    }

    public async Task<AuctionDraftDto> GenerateFromImagesAsync(List<IFormFile> images)
    {
        var imageContents = new List<object>();

        // 1. Konwersja obrazów na Base64
        foreach (var image in images)
        {
            if (image.Length > 0)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                var base64 = Convert.ToBase64String(fileBytes);

                imageContents.Add(new 
                {
                    type = "image_url",
                    image_url = new { url = $"data:{image.ContentType};base64,{base64}" }
                });
            }
        }

        string systemPrompt = await _promptProvider.GetSystemPromptAsync();

        var messages = new List<object>
        {
            new 
            {
                role = "system",
                content = systemPrompt,
            },
            new 
            {
                role = "user",
                content = imageContents
            }
        };

        var requestBody = new
        {
            model = _openruterModel, 
            messages = messages,
            response_format = new { type = "json_object" } // Wymuszamy tryb JSON
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseString;
        try
        {
            response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);
            responseString = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "AI service HTTP call failed after retries.");
            throw new AiGenerationException("Usługa AI jest tymczasowo niedostępna. Spróbuj ponownie.", httpEx);
        }
        catch (TaskCanceledException timeoutEx)
        {
            _logger.LogError(timeoutEx, "AI service request timed out.");
            throw new AiGenerationException("Przekroczono czas oczekiwania na odpowiedź AI.", timeoutEx);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenRouter API Error: {StatusCode} - {ResponseString}", response.StatusCode, responseString);
            throw new AiGenerationException($"OpenRouter API Error: {response.StatusCode}");
        }

        try
        {
            // 4. Wyciąganie danych z zagnieżdżonej struktury OpenAI
            using var doc = JsonDocument.Parse(responseString);
            var contentString = doc.RootElement
                             .GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString();

            if (string.IsNullOrEmpty(contentString))
            {
                 _logger.LogError("AI returned an empty content string.");
                 throw new AiGenerationException("AI zwróciło pustą odpowiedź.");
            }

            // 5. Sanitize potential Markdown code-block wrappers (e.g. ```json ... ```) before parsing
            var sanitizedContent = SanitizeJsonResponse(contentString);

            // 6. Deserializacja właściwego JSONa z danymi aukcji
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var draft = JsonSerializer.Deserialize<AuctionDraftDto>(sanitizedContent, options);
            return draft ?? new AuctionDraftDto();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response JSON. Raw response: {ResponseString}", responseString);
            throw new AiGenerationException("Błąd parsowania JSON z AI.", ex);
        }
    }

    /// <summary>
    /// Strips Markdown code-block fences that LLMs sometimes wrap around JSON output.
    /// Handles both ```json ... ``` and ``` ... ``` variants.
    /// </summary>
    private static string SanitizeJsonResponse(string raw)
    {
        var trimmed = raw.Trim();

        // Remove opening fence: ```json or ```
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline != -1)
                trimmed = trimmed[(firstNewline + 1)..];
        }

        // Remove closing fence: ```
        if (trimmed.EndsWith("```", StringComparison.Ordinal))
            trimmed = trimmed[..^3];

        return trimmed.Trim();
    }
}
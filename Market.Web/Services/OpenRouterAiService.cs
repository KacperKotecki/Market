using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Market.Web.DTOs;

namespace Market.Web.Services;

public class OpenRouterAiService : IADescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private readonly string _openruterModel;

    public OpenRouterAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        var apiKey = _configuration["OpenRouter:ApiKey"];
        _openruterModel = _configuration["OpenRouter:Model"];

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        // OpenRouter wymaga referera i tytułu strony dla statystyk
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost:7000"); 
        _httpClient.DefaultRequestHeaders.Add("X-Title", "MarketApp");
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

        string promptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts", "system_prompt.txt");
        string systemPrompt = await File.ReadAllTextAsync(promptPath);
        if(!File.Exists(promptPath) || string.IsNullOrEmpty(systemPrompt))
        {
            throw new Exception("Nie można znaleźć lub odczytać pliku z promptem systemowym.");
        }

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
        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);
        
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenRouter API Error: {response.StatusCode} - {responseString}");
        }

        // 4. Wyciąganie danych z zagnieżdżonej struktury OpenAI
        // Struktura: { "choices": [ { "message": { "content": "{ TU_JEST_NASZ_JSON }" } } ] }
        using var doc = JsonDocument.Parse(responseString);
        var contentString = doc.RootElement
                         .GetProperty("choices")[0]
                         .GetProperty("message")
                         .GetProperty("content")
                         .GetString();

        if (string.IsNullOrEmpty(contentString))
        {
             throw new Exception("AI zwróciło pustą odpowiedź.");
        }

        // 5. Deserializacja właściwego JSONa z danymi aukcji
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try 
        {
            var draft = JsonSerializer.Deserialize<AuctionDraftDto>(contentString, options);
            return draft ?? new AuctionDraftDto();
        }
        catch (JsonException)
        {
            // Fallback: czasami AI doda ```json na początku mimo zakazu, warto to obsłużyć lub po prostu rzucić błąd
            throw new Exception("Błąd parsowania JSON z AI. Treść: " + contentString);
        }
    }
}
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Market.Web.DTOs;

namespace Market.Web.Services;

public class OpenRouterAiService : IADescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenRouterAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        var apiKey = _configuration["OpenRouter:ApiKey"];
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

                // Budujemy strukturę wiadomości obrazkowej dla GPT-4o
                imageContents.Add(new 
                {
                    type = "image_url",
                    image_url = new { url = $"data:{image.ContentType};base64,{base64}" }
                });
            }
        }

        // 2. Budowanie promptu
        var messages = new List<object>
        {
            new 
            {
                role = "system",
                content = @"
            Jesteś Ekspertem E-commerce i Technicznym Rzeczoznawcą. Tworzysz profesjonalne oferty sprzedaży.

            ### CEL
            Stwórz ofertę z **BARDZO ROZBUDOWANĄ** specyfikacją techniczną. Twoim priorytetem jest przygotowanie szczegółowego formularza dla użytkownika.

            ### FAZA 1: IDENTYFIKACJA I BEZPIECZEŃSTWO
            1. **Safety & Consistency:** Sprawdź czy zdjęcia są bezpieczne i przedstawiają ten sam przedmiot. Jeśli nie -> Zwróć JSON z Title: ""ERROR: [Powód]"" i zakończ.
            2. **Rozpoznanie:** Zidentyfikuj przedmiot. Jeśli to sprzęt techniczny  który moźna opisać za pomocą bardziej szczegółowej specyfikacji np(komputer, auto, telefon), uruchom tryb ""DEEP SPECS"".

            ### FAZA 2: STRUKTURA SPECYFIKACJI (TRYB DEEP SPECS)
            Dla przedmiotów złożonych nie ograniczaj się do 3 cech. Musisz wypisać WSZYSTKIE standardowe parametry dla danej kategorii (minimum 8-12 pozycji).

            **ZASADA WYPEŁNIANIA:**
            - Widzisz parametr na zdjęciu? -> **WPISZ GO**.
            - Nie widzisz? -> **Zostaw placeholder do uzupełnienia: `..........`**.

            **WZORCE KATEGORII (Stosuj te schematy):**
                PC/Laptop: Procesor, Płyta główna, RAM (typ/ilość), Karta graficzna, Dysk (SSD/HDD), Zasilacz, Chłodzenie, Obudowa, System, Złącza.

                Samochód: Marka/Model, Rok, Przebieg, Silnik (Pojemność/Moc), Paliwo, Skrzynia, Napęd, Wyposażenie (Klima, ABS), Stan blacharski.

                Smartfon/Tablet: Procesor, RAM, Pamięć, Ekran (Cale/Hz), Bateria, Aparaty, Stan ekranu, Blokady (iCloud/Simlock).

                Monitor/TV: Rozdzielczość, Matryca, Odświeżanie (Hz), Czas reakcji, Złącza, VESA, Smart TV.

                Sofa/Kanapa: Typ, Wymiary (SxGxW), Powierzchnia spania, Materiał obicia, Wypełnienie, Stelaż, Pojemnik na pościel, Kolor, Stan.

                Zegarek: Typ mechanizmu (Kwarc/Automat), Koperta (Materiał/Rozmiar), Szkło, Wodoszczelność (ATM), Pasek, Rezerwa chodu, Funkcje (Data/Chronograf), Stan.

                Namiot turystyczny: Liczba osób, Konstrukcja, Wodoodporność tropiku/podłogi, Materiał, Stelaż, Wymiary, Waga, Czas rozkładania, Stan.

                Wózek dziecięcy: Typ (2w1/3w1), Materiał ramy, Koła, Amortyzacja, Waga, Maksymalne obciążenie, Pasy bezpieczeństwa, System składania, Akcesoria.

                Materac: Typ (Piankowy/Sprężynowy), Twardość (H), Wymiary, Grubość, Strefy komfortu, Pokrowiec, Wentylacja, Nośność, Stan.

                Walizka podróżna: Pojemność, Materiał, Wymiary, Waga, Kółka (Ilość/Typ), Zamek (TSA), Przegrody, Stan.

                Deska SUP: Długość, Szerokość, Grubość, Materiał, Maksymalne obciążenie, Ciśnienie robocze (PSI), Waga, Zestaw akcesoriów, Stan.

                Drabina: Typ, Materiał, Maksymalna wysokość, Udźwig, Liczba stopni, Certyfikaty, Waga, System blokady, Stan.

                Inne: Dostosuj szczegółowość analogicznie do powyższych.

            ### FAZA 3: GENEROWANIE TREŚCI (JSON)

            1. Title: [Marka] [Model] [Kluczowe Parametry] - [Stan]. Max 80 znaków.
            2. Category: Wybierz z: 'Elektronika', 'Moda', 'Dom i Ogród', 'Sport i Hobby', 'Motoryzacja', 'Kultura i Rozrywka', 'Inne'.
            3. SuggestedPrice: Oszacuj w PLN (lub 0).
            4. Description (Markdown):
            - Wstęp: Zachęcający opis marketingowy (2 zdania).
            - SZCZEGÓŁOWA SPECYFIKACJA: Tu wstaw wygenerowaną listę (Deep Specs).
                Przykład formatowania dla PC (gdy nie widać podzespołów):
                * Procesor: .......... (np. i5 / Ryzen 5)
                * Karta Graficzna: ..........
                * Pamięć RAM: .......... GB taktowana na .......... MHz
                * Płyta Główna: ..........
                * Dysk SSD/HDD: ..........
                * Zasilacz: .......... W
                * Obudowa: Widoczna na zdjęciu (np. SilentiumPC)
                * System operacyjny: ..........
            - Stan Wizualny: Opisz dokładnie rysy, wady, zabrudzenia.
            - Call to Action.

            ### OUTPUT FORMAT
            Zwróć TYLKO czysty obiekt JSON (bez ```json):
            {
            ""Title"": ""string"",
            ""Description"": ""string"",
            ""SuggestedPrice"": number,
            ""Category"": ""string""
            }"
            },
            new 
            {
                role = "user",
                content = imageContents
            }
        };

        var requestBody = new
        {
            model = "openai/gpt-4o", 
            messages = messages,
            response_format = new { type = "json_object" } // Wymuszamy tryb JSON
        };

        // 3. Wysłanie żądania
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
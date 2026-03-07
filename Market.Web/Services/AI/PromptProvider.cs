using Market.Web.Core.Exceptions;


namespace Market.Web.Services.AI;  
public class PromptProvider : IPromptProvider
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PromptProvider> _logger;
    public PromptProvider(IWebHostEnvironment env, ILogger<PromptProvider> logger)
    {
        _env = env;
        _logger = logger;
    }
    public async Task<string> GetSystemPromptAsync()
    {
        string promptPath = Path.Combine(_env.ContentRootPath, "Prompts", "system_prompt.txt");
        
        if (!File.Exists(promptPath))
        {
            _logger.LogError("System prompt file not found at path: {PromptPath}", promptPath);
            throw new AiGenerationException("Nie można znaleźć lub odczytać pliku z promptem systemowym.");
        }
        string systemPrompt;
        try
        {
            systemPrompt = await File.ReadAllTextAsync(promptPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read system prompt file at path: {PromptPath}", promptPath);
            throw new AiGenerationException("Nie można znaleźć lub odczytać pliku z promptem systemowym.", ex);
        }

        if (string.IsNullOrEmpty(systemPrompt))
        {
            _logger.LogError("System prompt file is empty at path: {PromptPath}", promptPath);
            throw new AiGenerationException("Nie można znaleźć lub odczytać pliku z promptem systemowym.");
        }
        return systemPrompt;
    }
}
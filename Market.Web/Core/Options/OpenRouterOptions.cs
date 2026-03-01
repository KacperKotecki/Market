using System.ComponentModel.DataAnnotations;

namespace Market.Web.Core.Options;

public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string Referer { get; set; } = string.Empty;

    public string AppTitle { get; set; } = "Market.AI";
}
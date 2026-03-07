
namespace Market.Web.Services.AI;

public interface IPromptProvider
{
    Task<string> GetSystemPromptAsync();
}
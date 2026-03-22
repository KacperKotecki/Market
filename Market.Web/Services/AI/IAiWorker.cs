namespace Market.Web.Services.AI;

public interface IAiWorker
{
    Task GenerateDescriptionJobAsync(int auctionId);
}
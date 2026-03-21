namespace Market.Web.Services;

public interface IAuctionImageWorker
{
    Task ProcessImagesJobAsync(int auctionId, string[] tempPaths);
}
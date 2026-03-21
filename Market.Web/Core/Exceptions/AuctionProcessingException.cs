namespace Market.Web.Core.Exceptions;

public class AuctionProcessingException : Exception
{
    public AuctionProcessingException(string message) : base(message) { }
    public AuctionProcessingException(string message, Exception innerException) : base(message, innerException) { }
}
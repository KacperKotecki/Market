namespace Market.Web.Core.Exceptions;

public class AiProcessingConflictException : AuctionProcessingException
{
    public AiProcessingConflictException(string message) : base(message) { }
    public AiProcessingConflictException(string message, Exception innerException) : base(message, innerException) { }
}

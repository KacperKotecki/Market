namespace Market.Web.Core.Exceptions;

public class AuctionException : Exception
{
    /// <summary>Optional ViewModel property name for ModelState binding in the controller.</summary>
    public string? PropertyName { get; }

    public AuctionException()
        : base("An auction error occurred.") { }

    public AuctionException(string message)
        : base(message) { }

    public AuctionException(string message, string propertyName)
        : base(message)
    {
        PropertyName = propertyName;
    }

    public AuctionException(string message, Exception innerException)
        : base(message, innerException) { }
}

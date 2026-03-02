namespace Market.Web.Core.Exceptions;

public class InvalidOrderStateException : Exception
{
    public InvalidOrderStateException()
        : base("The order is not in a valid state for this operation.") { }

    public InvalidOrderStateException(string message)
        : base(message) { }

    public InvalidOrderStateException(string message, Exception innerException)
        : base(message, innerException) { }
}

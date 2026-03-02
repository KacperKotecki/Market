namespace Market.Web.Core.Exceptions;

public class OrderAuthorizationException : Exception
{
    public OrderAuthorizationException()
        : base("You are not authorized to perform this action on the order.") { }

    public OrderAuthorizationException(string message)
        : base(message) { }

    public OrderAuthorizationException(string message, Exception innerException)
        : base(message, innerException) { }
}

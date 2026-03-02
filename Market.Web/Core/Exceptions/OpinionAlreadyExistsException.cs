namespace Market.Web.Core.Exceptions;

public class OpinionAlreadyExistsException : Exception
{
    public OpinionAlreadyExistsException()
        : base("An opinion has already been submitted for this order.") { }

    public OpinionAlreadyExistsException(string message)
        : base(message) { }

    public OpinionAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException) { }
}

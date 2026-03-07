using System;

namespace Market.Web.Core.Exceptions;

public class StockUnavailableException : Exception
{
    public StockUnavailableException(string message) : base(message) { }
    public StockUnavailableException(string message, Exception? innerException) : base(message, innerException) { }
}
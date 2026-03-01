using System;

namespace Market.Web.Core.Exceptions;

public class StockUnavailableException : Exception
{
    public StockUnavailableException(string message) : base(message) { }
}
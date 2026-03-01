using System;

namespace Market.Web.Core.Exceptions;

public class PaymentProcessingException : Exception
{
    public PaymentProcessingException(string message, Exception innerException) : base(message, innerException) { }
}
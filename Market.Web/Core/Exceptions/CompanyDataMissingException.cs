using System;

namespace Market.Web.Core.Exceptions;

public class CompanyDataMissingException : Exception
{
    public CompanyDataMissingException(string message) : base(message) { }
}
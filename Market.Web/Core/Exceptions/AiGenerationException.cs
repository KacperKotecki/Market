namespace Market.Web.Core.Exceptions;

public class AiGenerationException : Exception
{
    public AiGenerationException(string message) : base(message) { }
    
    public AiGenerationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
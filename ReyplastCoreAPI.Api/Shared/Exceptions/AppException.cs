namespace ReyplastCoreAPI.Shared.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400, Exception? innerException = null)
        : base(message, innerException) => StatusCode = statusCode;
}

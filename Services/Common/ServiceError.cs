namespace OtelierBackend.Services.Common;

public sealed class ServiceError
{
    public ServiceError(ServiceErrorType type, string code, string message)
    {
        Type = type;
        Code = code;
        Message = message;
    }

    public ServiceErrorType Type { get; }
    public string Code { get; }
    public string Message { get; }
}

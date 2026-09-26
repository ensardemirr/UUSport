namespace UUSport.Api.Infrastructure;

/// <summary>Is kurali ihlali. Kullaniciya kisa ve anlasilir mesaj gosterilir.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string what) : Exception($"{what} bulunamadı.");

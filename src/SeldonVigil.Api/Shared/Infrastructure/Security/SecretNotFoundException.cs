namespace SeldonVigil.Api.Shared.Infrastructure.Security;

public class SecretNotFoundException(string message, Exception inner) : Exception(message, inner);
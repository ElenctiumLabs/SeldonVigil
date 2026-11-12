namespace SeldonVigil.Api.Modules.Domain.Services;

public sealed class ToolNotFoundException(string message) : Exception(message);
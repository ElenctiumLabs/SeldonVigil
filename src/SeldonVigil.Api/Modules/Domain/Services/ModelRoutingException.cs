namespace SeldonVigil.Api.Modules.Domain.Services;

public class ModelRoutingException(string message, Exception inner) : Exception(message, inner);
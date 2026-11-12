namespace SeldonVigil.Api.Modules.Domain.Services;

public class PromptValidationException(string message) : Exception(message);
namespace SeldonVigil.Api.Modules.Domain.Services;

public class PromptNotFoundException(string message) : Exception(message);
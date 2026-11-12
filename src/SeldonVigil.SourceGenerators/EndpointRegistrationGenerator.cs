using Microsoft.CodeAnalysis;

namespace SeldonVigil.SourceGenerators;

[Generator]
public class EndpointRegistrationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        // Auto-register API endpoints
    }
}

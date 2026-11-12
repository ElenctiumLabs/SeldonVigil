using Microsoft.CodeAnalysis;

namespace SeldonVigil.SourceGenerators;

[Generator]
public class PolicyExpressionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        // Compile policy expressions at build time
    }
}

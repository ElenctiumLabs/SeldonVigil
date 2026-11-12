using Microsoft.CodeAnalysis;

namespace SeldonVigil.SourceGenerators;

[Generator]
public class StronglyTypedIdGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        // Generate strongly-typed ID structs
    }
}

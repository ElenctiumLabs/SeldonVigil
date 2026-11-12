namespace SeldonVigil.Api.Modules.Domain.Services;

internal enum TaskComplexity
{
    Simple,      // Classification, extraction
    Standard,    // Analysis, summarization
    Complex,     // Multi-step reasoning, planning
    Critical     // Safety-critical decisions
}

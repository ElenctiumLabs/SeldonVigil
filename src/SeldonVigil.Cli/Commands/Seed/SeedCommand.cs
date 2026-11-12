using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Observability;

namespace SeldonVigil.Cli.Commands.Seed;

internal static class SeedCommand
{
    public static Command Create()
    {
        var urlOption = new Option<string?>("--url")
        {
            Description = "SeldonVigil API base URL (defaults to config or http://localhost:5000)",
        };
        var apiKeyOption = new Option<string?>("--api-key")
        {
            Description = "SeldonVigil API key for authentication",
        };

        var command = new Command("seed", "Seed demo agents and traces for development and testing")
        {
            urlOption,
            apiKeyOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var url = parseResult.GetValue(urlOption);
            var apiKey = parseResult.GetValue(apiKeyOption);
            return await ExecuteAsync(url, apiKey).ConfigureAwait(false);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string? url, string? apiKey)
    {
        var config = ConfigManager.Load();
        var effectiveApiKey = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : config.ApiKey;

        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            OutputFormatter.Error("No API key provided. Pass --api-key or run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        var effectiveUrl = !string.IsNullOrWhiteSpace(url)
            ? url
            : (!string.IsNullOrWhiteSpace(config.BaseUrl) ? config.BaseUrl : "http://localhost:5000");

        OutputFormatter.Info($"Seeding demo data against SeldonVigil API at {effectiveUrl}...");

        var services = new ServiceCollection();
        services.AddSeldonVigilSdk(
            auth => { auth.ApiKey = effectiveApiKey; auth.BaseUrl = effectiveUrl; },
            cfg => { cfg.BaseUrl = effectiveUrl; });

        var sp = services.BuildServiceProvider();
        var agentClient = sp.GetRequiredService<AgentClient>();
        var obsClient = sp.GetRequiredService<ObservabilityClient>();

        try
        {
            // 1. Ensure Agent
            const string externalId = "seldonvigil-reasoning-core";
            string agentId;

            var agents = await agentClient.GetAgentsAsync().ConfigureAwait(false);
            var existing = agents.FirstOrDefault(a => a.ExternalId == externalId);

            if (existing is not null)
            {
                agentId = existing.Id;
                OutputFormatter.Info($"Found existing agent: {existing.Name} ({agentId})");

                if (existing.Status != AgentStatus.Active)
                {
                    await agentClient.ActivateAgentAsync(agentId, "Seed activation").ConfigureAwait(false);
                    OutputFormatter.Success($"Activated agent {agentId}");
                }
            }
            else
            {
                var created = await agentClient.RegisterAgentAsync(new CreateAgentRequest
                {
                    ExternalId = externalId,
                    Name = "SeldonVigil Reasoning Core",
                    Description = "Dialectical reasoning multi-agent engine with Merkle DAG verification",
                    Framework = "Custom",
                    ModelProvider = "Custom",
                    ModelName = "gpt-4o",
                    Manifest = new CapabilityManifest
                    {
                        Tools = ["dialectical-reasoning", "merkle-dag", "prompt-injection-detection"],
                        DataAccess = ["sessions", "nodes", "settings"],
                        Permissions = ["read", "write", "admin"],
                        RateLimits = new Dictionary<string, int>
                        {
                            ["ascend"] = 30,
                            ["ingestion"] = 10,
                        },
                    },
                }).ConfigureAwait(false);

                agentId = created.Id;
                OutputFormatter.Success($"Registered demo agent: {created.Name} ({agentId})");

                await agentClient.ActivateAgentAsync(agentId, "Initial seed activation").ConfigureAwait(false);
                OutputFormatter.Success($"Activated demo agent {agentId}");
            }

            // 2. Ingest Multi-Span Trace Session
            var rootTraceId = Guid.NewGuid().ToString();
            var sessionId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            var rootTrace = new SeldonVigilTrace
            {
                Id = rootTraceId,
                AgentId = agentId,
                OperationName = "session.level1.dialectical_debate",
                OperationType = "session",
                ModelName = "gpt-4o",
                Status = TraceStatus.Ok,
                StartedAt = now.AddSeconds(-2),
                DurationMs = 1850,
                Cost = 0.038m,
                TokenUsage = new TokenUsage { InputTokens = 1800, OutputTokens = 650, CachedTokens = 250 },
                Attributes = new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["framework"] = "seldonvigil",
                    ["dialectical_phase"] = "synthesis",
                    ["rounds"] = "1",
                    ["verdict"] = "accepted",
                    ["consensus_score"] = "0.94",
                },
                Events =
                [
                    new TraceEvent { Name = "SessionStarted", Timestamp = now.AddSeconds(-2) },
                    new TraceEvent { Name = "ConsensusReached", Timestamp = now.AddMilliseconds(-150) },
                ],
            };

            await obsClient.IngestTraceAsync(rootTrace).ConfigureAwait(false);
            OutputFormatter.Success($"Ingested root trace: {rootTrace.OperationName} ({rootTraceId})");

            // Child 1: Thesis Generation
            var child1 = new SeldonVigilTrace
            {
                Id = Guid.NewGuid().ToString(),
                ParentTraceId = rootTraceId,
                AgentId = agentId,
                OperationName = "agent.thesis.generate",
                OperationType = "llm",
                ModelName = "gpt-4o",
                Status = TraceStatus.Ok,
                StartedAt = now.AddSeconds(-2),
                DurationMs = 520,
                Cost = 0.012m,
                TokenUsage = new TokenUsage { InputTokens = 600, OutputTokens = 250 },
                Attributes = new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["parent_trace_id"] = rootTraceId,
                    ["speaker"] = "Thesis Agent",
                    ["phase"] = "Argumentation",
                    ["role"] = "proponent",
                },
            };
            await obsClient.IngestTraceAsync(child1).ConfigureAwait(false);

            // Child 2: Antithesis Critique
            var child2 = new SeldonVigilTrace
            {
                Id = Guid.NewGuid().ToString(),
                ParentTraceId = rootTraceId,
                AgentId = agentId,
                OperationName = "agent.antithesis.critique",
                OperationType = "llm",
                ModelName = "gpt-4o",
                Status = TraceStatus.Ok,
                StartedAt = now.AddMilliseconds(-1480),
                DurationMs = 610,
                Cost = 0.015m,
                TokenUsage = new TokenUsage { InputTokens = 750, OutputTokens = 170 },
                Attributes = new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["parent_trace_id"] = rootTraceId,
                    ["speaker"] = "Antithesis Agent",
                    ["phase"] = "Refutation",
                    ["counterarguments"] = "2",
                },
            };
            await obsClient.IngestTraceAsync(child2).ConfigureAwait(false);

            // Child 3: Merkle DAG Verification
            var child3 = new SeldonVigilTrace
            {
                Id = Guid.NewGuid().ToString(),
                ParentTraceId = rootTraceId,
                AgentId = agentId,
                OperationName = "tool.merkle_dag.verify",
                OperationType = "tool",
                ModelName = "deterministic",
                Status = TraceStatus.Ok,
                StartedAt = now.AddMilliseconds(-870),
                DurationMs = 120,
                Cost = 0.000m,
                TokenUsage = new TokenUsage { InputTokens = 0, OutputTokens = 0 },
                Attributes = new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["parent_trace_id"] = rootTraceId,
                    ["tool"] = "merkle-dag",
                    ["root_hash"] = "0x8f2a6b7c9e1d4f3a",
                    ["tamper_evident"] = "true",
                },
            };
            await obsClient.IngestTraceAsync(child3).ConfigureAwait(false);

            // Child 4: Synthesis & Consensus
            var child4 = new SeldonVigilTrace
            {
                Id = Guid.NewGuid().ToString(),
                ParentTraceId = rootTraceId,
                AgentId = agentId,
                OperationName = "agent.synthesis.verdict",
                OperationType = "llm",
                ModelName = "gpt-4o",
                Status = TraceStatus.Ok,
                StartedAt = now.AddMilliseconds(-750),
                DurationMs = 600,
                Cost = 0.011m,
                TokenUsage = new TokenUsage { InputTokens = 450, OutputTokens = 230 },
                Attributes = new Dictionary<string, object>
                {
                    ["session_id"] = sessionId,
                    ["parent_trace_id"] = rootTraceId,
                    ["speaker"] = "Synthesis Agent",
                    ["phase"] = "Consensus",
                    ["verdict"] = "Valid",
                    ["confidence"] = "0.94",
                },
            };
            await obsClient.IngestTraceAsync(child4).ConfigureAwait(false);

            OutputFormatter.Success("Ingested 4 child spans (thesis, antithesis, merkle_dag, synthesis).");
            OutputFormatter.Success("Seeding completed successfully! Open the SeldonVigil UI to inspect the live trace hierarchy.");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Seed failed: {ex.Message}");
            return 1;
        }
    }
}

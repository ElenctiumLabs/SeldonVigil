# What is SeldonVigil?

SeldonVigil is an **observability, security, and governance platform for AI agent systems**. It provides monitoring, policy enforcement, cost tracking, and intelligent analysis for any AI-powered application that deploys autonomous agents.

> **Brand:** the NuGet packages, the repository and the namespaces are all `SeldonVigil` — e.g. `SeldonVigil.Sdk`, `SeldonVigil.Telemetry`, `SeldonVigil.Cli`.

## Why SeldonVigil exists

As organisations deploy AI agents at scale they need answers to questions that existing APM tools don't address:

- Which agent called which model, with what inputs, and what did it cost?
- Did any agent violate a data-protection or rate-limiting policy?
- Is the total LLM spend within budget, and who is over-spending?
- Has a production agent drifted from its certified configuration?
- What is the root cause of a sudden spike in errors or latency?

SeldonVigil answers all of these by sitting between agent code and the outside world — collecting traces, evaluating policies in real time, tracking costs per token, and running AI-powered root-cause analysis.

## Primary consumers

| Consumer | Integration method | Notes |
|---|---|---|
| Any .NET app | `SeldonVigil.Sdk` NuGet or OpenTelemetry exporter | Registers agents and streams traces via the SDK; `SeldonVigilTraceExporter` acts as an OTel bridge |
| Any HTTP client | REST API | Full `/api/v1` surface; JWT or API-key auth |

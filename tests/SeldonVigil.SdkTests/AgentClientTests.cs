using SeldonVigil.Sdk.Agents;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.SdkTests;

public class AgentClientTests
{
    [Fact]
    public async Task GetAgentsAsyncUnwrapsValueObjectIdsAndParsesStatus()
    {
        var (client, handler) = StubApiHandler.CreateAgentClient();
        handler.RouteJson("/api/v1/agents", """
            {
              "items": [
                {
                  "id": { "value": "11111111-1111-1111-1111-111111111111" },
                  "externalId": "ext-1",
                  "name": "Support Agent",
                  "framework": "LangChain",
                  "modelProvider": "OpenAI",
                  "modelName": "gpt-4o",
                  "status": "Active",
                  "createdAt": "2026-08-01T10:00:00Z"
                }
              ],
              "totalCount": 1, "page": 1, "pageSize": 50
            }
            """);

        var agents = await client.GetAgentsAsync();

        agents.Should().HaveCount(1);
        var agent = agents[0];
        agent.Id.Should().Be("11111111-1111-1111-1111-111111111111");
        agent.Status.Should().Be(AgentStatus.Active);
        agent.Name.Should().Be("Support Agent");
    }

    [Fact]
    public async Task GetAgentAsyncMapsDetailWithWrappedTenantId()
    {
        var (client, handler) = StubApiHandler.CreateAgentClient();
        handler.RouteJson("/api/v1/agents/", """
            {
              "id": { "value": "22222222-2222-2222-2222-222222222222" },
              "tenantId": { "value": "00000000-0000-0000-0000-000000000001" },
              "externalId": "ext-2",
              "name": "Analyst",
              "framework": "LangGraph",
              "modelProvider": "Anthropic",
              "modelName": "claude",
              "status": "Active",
              "createdAt": "2026-08-01T10:00:00Z",
              "updatedAt": "2026-08-02T10:00:00Z"
            }
            """);

        var agent = await client.GetAgentAsync("22222222-2222-2222-2222-222222222222");

        agent.Should().NotBeNull();
        agent!.Id.Should().Be("22222222-2222-2222-2222-222222222222");
        agent.TenantId.Should().Be("00000000-0000-0000-0000-000000000001");
        agent.Status.Should().Be(AgentStatus.Active);
    }

    [Fact]
    public async Task RegisterAgentAsyncNormalizesUnknownFrameworkAndStructuresManifest()
    {
        var (client, handler) = StubApiHandler.CreateAgentClient();
        string? capturedBody = null;
        handler.Route("/api/v1/agents", () =>
        {
            capturedBody = handler.LastJsonBody;
            return StubApiHandler.Json("""{"id":"33333333-3333-3333-3333-333333333333"}""");
        });

        var result = await client.RegisterAgentAsync(new CreateAgentRequest
        {
            ExternalId = "ext-3",
            Name = "Custom Agent",
            Framework = "LlamaIndex",
            ModelProvider = "vllm",
            ModelName = "mixtral",
            Manifest = new CapabilityManifest { Tools = ["search"], DataAccess = ["db"], Permissions = ["read"], RateLimits = new Dictionary<string, int> { ["requestsPerMinute"] = 60 } },
        });

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"framework\":\"Custom\"");
        capturedBody.Should().Contain("\"modelProvider\":\"Custom\"");
        capturedBody.Should().Contain("\"name\":\"search\"");
        capturedBody.Should().Contain("\"resource\":\"db\"");

        result.Id.Should().Be("33333333-3333-3333-3333-333333333333");
        // Public surface keeps the caller's original free-form values.
        result.Framework.Should().Be("LlamaIndex");
    }
}

using System.Text.Json.Serialization;

namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

[JsonSerializable(typeof(CapabilityManifest))]
[JsonSerializable(typeof(AgentTool))]
[JsonSerializable(typeof(DataAccessRule))]
[JsonSerializable(typeof(RateLimit))]
internal partial class CapabilityManifestContext : JsonSerializerContext { }

using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// Machine-readable status for a tool response.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ToolResultStatus>))]
public enum ToolResultStatus
{
    /// <summary>
    /// The tool contract exists, but the capability is planned for a later stage.
    /// </summary>
    [JsonStringEnumMemberName("not_ready")]
    NotReady,

    /// <summary>
    /// The tool found evidence and returned a normal result.
    /// </summary>
    [JsonStringEnumMemberName("ok")]
    Ok,

    /// <summary>
    /// The requested package, version, operation, or symbol was not found.
    /// </summary>
    [JsonStringEnumMemberName("not_found")]
    NotFound,

    /// <summary>
    /// The index contained too little reliable evidence to answer.
    /// </summary>
    [JsonStringEnumMemberName("insufficient_evidence")]
    InsufficientEvidence
}

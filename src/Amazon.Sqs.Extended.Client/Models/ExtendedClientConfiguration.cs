using System.Diagnostics.CodeAnalysis;

namespace Amazon.Sqs.Extended.Client.Models;

[ExcludeFromCodeCoverage(Justification = "Model")]
public record ExtendedClientConfiguration
{
    public bool CleanupPayload { get; init; } = true;

    public long PayloadSizeThreshold { get; init; } = SqsExtendedClientConstants.DefaultMessageSizeThreshold;

    public bool AlwaysThroughS3 { get; init; }

    public bool LargePayloadSupport { get; init; } = true;
}

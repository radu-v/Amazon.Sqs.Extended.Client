using System.Diagnostics.CodeAnalysis;
using Amazon.S3;

namespace Amazon.Sqs.Extended.Client.Models;

[ExcludeFromCodeCoverage(Justification = "Model")]
public record ExtendedClientConfiguration
{
    public bool CleanupS3Payload { get; init; }
    public long PayloadSizeThreshold { get; init; } = SqsExtendedClientConstants.DefaultMessageSizeThreshold;
    public bool AlwaysThroughS3 { get; init; }
    public S3CannedACL S3CannedAcl { get; init; } = S3CannedACL.BucketOwnerFullControl;
    public bool LargePayloadSupport { get; init; }
    public IAmazonS3? S3 { get; init; }
    public string? S3BucketName { get; init; }
}

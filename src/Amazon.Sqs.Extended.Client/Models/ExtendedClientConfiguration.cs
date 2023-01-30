using Amazon.S3;

namespace Amazon.Sqs.Extended.Client.Models;

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

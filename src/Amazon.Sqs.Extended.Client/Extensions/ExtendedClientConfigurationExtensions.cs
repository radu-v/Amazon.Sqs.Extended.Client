using System.Diagnostics.CodeAnalysis;
using Amazon.S3;
using Amazon.Sqs.Extended.Client.Models;

namespace Amazon.Sqs.Extended.Client.Extensions;

[ExcludeFromCodeCoverage]
public static class ExtendedClientConfigurationExtensions
{
    public static ExtendedClientConfiguration WithLargePayloadSupportEnabled(this ExtendedClientConfiguration config,
        IAmazonS3 s3, string s3BucketName, bool cleanupS3Payload)
        => config with
        {
            S3 = s3,
            S3BucketName = s3BucketName,
            LargePayloadSupport = true,
            CleanupS3Payload = cleanupS3Payload
        };

    public static ExtendedClientConfiguration WithLargePayloadSupportEnabled(this ExtendedClientConfiguration config,
        IAmazonS3 s3, string s3BucketName)
        => config with
        {
            S3 = s3,
            S3BucketName = s3BucketName,
            LargePayloadSupport = true
        };

    public static ExtendedClientConfiguration WithLargePayloadSupportDisabled(this ExtendedClientConfiguration config)
        => config with
        {
            S3 = null,
            S3BucketName = null,
            LargePayloadSupport = false
        };

    public static ExtendedClientConfiguration WithAlwaysThroughS3(this ExtendedClientConfiguration config,
        bool alwaysThroughS3)
        => config with
        {
            AlwaysThroughS3 = alwaysThroughS3
        };

    public static ExtendedClientConfiguration WithS3CannedAcl(this ExtendedClientConfiguration config,
        S3CannedACL s3CannedAcl)
        => config with
        {
            S3CannedAcl = s3CannedAcl
        };

    public static ExtendedClientConfiguration WithPayloadSizeThreshold(this ExtendedClientConfiguration config,
        int payloadSizeThreshold)
        => config with
        {
            PayloadSizeThreshold = payloadSizeThreshold
        };
}
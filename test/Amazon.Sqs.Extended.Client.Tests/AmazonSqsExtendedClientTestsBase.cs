using System.Text;
using Amazon.S3;
using Amazon.SQS;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

public class AmazonSqsExtendedClientTestsBase
{
    protected const string S3BucketName = "test-bucket-name";
    protected const string S3Key = "S3Key";
    protected const string SqsQueueUrl = "test-queue-url";
    protected const int LessThanSqsSizeLimit = 3;
    protected const int SqsSizeLimit = 100;
    protected const int MoreThanSqsSizeLimit = SqsSizeLimit + 1;
    protected const string DefaultS3Key = "12345678901234567890123456789012";

    protected IAmazonS3 S3Sub { get; private set; } = null!;
    protected IAmazonSQS SqsClientSub { get; private set; } = null!;
    protected ExtendedClientConfiguration ExtendedClientConfiguration { get; private set; } = null!;
    protected IS3KeyProvider S3KeyProviderSub { get; private set; } = null!;
    protected AmazonSqsExtendedClient ExtendedSqsWithLargePayloadEnabled { get; private set; } = null!;
    protected AmazonSqsExtendedClient ExtendedSqsWithLargePayloadDisabled { get; private set; } = null!;
    protected string LargeMessageBody { get; private set; } = null!;
    protected string SmallMessageBody { get; private set; } = null!;

    protected ILogger<AmazonSqsExtendedClient> DummyLogger = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        DummyLogger = Substitute.For<ILogger<AmazonSqsExtendedClient>>();
        LargeMessageBody = GenerateStringWithLength(MoreThanSqsSizeLimit);
        SmallMessageBody = GenerateStringWithLength(LessThanSqsSizeLimit);
    }

    [SetUp]
    public void Setup()
    {
        S3Sub = Substitute.For<IAmazonS3>();
        SqsClientSub = Substitute.For<IAmazonSQS>();
        S3KeyProviderSub = Substitute.For<IS3KeyProvider>();

        S3KeyProviderSub.GenerateKey().Returns(DefaultS3Key);

        ExtendedClientConfiguration = new ExtendedClientConfiguration()
            .WithPayloadSizeThreshold(SqsSizeLimit);

        ExtendedSqsWithLargePayloadEnabled =
            new AmazonSqsExtendedClient(SqsClientSub,
                ExtendedClientConfiguration.WithLargePayloadSupportEnabled(S3Sub, S3BucketName), S3KeyProviderSub,
                DummyLogger);

        ExtendedSqsWithLargePayloadDisabled =
            new AmazonSqsExtendedClient(SqsClientSub, ExtendedClientConfiguration.WithLargePayloadSupportDisabled(),
                S3KeyProviderSub, DummyLogger);
    }


    protected static string GenerateStringWithLength(int messageLength) => new('Q', messageLength);

    protected static string GenerateReceiptHandle(bool isS3ReceiptHandle, string originalReceiptHandle,
        string bucketName = "",
        string s3Key = "")
    {
        if (!isS3ReceiptHandle) return originalReceiptHandle;

        return new StringBuilder(SqsExtendedClientConstants.S3BucketNameMarker)
            .Append(bucketName)
            .Append(SqsExtendedClientConstants.S3BucketNameMarker)
            .Append(SqsExtendedClientConstants.S3KeyMarker)
            .Append(s3Key)
            .Append(SqsExtendedClientConstants.S3KeyMarker)
            .Append(originalReceiptHandle)
            .ToString();
    }
}
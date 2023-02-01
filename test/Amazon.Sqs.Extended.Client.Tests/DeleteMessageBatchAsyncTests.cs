using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

[TestFixture]
public class DeleteMessageBatchAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [TestCase(false, false, false, false)]
    [TestCase(false, false, true, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, true, true, false)]
    [TestCase(true, false, false, false)]
    [TestCase(true, false, true, false)]
    [TestCase(true, true, false, false)]
    [TestCase(true, true, true, true)]
    public async Task CallsS3DeleteObjectAsyncOnlyWhenClientSupportsLargePayloadCleanupIsEnabledAndIsS3ReceiptHandle(
        bool isS3ReceiptHandle, bool isLargePayloadEnabled, bool isCleanupS3PayloadEnabled, bool shouldCallS3)
    {
        // arrange
        var receiptHandle1 = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle1", S3BucketName, S3Key);
        var receiptHandle2 = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle2", S3BucketName, S3Key);
        var request = new DeleteMessageBatchRequest(SqsQueueUrl, new List<DeleteMessageBatchRequestEntry>
        {
            new("1", receiptHandle1),
            new("2", receiptHandle2)
        });

        var config = ExtendedClientConfiguration;

        if (isLargePayloadEnabled)
        {
            config = config.WithLargePayloadSupportEnabled(S3Sub, S3BucketName, isCleanupS3PayloadEnabled);
        }

        var client = new AmazonSqsExtendedClient(SqsClientSub, config, DummyLogger);

        // act
        await client.DeleteMessageBatchAsync(request);

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.ReceivedWithAnyArgs(1).DeleteMessageBatchAsync(Arg.Any<DeleteMessageBatchRequest>());

            if (shouldCallS3)
            {
                await S3Sub.Received(2).DeleteObjectAsync(S3BucketName, S3Key, Arg.Any<CancellationToken>());
            }
            else
            {
                await S3Sub.DidNotReceiveWithAnyArgs()
                    .DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

                await S3Sub.DidNotReceiveWithAnyArgs()
                    .DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>());
            }
        });
    }
}
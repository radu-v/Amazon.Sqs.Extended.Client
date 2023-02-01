using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

[TestFixture]
public class DeleteMessageAsyncTests : AmazonSqsExtendedClientTestsBase
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
        var receiptHandle = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle", S3BucketName, S3Key);
        var request = new DeleteMessageRequest(SqsQueueUrl, receiptHandle);
        var config = ExtendedClientConfiguration;

        if (isLargePayloadEnabled)
        {
            config = config.WithLargePayloadSupportEnabled(S3Sub, S3BucketName, isCleanupS3PayloadEnabled);
        }

        var client = new AmazonSqsExtendedClient(SqsClientSub, config, DummyLogger);

        // act
        await client.DeleteMessageAsync(request);

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.ReceivedWithAnyArgs(1).DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());

            if (shouldCallS3)
            {
                await S3Sub.Received(1).DeleteObjectAsync(S3BucketName, S3Key, Arg.Any<CancellationToken>());
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
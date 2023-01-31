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
        const string bucketName = "bucket";
        const string s3Key = "s3key";
        var receiptHandle = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle", bucketName, s3Key);
        var request = new DeleteMessageRequest("url", receiptHandle);
        var config = ExtendedClientConfiguration;

        if (isLargePayloadEnabled)
        {
            config = config.WithLargePayloadSupportEnabled(S3Sub, bucketName, isCleanupS3PayloadEnabled);
        }

        var client = new AmazonSqsExtendedClient(SqsClientSub, config);

        // act
        await client.DeleteMessageAsync(request);

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.ReceivedWithAnyArgs(1).DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());

            if (shouldCallS3)
            {
                await S3Sub.Received(1).DeleteObjectAsync(bucketName, s3Key, Arg.Any<CancellationToken>());
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
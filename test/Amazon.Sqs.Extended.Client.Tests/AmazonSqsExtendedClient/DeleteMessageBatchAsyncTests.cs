using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests.AmazonSqsExtendedClient;

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
        bool isS3ReceiptHandle, bool isLargePayloadEnabled, bool isCleanupPayloadEnabled, bool willCallS3)
    {
        // arrange
        var receiptHandle1 = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle1", S3BucketName, S3Key);
        var receiptHandle2 = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle2", S3BucketName, S3Key);
        var request = new DeleteMessageBatchRequest(SqsQueueUrl, new List<DeleteMessageBatchRequestEntry>
        {
            new("1", receiptHandle1),
            new("2", receiptHandle2)
        });

        var config = ExtendedClientConfiguration.WithLargePayloadSupportDisabled();

        if (isLargePayloadEnabled)
        {
            config = config.WithLargePayloadSupportEnabled(isCleanupPayloadEnabled);
        }

        var client = new Client.AmazonSqsExtendedClient(SqsClientSub, PayloadStoreSub, config, DummyLogger);

        // act
        await client.DeleteMessageBatchAsync(request);

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.ReceivedWithAnyArgs(1).DeleteMessageBatchAsync(Arg.Any<DeleteMessageBatchRequest>());

            if (willCallS3)
            {
                await PayloadStoreSub.Received(2).DeletePayloadAsync(
                    Arg.Is<PayloadPointer>(p => p.BucketName == S3BucketName && p.Key == S3Key),
                    Arg.Any<CancellationToken>());
            }
            else
            {
                await PayloadStoreSub.DidNotReceiveWithAnyArgs()
                    .DeletePayloadAsync(Arg.Any<PayloadPointer>(), Arg.Any<CancellationToken>());
            }
        });
    }
}
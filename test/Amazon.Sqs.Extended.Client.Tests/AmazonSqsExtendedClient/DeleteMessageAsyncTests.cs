using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests.AmazonSqsExtendedClient;

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
        bool isS3ReceiptHandle,
        bool isLargePayloadEnabled,
        bool isCleanupPayloadEnabled,
        bool willCallS3)
    {
        // arrange
        var receiptHandle = GenerateReceiptHandle(isS3ReceiptHandle, "originalReceiptHandle", S3BucketName, S3Key);
        var request = new DeleteMessageRequest(SqsQueueUrl, receiptHandle);
        var config = ExtendedClientConfiguration.WithLargePayloadSupportDisabled();

        if (isLargePayloadEnabled)
        {
            config = config.WithLargePayloadSupportEnabled(isCleanupPayloadEnabled);
        }
        
        var options = Options.Create(config);

        var client = new Client.AmazonSqsExtendedClient(SqsClientSub, PayloadStoreSub, options, DummyLogger);

        // act
        await client.DeleteMessageAsync(request);

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.ReceivedWithAnyArgs(1).DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());

            if (willCallS3)
            {
                await PayloadStoreSub.Received(1).DeletePayloadAsync(Arg.Is<PayloadPointer>(p => p.BucketName == S3BucketName && p.Key == S3Key));
            }
            else
            {
                await PayloadStoreSub.DidNotReceiveWithAnyArgs()
                    .DeletePayloadAsync(Arg.Any<PayloadPointer>(), Arg.Any<CancellationToken>());
            }
        });
    }
}

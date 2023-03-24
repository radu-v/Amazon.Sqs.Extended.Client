using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests.AmazonSqsExtendedClient;

public class SendMessageBatchAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [Test]
    public async Task PayloadIsStoredInS3WhenSendingLargeMessage()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", LargeMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageBatchAsync(messageRequest);

        // assert
        await PayloadStoreSub.Received(1).StorePayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PayloadIsNotStoredInS3WhenSendingSmallMessage()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageBatchAsync(messageRequest);

        // assert
        Assert.Multiple(async () =>
        {
            await PayloadStoreSub.DidNotReceiveWithAnyArgs()
                .StorePayloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await PayloadStoreSub.DidNotReceiveWithAnyArgs()
                .StorePayloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task PayloadIsNotStoredInS3WhenSendingLargeMessageAndLargePayloadSupportIsDisabled()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody),
            new("2", LargeMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadDisabled.SendMessageBatchAsync(messageRequest);

        // assert
        Assert.Multiple(async () =>
        {
            await PayloadStoreSub.DidNotReceiveWithAnyArgs()
                .StorePayloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await PayloadStoreSub.DidNotReceiveWithAnyArgs()
                .StorePayloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task PayloadIsStoredInS3WhenAlwaysThroughS3IsEnabled()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody),
            new("2", LargeMessageBody)
        });

        using var client = new Client.AmazonSqsExtendedClient(SqsClientSub, PayloadStoreSub,
            ExtendedClientConfiguration.WithLargePayloadSupportEnabled().WithAlwaysThroughS3(true), DummyLogger);

        // act
        await client.SendMessageBatchAsync(messageRequest);

        // assert
        Assert.Multiple(async () =>
        {
            await PayloadStoreSub.Received(1).StorePayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
            await PayloadStoreSub.Received(1).StorePayloadAsync(SmallMessageBody, Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task OnlyTheLargePayloadIsStoredInS3()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody),
            new("2", LargeMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageBatchAsync(messageRequest);

        // assert
        await PayloadStoreSub.Received(1).StorePayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
    }
}
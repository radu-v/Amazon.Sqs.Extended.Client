using Amazon.S3.Model;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

[TestFixture]
public class ReceiveMessageAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [Test]
    public async Task DoesNotCallS3WhenPayloadSizeIsSmall()
    {
        // arrange
        var receiveMessageResponse = new ReceiveMessageResponse
        {
            Messages = new List<Message> { new() }
        };

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(receiveMessageResponse));

        // act
        await ExtendedSqsWithLargePayloadEnabled.ReceiveMessageAsync(new ReceiveMessageRequest("url"));

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.Received(1).ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(r =>
                r.QueueUrl == "url"
                && r.MessageAttributeNames.Contains(SqsExtendedClientConstants.ReservedAttributeName)));

            await S3Sub.DidNotReceiveWithAnyArgs().GetObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }
    
    [Test]
    public async Task DoesNotCallS3WhenLargePayloadIsDisabled()
    {
        // arrange
        var receiveMessageResponse = new ReceiveMessageResponse
        {
            Messages = new List<Message> { new() }
        };

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(receiveMessageResponse));

        // act
        await ExtendedSqsWithLargePayloadDisabled.ReceiveMessageAsync(new ReceiveMessageRequest("url"));

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.Received(1).ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(r =>
                r.QueueUrl == "url"
                && !r.MessageAttributeNames.Contains(SqsExtendedClientConstants.ReservedAttributeName)));

            await S3Sub.DidNotReceiveWithAnyArgs().GetObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }
}
